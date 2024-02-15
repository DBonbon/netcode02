using Unity.Netcode;
using UnityEngine;
using TMPro;
using Unity.Collections; // Required for FixedString32Bytes
using System.Collections.Generic;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{   
    //[SerializeField] private TextMeshProUGUI playerNameText;
    //private float xOffset = 2f;
    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>();
    public NetworkVariable<int> PlayerDbId = new NetworkVariable<int>();
    public NetworkVariable<FixedString128Bytes> PlayerImagePath = new NetworkVariable<FixedString128Bytes>();
    public NetworkVariable<int> Score = new NetworkVariable<int>(0);
    public NetworkVariable<int> Result = new NetworkVariable<int>(0);
    public NetworkVariable<bool> IsWinner = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> HasTurn = new NetworkVariable<bool>(false);

    public List<Card> HandCards { get; set; } = new List<Card>();
    public List<Card> CardsPlayerCanAsk { get; private set; }
    public List<Player> PlayerToAsk { get; private set; } = new List<Player>();

    [SerializeField] private GameObject playerHandPrefab;
    private Transform playerHandTransform; // To store the instantiated playerHandPrefab's transform

    /*[SerializeField]
    private Transform playerHand;
    public Transform PlayerHand { get { return playerHand; } set { playerHand = value; } }
    */
    [SerializeField]
    private Transform playerQuartets;
    public Transform PlayerQuartets { get { return playerQuartets; } set { playerQuartets = value; } }

    private PlayerUI playerUI;

    public override void OnNetworkSpawn()
    {
        //playerUI = GetComponent<PlayerUI>();

        if (IsServer)
        {
            Score.Value = 0; // Initialize or re-assign to ensure it's set on all clients.
            PlayerManager.Instance.RegisterPlayer(NetworkObjectId);
        }

        // Subscribe to Score value changes to update UI accordingly.
        Score.OnValueChanged += OnScoreChanged;
        OnScoreChanged(0, Score.Value); // Manually trigger the update to set initial UI state.
    }

    private void OnScoreChanged(int oldValue, int newValue)
    {
        // This method is called whenever Score changes
        UpdateScoreUI(newValue);
    }

    private void UpdateScoreUI(int score)
    {
        if (playerUI != null)
        {
            playerUI.UpdateScoreUI(score);
        }
    }

    public void InitializePlayer(string name, int dbId, string imagePath)
    {
        if (IsServer)
        {
            PlayerName.Value = name;
            PlayerDbId.Value = dbId;
            PlayerImagePath.Value = imagePath;

            // Update server UI directly here
            UpdateServerUI(name, imagePath);
            // setup and we want to immediately propagate these values to all clients.
            BroadcastPlayerDbAttributes();
            // Spawn and parent the player hand
            SpawnAndParentPlayerHand();
        }
        
    }

    private void UpdateServerUI(string playerName, string playerImagePath)
    {
        if (playerUI != null)
        {
            playerUI.InitializePlayerUI(playerName, playerImagePath);
            playerUI.UpdateHasTurnUI(HasTurn.Value);
        }
    }

    public void BroadcastPlayerDbAttributes()
    {
        if (IsServer)
        {
            UpdatePlayerDbAttributes_ClientRpc(PlayerName.Value.ToString(), PlayerImagePath.Value.ToString());
        }
    }

    [ClientRpc]
    private void UpdatePlayerDbAttributes_ClientRpc(string playerName, string playerImagePath)
    {
        if (playerUI != null)
        {
            playerUI.InitializePlayerUI(playerName, playerImagePath);
        }
    }

    public void AddCardToHand(GameObject cardGameObject)
    {
        if (cardGameObject != null && playerHandTransform != null)
        {
            // Parent the card to the playerHandTransform
            cardGameObject.transform.SetParent(playerHandTransform, false);
            cardGameObject.transform.localPosition = Vector3.zero;
            Debug.Log($"Card {cardGameObject.name} parented to player hand {PlayerName.Value}.");

            // Attempt to retrieve the Card component from the card GameObject
            var cardComponent = cardGameObject.GetComponent<Card>();
            if (cardComponent != null)
            {
                // Add the Card component to the HandCards list
                HandCards.Add(cardComponent);
                Debug.Log($"Card {cardComponent.name} added to player {PlayerName.Value}'s HandCards list.");
            }
            else
            {
                Debug.LogError($"The GameObject {cardGameObject.name} does not have a Card component.");
            }
        }
        else
        {
            Debug.LogError("playerHandTransform is not assigned or cardGameObject is null.");
        }
    }

    public void UpdateTurnStatus(bool hasTurn)
    {
        HasTurn.Value = hasTurn;
        if (playerUI != null)
        {
            playerUI.UpdateHasTurnUI(hasTurn);
        }
    }

    public void UpdatePlayerToAskList(List<Player> allPlayers)
    {
        PlayerToAsk.Clear(); // Clear the list to ensure it's up-to-date

        foreach (var potentialPlayer in allPlayers)
        {
            if (potentialPlayer != this) // Exclude the current player
            {
                PlayerToAsk.Add(potentialPlayer);
            }
        }

        // Optional: Log the count for verification
        Debug.Log($"Player {PlayerName.Value} has {PlayerToAsk.Count} players to ask.");
    }

    // Test method to increment score
    void Update()
    {
        // Simple test: Increment score on mouse click
        if (IsServer && Input.GetMouseButtonDown(0)) // Check for left mouse click
        {
            IncrementScoreTest();
        }
    }

    //to remove when isn't needed anymore:
    public void IncrementScoreTest()
    {
        Score.Value += 1; // Increment score by 1 for test
        Debug.Log($"Test: Incremented Score to {Score.Value}");
    }

    private void SpawnAndParentPlayerHand()
    {
        if (IsServer)
        {
            GameObject handObject = Instantiate(playerHandPrefab);
            NetworkObject handNetworkObject = handObject.GetComponent<NetworkObject>();
            if (handNetworkObject != null)
            {
                handNetworkObject.Spawn();
                playerHandTransform = handObject.transform;
                playerHandTransform.SetParent(this.transform, false);
                playerHandTransform.localPosition = Vector3.zero;

                // Check if the playerHandPrefab has a Canvas, and if so, add the Horizontal Layout Group to it.
                var canvas = handObject.GetComponentInChildren<Canvas>();
                if (canvas != null)
                {
                    var layoutGroup = canvas.gameObject.AddComponent<HorizontalLayoutGroup>();
                    layoutGroup.spacing = 10; // Adjust spacing as needed
                    layoutGroup.childAlignment = TextAnchor.MiddleCenter;
                    layoutGroup.childForceExpandWidth = false;
                    layoutGroup.childForceExpandHeight = false;
                }
                else
                {
                    Debug.LogWarning("Player hand prefab does not contain a Canvas component for the layout group.");
                }
            }
            else
            {
                Debug.LogError("Spawned PlayerHand does not have a NetworkObject component.");
            }
        }
    }

    // Ensure OnDestroy is correctly implemented to handle any cleanup
    public override void OnDestroy()
    {
        base.OnDestroy();
        // Your cleanup logic here
    }

}