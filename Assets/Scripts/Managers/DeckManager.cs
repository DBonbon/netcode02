using Unity.Netcode;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance;
    public GameObject deckPrefab;
    public GameObject DeckInstance { get; private set; } // Store the spawned deck instance

    // Property to access the Deck component of the DeckInstance
    public Deck CurrentDeck { get; private set; }

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
        if (NetworkManager.Singleton.IsServer && deckPrefab != null)
        {
            DeckInstance = Instantiate(deckPrefab);
            CurrentDeck = DeckInstance.GetComponent<Deck>();
            DeckInstance.GetComponent<NetworkObject>().Spawn();
        }
    }
}
