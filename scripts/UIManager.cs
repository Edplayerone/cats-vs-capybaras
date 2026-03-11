using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace CatsVsCapybaras
{
    /// <summary>
    /// Manages all UI elements for Cats vs Capybaras including turn timer, health bars,
    /// weapon selection, wind indicator, phase display, and damage popups.
    /// Subscribes to game events for real-time UI updates.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        #region UI References

        [SerializeField]
        [Tooltip("TextMeshPro component displaying remaining seconds in current turn")]
        private TextMeshProUGUI timerText;

        [SerializeField]
        [Tooltip("TextMeshPro component displaying current active character name")]
        private TextMeshProUGUI currentCharacterNameText;

        [SerializeField]
        [Tooltip("TextMeshPro component displaying current team name")]
        private TextMeshProUGUI currentTeamNameText;

        [SerializeField]
        [Tooltip("TextMeshPro component displaying current game phase")]
        private TextMeshProUGUI phaseIndicatorText;

        [SerializeField]
        [Tooltip("Container panel for weapon selection buttons")]
        private CanvasGroup weaponPanelCanvasGroup;

        [SerializeField]
        [Tooltip("Horizontal layout group containing weapon buttons")]
        private HorizontalLayoutGroup weaponButtonLayout;

        [SerializeField]
        [Tooltip("Prefab for weapon selection buttons (Button with Text child)")]
        private GameObject weaponButtonPrefab;

        [SerializeField]
        [Tooltip("Arrow image for wind direction indicator")]
        private Image windArrowImage;

        [SerializeField]
        [Tooltip("TextMeshPro for wind strength/speed display")]
        private TextMeshProUGUI windStrengthText;

        [SerializeField]
        [Tooltip("Panel shown when game ends")]
        private CanvasGroup gameOverPanelCanvasGroup;

        [SerializeField]
        [Tooltip("TextMeshPro displaying winner in game over panel")]
        private TextMeshProUGUI gameOverWinnerText;

        [SerializeField]
        [Tooltip("TextMeshPro displaying round score")]
        private TextMeshProUGUI roundScoreText;

        [SerializeField]
        [Tooltip("LineRenderer for displaying aim trajectory")]
        private LineRenderer aimIndicatorLineRenderer;

        [SerializeField]
        [Tooltip("Prefab for damage popup floating text")]
        private GameObject damagePopupPrefab;

        [SerializeField]
        [Tooltip("Canvas for worldspace or screenspace damage popups")]
        private Canvas uiCanvas;

        #endregion

        #region Health Bar References

        [SerializeField]
        [Tooltip("Array of health bar images (one per character, ordered by team/character)")]
        private Image[] healthBarImages = new Image[4];

        [SerializeField]
        [Tooltip("Array of health text displays (one per character)")]
        private TextMeshProUGUI[] healthTexts = new TextMeshProUGUI[4];

        #endregion

        #region Configuration

        [SerializeField]
        [Tooltip("Fade-in/out duration for panel animations (seconds)")]
        private float panelAnimDuration = 0.3f;

        [SerializeField]
        [Tooltip("Duration for damage popup floating animation (seconds)")]
        private float damagePopupDuration = 1.5f;

        [SerializeField]
        [Tooltip("Max length of aim indicator line in world units")]
        private float aimIndicatorLength = 10f;

        #endregion

        #region Private Fields

        private Dictionary<int, Button> weaponButtons = new Dictionary<int, Button>();
        private Coroutine panelFadeCoroutine;
        private Coroutine gameOverFadeCoroutine;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            InitializeUI();
            HideGameOverPanel();
            HideWeaponPanel();
            aimIndicatorLineRenderer.enabled = false;
        }

        private void OnEnable()
        {
            // Subscribe to game events here
            // Example: GameManager.OnTurnChanged += UpdatePhaseIndicator;
        }

        private void OnDisable()
        {
            // Unsubscribe from game events
            // Example: GameManager.OnTurnChanged -= UpdatePhaseIndicator;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes UI elements (health bars, weapon buttons, etc.).
        /// </summary>
        private void InitializeUI()
        {
            // Hide initial panels
            if (weaponPanelCanvasGroup != null)
                weaponPanelCanvasGroup.alpha = 0f;

            if (gameOverPanelCanvasGroup != null)
                gameOverPanelCanvasGroup.alpha = 0f;

            // Initialize health bar tracking
            for (int i = 0; i < healthBarImages.Length; i++)
            {
                if (healthBarImages[i] != null)
                {
                    healthBarImages[i].fillAmount = 1f;
                }
            }

            // Initialize weapon buttons (call CreateWeaponButtons from game setup)
        }

        /// <summary>
        /// Creates weapon selection buttons from weapon data.
        /// Call this during game initialization.
        /// </summary>
        public void CreateWeaponButtons(int weaponCount, System.Action<int> onWeaponSelected)
        {
            if (weaponButtonLayout == null)
                return;

            // Clear existing buttons
            foreach (var kvp in weaponButtons)
            {
                Destroy(kvp.Value.gameObject);
            }
            weaponButtons.Clear();

            // Create new buttons
            for (int i = 0; i < weaponCount; i++)
            {
                GameObject buttonObj = Instantiate(weaponButtonPrefab, weaponButtonLayout.transform);
                Button button = buttonObj.GetComponent<Button>();
                int weaponIndex = i;

                button.onClick.AddListener(() => onWeaponSelected?.Invoke(weaponIndex));
                weaponButtons[i] = button;

                // Set button label (integrate with WeaponData if available)
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = $"Weapon {i + 1}";
                }
            }
        }

        #endregion

        #region Timer

        /// <summary>
        /// Updates the turn timer display with seconds remaining.
        /// </summary>
        public void UpdateTimer(float secondsRemaining)
        {
            if (timerText != null)
            {
                timerText.text = Mathf.Max(0, secondsRemaining).ToString("F1");
            }
        }

        #endregion

        #region Health Bars

        /// <summary>
        /// Updates health bar for a specific character.
        /// </summary>
        /// <param name="characterIndex">Index of character (0-3 for 2v2)</param>
        /// <param name="normalizedHealth">Health as 0-1 normalized value</param>
        public void UpdateHealthBar(int characterIndex, float normalizedHealth)
        {
            if (characterIndex < 0 || characterIndex >= healthBarImages.Length)
                return;

            normalizedHealth = Mathf.Clamp01(normalizedHealth);

            if (healthBarImages[characterIndex] != null)
            {
                healthBarImages[characterIndex].fillAmount = normalizedHealth;
            }

            if (healthTexts[characterIndex] != null)
            {
                // Assuming max health is 100 for display purposes
                int displayHealth = Mathf.RoundToInt(normalizedHealth * 100f);
                healthTexts[characterIndex].text = displayHealth.ToString();
            }
        }

        #endregion

        #region Character Display

        /// <summary>
        /// Updates the active character name and team display.
        /// </summary>
        public void SetActiveCharacterDisplay(string characterName, int teamIndex)
        {
            if (currentCharacterNameText != null)
            {
                currentCharacterNameText.text = characterName;
            }

            if (currentTeamNameText != null)
            {
                string teamName = teamIndex == 0 ? "Team Cats" : "Team Capybaras";
                currentTeamNameText.text = teamName;
            }
        }

        #endregion

        #region Phase Indicator

        /// <summary>
        /// Updates the phase indicator text display.
        /// </summary>
        public void UpdatePhaseIndicator(string phaseName)
        {
            if (phaseIndicatorText != null)
            {
                phaseIndicatorText.text = phaseName;
            }
        }

        #endregion

        #region Weapon Panel

        /// <summary>
        /// Shows the weapon selection panel with fade animation.
        /// </summary>
        public void ShowWeaponPanel()
        {
            if (weaponPanelCanvasGroup == null)
                return;

            if (panelFadeCoroutine != null)
                StopCoroutine(panelFadeCoroutine);

            panelFadeCoroutine = StartCoroutine(FadeCanvasGroup(weaponPanelCanvasGroup, 0f, 1f, panelAnimDuration));
            weaponPanelCanvasGroup.blocksRaycasts = true;
            weaponPanelCanvasGroup.interactable = true;
        }

        /// <summary>
        /// Hides the weapon selection panel with fade animation.
        /// </summary>
        public void HideWeaponPanel()
        {
            if (weaponPanelCanvasGroup == null)
                return;

            if (panelFadeCoroutine != null)
                StopCoroutine(panelFadeCoroutine);

            panelFadeCoroutine = StartCoroutine(FadeCanvasGroup(weaponPanelCanvasGroup, 1f, 0f, panelAnimDuration));
            weaponPanelCanvasGroup.blocksRaycasts = false;
            weaponPanelCanvasGroup.interactable = false;
        }

        #endregion

        #region Wind Indicator

        /// <summary>
        /// Updates the wind indicator arrow and text display.
        /// </summary>
        /// <param name="windStrength">Wind magnitude (0-1 normalized)</param>
        /// <param name="windDirection">Wind direction in radians</param>
        public void UpdateWindIndicator(float windStrength, float windDirection)
        {
            if (windArrowImage != null)
            {
                // Convert radians to degrees for UI rotation
                float rotationZ = windDirection * Mathf.Rad2Deg;
                windArrowImage.rectTransform.rotation = Quaternion.Euler(0, 0, rotationZ);
            }

            if (windStrengthText != null)
            {
                windStrengthText.text = Mathf.Round(windStrength * 100f).ToString("F0");
            }
        }

        #endregion

        #region Game Over

        /// <summary>
        /// Displays the game over panel with winner information.
        /// </summary>
        public void ShowGameOver(string winnerTeamName)
        {
            if (gameOverPanelCanvasGroup == null)
                return;

            if (gameOverWinnerText != null)
            {
                gameOverWinnerText.text = $"{winnerTeamName} Wins!";
            }

            if (gameOverFadeCoroutine != null)
                StopCoroutine(gameOverFadeCoroutine);

            gameOverFadeCoroutine = StartCoroutine(FadeCanvasGroup(gameOverPanelCanvasGroup, 0f, 1f, panelAnimDuration));
            gameOverPanelCanvasGroup.blocksRaycasts = true;
            gameOverPanelCanvasGroup.interactable = true;
        }

        /// <summary>
        /// Hides the game over panel.
        /// </summary>
        private void HideGameOverPanel()
        {
            if (gameOverPanelCanvasGroup == null)
                return;

            gameOverPanelCanvasGroup.alpha = 0f;
            gameOverPanelCanvasGroup.blocksRaycasts = false;
            gameOverPanelCanvasGroup.interactable = false;
        }

        #endregion

        #region Score Display

        /// <summary>
        /// Updates the round score display.
        /// </summary>
        public void UpdateRoundScore(int team1Score, int team2Score)
        {
            if (roundScoreText != null)
            {
                roundScoreText.text = $"Cats {team1Score} - {team2Score} Capybaras";
            }
        }

        #endregion

        #region Aim Indicator

        /// <summary>
        /// Updates the aim indicator line showing projected trajectory.
        /// Call this every frame during AimPhase to show live aim preview.
        /// </summary>
        public void UpdateAimIndicator(Vector3 startPos, Vector3 direction, float power)
        {
            if (aimIndicatorLineRenderer == null || power <= 0f)
            {
                aimIndicatorLineRenderer.enabled = false;
                return;
            }

            aimIndicatorLineRenderer.enabled = true;

            // Show line from character extending in aim direction
            Vector3 endPos = startPos + (direction * aimIndicatorLength * power);

            aimIndicatorLineRenderer.SetPosition(0, startPos);
            aimIndicatorLineRenderer.SetPosition(1, endPos);
        }

        /// <summary>
        /// Disables the aim indicator line.
        /// </summary>
        public void HideAimIndicator()
        {
            if (aimIndicatorLineRenderer != null)
            {
                aimIndicatorLineRenderer.enabled = false;
            }
        }

        #endregion

        #region Damage Popup

        /// <summary>
        /// Shows a floating damage number at a world position.
        /// </summary>
        public void ShowDamagePopup(Vector3 worldPos, int damageAmount)
        {
            if (damagePopupPrefab == null || uiCanvas == null)
                return;

            GameObject popupObj = Instantiate(damagePopupPrefab, uiCanvas.transform);

            // Convert world position to screen position, then to canvas position
            Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                uiCanvas.GetComponent<RectTransform>(),
                screenPos,
                uiCanvas.worldCamera,
                out Vector2 canvasPos
            );

            RectTransform popupRect = popupObj.GetComponent<RectTransform>();
            popupRect.anchoredPosition = canvasPos;

            TextMeshProUGUI popupText = popupObj.GetComponentInChildren<TextMeshProUGUI>();
            if (popupText != null)
            {
                popupText.text = damageAmount.ToString();
            }

            // Animate and destroy after duration
            StartCoroutine(AnimateDamagePopup(popupObj, damagePopupDuration));
        }

        /// <summary>
        /// Animates a damage popup (float up and fade out).
        /// </summary>
        private System.Collections.IEnumerator AnimateDamagePopup(GameObject popupObj, float duration)
        {
            RectTransform rect = popupObj.GetComponent<RectTransform>();
            TextMeshProUGUI text = popupObj.GetComponentInChildren<TextMeshProUGUI>();
            CanvasGroup canvasGroup = popupObj.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = popupObj.AddComponent<CanvasGroup>();

            Vector2 startPos = rect.anchoredPosition;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / duration;

                // Float upward
                rect.anchoredPosition = startPos + Vector2.up * (100f * normalizedTime);

                // Fade out
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, normalizedTime);

                yield return null;
            }

            Destroy(popupObj);
        }

        #endregion

        #region Utility

        /// <summary>
        /// Fades a CanvasGroup from one alpha to another.
        /// </summary>
        private System.Collections.IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
        {
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
                yield return null;
            }

            canvasGroup.alpha = endAlpha;
        }

        #endregion
    }
}
