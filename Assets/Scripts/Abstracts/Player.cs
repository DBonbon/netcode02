using Unity.Netcode;
using UnityEngine;
using TMPro;
using Unity.Collections; // Required for FixedString32Bytes
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using System;

    public class Player1 : NetworkBehaviour
    {   
        //[SerializeField] private TextMeshProUGUI playerNameText;
        //private float xOffset = 2f;
        public NetworkVariable<FixedString128Bytes> playerName = new NetworkVariable<FixedString128Bytes>();
        public NetworkVariable<int> PlayerDbId = new NetworkVariable<int>();
        public NetworkVariable<FixedString128Bytes> PlayerImagePath = new NetworkVariable<FixedString128Bytes>();
        public NetworkVariable<int> Score = new NetworkVariable<int>(0);
        public NetworkVariable<int> Result = new NetworkVariable<int>(0);
        public NetworkVariable<bool> IsWinner = new NetworkVariable<bool>(false);
        public NetworkVariable<bool> HasTurn = new NetworkVariable<bool>(false);

        public List<Card> HandCards { get; set; } = new List<Card>();
        public List<Player1> PlayerToAsk { get; private set; } = new List<Player1>();
        public List<Card> CardsPlayerCanAsk { get; private set; } = new List<Card>();
        public List<Card> Quartets { get; private set; } = new List<Card>();

        //we use Action event since these aren't Network variables like score for which the event onValueChange is already incuded in the 
        //from the networkvariable wrapper, NetworkVariable<T>.OnValueChanged
        public event Action OnPlayerToAskListUpdated;
        public event Action OnCardsPlayerCanAskListUpdated;

        private PlayerUI playerUI;

        public override void OnNetworkSpawn()
        {
            //PlayerManager.Instance.OnClientConnected(OwnerClientId);
            playerUI = GetComponent<PlayerUI>();

            if (IsServer)
            {
                Score.Value = 0; // Initialize or re-assign to ensure it's set on all clients.
            }

            // Subscribe to Score value changes to update UI accordingly.
            Score.OnValueChanged += OnScoreChanged;
            HasTurn.OnValueChanged += OnHasTurnChanged;

            OnScoreChanged(0, Score.Value); // Manually trigger the update to set initial UI state.
            OnHasTurnChanged(false, HasTurn.Value); // Manually trigger to ensure UI is correctly set up on spawn.
        }

        public void InitializePlayer(string name, int dbId, string imagePath)
        {
            if (IsServer)
            {
                playerName.Value = name;
                PlayerDbId.Value = dbId;
                PlayerImagePath.Value = imagePath;

                // Update server UI directly here
                UpdateServerUI(name, imagePath);
                // setup and we want to immediately propagate these values to all clients.
                BroadcastPlayerDbAttributes();
            }
        }

        private void UpdateServerUI(string playerName, string playerImagePath)
        {
            if (playerUI != null)
            {
                playerUI.InitializePlayerUI(playerName, playerImagePath);
                //playerUI.UpdateHasTurnUI(HasTurn.Value);
            }
        }

        public void BroadcastPlayerDbAttributes()
        {
            if (IsServer)
            {
                UpdatePlayerDbAttributes_ClientRpc(playerName.Value.ToString(), PlayerImagePath.Value.ToString());
                Debug.Log("UpdatePlayerDbAttributes_ClientRpc is called");
            }
        }

        [ClientRpc]
        private void UpdatePlayerDbAttributes_ClientRpc(string playerName, string playerImagePath)
        {
            if (playerUI != null)
            {
                playerUI.InitializePlayerUI(playerName, playerImagePath);
                Debug.Log("UpdatePlayerDbAttributes_ClientRpc is running");
            }
        }

        // In Player.cs
        public void AddCardToHand(Card card) 
        {
            if (IsServer) {
                HandCards.Add(card);
                // Potentially update UI here as necessary
                //Debug.Log($"Card {card.name} added to player {playerName.Value}'s HandCards list.");
                //UpdatePlayerHandUI();
                CheckForQuartets();
                SendCardIDsToClient();
                UpdateCardsPlayerCanAsk();  
            }
        }

        public void RemoveCardFromHand(Card card)
        {
            if (card != null && IsServer)
            {
                HandCards.Remove(card);
                Debug.Log($"Removed card {card.cardName} from player's hand.");
                // Update UI if necessary
                //UpdatePlayerHandUI();
                UpdateCardsPlayerCanAsk();
                SendCardIDsToClient();
            }
        }
       
        private void OnHasTurnChanged(bool oldValue, bool newValue)
        {
            if (playerUI != null && IsOwner)
            {
                playerUI.UpdateTurnUI(newValue);
            }
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

        //to remove when isn't needed anymore:
        public void IncrementScore()
        {
            Score.Value += 1; // Increment score by 1 for test
            //Debug.Log($"Test: Incremented Score to {Score.Value}");
        }

        // This method is called on the server to send the card IDs to the client
        public void SendCardIDsToClient()
        {
            if (IsServer)
            {
                int[] cardIDs = HandCards.Select(card => card.cardId.Value).ToArray();
                UpdatePlayerHandUI_ClientRpc(cardIDs, OwnerClientId);
                Debug.Log("UpdatePlayerHandUI_ClientRpc is called");
                
            }
        }

        // ClientRpc to update the player's hand UI with the given card IDs
        [ClientRpc]
        private void UpdatePlayerHandUI_ClientRpc(int[] cardIDs, ulong targetClient)
        {
            // Ensure that this RPC is executed only by the target client
            if (IsOwner)
            {
                playerUI?.UpdatePlayerHandUIWithIDs(cardIDs.ToList());
                Debug.Log("UpdatePlayerHandUI_ClientRpc is running");
            }
        }

        public void UpdateCardsPlayerCanAsk()
        {
            // Ensure CardsPlayerCanAsk is initialized
            if (CardsPlayerCanAsk == null)
            {
                CardsPlayerCanAsk = new List<Card>();
            }
            else
            {
                CardsPlayerCanAsk.Clear();
            }
           
            //var allCards = CardManager.Instance.allSpawnedCards; // Make sure this is a List<Card>
            var allCardComponents = CardManager.Instance.allSpawnedCards.Select(go => go.GetComponent<Card>()).Where(c => c != null);

            foreach (var card in allCardComponents)
            {
                if (HandCards.Any(handCard => handCard.Suit.Value == card.Suit.Value) && !HandCards.Contains(card))
                {
                    CardsPlayerCanAsk.Add(card);
                }
            }
            
            if (IsServer && HasTurn.Value)
            {
                int[] cardIDs = CardsPlayerCanAsk.Select(card => card.cardId.Value).ToArray();
                UpdateCardDropdown_ClientRpc(cardIDs);
            }
            Debug.Log($"Player {playerName.Value} can ask for {CardsPlayerCanAsk.Count} cards based on suits.");
        }

        [ClientRpc]
        private void UpdateCardDropdown_ClientRpc(int[] cardIDs)
        {
            Debug.Log($"UpdateTurnUIObjectsClientRpc is called: {cardIDs}");
            if (IsOwner)
            {
                Debug.Log($"Player cs Updating cards dropdown. IDs count: {cardIDs.Length}");
                playerUI?.UpdateCardsDropdownWithIDs(cardIDs);
            }
        }

        public void UpdatePlayerToAskList(List<Player1> allPlayers)
        {
            PlayerToAsk.Clear();
            foreach (var potentialPlayer in allPlayers)
            {
                if (potentialPlayer != this)
                {
                    PlayerToAsk.Add(potentialPlayer);
                    Debug.Log($"Added {potentialPlayer.playerName.Value} to PlayerToAsk.");
                }
            }

            if (IsServer && HasTurn.Value)
            {
                ulong[] playerIDs = PlayerToAsk.Select(player => player.OwnerClientId).ToArray();
                string playerNamesConcatenated = string.Join(",", PlayerToAsk.Select(player => player.playerName.Value.ToString()));
                Debug.Log("UpdatePlayerToAskList calling TurnUIForPlayer_ClientRp() ");
                TurnUIForPlayer_ClientRpc(playerIDs, playerNamesConcatenated); // Adjusted to pass concatenated names
            }
        }

        [ClientRpc]
        public void TurnUIForPlayer_ClientRpc(ulong[] playerIDs, string playerNamesConcatenated)
        {
            Debug.Log($"TurnUIForPlayer_ClientRpc is owner check: {IsOwner}");
            if (IsOwner) // Ensure this runs only for the player whose turn it is
            {
                // Split the concatenated string back into an array
                string[] playerNames = playerNamesConcatenated.Split(',');
                playerUI.UpdatePlayersDropdown(playerIDs, playerNames);
                Debug.Log("TurnUIForPlayer_ClientRpc is running");
            }
        }
        
        //utility method section:
        public void CheckForQuartets()
        {
            // Group cards by their Suit value
            var groupedBySuit = HandCards.GroupBy(card => card.Suit.Value.ToString());

            foreach (var suitGroup in groupedBySuit)
            {
                if (suitGroup.Count() == 4) // Exactly 4 cards of the same suit
                {
                    MoveCardsToQuartetsArea(suitGroup.ToList());
                }
            }
        }

        public void MoveCardsToQuartetsArea(List<Card> quartet)
        {
            //Debug.Log("Moving cards to quartets area.");

            Quartets quartetZone = QuartetsManager.Instance.QuartetsInstance.GetComponent<Quartets>();
            if (quartetZone == null)
            {
                Debug.LogError("Quartets zone not found.");
                return;
            }

            foreach (var card in quartet)
            {
                RemoveCardFromHand(card);
                quartetZone.AddCardToQuartets(card);
                Debug.Log($"Moved card {card.cardName} to Quartets.");
            }
            IncrementScore();

            // You may want to update the player's UI here to reflect the removal of these cards from their hand            
        }

        // Check if the player's hand is empty
        public bool IsHandEmpty()
        {
            return HandCards.Count == 0;
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
            //Debug.Log($"Test: Incremented Score to {Score.Value}");
        }

        // Ensure OnDestroy is correctly implemented to handle any cleanup
        public override void OnDestroy()
        {
            base.OnDestroy();
            // Your cleanup logic here
        }

        [ServerRpc(RequireOwnership = true)]
        public void OnEventGuessClickServerRpc(ulong selectedPlayerId, int cardId)
        {
            NetworkVariable<int> networkCardId = new NetworkVariable<int>(cardId);
            Debug.Log($"PingServerRpc us called {selectedPlayerId}, {networkCardId.Value}");
            TurnManager.Instance.OnEventGuessClick(selectedPlayerId, networkCardId);
        }
        
}