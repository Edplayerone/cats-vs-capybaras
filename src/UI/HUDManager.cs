using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CatsVsCapybaras
{
    /// <summary>
    /// Manages all in-game UI: turn timer, health bars, weapon selection,
    /// wind indicator, active character display, and game-over screen.
    ///
    /// All UI references are optional — missing elements are silently skipped
    /// so you can build the UI incrementally.
    ///
    /// Subscribes to game events automatically via Initialize().
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("Timer")]
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Active Character")]
        [SerializeField] private TextMeshProUGUI activeCharacterText;
        [SerializeField] private TextMeshProUGUI activeTeamText;

        [Header("Phase")]
        [SerializeField] private TextMeshProUGUI phaseText;

        [Header("Wind")]
        [SerializeField] private TextMeshProUGUI windText;

        [Header("Weapon Selection")]
        [SerializeField] private RectTransform weaponPanel;
        [SerializeField] private GameObject weaponButtonPrefab;

        [Header("Game Over")]
        [SerializeField] private CanvasGroup gameOverPanel;
        [SerializeField] private TextMeshProUGUI gameOverText;
        [SerializeField] private Button restartButton;

        [Header("Damage Popups")]
        [SerializeField] private GameObject damagePopupPrefab;
        [SerializeField] private Canvas worldCanvas;

        [Header("Health Bars")]
        [Tooltip("Assign one Image per character, ordered by team then character index")]
        [SerializeField] private Image[] healthBarFills;
        [SerializeField] private TextMeshProUGUI[] healthTexts;

        private GameManager gameManager;
        private TurnManager turnManager;
        private TeamManager teamManager;
        private List<Button> weaponButtons = new List<Button>();
        private int currentSelectedWeapon;

        /// <summary>
        /// Called by GameManager after all systems are ready.
        /// Subscribes to events and builds dynamic UI.
        /// </summary>
        public void Initialize(GameManager gm, TurnManager tm, TeamManager tmgr)
        {
            gameManager = gm;
            turnManager = tm;
            teamManager = tmgr;

            // Subscribe to events
            turnManager.OnTimerUpdated += UpdateTimer;
            turnManager.OnTurnStarted += HandleTurnStarted;
            turnManager.OnPhaseChanged += HandlePhaseChanged;
            turnManager.OnSelectedWeaponChanged += HandleWeaponChanged;
            gameManager.OnGameEnded += HandleGameEnded;
            gameManager.OnGameStateChanged += HandleStateChanged;

            // Subscribe to character damage for health bar updates
            for (int t = 0; t < teamManager.TeamCount; t++)
            {
                int count = teamManager.GetCharacterCount(t);
                for (int c = 0; c < count; c++)
                {
                    var ch = teamManager.GetCharacter(t, c);
                    if (ch != null)
                    {
                        ch.OnDamaged += HandleCharacterDamaged;
                        ch.OnHealed += HandleCharacterHealed;
                    }
                }
            }

            // Build weapon buttons
            BuildWeaponButtons();

            // Hide game over panel
            if (gameOverPanel != null)
            {
                gameOverPanel.alpha = 0f;
                gameOverPanel.blocksRaycasts = false;
                gameOverPanel.interactable = false;
            }

            if (restartButton != null)
                restartButton.onClick.AddListener(() => gameManager.RestartGame());

            // Initial health bar update
            RefreshAllHealthBars();
        }

        private void OnDestroy()
        {
            if (turnManager != null)
            {
                turnManager.OnTimerUpdated -= UpdateTimer;
                turnManager.OnTurnStarted -= HandleTurnStarted;
                turnManager.OnPhaseChanged -= HandlePhaseChanged;
                turnManager.OnSelectedWeaponChanged -= HandleWeaponChanged;
            }
            if (gameManager != null)
            {
                gameManager.OnGameEnded -= HandleGameEnded;
                gameManager.OnGameStateChanged -= HandleStateChanged;
            }
        }

        // ── Timer ──────────────────────────────────────────────────

        private void UpdateTimer(float seconds)
        {
            if (timerText != null)
                timerText.text = Mathf.CeilToInt(Mathf.Max(0f, seconds)).ToString();
        }

        // ── Turn / Phase ───────────────────────────────────────────

        private void HandleTurnStarted(int teamIndex, CharacterController2D character)
        {
            if (activeCharacterText != null)
                activeCharacterText.text = character.CharacterName;

            if (activeTeamText != null)
                activeTeamText.text = teamManager.GetTeamName(teamIndex);

            if (windText != null)
            {
                float w = gameManager.Wind;
                string dir = w >= 0 ? "\u2192" : "\u2190";
                windText.text = $"{dir} {Mathf.Abs(w):F1}";
            }

            HighlightSelectedWeapon(turnManager.SelectedWeaponIndex);
        }

        private void HandlePhaseChanged(TurnManager.TurnPhase phase)
        {
            if (phaseText != null)
                phaseText.text = phase.ToString();

            // Show/hide weapon panel during action phase
            bool showWeapons = phase == TurnManager.TurnPhase.Action;
            if (weaponPanel != null)
                weaponPanel.gameObject.SetActive(showWeapons);
        }

        // ── Weapons ────────────────────────────────────────────────

        private void BuildWeaponButtons()
        {
            if (weaponPanel == null || weaponButtonPrefab == null || gameManager.Weapons == null) return;

            foreach (var btn in weaponButtons)
            {
                if (btn != null) Destroy(btn.gameObject);
            }
            weaponButtons.Clear();

            for (int i = 0; i < gameManager.Weapons.Length; i++)
            {
                var weapon = gameManager.Weapons[i];
                var obj = Instantiate(weaponButtonPrefab, weaponPanel);
                var btn = obj.GetComponent<Button>();

                int capturedIndex = i;
                if (btn != null)
                {
                    btn.onClick.AddListener(() =>
                    {
                        var ih = FindAnyObjectByType<PlayerInputHandler>();
                        if (ih != null) ih.SelectWeapon(capturedIndex);
                    });
                    weaponButtons.Add(btn);
                }

                // Set label text if the prefab has a TMP text child
                var label = obj.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = weapon.weaponName;

                // Set icon if the prefab has an Image child named "Icon"
                var iconTransform = obj.transform.Find("Icon");
                if (iconTransform != null && weapon.icon != null)
                {
                    var img = iconTransform.GetComponent<Image>();
                    if (img != null) img.sprite = weapon.icon;
                }
            }
        }

        private void HandleWeaponChanged(int index)
        {
            currentSelectedWeapon = index;
            HighlightSelectedWeapon(index);
        }

        private void HighlightSelectedWeapon(int index)
        {
            for (int i = 0; i < weaponButtons.Count; i++)
            {
                if (weaponButtons[i] == null) continue;
                var colors = weaponButtons[i].colors;
                colors.normalColor = i == index ? new Color(1f, 0.85f, 0f) : Color.white;
                weaponButtons[i].colors = colors;
            }
        }

        // ── Health Bars ────────────────────────────────────────────

        private void HandleCharacterDamaged(CharacterController2D ch, float amount)
        {
            RefreshAllHealthBars();
            SpawnDamagePopup(ch.transform.position, Mathf.RoundToInt(amount));
        }

        private void HandleCharacterHealed(CharacterController2D ch)
        {
            RefreshAllHealthBars();
        }

        private void RefreshAllHealthBars()
        {
            int slot = 0;
            for (int t = 0; t < teamManager.TeamCount; t++)
            {
                int count = teamManager.GetCharacterCount(t);
                for (int c = 0; c < count; c++)
                {
                    var ch = teamManager.GetCharacter(t, c);
                    if (ch == null) continue;

                    if (healthBarFills != null && slot < healthBarFills.Length && healthBarFills[slot] != null)
                        healthBarFills[slot].fillAmount = ch.HealthNormalized;

                    if (healthTexts != null && slot < healthTexts.Length && healthTexts[slot] != null)
                        healthTexts[slot].text = Mathf.RoundToInt(ch.CurrentHealth).ToString();

                    slot++;
                }
            }
        }

        // ── Damage Popups ──────────────────────────────────────────

        private void SpawnDamagePopup(Vector3 worldPos, int amount)
        {
            if (damagePopupPrefab == null || worldCanvas == null) return;

            var obj = Instantiate(damagePopupPrefab, worldCanvas.transform);
            var rect = obj.GetComponent<RectTransform>();

            if (rect != null)
            {
                Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    worldCanvas.GetComponent<RectTransform>(), screenPos,
                    worldCanvas.worldCamera, out Vector2 localPos);
                rect.anchoredPosition = localPos;
            }

            var text = obj.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = $"-{amount}";

            StartCoroutine(AnimatePopup(obj, 1.2f));
        }

        private IEnumerator AnimatePopup(GameObject obj, float duration)
        {
            var rect = obj.GetComponent<RectTransform>();
            var cg = obj.GetComponent<CanvasGroup>();
            if (cg == null) cg = obj.AddComponent<CanvasGroup>();

            Vector2 start = rect != null ? rect.anchoredPosition : Vector2.zero;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (rect != null) rect.anchoredPosition = start + Vector2.up * (80f * t);
                cg.alpha = Mathf.Lerp(1f, 0f, t);

                yield return null;
            }

            Destroy(obj);
        }

        // ── Game Over ──────────────────────────────────────────────

        private void HandleGameEnded(int winningTeamIndex)
        {
            if (gameOverPanel == null) return;

            string winnerName = winningTeamIndex >= 0
                ? teamManager.GetTeamName(winningTeamIndex)
                : "Nobody";

            if (gameOverText != null)
                gameOverText.text = $"{winnerName} Wins!";

            StartCoroutine(FadeCanvasGroup(gameOverPanel, 0f, 1f, 0.5f));
            gameOverPanel.blocksRaycasts = true;
            gameOverPanel.interactable = true;
        }

        private void HandleStateChanged(GameManager.GameState state)
        {
            if (state == GameManager.GameState.Playing && gameOverPanel != null)
            {
                gameOverPanel.alpha = 0f;
                gameOverPanel.blocksRaycasts = false;
                gameOverPanel.interactable = false;
            }
        }

        /// <summary>
        /// Public entry point for external code to show the game-over screen.
        /// </summary>
        public void ShowGameOverScreen()
        {
            HandleGameEnded(teamManager.CheckWinCondition());
        }

        // ── Utility ────────────────────────────────────────────────

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            cg.alpha = to;
        }
    }
}
