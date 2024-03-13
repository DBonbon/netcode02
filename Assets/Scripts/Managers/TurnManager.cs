using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TurnManager : NetworkBehaviour
{
    public static TurnManager Instance;
    
    public delegate void EnableUIEvent(bool enableUI);
    public static event EnableUIEvent OnEnableUI;
    // Start is called before the first frame update
    private Card selectedCard;
    private Player selectedPlayer;
    private Player currentPlayer;
    private bool isPlayerUIEnabled = true;
    private bool isDrawingCard = false;
    private bool hasHandledCurrentPlayer = false;
    private bool isInitialized = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void StartTurnManager()
    {
        Debug.Log("turnmanmager started");
        AssignTurnToPlayer();
        StartTurnLoop();
    }

    private void AssignTurnToPlayer()
    {
        Debug.Log("AssignTurnToPlayer is called");
        var players = PlayerManager.Instance.players;
        if (players.Count == 0) return;

        foreach (var player in players)
        {
            player.HasTurn.Value = false;
        }

        int randomIndex = Random.Range(0, players.Count);
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
            currentPlayer = PlayerManager.Instance.players.Find(player => player.HasTurn.Value);
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
        Player selectedPlayer = PlayerManager.Instance.players.Find(player => player.OwnerClientId == playerId);
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

    private void HandlePlayerTurn(Player currentPlayer)
    {
        //MakeGuess(currentPlayer);
        Debug.Log("HandlePlayerTurn is called");
        Debug.Log($"currentPlazer is: {currentPlayer}");
        Debug.Log($"Selected Card: {selectedCard.cardName}");

        if (selectedCard != null && selectedPlayer != null)
        {
            AskForCard(selectedCard, selectedPlayer);
            Debug.Log("askforcard is called");
            // Reset selectedCard and selectedPlayer to null
            selectedCard = null;
            selectedPlayer = null;
        }
        else
        {
            Debug.Log($"handle player method waits for selectedCard: {selectedCard} and/or selectedPlayer {selectedPlayer}");
        }
    }

    private void AskForCard(Card selectedCard, Player selectedPlayer)
    {
        Debug.Log("askforcard is running");
        if (selectedPlayer.HandCards.Contains(selectedCard))
        {
            Debug.Log("AskForCar guess is correct");
            TransferCard(selectedCard, currentPlayer);
            Debug.Log("TransferCard is correct");
            CheckForQuartets(); 
            selectedCard = null;
            selectedPlayer = null;
            Debug.Log($"Selected Card: {selectedCard.cardName}");  
            //HandlePlayerTurn(currentPlayer);*/
            
            // If the guess is correct and the player's hand isn't empty, allow another guess.
            if (!IsPlayerHandEmpty(currentPlayer))
            {
                // Allow the player to make another guess without drawing a card or ending the turn.
                //selectedCard = null;
                //selectedPlayer = null;
                HandlePlayerTurn(currentPlayer);
            }
            else if (DeckManager.Instance.CurrentDeck != null && DeckManager.Instance.CurrentDeck.DeckCards.Count > 0)
            {
                // If the player's hand is empty but the deck isn't, draw a card from the deck.
                DrawCardFromDeck();
                // After drawing a card, re-evaluate the hand.
                if (!IsPlayerHandEmpty(currentPlayer))
                {
                    // Allow the player to make another guess.
                    selectedCard = null;
                    selectedPlayer = null;
                    HandlePlayerTurn(currentPlayer);
                }
            }
            // No need to check for the deck being empty here, as we've alrug.Loady handled that case.
        }
        else
        {
            // If the guess is wrong, draw a card from the deck and end the turn.
            Debug.Log("ask for card, player doesn't have card");
            DisplayMessage($"{selectedPlayer.playerName} does not have {selectedCard.cardName}.");
            DrawCardFromDeck();
            Debug.Log("ask for card, call end turn");
            EndTurn(); // This is the correct place to call EndTurn for an incorrect guess.
        }
    }

    private void TransferCard(Card selectedCard, Player curPlayer)
    {
        Debug.Log("TransferCard is correct");    
        selectedPlayer.RemoveCardFromHand(selectedCard);
        currentPlayer.AddCardToHand(selectedCard);
    }

    private bool IsPlayerHandEmpty(Player currentPlayer)
    {
        return currentPlayer.IsHandEmpty();
    }

    private void EndTurn()
    {
        NextCurrentPlayer();
        Debug.Log("end turn is running");
    }

    /*private void NextCurrentPlayer()
    {
        int currentIndex = PlayerManager.Instance.players.IndexOf(currentPlayer);
        int nextIndex = (currentIndex + 1) % PlayerManager.Instance.players.Count;

        int skippedPlayers = 0;

        Debug.Log("next current player is called");
        while (skippedPlayers < PlayerManager.Instance.players.Count)
        {
            Player nextPlayer = PlayerManager.Instance.players[nextIndex];

            if (!nextPlayer.IsHandEmpty())
            {
                currentPlayer.HasTurn.Value = false;
                nextPlayer.HasTurn.Value = true;
                currentPlayer = nextPlayer;
                return; // Found a non-empty-handed player, exit the loop
            }
            Debug.Log($"next current player: {nextPlayer}");
            // If the next player's hand is empty, skip them and move to the next one.
            nextIndex = (nextIndex + 1) % PlayerManager.Instance.players.Count;
            skippedPlayers++;
        }

        // If all players have empty hands, you can handle this case or end the game.
        // For example, you can call a function to end the game.
        // HandleAllEmptyHands();
    }*/

    private void NextCurrentPlayer()
    {
        Debug.Log("next current player is called");
        var players = PlayerManager.Instance.players;
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

    public void DrawCardFromDeck()
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
    }

    private void CheckGameEnd()
    {
        /*bool allHandsEmpty = true;
        foreach (Player player in PlayerManager.Players)
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
        }*/
    }

    private void GameEnd()
    {
        Debug.Log($"{Time.time}: Game Ended");
        // Call the method to display end game results
        //gameFlowManager.DisplayEndGameResults();
    }
}
