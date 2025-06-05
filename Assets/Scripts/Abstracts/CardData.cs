using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[System.Serializable]

public class CardData
{
    public int cardId;
    public string suit;
    public string cardName;
    public string hint;
    public string level;
    public string icon;
    public List<string> siblings; // List to store sibling card names
    

    public void PopulateSiblings(List<CardData> allCards)
    {
        // Find cards with the same suit and add their names to the sibling list
        siblings = allCards
            .Where(card => card.suit == suit)
            .Select(card => card.cardName)
            .ToList();

        // Debug log to check the populated siblings
        Debug.Log($"Siblings for card {cardName}: {string.Join(", ", siblings)}");
    }

}