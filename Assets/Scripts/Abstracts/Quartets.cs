using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Quartets : NetworkBehaviour
{
    [SerializeField] public Transform cardsContainer; // Assign in the inspector

    public List<Card> QuartetsCards { get; set; } = new List<Card>();

    private QuartetsUI quartetsUI;

    public override void OnNetworkSpawn()
    { 
        base.OnNetworkSpawn();
    }

    public void SetQuartetsUI(QuartetsUI newQuartetsUI)
    {
        quartetsUI = newQuartetsUI;
    }

    public void AddCardToQuartets(Card card)
    {
        if (card != null && IsServer)
        {
            QuartetsCards.Add(card);
            Debug.Log($"Card {card.cardName} added to QuartetsCards list.");

            // Immediately update the list of card IDs and UI
            UpdateQuartetsUI();
        }
        else
        {
            Debug.LogError("Attempted to add a null card to Quartets.");
        }
    }

    private void UpdateQuartetsUI()
    {
        if (IsServer)
        {
            List<int> cardIDs = QuartetsCards.Select(card => card.cardId.Value).ToList();
            if (quartetsUI != null)
            {
                quartetsUI.UpdateQuartetsUIWithIDs(cardIDs);
            }
            // Synchronize this list with clients
            UpdateQuartetsUIOnClients_ClientRpc(cardIDs.ToArray());
        }
    }

    [ClientRpc]
    private void UpdateQuartetsUIOnClients_ClientRpc(int[] cardIDs)
    {
        if (quartetsUI != null)
        {
            quartetsUI.UpdateQuartetsUIWithIDs(new List<int>(cardIDs));
        }
    }
}
