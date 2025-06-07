using Unity.Netcode;
using UnityEngine;

public class QuartetsManager : MonoBehaviour
{
    public static QuartetsManager Instance;

    [SerializeField] private GameObject quartetsUIPrefab;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private GameObject quartetsPrefab;

    public GameObject QuartetsInstance { get; private set; } // Store the spawned Quartets instance
    public Quartets CurrentQuartets { get; private set; }

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
        NetworkManager.Singleton.OnServerStarted += SpawnQuartetsPrefab;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= SpawnQuartetsPrefab;
        }
    }

    private void SpawnQuartetsPrefab()
    {
        if (NetworkManager.Singleton.IsServer && quartetsPrefab != null && targetCanvas != null && quartetsUIPrefab != null)
        {
            // 1️⃣ Instantiate QuartetsPrefab (logic object)
            QuartetsInstance = Instantiate(quartetsPrefab); // NO parent
            CurrentQuartets = QuartetsInstance.GetComponent<Quartets>();

            // 2️⃣ Spawn the network object
            QuartetsInstance.GetComponent<NetworkObject>().Spawn();

            // 3️⃣ Instantiate QuartetsUIPrefab under targetCanvas
            GameObject quartetsUIInstance = Instantiate(quartetsUIPrefab, targetCanvas.transform);
            QuartetsUI quartetsUIComponent = quartetsUIInstance.GetComponent<QuartetsUI>();

            // 4️⃣ Link QuartetsUI to Quartets logic object
            CurrentQuartets.SetQuartetsUI(quartetsUIComponent);
        }
        else
        {
            Debug.LogError("Cannot spawn QuartetsPrefab or QuartetsUIPrefab — check quartetsPrefab, quartetsUIPrefab, and targetCanvas.");
        }
    }
}
