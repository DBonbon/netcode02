using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class CardManager : MonoBehaviour
{
    public static CardManager Instance;
    public GameObject cardPrefab; // Network object prefab for cards
    public GameObject cardUIPrefab; // UI prefab for cards
    public Transform DecklTransform; // Parent object for Card UI instances

    private List<CardUI> cardUIPool = new List<CardUI>(); // Pool for Card UI
    public List<CardData> allCardsList = new List<CardData>(); // Loaded card data
    public List<GameObject> allSpawnedCards = new List<GameObject>(); // Inventory of spawned network card object

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            DataManager.OnCardDataLoaded += LoadCardDataLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        NetworkManager.Singleton.OnServerStarted += () => StartCoroutine(StartCardSpawningProcess());
    }

    private void LoadCardDataLoaded(List<CardData> loadedCardDataList)
    {
        allCardsList = loadedCardDataList;
        ShuffleCards();
        InitializeCardUIPool(); // Ensure CardUI pool is initialized after loading data
    }

    private void InitializeCardUIPool()
    {
        Debug.Log($"[CardManager] InitializeCardUIPool called on ClientID={NetworkManager.Singleton.LocalClientId}, isServer={NetworkManager.Singleton.IsServer}, isClient={NetworkManager.Singleton.IsClient}");
        foreach (var cardUIInstance in cardUIPool)
        {
            Destroy(cardUIInstance.gameObject); // Clear existing pool
        }
        cardUIPool.Clear();

        foreach (var cardData in allCardsList)
        {
            var cardUIObject = Instantiate(cardUIPrefab, DecklTransform);
            var cardUIComponent = cardUIObject.GetComponent<CardUI>();
            if (cardUIComponent)
            {
                cardUIComponent.UpdateCardUIWithCardData(cardData);
                cardUIPool.Add(cardUIComponent);
                cardUIObject.SetActive(false); // Start inactive
            }
        }
    }

    public CardUI FetchCardUIById(int cardId)
    {
        // Safety for late clients
        if (cardUIPool.Count == 0)
        {
            if (allCardsList.Count == 0)
            {
                Debug.LogError($"[CardManager] Cannot FetchCardUIById → allCardsList is empty on ClientID={NetworkManager.Singleton.LocalClientId}");
                return null; // No way to build UI pool → return null
            }

            Debug.LogWarning($"[CardManager] cardUIPool was empty → initializing UI pool on ClientID={NetworkManager.Singleton.LocalClientId}");
            InitializeCardUIPool();
        }

        Debug.Log($"[CardManager] FetchCardUIById called on ClientID={NetworkManager.Singleton.LocalClientId}, cardId={cardId}, cardUIPool.Count={cardUIPool.Count}");

        foreach (CardUI cardUI in cardUIPool)
        {
            if (cardUI.cardId == cardId && !cardUI.gameObject.activeInHierarchy)
            {
                return cardUI;
            }
        }
        return null;
    }

    // to be used by turn manager to fetch the selectedCard
    public Card FetchCardById(NetworkVariable<int> cardId)
    {
        foreach (GameObject cardObject in allSpawnedCards)
        {
            Card cardComponent = cardObject.GetComponent<Card>();
            if (cardComponent != null && cardComponent.cardId.Value == cardId.Value)
            {
                return cardComponent;
            }
        }
        return null;
    }

    System.Collections.IEnumerator StartCardSpawningProcess()
    {
        while (DeckManager.Instance.DeckInstance == null)
        {
            yield return null;
        }
        SpawnCards();
    }

    private void SpawnCards()
    {
        foreach (var cardData in allCardsList)
        {
            var spawnedCard = Instantiate(cardPrefab, transform); // Instantiate without parent to avoid hierarchy issues
            var networkObject = spawnedCard.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();

                var cardComponent = spawnedCard.GetComponent<Card>();
                if (cardComponent != null)
                {
                    // Initialize the card
                    cardComponent.InitializeCard(cardData.cardId, cardData.cardName, cardData.suit, cardData.hint, cardData.siblings, cardData.icon);

                    allSpawnedCards.Add(spawnedCard);

                    // Assuming DeckInstance holds the deck GameObject, access Deck component to call AddCardToDeck
                    if (DeckManager.Instance.DeckInstance != null)
                    {
                        var deckComponent = DeckManager.Instance.DeckInstance.GetComponent<Deck>();
                        if (deckComponent != null)
                        {
                            deckComponent.AddCardToDeck(spawnedCard); // Pass the GameObject directly
                        }
                        else
                        {
                            Debug.LogError("Deck component not found on DeckInstance.");
                        }
                    }
                    else
                    {
                        Debug.LogError("DeckInstance is null.");
                    }
                }
            }
        }
    }

    private void ShuffleCards()
    {
        System.Random rng = new System.Random();
        int n = allCardsList.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            var value = allCardsList[k];
            allCardsList[k] = allCardsList[n];
            allCardsList[n] = value;
        }
    }

    private void OnDestroy()
    {
        DataManager.OnCardDataLoaded -= LoadCardDataLoaded;
    }

    public void DistributeCards(List<Player1> players1)
    {
        Debug.Log($"[DistributeCards] Called. Players count: {players1.Count}");

        int cardsPerPlayer = 5; // Assuming 5 cards per player

        Deck deck = DeckManager.Instance.DeckInstance.GetComponent<Deck>();
        if (deck == null)
        {
            Debug.LogError("Deck is not found.");
            return;
        }

        foreach (var player in players1)
        {
            Debug.Log($"[DistributeCards] Distributing to Player: OwnerClientId={player.OwnerClientId}, Name={player.playerName.Value}");

            for (int i = 0; i < cardsPerPlayer; i++)
            {
                Card card = deck.RemoveCardFromDeck(); // This now returns a Card object
                if (card != null)
                {
                    player.AddCardToHand(card); // Adjusted to accept Card objects
                    Debug.Log($"[DistributeCards] Gave card {card.cardId.Value} to Player {player.playerName.Value}");
                }
                else
                {
                    Debug.LogWarning("Deck is out of cards.");
                    break; // Stop if there are no more cards
                }
            }
        }
    }

    public void DrawCardFromDeck(Player1 currentPlayer)
    {
        if (currentPlayer == null)
        {
            Debug.LogWarning("Invalid player.");
            return;
        }

        Deck deck = DeckManager.Instance.DeckInstance.GetComponent<Deck>();
        if (deck == null)
        {
            Debug.LogError("Deck is not found.");
            return;
        }

        Card card = deck.RemoveCardFromDeck(); // Adjust to use your actual method for removing a card from the deck.
        if (card != null)
        {
            currentPlayer.AddCardToHand(card); // Adapt to network context as in DistributeCards.
            // Assuming AddCardToHand handles the necessary network synchronization internally.
        }
        else
        {
            Debug.LogWarning("Deck is out of cards.");
        }
        currentPlayer.SendCardIDsToClient();
    }

    public void EnsureCardUIPoolInitializedIfNeeded()
    {
        if (cardUIPool.Count == 0 && allCardsList.Count > 0)
        {
            Debug.LogWarning("[CardManager] CardUIPool was empty on EnsureCardUIPoolInitializedIfNeeded → Reinitializing.");
            InitializeCardUIPool();
        }
    }

    public string GetCardNameById(int cardId)
    {
        var cardData = allCardsList.Find(card => card.cardId == cardId);
        return cardData != null ? cardData.cardName : "Unknown Card";
    }
    // Utility methods for CardUI pool management can be added here if needed
}
