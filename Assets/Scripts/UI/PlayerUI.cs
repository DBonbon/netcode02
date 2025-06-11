using Unity.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class PlayerUI : MonoBehaviour
{
    // Personal UI Elements
    #region Personal
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Image playerImage;
    [SerializeField] private TextMeshProUGUI scoreText;
    #endregion

    // Turn UI Elements
    #region Turn
    [SerializeField] private TMP_Dropdown cardsDropdown; 
    [SerializeField] private TMP_Dropdown playersDropdown;
    [SerializeField] private GameObject hasTurnIndicator;
    [SerializeField] private Button guessButton;
    #endregion

    // Hand UI Elements
    #region Hand
    [SerializeField] private Transform cardDisplayTransform;
    #endregion

    //lists to retreive cards value in the guessbuttonclickhandler
    private List<Card> CardsPlayerCanAsk;
    private List<Player1> PlayerToAsk;

    private List<int> playerIDs = new List<int>();
    private List<int> cardIDs = new List<int>();

    private const string DefaultImagePath = "Images/character_01";
    private Player1 player;

    // In PlayerUI
    private List<ulong> playerIdsInDropdown = new List<ulong>();


    public Player1 currentPlayer;

    void Awake()
    {
        // Add an onClick handler to the guessButton
        guessButton.onClick.AddListener(OnEventGuessClick);
        player = GetComponent<Player1>();
    }
    
    public void InitializePlayerUI(string playerName, string imagePath)
    {
        if (playerNameText != null)
        {
            playerNameText.text = playerName;
            Debug.Log("UpdatePlayerDbAttributes_ClientRpc was called");
            playersDropdown.gameObject.SetActive(false);
            cardsDropdown.gameObject.SetActive(false);
            guessButton.gameObject.SetActive(false);
        }

        if (!string.IsNullOrEmpty(imagePath))
        {
            var imageSprite = Resources.Load<Sprite>(imagePath);
            if (playerImage != null && imageSprite != null)
            {
                playerImage.sprite = imageSprite;
            }
        }
    }

    // New method to update UI based on card IDs
    public void UpdatePlayerHandUIWithIDs(List<int> cardIDs)
    {
        // Ensure CardUIPool is ready (safe for client joining late)
        CardManager.Instance.EnsureCardUIPoolInitializedIfNeeded();

        // Hide old cards
        foreach (Transform child in cardDisplayTransform)
        {
            child.gameObject.SetActive(false);
        }

        Debug.Log("playerui UpdatePlayerHandUIWithIDs is called");

        // Show new cards
        foreach (int cardID in cardIDs)
        {
            Debug.Log("playerui UpdatePlayerHandUIWithIDs is called in the loop");
            CardUI cardUI = CardManager.Instance.FetchCardUIById(cardID);
            if (cardUI != null)
            {
                cardUI.gameObject.SetActive(true);
                cardUI.transform.SetParent(cardDisplayTransform, false);
            }
            else
            {
                Debug.LogWarning($"No CardUI found for card ID: {cardID}");
            }
        }
    }

    public void UpdateScoreUI(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score.ToString();
        }
    }

    public void UpdateTurnUI(bool hasTurn)
    {
        // This ensures that the dropdown is only active for the player with the turn.
        if (hasTurnIndicator != null)
        {
            hasTurnIndicator.SetActive(hasTurn);
        }
        ActivateTurnUI(hasTurn); // Make sure this method is responsible for showing/hiding relevant UI elements.
    }

    public void ActivateTurnUI(bool hasTurn)
    {
        // Assuming 'playersDropdown' is part of a larger turn UI GameObject
        playersDropdown.gameObject.SetActive(hasTurn); // Activate or deactivate the turn UI
        cardsDropdown.gameObject.SetActive(hasTurn);
        guessButton.gameObject.SetActive(hasTurn); 
        Debug.Log($"ActivateTurnUI running {hasTurn}");
    }

    public void UpdatePlayersDropdown(ulong[] playerIDs, string[] playerNames)
    {
        Debug.Log($"Updating players dropdown. IDs count: {playerIDs.Length}, Names count: {playerNames.Length}");
        playersDropdown.ClearOptions();
        List<string> playerNamesList = new List<string>(playerNames);

        for(int i = 0; i < playerIDs.Length; i++)
        {
            playerIdsInDropdown.Add(playerIDs[i]);
        }
            
        playersDropdown.AddOptions(playerNamesList);
        // This log helps you verify that the dropdown options have been successfully updated.
        Debug.Log("Players Dropdown updated with names: " + string.Join(", ", playerNames));  
    }

    public void UpdateCardsDropdownWithIDs(int[] cardIDs)
    {
        cardsDropdown.ClearOptions();
        List<string> cardNames = new List<string>();
        this.cardIDs.Clear(); // Ensure the list is cleared before repopulating

        foreach (int id in cardIDs)
        {
            string cardName = CardManager.Instance.GetCardNameById(id);
            cardNames.Add(cardName);
            this.cardIDs.Add(id); // Repopulate with current card IDs
        }

        cardsDropdown.AddOptions(cardNames);
    }


    //event
    
    public void OnEventGuessClick()
    {
        // Get the selected index from the dropdowns
        int selectedPlayerIndex = playersDropdown.value;
        int selectedCardIndex = cardsDropdown.value;
        if (selectedCardIndex < 0 || selectedCardIndex >= this.cardIDs.Count)
        {
            Debug.LogError("Invalid card dropdown selection.");
            return;
        }

        if (selectedPlayerIndex >= 0 && selectedPlayerIndex < playerIdsInDropdown.Count)
        {
            ulong selectedPlayerId = playerIdsInDropdown[selectedPlayerIndex];
            Debug.Log($"Selected Player Client ID: {selectedPlayerId}");

            //int selectedCardId = cardIDs[selectedCardIndex];
            int selectedCardId = this.cardIDs[selectedCardIndex];
            // Wrap selectedCardId into a NetworkVariableIntWrapper
            NetworkVariableIntWrapper networkCardId = new NetworkVariableIntWrapper(selectedCardId);

            // Call the ServerRpc from the client-side
            player.OnEventGuessClickServerRpc(selectedPlayerId, selectedCardId);
            Debug.Log($"Selected Card ID: {selectedCardId} and Selected Player ID: {selectedPlayerId}");
        }
        else
        {
            Debug.LogError("Invalid player selection.");
        }

        // Re-enable the button at the end of the method
        guessButton.interactable = true;
    }

}