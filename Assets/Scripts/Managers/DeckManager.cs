using Unity.Netcode;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance;
    public GameObject deckPrefab;
    public GameObject DeckInstance { get; private set; }

    public Deck CurrentDeck { get; private set; }

    [SerializeField] private GameObject deckUIPrefab;
    [SerializeField] private Canvas targetCanvas;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        NetworkManager.Singleton.OnServerStarted += SpawnDeckPrefab;

        // SAFE: add client DeckUI instantiation without touching server flow
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= SpawnDeckPrefab;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void SpawnDeckPrefab()
    {
        if (NetworkManager.Singleton.IsServer && deckPrefab != null)
        {
            // 1️⃣ Spawn Deck logic
            DeckInstance = Instantiate(deckPrefab);
            CurrentDeck = DeckInstance.GetComponent<Deck>();
            DeckInstance.GetComponent<NetworkObject>().Spawn();

            Debug.Log("[DeckManager] DeckPrefab (logic object) spawned and networked.");

            // 2️⃣ Host must spawn DeckUI and link to Deck logic
            InstantiateDeckUIOnLocalClient(linkToDeck: true);
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId && !NetworkManager.Singleton.IsHost)
        {
            Debug.Log($"[DeckManager] OnClientConnected → Instantiating DeckUI for CLIENT (not Host), clientId {clientId}");
            InstantiateDeckUIOnLocalClient(linkToDeck: false);
        }
        else
        {
            Debug.Log($"[DeckManager] OnClientConnected skipped for Host.");
        }
    }

    private void InstantiateDeckUIOnLocalClient(bool linkToDeck)
    {
        Debug.Log($"[DeckManager] InstantiateDeckUIOnLocalClient CALLED → IsServer: {NetworkManager.Singleton.IsServer}, IsClient: {NetworkManager.Singleton.IsClient}, LocalClientId: {NetworkManager.Singleton.LocalClientId}");

        if (targetCanvas != null && deckUIPrefab != null)
        {
            GameObject deckUIInstance = Instantiate(deckUIPrefab, targetCanvas.transform);
            DeckUI deckUIComponent = deckUIInstance.GetComponent<DeckUI>();

            if (linkToDeck && CurrentDeck != null)
            {
                // Only the server should do this
                CurrentDeck.SetDeckUI(deckUIComponent);
                Debug.Log("[DeckManager] Linked DeckUI to CurrentDeck on server.");
            }
            else
            {
                Debug.Log("[DeckManager] DeckUI instantiated on client (not linked to Deck logic).");
            }
        }
        else
        {
            Debug.LogError("[DeckManager] ERROR: Cannot instantiate DeckUI → deckUIPrefab or targetCanvas is null.");
        }
    }
}
