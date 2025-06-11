using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using System.Collections.Generic;

public class DataManager : MonoBehaviour
{
    public PlayerManager playerManager; // Reference to the PlayerManager script
    public CardManager cardManager; // Reference to the CardManager script
    
    // Define events for player data and card data loading
    public delegate void PlayerDataLoadedHandler(List<PlayerData> players);
    public static event PlayerDataLoadedHandler OnPlayerDataLoaded;

    public delegate void CardDataLoadedHandler(List<CardData> cards);
    public static event CardDataLoadedHandler OnCardDataLoaded;

    // Static properties to access loaded data
    public static List<PlayerData> LoadedPlayerData { get; private set; }
    public static List<CardData> LoadedCardData { get; private set; }

    private bool playerDataLoaded = false;
    private bool cardDataLoaded = false;

    private void Awake()
    {
        DataManager.OnPlayerDataLoaded += OnPlayerDataLoaded;
        DataManager.OnCardDataLoaded += OnCardDataLoaded; // Ensure this line exists
    }

    private void Start()
    {
        LoadPlayersFromJson();
        LoadCardsFromJson();
    }

    private void LoadPlayersFromJson()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "players.json");

        StartCoroutine(LoadPlayersData(path));
    }

    private IEnumerator LoadPlayersData(string path)
    {
        string json;
        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
        {
            UnityWebRequest www = UnityWebRequest.Get(path);
            yield return www.SendWebRequest();
            json = www.downloadHandler.text;
            // Use UnityWebRequest for mobile platforms
            // ... (code to load JSON using UnityWebRequest)
        }
        else
        {
            json = File.ReadAllText(path);
        }

        // Create a wrapper class to hold the "players" array from the JSON data
        PlayersDataWrapper dataWrapper = JsonUtility.FromJson<PlayersDataWrapper>(json);

        // Get the player data list from the wrapper
        List<PlayerData> playerDataList = dataWrapper.players;

        // Set the LoadedPlayerData property first
        LoadedPlayerData = playerDataList;

        // Pass the player data to the PlayerManager
        //playerManager.InitializePlayers(playerDataList);

        // Notify subscribers that player data is loaded
        OnPlayerDataLoaded?.Invoke(playerDataList);
        playerDataLoaded = true;
        CheckStartManagers(); // Check if both player and card data are loaded
    }

    [System.Serializable]
    private class PlayersDataWrapper
    {
        public List<PlayerData> players;
    }

    private void LoadCardsFromJson()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "animals.json");
        StartCoroutine(LoadCardsData(path));
        //string apiUrl = "http://localhost:8081/wt/api/nextjs/v1/page_by_path/?html_path=authors/yoga/unity";
        //StartCoroutine(LoadCardsDataFromJson(apiUrl));
    }

    private IEnumerator LoadCardsData(string path)
    {
        string json;
        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
        {
            UnityWebRequest www = UnityWebRequest.Get(path);
            yield return www.SendWebRequest();
            json = www.downloadHandler.text;
            // Use UnityWebRequest for mobile platforms
            // ... (code to load JSON using UnityWebRequest)
        }
        else
        {
            json = File.ReadAllText(path);
        }

        // Create a wrapper class to hold the "players" array from the JSON data
        CardsDataWrapper dataWrapper = JsonUtility.FromJson<CardsDataWrapper>(json);

        // Get the player data list from the wrapper
        List<CardData> cardDataList = dataWrapper.cards;

        if (cardDataList != null)
        {
            foreach (CardData cardData in cardDataList)
            {
                cardData.PopulateSiblings(cardDataList);
            }

            OnCardDataLoaded?.Invoke(cardDataList);
            cardDataLoaded = true;
            CheckStartManagers();
        }
        else
        {
            Debug.LogError("No card data found in the API response.");
        }
    }

    [System.Serializable]
    private class CardsDataWrapper
    {
        public List<CardData> cards;
    }


    // Check if both player and card data are loaded
    private void CheckStartManagers()
    {
        if (playerDataLoaded && cardDataLoaded)
        {
            // Both data sets are loaded, you can perform any necessary actions here
        }
    }


}