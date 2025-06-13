using Unity.Netcode;
using UnityEngine;
using TMPro;
using Unity.Collections; // Required for FixedString32Bytes
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using System;

    public class Player : NetworkBehaviour
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

        public List<CardInstance> HandCards { get; set; } = new List<CardInstance>();
        public List<Player> PlayerToAsk { get; private set; } = new List<Player>();
        public List<CardInstance> CardsPlayerCanAsk { get; private set; } = new List<CardInstance>();
        public List<CardInstance> Quartets { get; private set; } = new List<CardInstance>();
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
                //Debug.Log("UpdatePlayerDbAttributes_ClientRpc is called");
            }
        }

        [ClientRpc]
        private void UpdatePlayerDbAttributes_ClientRpc(string playerName, string playerImagePath)
        {
            if (playerUI != null)
            {
                playerUI.InitializePlayerUI(playerName, playerImagePath);
                //Debug.Log("UpdatePlayerDbAttributes_ClientRpc is running");
            }
        }

        public void ShowHand(bool isLocalPlayer)
        {
            foreach (var card in HandCards)
            {
                var cardUI = card.GetComponent<CardUI>();
                if (cardUI != null)
                {
                    cardUI.SetFaceUp(isLocalPlayer); // Show only if this is the local player's hand
                }
            }
        }


        // In Player.cs
        // In Player.cs - MODIFY existing method
        public void AddCardToHand(CardInstance card) 
        {
            if (IsServer) {
                // OLD WAY (keep commented for safety):
                /*
                HandCards.Add(card);
                CardUI cardUI = CardManager.Instance.FetchCardUIById(card.cardId.Value);
                if (cardUI != null)
                {
                    cardUI.gameObject.SetActive(true);
                    cardUI.SetFaceUp(IsOwner);
                }
                CheckForQuartets();
                UpdateCardsPlayerCanAsk();
                */
                
                // NEW WAY:
                if (PlayerManager.Instance.playerInstances != null)
                {
                    var playerInstance = PlayerManager.Instance.playerInstances
                        .Find(pi => pi.Data.playerDbId == PlayerDbId.Value);
                    if (playerInstance != null)
                    {
                        playerInstance.AddCardToHand(card);
                    }
                }
                
                // Keep network synchronization here
                SendCardIDsToClient();
            }
        }
        // In Player.cs - MODIFY existing method
    public void RemoveCardFromHand(CardInstance card)
    {
        if (card != null && IsServer)
        {
            // OLD WAY (keep commented for safety):
            /*
            HandCards.Remove(card);
            UpdateCardsPlayerCanAsk();
            */
            
            // NEW WAY:
            if (PlayerManager.Instance.playerInstances != null)
            {
                var playerInstance = PlayerManager.Instance.playerInstances
                    .Find(pi => pi.Data.playerDbId == PlayerDbId.Value);
                if (playerInstance != null)
                {
                    playerInstance.RemoveCardFromHand(card);
                }
            }
            
            // Keep network synchronization here
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
            // OLD WAY (keep commented for safety):
            // Score.Value += 1;
            // Debug.Log($"Test: Incremented Score to {Score.Value}");
            
            // NEW WAY:
            if (PlayerManager.Instance.playerInstances != null)
            {
                var playerInstance = PlayerManager.Instance.playerInstances
                    .Find(pi => pi.Data.playerDbId == PlayerDbId.Value);
                if (playerInstance != null)
                {
                    playerInstance.IncrementScore();
                }
            }
        }

        // This method is called on the server to send the card IDs to the client
        public void SendCardIDsToClient()
        {
            if (IsServer)
            {
                int[] cardIDs = HandCards.Select(card => card.cardId.Value).ToArray();
                UpdatePlayerHandUI_ClientRpc(cardIDs, OwnerClientId);
                //Debug.Log("UpdatePlayerHandUI_ClientRpc is called");
                
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
                //Debug.Log("UpdatePlayerHandUI_ClientRpc is running");
            }
        }

        // In Player.cs - MODIFY existing method
        public void UpdateCardsPlayerCanAsk()
        {
            // OLD WAY (keep commented for safety):
            /*
            if (CardsPlayerCanAsk == null)
            {
                CardsPlayerCanAsk = new List<CardInstance>();
            }
            else
            {
                CardsPlayerCanAsk.Clear();
            }
        
            var allCardComponents = CardManager.Instance.allSpawnedCards;

            foreach (var card in allCardComponents)
            {
                if (HandCards.Any(handCard => handCard.suit.Value == card.suit.Value) && !HandCards.Contains(card))
                {
                    CardsPlayerCanAsk.Add(card);
                }
            }
            
            if (IsServer && HasTurn.Value)
            {
                int[] cardIDs = CardsPlayerCanAsk.Select(card => card.cardId.Value).ToArray();
                UpdateCardDropdown_ClientRpc(cardIDs);
            }
            */
            
            // NEW WAY:
            if (PlayerManager.Instance.playerInstances != null)
            {
                var playerInstance = PlayerManager.Instance.playerInstances
                    .Find(pi => pi.Data.playerDbId == PlayerDbId.Value);
                if (playerInstance != null)
                {
                    playerInstance.UpdateCardsPlayerCanAsk();
                }
            }
        }

        [ClientRpc]
        public void UpdateCardDropdown_ClientRpc(int[] cardIDs)
        {
            Debug.Log($"UpdateTurnUIObjectsClientRpc is called: {cardIDs}");
            if (IsOwner)
            {
                //Debug.Log($"Player cs Updating cards dropdown. IDs count: {cardIDs.Length}");
                playerUI?.UpdateCardsDropdownWithIDs(cardIDs);
            }
        }

        // In Player.cs - MODIFY existing method
        public void UpdatePlayerToAskList(List<Player> allPlayers)
        {
            // OLD WAY (keep commented for safety):
            /*
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
                TurnUIForPlayer_ClientRpc(playerIDs, playerNamesConcatenated);
            }
            */
            
            // NEW WAY:
            if (PlayerManager.Instance.playerInstances != null)
            {
                var playerInstance = PlayerManager.Instance.playerInstances
                    .Find(pi => pi.Data.playerDbId == PlayerDbId.Value);
                if (playerInstance != null)
                {
                    playerInstance.UpdatePlayerToAskList(allPlayers);
                }
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
        // In Player.cs - MODIFY existing method
        public void CheckForQuartets()
        {
            // OLD WAY (keep commented for safety):
            /*
            var groupedBySuit = HandCards.GroupBy(card => card.suit.Value.ToString());
            foreach (var suitGroup in groupedBySuit)
            {
                if (suitGroup.Count() == 4)
                {
                    MoveCardsToQuartetsArea(suitGroup.ToList());
                }
            }
            */
            
            // NEW WAY:
            if (PlayerManager.Instance.playerInstances != null)
            {
                var playerInstance = PlayerManager.Instance.playerInstances
                    .Find(pi => pi.Data.playerDbId == PlayerDbId.Value);
                if (playerInstance != null)
                {
                    playerInstance.CheckForQuartets();
                }
            }
        }

        // In Player.cs - MODIFY existing method
        public void MoveCardsToQuartetsArea(List<CardInstance> quartets)
        {
            // OLD WAY (keep commented for safety):
            /*
            Quartets quartetZone = QuartetManager.Instance.QuartetInstance.GetComponent<Quartets>();
            if (quartetZone == null)
            {
                Debug.LogError("Quartets zone not found.");
                return;
            }
            foreach (var card in quartets)
            {
                RemoveCardFromHand(card);
                quartetZone.AddCardToQuartet(card);
            }
            IncrementScore();
            */
            
            // NEW WAY:
            if (PlayerManager.Instance.playerInstances != null)
            {
                var playerInstance = PlayerManager.Instance.playerInstances
                    .Find(pi => pi.Data.playerDbId == PlayerDbId.Value);
                if (playerInstance != null)
                {
                    playerInstance.MoveCardsToQuartetsArea(quartets);
                }
            }
        }

        // Check if the player's hand is empty
        // In Player.cs - MODIFY existing method
        public bool IsHandEmpty()
        {
            // OLD WAY (keep commented for safety):
            // return HandCards.Count == 0;
            
            // NEW WAY:
            if (PlayerManager.Instance.playerInstances != null)
            {
                var playerInstance = PlayerManager.Instance.playerInstances
                    .Find(pi => pi.Data.playerDbId == PlayerDbId.Value);
                if (playerInstance != null)
                {
                    return playerInstance.IsHandEmpty();
                }
            }
            
            // Fallback to old way if playerInstance not found
            return HandCards.Count == 0;
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
            //Debug.Log($"PingServerRpc us called {selectedPlayerId}, {networkCardId.Value}");
            TurnManager.Instance.OnEventGuessClick(selectedPlayerId, networkCardId);
        }
        
}