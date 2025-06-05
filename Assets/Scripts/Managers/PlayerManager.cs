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
    public List<Player1> players1 = new List<Player1>();
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
        Debug.Log($"PlayerManager: loaded {loadedPlayerDataList.Count} players.");
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
            var player = playerObject.GetComponent<Player1>();
            if (player != null && playerDataList != null && connectedPlayers <= playerDataList.Count)
            {
                string playerImagePath = "Images/character_01"; // Default image path
                var playerData = playerDataList[connectedPlayers - 1]; // Example, adjust as necessary
                player.InitializePlayer(playerData.playerName, playerData.playerDbId, playerData.playerImagePath);
                BroadcastPlayerNamesToNewClient(clientId);
                players1.Add(player);
                Debug.Log($"num of players after: {players1.Count}");

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

                if (players1.Count == playerDataList.Count && !gameInitialized) 
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
        foreach (var player in players1)
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
        CardManager.Instance.DistributeCards(players1);
        TurnManager.Instance.StartTurnManager();
        PrintPlayersListDetails();
    }

    private void UpdatePlayerToAsk()
    {
        Debug.Log("UpdatePlayerToAsk started");
        // Iterate through each player in the 'players1' list
        foreach (var player in players1)
        {
            Debug.Log($"UpdatePlayerToAsk playerName; {player.playerName.Value}");
            // Call a method on the player instance to update its PlayerToAsk list
            player.UpdatePlayerToAskList(players1);
        }
    }

    //testing players1 list
    public void PrintPlayersListDetails()
    {
        Debug.Log($"Printing details of all players1 in the PlayerManager list. Total players1: {players1.Count}");
        
        foreach (var player in players1)
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

        foreach (var playeri in players1)
        {
            // Directly accessing the playerName NetworkVariable
            var playerName = playeri.playerName.Value; // Adjust PlayerName to match the actual variable name in your Player class
            Debug.Log($"Player Name from the players list: {playerName}");

            // If you have other properties or fields to log, you can include them here as well
        }
    }

    /*private void AssignTurnToPlayer()
    {
        if (!IsServer || players.Count == 0) return;

        // Reset all players' HasTurn to false first
        foreach (var player in players)
        {
            player.HasTurn.Value = false;
        }

        // Randomly select a player to assign the turn
        int randomIndex = Random.Range(0, players.Count);
        players[randomIndex].HasTurn.Value = true;
        players[randomIndex].ActivateTurnUIForPlayerClientRpc();

        Debug.Log($"Turn assigned to player: {players[randomIndex].playerName.Value}");
    }*/

    // Method to clean up players, if necessary
    public void CleanupPlayers()
    {
        players1.Clear();
    }

    public void GenerateGamePlayers(List<PlayerData> name)
    {
        //return null;
    }

    /*public string GetPlayerNameByClientId(ulong clientId)
    {
        var player = players.Find(p => p.GetComponent<NetworkObject>().NetworkObjectId == clientId);
        return player != null ? player.playerName.Value.ToString() : "Unknown Player";
    }*/
    /*public string GetPlayerNameByClientId(ulong clientId)
    {
        Debug.Log("GetPlayerNameByClientId is started");
        foreach (var playero in players)
        {
            Debug.Log($"Iterating Player: OwnerClientId = {playero.OwnerClientId}, PlayerName = {playero.playerName.Value}");
        }
        var player = players.FirstOrDefault(p => p.OwnerClientId == clientId);
        Debug.Log($"Player fetched: {player != null}, Client ID: {clientId}");
        return player != null ? player.playerName.Value.ToString() : "Unknown Player";
        Debug.Log($"Getpolayer name by client id sith string is {player.playerName.Value}");
        Debug.Log($"Getpolayer name by client id is {player.playerName.Value}");
        Debug.Log($"Getpolayer name provided clientid is {clientId}");
        Debug.Log($"Getpolayer name by owenerclient id is {player.OwnerClientId}");
    }*/
    public string GetPlayerNameByClientId(ulong clientId)
    {
        // New diagnostic logging
        Debug.Log("Current client ID to Player Name mappings:");
        foreach (var entry in clientIdToPlayerName)
        {
            Debug.Log($"Client ID: {entry.Key}, Player Name: {entry.Value}");
        }

        if (clientIdToPlayerName.TryGetValue(clientId, out string playerName))
        {
            Debug.Log($"Retrieved from dictionary - Client ID: {clientId}, Player Name: {playerName}");
            return playerName;
        }
        else
        {
            Debug.Log($"Failed to retrieve - Client ID: {clientId} resulted in Unknown Player");
            return "Unknown Player"; // Fallback if the clientId is not found
        }
    }

}