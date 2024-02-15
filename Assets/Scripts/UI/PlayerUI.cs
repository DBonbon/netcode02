    using Unity.Collections;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class PlayerUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private Image playerImage;
        [SerializeField] private TextMeshProUGUI scoreText; // Add UI element for score
        [SerializeField] private GameObject hasTurnIndicator; // Add UI element for turn indicator

        private const string DefaultImagePath = "Images/character_01";

        public void InitializePlayerUI(string playerName, string imagePath)
        {
            if (playerNameText != null)
            {
                playerNameText.text = playerName;
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

        public void UpdateScoreUI(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = "Score: " + score.ToString();
            }
        }

        public void UpdateHasTurnUI(bool hasTurn)
        {
            if (hasTurnIndicator != null)
            {
                hasTurnIndicator.SetActive(hasTurn);
            }
        }
    }