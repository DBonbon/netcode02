//turnmanager
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/*
to check why the cardui aren't operates correctly. 
print a dict of cardui.name and cardui.id. test it matches that of cards

ensure it is returned correctly from the option dropdown list. 

check it then against the turnmanager. 

*/

public class TurnManager : NetworkBehaviour
{
    public static TurnManager Instance;
    
    public delegate void EnableUIEvent(bool enableUI);
    public static event EnableUIEvent OnEnableUI;
    // Start is called before the first frame update
    private Card selectedCard;
    private Player1 selectedPlayer;
    private Player1 currentPlayer;
    private bool isPlayerUIEnabled = true;
    private bool isDrawingCard = false;
    private bool hasHandledCurrentPlayer = false;
    private bool isInitialized = false;
    private bool activateTurnUIFlag = false;
    private List<Player1> players;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // ⚠️ Do not access players here — it may be too early
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void StartTurnManager()
    {
        players = PlayerManager.Instance.players1;

        if (players == null || players.Count == 0)
        {
            Debug.LogError("TurnManager cannot start: player list is empty!");
            return;
        }

        foreach (var p in players)
        {
            Debug.Log($"[TurnManager] Player ready: {p.playerName.Value}");
        }

        Debug.Log("turnmanmager started");
        AssignTurnToPlayer();
        StartTurnLoop();
    }


    private void AssignTurnToPlayer()
    {
        Debug.Log("AssignTurnToPlayer is called");
        //var players = PlayerManager.Instance.players;
        if (players.Count == 0) return;

        foreach (var player in players)
        {
            player.HasTurn.Value = false;
        }

        //int randomIndex = Random.Range(0, players.Count);
        int randomIndex = UnityEngine.Random.Range(0, players.Count);

        players[randomIndex].HasTurn.Value = true;
        // Let UpdatePlayerToAskList handle the RPC call with the correct data
        players[randomIndex].UpdatePlayerToAskList(players);
        players[randomIndex].UpdateCardsPlayerCanAsk();

        Debug.Log($"Turn assigned to player: {players[randomIndex].playerName.Value}");
    }

    private void StartTurnLoop()
    {
        if (!isInitialized)
        {
            Debug.Log("Turn Manager Started");
            //AssignTurnToPlayer();
            isInitialized = true;
            currentPlayer = players.Find(player => player.HasTurn.Value);
            Debug.Log($"call TurnLoop: {currentPlayer.playerName.Value}");

            if (currentPlayer != null)
            {
                StartCoroutine(TurnLoop());
                Debug.Log($"call coroutine TurnLoop: {currentPlayer.playerName.Value}");
            }
            else
            {
                Debug.LogError("No initial player with hasTurn == true found.");
            }
        }
    }

    private System.Collections.IEnumerator TurnLoop()
    {
        Debug.Log("Turn loop is rusnning");
        while (true)
        {
            Debug.Log($"Turn loop currewnt player: {currentPlayer.playerName.Value} with turn status: {currentPlayer.HasTurn.Value}");
            if (currentPlayer.HasTurn.Value)
            {
                if (!hasHandledCurrentPlayer)
                {
                    Debug.Log($"Turn loop hasHandledCurrentPlayer current player: {currentPlayer.playerName.Value} whith turn status: {currentPlayer.HasTurn.Value} and hashandlecurretplay flag is: {hasHandledCurrentPlayer}");
                    HandlePlayerTurn(currentPlayer);
                    Debug.Log($"Turn loop hasHandledCurrentPlayer1 current player: {currentPlayer.playerName.Value} whith turn status: {currentPlayer.HasTurn.Value}");
                    Debug.Log($" hashandlecurretplay flag is: {hasHandledCurrentPlayer}");
                    hasHandledCurrentPlayer = true;
                    Debug.Log($" hashandlecurretplay1 flag is: {hasHandledCurrentPlayer}");
                    Debug.Log($"Turn loop hasHandledCurrentPlayer1 current player: {currentPlayer.playerName.Value} ad hashandlecurretplay flag is: {hasHandledCurrentPlayer}");
                }
            }

            if (!currentPlayer.HasTurn.Value)
            {
                Debug.Log($"Turn loop hasHandledCurrentPlayer current player: {currentPlayer.playerName.Value} has Turn: {currentPlayer.HasTurn.Value}");
                hasHandledCurrentPlayer = false;
                NextCurrentPlayer();
            }
            // Debug log to check if the loop is still running
            Debug.Log("Turn loop is still running");
            yield return null;
        }
        // Debug log to check if the loop terminates
        Debug.Log("Turn loop terminated");
    }

    public void OnEventGuessClick(ulong playerId, NetworkVariable<int> cardId)
    {
        //NetworkVariable<int> networkCardId =???(cardId); 
        Debug.Log($"The playerid value is: {playerId}, and cardid: {cardId}");
        Card selectedCard = CardManager.Instance.FetchCardById(cardId);
        Debug.Log($"oneventguessclick selected card: {selectedCard.cardName.Value}");
        Player1 selectedPlayer = players.Find(player => player.OwnerClientId == playerId);
        Debug.Log($"oneventguessclick selected player: {selectedPlayer.playerName.Value}");
        this.selectedCard = selectedCard;
        this.selectedPlayer = selectedPlayer;
        if (currentPlayer != null && !isDrawingCard) //
        {
            HandlePlayerTurn(currentPlayer);
            Debug.Log($"HandlePlayerTurn currentPlayer is: {currentPlayer}");
        }
        else
        {
            Debug.LogWarning($"{Time.time}: Invalid player turn.");
        }
    }

    private void HandlePlayerTurn(Player1 currentPlayer)
    {
        //MakeGuess(currentPlayer);
        Debug.Log("HandlePlayerTurn is called");
        Debug.Log($"currentPlazer is: {currentPlayer}");
        Debug.Log($"Selected Card: {selectedCard.cardName.Value}");
        ActivateTurnUI();
        Debug.Log("ActivateTurnUI is called");

        if (selectedCard != null && selectedPlayer != null)
        {
            GuessCheck(selectedCard, selectedPlayer);
            Debug.Log("GuessCheck is called");
            // Reset selectedCard and selectedPlayer to null
            selectedCard = null;
            selectedPlayer = null;
        }
        else
        {
            Debug.Log($"handle player method waits for selectedCard: {selectedCard} and/or selectedPlayer {selectedPlayer}");
        }
    }

    private void ActivateTurnUI()
    {
        // Toggle the state of the activateTurnUIFlag each time the method is called
        activateTurnUIFlag = !activateTurnUIFlag;
        Debug.Log("starting Activating Turn UI for player: " + currentPlayer.name);

        // Based on the flag state, you can perform different actions
        if (activateTurnUIFlag)
        {
            // Code to activate or display the turn UI
            Debug.Log("Activating Turn UI for player: " + currentPlayer.name);
        }
        else
        {
            // Code to deactivate or hide the turn UI
            Debug.Log("Deactivating Turn UI for player: " + currentPlayer.name);
        }

        // You can also display messages or perform other operations here
    }

    private void GuessCheck(Card selectedCard, Player1 selectedPlayer)
    {
        ActivateTurnUI();
        Debug.Log("GuessCheck is running");
        if (selectedPlayer.HandCards.Contains(selectedCard))
        {
            CorrectGuess();
        }
        else
        {
            WrongGuess();
        }
    }

    private void CorrectGuess()
    {
        Debug.Log("AskForCar guess is correct");
        TransferCard(selectedCard, currentPlayer);
        Debug.Log($"TransferCard is correct: {selectedCard.cardName.Value}");
        CheckForQuartets(); 
        selectedCard = null;
        selectedPlayer = null;
        Debug.Log($"Selected Card: {selectedCard.cardName.Value}");  
        //HandlePlayerTurn(currentPlayer);*/
        
        // If the guess is correct and the player's hand isn't empty, allow another guess.
        if (!IsPlayerHandEmpty(currentPlayer))
        {
            HandlePlayerTurn(currentPlayer);
        }
        else if (DeckManager.Instance.CurrentDeck != null && DeckManager.Instance.CurrentDeck.DeckCards.Count > 0)
        {
            DrawCardFromDeck(() =>
            {
                // This callback ensures we re-evaluate the hand only after the card has been drawn
                if (!IsPlayerHandEmpty(currentPlayer))
                {
                    HandlePlayerTurn(currentPlayer);
                }
            });
        }
        // No need to check for the deck being empty here, as we've alrug.Loady handled that case.
    }

    private void WrongGuess()
    {
        Debug.Log("ask for card, player doesn't have card");
        DisplayMessage($"{selectedPlayer.playerName} does not have {selectedCard.cardName}.");
        DrawCardFromDeck(() => 
        {
            // Actions to take after drawing the card, if any.
            // For example, you might want to update some UI elements here to reflect the new card in the player's hand.
            
            Debug.Log("ask for card, call end turn");
            EndTurn(); // This is now inside the callback, ensuring it's called after drawing a card.
        });
    }
    
    private void TransferCard(Card selectedCard, Player1 curPlayer)
    {
        Debug.Log("TransferCard is correct");    
        selectedPlayer.RemoveCardFromHand(selectedCard);
        //selectedPlayer.SendCardIDsToClient();
        currentPlayer.AddCardToHand(selectedCard);
        //currentPlayer.SendCardIDsToClient();
    }

    private bool IsPlayerHandEmpty(Player1 currentPlayer)
    {
        return currentPlayer.IsHandEmpty();
    }

    private void EndTurn()
    {
        NextCurrentPlayer();
        Debug.Log("end turn is running");
    }

    private void NextCurrentPlayer()
    {
        Debug.Log("next current player is called");
        var players = PlayerManager.Instance.players1;
        if (players.Count == 0) return;

        // Find the index of the current player who has the turn.
        int currentIndex = players.IndexOf(currentPlayer);
        
        // Ensure currentIndex is valid.
        if (currentIndex == -1) return;

        // Turn off the current player's turn.
        currentPlayer.HasTurn.Value = false;

        // Calculate the index of the next player. Wrap around if necessary.
        int nextIndex = (currentIndex + 1) % players.Count;

        // Ensure the next player exists.
        if (nextIndex < players.Count && nextIndex >= 0)
        {
            // Set the next player as the current player and turn their turn on.
            currentPlayer = players[nextIndex];
            currentPlayer.HasTurn.Value = true;
            currentPlayer.UpdatePlayerToAskList(players);
            currentPlayer.UpdateCardsPlayerCanAsk();
            
            // Log or perform additional actions as necessary.
            Debug.Log($"Turn assigned to player: {currentPlayer.playerName.Value}");
        }
        else
        {
            // Handle the unexpected case where nextIndex is out of bounds.
            Debug.LogError("Next player index is out of valid range.");
        }
    }

    private void CheckForQuartets()
    {
        Debug.Log("check for quartets is called");
        currentPlayer.CheckForQuartets(); // Implement your quartet-checking logic here
        // Check if the player's hand is empty after quartets are checked.
        if (IsPlayerHandEmpty(currentPlayer) &&  DeckManager.Instance.CurrentDeck.DeckCards.Count == 0)
        {
            CheckGameEnd();
            EndTurn();
        }
    }

    private void DisplayMessage(string message)
    {
        // Your DisplayMessage logic here
        // ...
        Debug.Log("Display Message: " + message);
    }

    /*public void DrawCardFromDeck()
    {
        Debug.Log("draw card from deck is called");
        if (CardManager.Instance != null && currentPlayer != null)
        {
            CardManager.Instance.DrawCardFromDeck(currentPlayer);
        }
        else
        {
            Debug.LogError($"{Time.time}: CardManager reference or current player or selected card is not assigned.");
        }
    }*/
    public void DrawCardFromDeck(Action onCardDrawn)
    {
        Debug.Log("draw card from deck is called");
        // Assume this method involves adding a card to currentPlayer's hand
        CardManager.Instance.DrawCardFromDeck(currentPlayer);
        
        // Simulate the card drawing and hand update process
        // Once complete, invoke the callback
        onCardDrawn?.Invoke();
    }


    private void CheckGameEnd()
    {
        bool allHandsEmpty = true;
        foreach (var player in players)
        {
            if (!player.IsHandEmpty())
            {
                allHandsEmpty = false;
                break;
            }
        }

        if (allHandsEmpty)
        {
            GameEnd();
        }
    }

    private void GameEnd()
    {
        Debug.Log($"{Time.time}: Game Ended");
        // Call the method to display end game results
        //gameFlowManager.DisplayEndGameResults();
    }
}