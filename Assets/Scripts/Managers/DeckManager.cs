using Unity.Netcode;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance;
    public GameObject deckPrefab;
    public GameObject DeckInstance { get; private set; } // Store the spawned deck instance

    // Property to access the Deck component of the DeckInstance
    public Deck CurrentDeck { get; private set; }

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
        if (NetworkManager.Singleton.IsServer && deckPrefab != null && targetCanvas != null)
        {
            // Instantiate deckPrefab under targetCanvas
            DeckInstance = Instantiate(deckPrefab, targetCanvas.transform);
            
            // Get the Deck component
            CurrentDeck = DeckInstance.GetComponent<Deck>();
            
            // Spawn the network object
            DeckInstance.GetComponent<NetworkObject>().Spawn();
        }
        else
        {
            Debug.LogError("Cannot spawn DeckPrefab — check deckPrefab and targetCanvas.");
        }
    }
}
