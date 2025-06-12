using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using System.Reflection;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance;
    public static int TotalPlayerPrefabs = 2; // Adjust based on your game's needs
    private int connectedPlayers = 0;
    private bool gameInitialized = false; // Flag to track if the game start logic has been executed
    public List<Player> players = new List<Player>();
    private List<PlayerData> playerDataList;
    [SerializeField] private Transform deckUIContainer;

    private Dictionary<ulong, string> clientIdToPlayerName = new Dictionary<ulong, string>();
    private Dictionary<string, ulong> playerNameToClientId = new Dictionary<string, ulong>();

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
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    public void LoadPlayerDataLoaded(List<PlayerData> loadedPlayerDataList)
    {
        this.playerDataList = loadedPlayerDataList;
        //Debug.Log("Player data loaded into PlayerManager.");
    }

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        //Debug.Log("starting playerManager");
    }

    public void OnClientConnected(ulong clientId)
    {
        if (!IsServer || gameInitialized) return;
        Debug.Log("OnClientConnected started");
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
                BroadcastPlayerNamesToNewClient(clientId);
                players.Add(player);
                Debug.Log($"num of players after: {players.Count}");

                // Update dictionaries
                clientIdToPlayerName[clientId] = player.playerName.Value.ToString();
                Debug.Log($"dictionary playername: {clientIdToPlayerName[clientId]}");
                playerNameToClientId[player.playerName.Value.ToString()] = clientId;
                Debug.Log($"dictionary playername to id: {clientId}");

                // Test retrieval
                //string testRetrievalName = GetPlayerNameByClientId(clientId);
                //Debug.Log($"Test retrieval post-update - Client ID: {clientId}, Player Name: {testRetrievalName}");
                // Add a debug log here to confirm the dictionary is updated
                Debug.Log($"[PlayerManager] Added player with Client ID: {clientId} and Name: {player.playerName.Value}");

                if (players.Count == playerDataList.Count && !gameInitialized)
                {
                    gameInitialized = true;
                    StartGameLogic();
                    Debug.Log("calling StartGameLogic");
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

    private void StartGameLogic()
    {
        Debug.Log("running StartGameLogic");
        //PrintPlayersListDetails();
        //UpdatePlayerToAsk();
        CardManager.Instance.DistributeCards(players);

        TurnManager.Instance.StartTurnManager();
        PrintPlayersListDetails();
    }

    private void UpdatePlayerToAsk()
    {
        Debug.Log("UpdatePlayerToAsk started");
        // Iterate through each player in the 'players' list
        foreach (var player in players)
        {
            Debug.Log($"UpdatePlayerToAsk playerName; {player.playerName.Value}");
            // Call a method on the player instance to update its PlayerToAsk list
            player.UpdatePlayerToAskList(players);
        }
    }

    //testing players list
    public void PrintPlayersListDetails()
    {
        Debug.Log($"Printing details of all players in the PlayerManager list. Total players: {players.Count}");

        foreach (var player in players)
        {
            Debug.Log($"Player Details - OwnerClientId: {player.OwnerClientId}");

            // Using reflection to iterate through all properties
            PropertyInfo[] properties = player.GetType().GetProperties();
            foreach (PropertyInfo property in properties)
            {
                object value = property.GetValue(player, null);
                Debug.Log($"GetValue of properties name: {property.Name}: {value}");
            }


            // Additionally iterating through all fields if necessary
            FieldInfo[] fields = player.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                object value = field.GetValue(player);
                Debug.Log($"{field.Name}: {value}");
            }
        }

        foreach (var playeri in players)
        {
            // Directly accessing the playerName NetworkVariable
            var playerName = playeri.playerName.Value; // Adjust PlayerName to match the actual variable name in your Player class
            Debug.Log($"Player Name from the players list: {playerName}");

            // If you have other properties or fields to log, you can include them here as well
        }
    }

    // Method to clean up players, is used by networkmanagerui
    public void CleanupPlayers()
    {
        players.Clear();
    }

    public string GetPlayerNameByClientId(ulong clientId)
    {
        if (clientIdToPlayerName.TryGetValue(clientId, out string playerName))
        {
            return playerName;
        }
        else
        {
            return "Unknown Player";
        }
    }
}