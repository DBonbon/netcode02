using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Deck : NetworkBehaviour
{
    [SerializeField] public Transform cardsContainer; // Assign in the inspector

    public List<Card> DeckCards { get; set; } = new List<Card>();

    private DeckUI deckUI;

    public override void OnNetworkSpawn()
    { 
        base.OnNetworkSpawn();
        deckUI = GetComponent<DeckUI>(); // Get the DeckUI component
    }

    public void AddCardToDeck(GameObject cardGameObject)
    {
        if (cardGameObject != null)
        {
            var cardComponent = cardGameObject.GetComponent<Card>();
            if (IsServer && cardComponent != null)
            {
                DeckCards.Add(cardComponent);
                //Debug.Log($"Card {cardComponent.name} added to player to deckCards list.");

                // Prepare a list of card IDs to send to the UI
                List<int> cardIDs = new List<int>();
                foreach (var card in DeckCards)
                {
                    cardIDs.Add(card.cardId.Value);
                }

                
                // Update the UI with the list of card IDs
                if (deckUI != null)
                {
                    //SendCardIDsToClient();
                    deckUI.UpdateDeckUIWithIDs(cardIDs);
                }
            }
            else
            {
                Debug.LogError($"The GameObject deck does not have a Card component.");
            }
        }
        else
        {
            Debug.LogError("cardGameObject is null.");
        }
    }

    public Card RemoveCardFromDeck()
    {
        if (DeckCards.Count > 0)
        {
            Card cardToGive = DeckCards[0]; // Take the first card
            DeckCards.RemoveAt(0); // Remove it from the deck

            // Immediately update the list of card IDs
            List<int> cardIDs = DeckCards.Select(card => card.cardId.Value).ToList();
            
            // Update the UI with the list of card IDs
            deckUI.UpdateDeckUIWithIDs(cardIDs);

            // Ensure UI is updated on all clients
            UpdateDeckUIOnClients_ClientRpc(cardIDs.ToArray());

            return cardToGive; // Return the removed card
        }
        return null; // Return null if no cards are left
    }

    [ClientRpc]
    private void UpdateDeckUIOnClients_ClientRpc(int[] cardIDs)
    {
        if (deckUI != null)
        {
            deckUI.UpdateDeckUIWithIDs(new List<int>(cardIDs)); // Ensure UI update call on client
        }
    }
    
    /*
    private void UpdateDeckUIOnAllClients()
    {
        int[] cardIDs = DeckCards.Select(card => card.GetComponent<Card>().cardId.Value).ToArray();
        deckUI.UpdateDeckUIWithIDs(cardIDs.ToList()); // Continue to use List on the server side for convenience

        UpdateDeckUIOnClients_ClientRpc(cardIDs); // Convert to array for RPC call
    }

    [ClientRpc]
    private void UpdateDeckUIOnClients_ClientRpc(int[] cardIDs)
    {
        if (deckUI != null)
        {
            deckUI.UpdateDeckUIWithIDs(new List<int>(cardIDs)); // Convert back to List for the method call
        }
    }*/
}