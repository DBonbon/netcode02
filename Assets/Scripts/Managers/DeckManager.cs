using Unity.Netcode;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance;
    public GameObject deckPrefab;
    public GameObject DeckInstance { get; private set; } // Store the spawned deck instance

    // Property to access the Deck component of the DeckInstance
    public Deck CurrentDeck { get; private set; }

    [SerializeField] private GameObject deckUIPrefab;
    [SerializeField] private Canvas targetCanvas; // Added this line — reference to Canvas

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
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= SpawnDeckPrefab;
        }
    }

    private void SpawnDeckPrefab()
    {
        if (NetworkManager.Singleton.IsServer && deckPrefab != null && targetCanvas != null && deckUIPrefab != null)
        {
            // 1️⃣ Instantiate DeckPrefab (logic object)
            DeckInstance = Instantiate(deckPrefab); // NO parent
            CurrentDeck = DeckInstance.GetComponent<Deck>();

            // 2️⃣ Spawn the network object
            DeckInstance.GetComponent<NetworkObject>().Spawn();

            // 3️⃣ Instantiate DeckUIPrefab under targetCanvas
            GameObject deckUIInstance = Instantiate(deckUIPrefab, targetCanvas.transform);
            DeckUI deckUIComponent = deckUIInstance.GetComponent<DeckUI>();

            // 4️⃣ Link DeckUI to Deck logic object
            CurrentDeck.SetDeckUI(deckUIComponent);
        }
        else
        {
            Debug.LogError("Cannot spawn DeckPrefab or DeckUIPrefab — check deckPrefab, deckUIPrefab, and targetCanvas.");
        }
    }

}
