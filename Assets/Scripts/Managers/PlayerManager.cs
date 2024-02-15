using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance;
    
    public GameObject playerPrefab; // Prefab with Player script attached
    public GameObject playerUIPrefab; // Prefab with PlayerUI script attached
    public Transform playerUIParent;

    public static int TotalPlayerPrefabs = 2; // Adjust based on your game's needs
    private int connectedPlayers = 0;
    public List<Player> players = new List<Player>();
    private List<PlayerData> playerDataList;

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


    public void LoadPlayerDataLoaded(List<PlayerData> loadedPlayerDataList)
    {
        this.playerDataList = loadedPlayerDataList; 
        Debug.Log("Player data loaded into PlayerManager.");
    }

    private void Start()
    {
        /*if (NetworkManager.Singleton.IsServer)
        Debug.Log("pl.maanger has started");
        {
            NetworkManager.Singleton.OnClientConnectedCallback += RegisterPlayer;
        }*/
    }

    private void OnDestroy()
    {
        DataManager.OnPlayerDataLoaded -= LoadPlayerDataLoaded; // Unsubscribe from event
        /*if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= RegisterPlayer;
        }*/
    }

    public void RegisterPlayer(Player player)
    {
        ulong networkId = player.NetworkObjectId;

        // Assuming each player has predefined data you wish to associate
        if (playerDataList.Count > connectedPlayers)
        {
            var data = playerDataList[connectedPlayers++];
            player.InitializePlayer(data.playerName, data.playerDbId, data.playerImagePath);
        }

        // Instantiate and set up the PlayerUI
        PlayerUI playerUI = Instantiate(playerUIPrefab, playerUIParent).GetComponent<PlayerUI>();

        // Configure playerUI as needed...
        playerUI.InitializePlayerUI(player.PlayerName.Value.ToString(), player.PlayerImagePath.Value.ToString());
        
        playerUITracking.Add(player, playerUI);
        players.Add(player);
    }


    /*private Player FindPlayerByNetworkObjectId(ulong NetworkObjectId)
    {
        Debug.Log("FindPlayerByNetworkObjectId is called");
        foreach (var kvp in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
        {
            Debug.Log($"Checking SpawnedObject with NetworkObjectId: {kvp.Key}");
            if (kvp.Key == NetworkObjectId)
            {
                Player player = kvp.Value.GetComponent<Player>();
                if (player != null)
                {
                    Debug.Log($"Player found for NetworkObjectId: {NetworkObjectId}");
                    return player;
                }
            }
        }
        Debug.LogError($"Player not found for NetworkObjectId: {NetworkObjectId}");
        return null;
    }*/

    private Player FindPlayerByNetworkObjectId(ulong NetworkObjectId)
    {
        foreach (var kvp in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
        {
            if (kvp.Value.GetComponent<Player>() != null) // Check if it's a player object
            {
                Player player = kvp.Value.GetComponent<Player>();
                if (player != null && kvp.Key == NetworkObjectId)
                {
                    return player;
                }
            }
        }
        return null;
    }

    private void InitializePlayerData(ulong NetworkObjectId)
    {   
        Debug.Log("InitializePlayerData is called");
        Debug.Log($"Server side call : {IsServer}");
        if (!IsServer) return;
        Debug.Log("InstantiatePlayer0 is called");
        connectedPlayers++;
        var playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(NetworkObjectId);
        Debug.Log("InstantiatePlayer1 is called");
        if (playerObject != null)
        {
            Debug.Log("InstantiatePlayer2 is called");
            var player = playerObject.GetComponent<Player>();
            if (player != null && connectedPlayers <= playerDataList.Count)
            {
                string playerImagePath = "Images/character_01"; // Default image path
                var playerData = playerDataList[connectedPlayers - 1]; // Example, adjust as necessary
                player.InitializePlayer(playerData.playerName, playerData.playerDbId, playerData.playerImagePath);
                Debug.Log("InstantiatePlayer3 is called");
                // New logic to broadcast existing player names to the newly connected client
                BroadcastPlayerNamesToNewClient(NetworkObjectId);

                players.Add(player);
                if (connectedPlayers == TotalPlayerPrefabs)
                {
                    DistributeCards();
                    UpdatePlayerToAsk();
                }
            }
        }
    }

    private PlayerUI InstantiatePlayerUIForPlayer(Player player)
    {
        Debug.Log("InstantiatePlayerUIForPlayer is called");
        //GameObject uiGameObject = Instantiate(playerUIPrefab);
        GameObject uiGameObject = Instantiate(playerUIPrefab, playerUIParent); 
        PlayerUI playerUI = uiGameObject.GetComponent<PlayerUI>();
        // Setup playerUI based on player, if necessary
        return playerUI;
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