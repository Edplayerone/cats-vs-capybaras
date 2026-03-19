using System;
using System.Collections;
using UnityEngine;

namespace CatsVsCapybaras
{
    /// <summary>
    /// Drives the turn-based game loop. Each turn flows through four phases:
    ///
    ///   Action       → Player moves, aims, and fires (shared 35s timer)
    ///   Firing       → Projectile in flight, camera follows
    ///   Resolving    → Explosion settles, damage applied, brief pause
    ///   Transitioning→ Camera pans to next character, switch active
    ///
    /// Coordinates PlayerInputHandler → CharacterController2D → ProjectileBase → GameCamera.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        public enum TurnPhase { WaitingToStart, Action, Firing, Resolving, Transitioning }

        public event Action<TurnPhase> OnPhaseChanged;
        public event Action<int, CharacterController2D> OnTurnStarted;
        public event Action OnTurnEnded;
        public event Action<float> OnTimerUpdated;
        public event Action<int> OnSelectedWeaponChanged;

        [Header("Timing")]
        [SerializeField] private float turnDuration = 35f;
        [SerializeField] private float resolvingDelay = 1.5f;
        [SerializeField] private float transitionDuration = 1f;

        [Header("References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private TeamManager teamManager;
        [SerializeField] private GameCamera gameCamera;
        [SerializeField] private PlayerInputHandler inputHandler;

        private TurnPhase currentPhase = TurnPhase.WaitingToStart;
        private int currentTeamIndex;
        private CharacterController2D activeCharacter;
        private ProjectileBase activeProjectile;
        private float timeRemaining;
        private int selectedWeaponIndex;
        private Coroutine turnTimerCoroutine;

        public TurnPhase CurrentPhase => currentPhase;
        public int CurrentTeamIndex => currentTeamIndex;
        public CharacterController2D ActiveCharacter => activeCharacter;
        public float TimeRemaining => timeRemaining;
        public int SelectedWeaponIndex => selectedWeaponIndex;

        /// <summary>
        /// Wires up input events and initializes state.
        /// Called by GameManager before the first turn.
        /// </summary>
        public void Initialize()
        {
            if (gameManager == null) gameManager = FindAnyObjectByType<GameManager>();
            if (teamManager == null) teamManager = FindAnyObjectByType<TeamManager>();
            if (gameCamera == null) gameCamera = FindAnyObjectByType<GameCamera>();
            if (inputHandler == null) inputHandler = FindAnyObjectByType<PlayerInputHandler>();

            inputHandler.OnFireRequested += HandleFireRequested;
            inputHandler.OnMoveInput += HandleMoveInput;
            inputHandler.OnWeaponSelected += HandleWeaponSelected;
            inputHandler.OnJumpRequested += HandleJumpRequested;
        }

        private void OnDestroy()
        {
            if (inputHandler != null)
            {
                inputHandler.OnFireRequested -= HandleFireRequested;
                inputHandler.OnMoveInput -= HandleMoveInput;
                inputHandler.OnWeaponSelected -= HandleWeaponSelected;
                inputHandler.OnJumpRequested -= HandleJumpRequested;
            }
        }

        // ── Turn lifecycle ─────────────────────────────────────────

        public void StartFirstTurn(int startingTeam = 0)
        {
            currentTeamIndex = startingTeam;
            BeginTurn();
        }

        private void BeginTurn()
        {
            int charIdx = teamManager.GetNextAliveCharacterIndex(currentTeamIndex);
            if (charIdx < 0)
            {
                Debug.LogWarning("[TurnManager] No alive characters on team to start turn.");
                return;
            }

            activeCharacter = teamManager.GetCharacter(currentTeamIndex, charIdx);
            activeCharacter.ActivateForTurn();

            gameManager.RandomizeWind();

            selectedWeaponIndex = 0;
            OnSelectedWeaponChanged?.Invoke(selectedWeaponIndex);

            timeRemaining = turnDuration;

            gameCamera.FollowTarget(activeCharacter.transform);

            inputHandler.EnableInput(true, true);
            UpdateAimOrigin();

            SetPhase(TurnPhase.Action);
            OnTurnStarted?.Invoke(currentTeamIndex, activeCharacter);

            if (turnTimerCoroutine != null) StopCoroutine(turnTimerCoroutine);
            turnTimerCoroutine = StartCoroutine(TurnTimerRoutine());
        }

        private void EndTurn()
        {
            if (turnTimerCoroutine != null)
            {
                StopCoroutine(turnTimerCoroutine);
                turnTimerCoroutine = null;
            }

            if (activeCharacter != null)
            {
                activeCharacter.SetMoveInput(0f);
                activeCharacter.DeactivateAfterTurn();
            }

            inputHandler.DisableInput();
            OnTurnEnded?.Invoke();

            StartCoroutine(TransitionRoutine());
        }

        // ── Timer ──────────────────────────────────────────────────

        private IEnumerator TurnTimerRoutine()
        {
            while (timeRemaining > 0f && currentPhase == TurnPhase.Action)
            {
                timeRemaining -= Time.deltaTime;
                OnTimerUpdated?.Invoke(Mathf.Max(0f, timeRemaining));
                UpdateAimOrigin();
                yield return null;
            }

            // Timer expired without the player firing
            if (currentPhase == TurnPhase.Action)
                EndTurn();
        }

        // ── Phase management ───────────────────────────────────────

        private void SetPhase(TurnPhase phase)
        {
            if (currentPhase == phase) return;
            currentPhase = phase;
            OnPhaseChanged?.Invoke(phase);
        }

        // ── Input handlers ─────────────────────────────────────────

        private void HandleMoveInput(float horizontal)
        {
            if (currentPhase == TurnPhase.Action && activeCharacter != null)
                activeCharacter.SetMoveInput(horizontal);
        }

        private void HandleJumpRequested()
        {
            if (currentPhase == TurnPhase.Action && activeCharacter != null)
                activeCharacter.Jump();
        }

        private void HandleWeaponSelected(int index)
        {
            if (currentPhase != TurnPhase.Action) return;
            if (gameManager.Weapons == null || index < 0 || index >= gameManager.Weapons.Length) return;

            selectedWeaponIndex = index;
            OnSelectedWeaponChanged?.Invoke(selectedWeaponIndex);
        }

        private void HandleFireRequested(float angle, float normalizedPower)
        {
            if (currentPhase != TurnPhase.Action || activeCharacter == null) return;

            WeaponData weapon = gameManager.Weapons[selectedWeaponIndex];

            // Ammo check
            if (!teamManager.HasAmmo(currentTeamIndex, selectedWeaponIndex))
            {
                Debug.Log($"[TurnManager] No ammo for {weapon.weaponName}.");
                return;
            }

            // Stop timer and input
            if (turnTimerCoroutine != null)
            {
                StopCoroutine(turnTimerCoroutine);
                turnTimerCoroutine = null;
            }
            inputHandler.DisableInput();
            activeCharacter.SetMoveInput(0f);

            // Fire
            activeProjectile = activeCharacter.FireWeapon(weapon, angle, normalizedPower, gameManager.Wind);
            teamManager.ConsumeAmmo(currentTeamIndex, selectedWeaponIndex);

            if (activeProjectile != null)
            {
                activeProjectile.OnExploded += HandleProjectileExploded;
                activeProjectile.OnResolved += HandleProjectileResolved;
                gameCamera.FollowTarget(activeProjectile.transform, 0.05f);
                SetPhase(TurnPhase.Firing);
            }
            else
            {
                EndTurn();
            }
        }

        // ── Projectile callbacks ───────────────────────────────────

        private void HandleProjectileExploded(ProjectileBase proj, Vector2 position)
        {
            gameCamera.HoldPosition();
            gameCamera.TriggerShake();
        }

        private void HandleProjectileResolved(ProjectileBase proj)
        {
            if (proj != activeProjectile) return;

            activeProjectile.OnExploded -= HandleProjectileExploded;
            activeProjectile.OnResolved -= HandleProjectileResolved;
            activeProjectile = null;

            StartCoroutine(ResolveRoutine());
        }

        // ── Phase routines ─────────────────────────────────────────

        private IEnumerator ResolveRoutine()
        {
            SetPhase(TurnPhase.Resolving);
            yield return new WaitForSeconds(resolvingDelay);

            int winResult = teamManager.CheckWinCondition();
            if (winResult >= -1 && winResult != -1)
            {
                // winResult >= 0 means a team won; -2 means draw
                gameManager.EndGame(winResult);
                yield break;
            }
            if (winResult == -2)
            {
                gameManager.EndGame(-1);
                yield break;
            }

            EndTurn();
        }

        private IEnumerator TransitionRoutine()
        {
            SetPhase(TurnPhase.Transitioning);

            // Advance to next team
            int nextTeam = teamManager.GetNextAliveTeam(currentTeamIndex);
            if (nextTeam < 0)
            {
                gameManager.EndGame(currentTeamIndex);
                yield break;
            }
            currentTeamIndex = nextTeam;

            // Peek at next character for camera pan
            int peekIdx = PeekNextCharacterIndex(currentTeamIndex);
            if (peekIdx >= 0)
            {
                var nextChar = teamManager.GetCharacter(currentTeamIndex, peekIdx);
                if (nextChar != null)
                    gameCamera.PanTo(nextChar.transform.position, transitionDuration);
            }

            yield return new WaitForSeconds(transitionDuration + 0.1f);
            BeginTurn();
        }

        // ── Helpers ────────────────────────────────────────────────

        private void UpdateAimOrigin()
        {
            if (activeCharacter != null)
                inputHandler.SetAimOrigin((Vector2)activeCharacter.transform.position + Vector2.up * 0.5f);
        }

        /// <summary>
        /// Peeks at which character index would be next for a team without advancing the counter.
        /// </summary>
        private int PeekNextCharacterIndex(int teamIndex)
        {
            int count = teamManager.GetCharacterCount(teamIndex);
            for (int i = 0; i < count; i++)
            {
                var ch = teamManager.GetCharacter(teamIndex, i);
                if (ch != null && ch.IsAlive) return i;
            }
            return -1;
        }
    }
}
