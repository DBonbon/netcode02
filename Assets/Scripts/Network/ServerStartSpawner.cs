using UnityEngine;
using Unity.Netcode;


//general template to spawn a simple networkobject

public class ServerStartSpawner : MonoBehaviour
{
    public GameObject prefabToSpawn;

    private void Start()
    {
        NetworkManager.Singleton.OnServerStarted += SpawnPrefab;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= SpawnPrefab;
        }
    }

    private void SpawnPrefab()
    {
        if (NetworkManager.Singleton.IsServer && prefabToSpawn != null)
        {
            GameObject spawnedObject = Instantiate(prefabToSpawn);
            spawnedObject.GetComponent<NetworkObject>().Spawn();
            //Debug.Log("Prefab spawned on server start.");
        }
    }
}
