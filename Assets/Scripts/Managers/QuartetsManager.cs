using Unity.Netcode;
using UnityEngine;

public class QuartetManager : MonoBehaviour
{
    public static QuartetManager Instance;
    [SerializeField] private Canvas targetCanvas;
    public GameObject quartetPrefab;
    public GameObject QuartetInstance { get; private set; } // Store the spawned quartet instance

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
        NetworkManager.Singleton.OnServerStarted += SpawnQuartetPrefab;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= SpawnQuartetPrefab;
        }
    }

    private void SpawnQuartetPrefab()
    {
        if (NetworkManager.Singleton.IsServer && quartetPrefab != null && targetCanvas != null)
        {
            // Instantiate under targetCanvas
            QuartetInstance = Instantiate(quartetPrefab, targetCanvas.transform);

            // Spawn on network
            QuartetInstance.GetComponent<NetworkObject>().Spawn();

            Debug.Log("Quartet prefab spawned on server start.");
        }
        else
        {
            Debug.LogError("Cannot spawn QuartetPrefab — check quartetPrefab and targetCanvas.");
        }
    }
}
