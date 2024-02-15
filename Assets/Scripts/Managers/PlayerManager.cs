using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance;
    public static int TotalPlayerPrefabs = 2; // Adjust based on your game's needs
    private int connectedPlayers = 0;
    public List<Player> players = new List<Player>();
    private List<PlayerData> playerDataList;

    public GameObject playerPrefab; // Prefab with Player script attached
    public GameObject playerUIPrefab; // Prefab with PlayerUI script attached


    private Dictionary<Player, PlayerUI> playerUITracking = new Dictionary<Player, PlayerUI>();


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            DataManager.OnPlayerDataLoaded += LoadPlayerDataLoaded; // Subscribe to event
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        DataManager.OnPlayerDataLoaded -= LoadPlayerDataLoaded; // Unsubscribe from event
        /*if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }*/
    }

    public void LoadPlayerDataLoaded(List<PlayerData> loadedPlayerDataList)
    {
        this.playerDataList = loadedPlayerDataList; 
        Debug.Log("Player data loaded into PlayerManager.");
    }

    private void Start()
    {
        //NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    public void RegisterPlayer(ulong networkId)
    {
        Debug.Log("RegisterPlayer is called");
        Player player = FindPlayerByNetworkId(networkId);
        if (player == null)
        {
            Debug.LogError("Player not found for NetworkID: " + networkId);
            return;
        }
        
        PlayerUI playerUI = InstantiatePlayerUIForPlayer(player);
        playerUITracking[player] = playerUI;
    }

    private Player FindPlayerByNetworkId(ulong networkId)
    {
        Debug.Log("FindPlayerByNetworkId is called");
        foreach (var kvp in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
        {
            if (kvp.Key == networkId)
            {
                Player player = kvp.Value.GetComponent<Player>();
                if (player != null)
                {
                    return player;
                }
            }
        }
        return null;
    }

    private PlayerUI InstantiatePlayerUIForPlayer(Player player)
    {
        Debug.Log("InstantiatePlayerUIForPlayer is called");
        GameObject uiGameObject = Instantiate(playerUIPrefab);
        PlayerUI playerUI = uiGameObject.GetComponent<PlayerUI>();
        // Setup playerUI based on player, if necessary
        return playerUI;
    }



    private void InstantiatePlayer(ulong clientId)
    {
        if (!IsServer) return;

        connectedPlayers++;
        var playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
        if (playerObject != null)
        {
            var player = playerObject.GetComponent<Player>();
            if (player != null && connectedPlayers <= playerDataList.Count)
            {
                string playerImagePath = "Images/character_01"; // Default image path
                var playerData = playerDataList[connectedPlayers - 1]; // Example, adjust as necessary
                player.InitializePlayer(playerData.playerName, playerData.playerDbId, playerData.playerImagePath);
                // New logic to broadcast existing player names to the newly connected client
                BroadcastPlayerNamesToNewClient(clientId);

                players.Add(player);
                if (connectedPlayers == TotalPlayerPrefabs)
                {
                    DistributeCards();
                    UpdatePlayerToAsk();
                }
            }
        }
    }

    private void BroadcastPlayerNamesToNewClient(ulong newClientId)
    {
        foreach (var player in players)
        {
            // Use the new method to broadcast both attributes
            player.BroadcastPlayerDbAttributes();
        }
    }

    private void DistributeCards()
    {
        int index = 0; // Index to keep track of the card to distribute
        int cardsPerPlayer = 5; // Set to 5 cards per player

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject;
            var player = playerObject?.GetComponent<Player>();
            /*if (player != null)
            {
                // Dynamically assign cards based on cardsPerPlayer
                for (int i = 0; i < cardsPerPlayer; i++)
                {
                    // Check if the index is within the bounds of the SpawnedCards list
                    if (index < CardManager.SpawnedCards.Count)
                    {
                        player.AddCardToHand(CardManager.SpawnedCards[index++]);
                    }
                    else
                    {
                        Debug.LogWarning("Not enough cards to distribute to all players.");
                        break; // Exit the loop if there are not enough cards
                    }
                }
            }*/
        }
    }

    private void UpdatePlayerToAsk()
    {
        // Iterate through each player in the 'players' list
        foreach (var player in players)
        {
            // Call a method on the player instance to update its PlayerToAsk list
            player.UpdatePlayerToAskList(players);
        }
    }

    // Method to clean up players, if necessary
    public void CleanupPlayers()
    {
        players.Clear();
    }

    public void GenerateGamePlayers(List<PlayerData> name)
    {
        //return null;
    }

}