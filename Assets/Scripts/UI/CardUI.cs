using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CardUI : MonoBehaviour
{
    public string CardName;
    public int cardId;

    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI suitText;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private Image iconImage;
    [SerializeField] private List<TextMeshProUGUI> siblingTexts;
    [SerializeField] private GameObject cardFront;
    [SerializeField] private GameObject cardBack;

    private Sprite[] cardIcons;

    private void Awake()
    {
        cardIcons = Resources.LoadAll<Sprite>("Images/character_01"); // Adjust the path as needed
    }
    private void Start()
    {
        SetFaceUp(true); // Optional: start all cards face down
    }


   public void UpdateCardUIWithCardData(CardData cardData)
    {
        this.cardId = cardData.cardId;
        CardName = cardData.cardName;

        string matchingSiblingName = cardData.cardName;
        UpdateUI(cardData.cardName, cardData.suit, cardData.hint, cardData.siblings, matchingSiblingName, cardData.icon);
    }

    public void SetFaceUp(bool faceUp)
    {
        if (cardFront != null && cardBack != null)
        {
            cardFront.SetActive(faceUp);
            cardBack.SetActive(!faceUp);
        }
    }

    public void UpdateUI(string cardName, string suit, string hint, List<string> siblings, string matchingSiblingName, string iconFileName)
    {
        cardNameText.text = cardName;
        suitText.text = suit;
        hintText.text = hint;

        // Load the sprite dynamically
        if (iconImage != null && !string.IsNullOrEmpty(iconFileName))
        {
            Sprite loadedIcon = Resources.Load<Sprite>("Icons/" + System.IO.Path.GetFileNameWithoutExtension(iconFileName));
            if (loadedIcon != null)
            {
                iconImage.sprite = loadedIcon;
            }
            else
            {
                Debug.LogWarning($"Missing icon: {iconFileName}, using fallback.");
                iconImage.sprite = null; // or use a default sprite
            }
        }

        for (int i = 0; i < siblingTexts.Count; i++)
        {
            if (i < siblings.Count)
            {
                siblingTexts[i].gameObject.SetActive(true);
                siblingTexts[i].text = siblings[i];
                siblingTexts[i].color = siblings[i] == matchingSiblingName ? Color.red : Color.white;
            }
            else
            {
                siblingTexts[i].gameObject.SetActive(false);
            }
        }
    }


    public void ResetUI()
    {
        cardNameText.text = "";
        suitText.text = "";
        hintText.text = "";
        iconImage.sprite = null;
        foreach (var siblingText in siblingTexts)
        {
            siblingText.gameObject.SetActive(false);
            siblingText.text = "";
            siblingText.color = Color.white;
        }
    }
}
