using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuartetsUI : MonoBehaviour
{
    [SerializeField] private Transform quartetsDisplayTransform; // UI container for quartets cards. this is a Fallback or Default option, i.e. since the transform is passed from the quartetsmanager

     // New method to set the quartetsDisplayTransform
    public void SetQuartetsDisplayTransform(Transform newDisplayTransform)
    {
        quartetsDisplayTransform = newDisplayTransform;
    }
    
    // Method to be called to update the UI representation of the quartets
    public void UpdateQuartetsUIWithIDs(List<int> cardIDs)
    {
        // Deactivate all child GameObjects to reset the state
        foreach (Transform child in quartetsDisplayTransform)
        {
            child.gameObject.SetActive(false); // Disable all cards initially
        }

        // Activate and reparent the CardUI objects based on the current quartets
        foreach (int cardID in cardIDs)
        {
            CardUI cardUI = CardManager.Instance.FetchCardUIById(cardID);
            if (cardUI != null)
            {
                cardUI.gameObject.SetActive(true);
                cardUI.transform.SetParent(quartetsDisplayTransform, false);
            }
            else
            {
                Debug.LogWarning($"No CardUI found for card ID: {cardID}");
            }
        }
    }
    
}