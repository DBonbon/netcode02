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
    
    public void SetDeckUI(DeckUI newDeckUI)
    {
        deckUI = newDeckUI;
    }


    public void AddCardToDeck(GameObject cardGameObject)
    {
        if (cardGameObject != null)
        {
            var cardComponent = cardGameObject.GetComponent<Card>();
            if (IsServer && cardComponent != null)
            {
                DeckCards.Add(cardComponent);
                //Debug.Log($"[Deck] ADD → Card {cardComponent.cardId.Value} | {cardComponent.cardName.Value} added to DeckCards. Total now: {DeckCards.Count}");

                List<int> cardIDs = new List<int>();
                foreach (var card in DeckCards)
                {
                    cardIDs.Add(card.cardId.Value);
                }

                if (deckUI != null)
                {
                    deckUI.UpdateDeckUIWithIDs(cardIDs);
                }
            }
            else
            {
                Debug.LogError($"[Deck] ERROR: The GameObject deck does not have a Card component.");
            }
        }
        else
        {
            Debug.LogError("[Deck] ERROR: cardGameObject is null.");
        }
    }

    public Card RemoveCardFromDeck()
    {
        if (DeckCards.Count > 0)
        {
            Card cardToGive = DeckCards[0];
            DeckCards.RemoveAt(0);
            Debug.Log($"[Deck] REMOVE → Card {cardToGive.cardId.Value} | {cardToGive.cardName.Value} removed from DeckCards. Total now: {DeckCards.Count}");

            List<int> cardIDs = DeckCards.Select(card => card.cardId.Value).ToList();

            deckUI.UpdateDeckUIWithIDs(cardIDs);
            UpdateDeckUIOnClients_ClientRpc(cardIDs.ToArray());

            return cardToGive;
        }
        Debug.LogWarning("[Deck] WARNING: Tried to RemoveCardFromDeck but DeckCards was empty.");
        return null;
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