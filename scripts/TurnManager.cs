using System;
using System.Collections;
using UnityEngine;

namespace CatsVsCapybaras
{
    /// <summary>
    /// Manages turn flow, phase transitions, and timer for the turn-based combat system.
    /// Controls the sequence: MovePhase → AimPhase → FirePhase → ResolvingPhase → TransitionPhase.
    /// Coordinates with GameManager, CameraController, and character controllers.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        #region Enums

        /// <summary>
        /// Represents the current phase of a turn in the combat system.
        /// </summary>
        public enum TurnPhase
        {
            /// <summary>Character movement phase</summary>
            MovePhase,
            /// <summary>Weapon aiming phase</summary>
            AimPhase,
            /// <summary>Weapon fire phase</summary>
            FirePhase,
            /// <summary>Projectile and explosion resolution phase</summary>
            ResolvingPhase,
            /// <summary>Transition to next team/character phase</summary>
            TransitionPhase
        }

        #endregion

        #region Events

        /// <summary>
        /// Invoked when the turn phase changes.
        /// Provides the new TurnPhase as parameter.
        /// </summary>
        public event Action<TurnPhase> OnPhaseChanged;

        /// <summary>
        /// Invoked when a new turn starts.
        /// Provides team index (0 or 1) and character index.
        /// </summary>
        public event Action<int, int> OnTurnStarted;

        /// <summary>
        /// Invoked when a turn ends and transitions to the next character/team.
        /// </summary>
        public event Action OnTurnEnded;

        /// <summary>
        /// Invoked every second during the turn to update UI with remaining time.
        /// Provides remaining seconds as parameter.
        /// </summary>
        public event Action<float> OnTimerUpdated;

        /// <summary>
        /// Invoked when the turn timer expires.
        /// </summary>
        public event Action OnTimerExpired;

        #endregion

        #region Inspector Fields

        [SerializeField]
        private float _turnDurationSeconds = 35f;

        [SerializeField]
        private GameManager _gameManager;

        [SerializeField]
        private CameraController _cameraController;

        /// <summary>
        /// Delay in seconds before transitioning to the next phase.
        /// Allows animations and effects to play out.
        /// </summary>
        [SerializeField]
        private float _phaseTransitionDelay = 0.5f;

        /// <summary>
        /// Maximum time to wait for projectile resolution before auto-advancing.
        /// </summary>
        [SerializeField]
        private float _projectileResolutionTimeout = 5f;

        #endregion

        #region Private Fields

        private TurnPhase _currentPhase = TurnPhase.MovePhase;
        private int _currentTeamIndex = 0; // 0 = Team A, 1 = Team B
        private int _currentCharacterIndex = 0;
        private float _timeRemaining;
        private bool _timerActive = false;
        private bool _isWaitingForProjectileResolution = false;
        private Coroutine _timerCoroutine;
        private Coroutine _phaseCoroutine;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current turn phase.
        /// </summary>
        public TurnPhase CurrentPhase => _currentPhase;

        /// <summary>
        /// Gets the index of the team whose turn it is (0 or 1).
        /// </summary>
        public int CurrentTeamIndex => _currentTeamIndex;

        /// <summary>
        /// Gets the index of the character within the current team.
        /// </summary>
        public int CurrentCharacterIndex => _currentCharacterIndex;

        /// <summary>
        /// Gets the time remaining on the current turn in seconds.
        /// </summary>
        public float TimeRemaining => _timeRemaining;

        /// <summary>
        /// Gets whether the timer is currently active.
        /// </summary>
        public bool IsTimerActive => _timerActive;

        /// <summary>
        /// Gets the active character for the current turn.
        /// </summary>
        public CharacterController2D ActiveCharacter
        {
            get
            {
                return _gameManager.GetCharacter(_currentTeamIndex, _currentCharacterIndex);
            }
        }

        #endregion

        #region Lifecycle Methods

        private void Awake()
        {
            if (_gameManager == null)
                _gameManager = GameManager.Instance;
            if (_cameraController == null)
                _cameraController = FindObjectOfType<CameraController>();
        }

        private void Start()
        {
            _timeRemaining = _turnDurationSeconds;
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        #endregion

        #region Turn Management

        /// <summary>
        /// Starts a new turn for the next available character.
        /// Automatically cycles through living characters on each team.
        /// </summary>
        public void StartTurn()
        {
            if (_gameManager == null)
            {
                Debug.LogError("TurnManager: GameManager reference not found!");
                return;
            }

            // Find next alive character
            CharacterController2D activeCharacter = FindNextAliveCharacter();
            if (activeCharacter == null)
            {
                Debug.LogError("TurnManager: No alive characters found to start turn!");
                return;
            }

            // Reset timer
            _timeRemaining = _turnDurationSeconds;
            _isWaitingForProjectileResolution = false;

            // Set active character
            activeCharacter.SetActive(true);

            // Invoke turn started event
            OnTurnStarted?.Invoke(_currentTeamIndex, _currentCharacterIndex);

            Debug.Log($"Turn started - Team {_currentTeamIndex}, Character {_currentCharacterIndex}");

            // Start first phase
            StartPhase(TurnPhase.MovePhase);
        }

        /// <summary>
        /// Ends the current turn and cycles to the next character.
        /// </summary>
        public void EndTurn()
        {
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }

            _timerActive = false;

            // Deactivate current character
            CharacterController2D currentCharacter = ActiveCharacter;
            if (currentCharacter != null)
                currentCharacter.SetActive(false);

            // Cycle to next character
            CycleToNextCharacter();

            OnTurnEnded?.Invoke();

            Debug.Log("Turn ended");

            // Start new turn after delay
            Invoke(nameof(StartTurn), _phaseTransitionDelay);
        }

        /// <summary>
        /// Finds the next alive character to take a turn.
        /// Alternates between teams and cycles through team members.
        /// </summary>
        /// <returns>The next alive character, or null if none available.</returns>
        private CharacterController2D FindNextAliveCharacter()
        {
            // Try current team first
            for (int i = 0; i < 2; i++)
            {
                var team = _currentTeamIndex == 0 ? _gameManager.TeamA : _gameManager.TeamB;

                for (int j = 0; j < team.Count; j++)
                {
                    int characterIndex = (_currentCharacterIndex + j + 1) % team.Count;
                    CharacterController2D character = team[characterIndex];

                    if (character != null && !character.IsEliminated)
                    {
                        _currentCharacterIndex = characterIndex;
                        return character;
                    }
                }

                // Switch to other team if no alive characters found on current team
                _currentTeamIndex = (_currentTeamIndex + 1) % 2;
            }

            Debug.LogWarning("TurnManager: No alive characters found!");
            return null;
        }

        /// <summary>
        /// Cycles to the next character, alternating between teams.
        /// </summary>
        private void CycleToNextCharacter()
        {
            _currentTeamIndex = (_currentTeamIndex + 1) % 2;
            _currentCharacterIndex = 0;
        }

        /// <summary>
        /// Resets the turn state for a new round.
        /// </summary>
        public void ResetTurn()
        {
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }

            if (_phaseCoroutine != null)
            {
                StopCoroutine(_phaseCoroutine);
                _phaseCoroutine = null;
            }

            _currentTeamIndex = 0;
            _currentCharacterIndex = 0;
            _currentPhase = TurnPhase.MovePhase;
            _timeRemaining = _turnDurationSeconds;
            _timerActive = false;
            _isWaitingForProjectileResolution = false;

            Debug.Log("Turn reset");
        }

        #endregion

        #region Phase Management

        /// <summary>
        /// Starts a new phase within the current turn.
        /// Initiates phase-specific logic and starts the timer if appropriate.
        /// </summary>
        /// <param name="phase">The phase to start.</param>
        public void StartPhase(TurnPhase phase)
        {
            _currentPhase = phase;
            OnPhaseChanged?.Invoke(_currentPhase);

            Debug.Log($"Phase started: {phase}");

            // Stop existing phase coroutine
            if (_phaseCoroutine != null)
            {
                StopCoroutine(_phaseCoroutine);
            }

            switch (phase)
            {
                case TurnPhase.MovePhase:
                    _phaseCoroutine = StartCoroutine(HandleMovePhase());
                    break;
                case TurnPhase.AimPhase:
                    _phaseCoroutine = StartCoroutine(HandleAimPhase());
                    break;
                case TurnPhase.FirePhase:
                    _phaseCoroutine = StartCoroutine(HandleFirePhase());
                    break;
                case TurnPhase.ResolvingPhase:
                    _phaseCoroutine = StartCoroutine(HandleResolvingPhase());
                    break;
                case TurnPhase.TransitionPhase:
                    _phaseCoroutine = StartCoroutine(HandleTransitionPhase());
                    break;
            }
        }

        /// <summary>
        /// Advances to the next phase in the turn sequence.
        /// Automatically cycles through all phases and then ends the turn.
        /// </summary>
        public void AdvancePhase()
        {
            TurnPhase nextPhase;

            switch (_currentPhase)
            {
                case TurnPhase.MovePhase:
                    nextPhase = TurnPhase.AimPhase;
                    break;
                case TurnPhase.AimPhase:
                    nextPhase = TurnPhase.FirePhase;
                    break;
                case TurnPhase.FirePhase:
                    nextPhase = TurnPhase.ResolvingPhase;
                    break;
                case TurnPhase.ResolvingPhase:
                    nextPhase = TurnPhase.TransitionPhase;
                    break;
                case TurnPhase.TransitionPhase:
                    EndTurn();
                    return;
                default:
                    nextPhase = TurnPhase.MovePhase;
                    break;
            }

            StartPhase(nextPhase);
        }

        #endregion

        #region Phase Handlers

        /// <summary>
        /// Handles the move phase coroutine.
        /// Player can move the character during this phase.
        /// Automatically advances after timeout.
        /// </summary>
        private IEnumerator HandleMovePhase()
        {
            StartTimer();

            // Wait for phase to complete or timeout
            yield return WaitForPhaseCompletion();

            StopTimer();
            AdvancePhase();
        }

        /// <summary>
        /// Handles the aim phase coroutine.
        /// Player can aim the weapon during this phase.
        /// Automatically advances after timeout.
        /// </summary>
        private IEnumerator HandleAimPhase()
        {
            StartTimer();

            // Wait for phase to complete or timeout
            yield return WaitForPhaseCompletion();

            StopTimer();
            AdvancePhase();
        }

        /// <summary>
        /// Handles the fire phase coroutine.
        /// Player fires the weapon during this phase.
        /// Projectile spawning happens here.
        /// </summary>
        private IEnumerator HandleFirePhase()
        {
            StopTimer();

            // Fire the weapon
            CharacterController2D activeCharacter = ActiveCharacter;
            if (activeCharacter != null)
            {
                activeCharacter.FireWeapon();
            }

            // Brief delay before transitioning
            yield return new WaitForSeconds(_phaseTransitionDelay);

            AdvancePhase();
        }

        /// <summary>
        /// Handles the resolving phase coroutine.
        /// Waits for projectiles and explosions to complete before advancing.
        /// </summary>
        private IEnumerator HandleResolvingPhase()
        {
            _isWaitingForProjectileResolution = true;

            // Wait for projectiles to resolve or timeout
            float timeWaited = 0f;
            while (_isWaitingForProjectileResolution && timeWaited < _projectileResolutionTimeout)
            {
                timeWaited += Time.deltaTime;
                yield return null;
            }

            _isWaitingForProjectileResolution = false;

            // Brief delay before transitioning
            yield return new WaitForSeconds(_phaseTransitionDelay);

            AdvancePhase();
        }

        /// <summary>
        /// Handles the transition phase coroutine.
        /// Brief phase for UI updates and state transitions between turns.
        /// </summary>
        private IEnumerator HandleTransitionPhase()
        {
            // Transition delay for UI updates
            yield return new WaitForSeconds(_phaseTransitionDelay);

            AdvancePhase();
        }

        /// <summary>
        /// Waits for a phase to complete naturally or timeout.
        /// </summary>
        /// <returns>IEnumerator for coroutine.</returns>
        private IEnumerator WaitForPhaseCompletion()
        {
            // Check if active character has input (can be extended for more complex logic)
            CharacterController2D activeCharacter = ActiveCharacter;

            // Wait until timer expires or player manually advances phase
            while (_timerActive && _timeRemaining > 0)
            {
                yield return null;
            }
        }

        #endregion

        #region Timer Management

        /// <summary>
        /// Starts the turn timer countdown.
        /// </summary>
        public void StartTimer()
        {
            if (_timerActive)
                return;

            _timerActive = true;

            if (_timerCoroutine != null)
                StopCoroutine(_timerCoroutine);

            _timerCoroutine = StartCoroutine(TimerCoroutine());
        }

        /// <summary>
        /// Stops the turn timer without resetting it.
        /// </summary>
        public void StopTimer()
        {
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }

            _timerActive = false;
        }

        /// <summary>
        /// Pauses the turn timer.
        /// </summary>
        public void PauseTimer()
        {
            StopTimer();
        }

        /// <summary>
        /// Resumes the turn timer.
        /// </summary>
        public void ResumeTimer()
        {
            StartTimer();
        }

        /// <summary>
        /// Resets the timer to full duration.
        /// </summary>
        public void ResetTimer()
        {
            _timeRemaining = _turnDurationSeconds;
        }

        /// <summary>
        /// Coroutine that handles the timer countdown.
        /// Invokes events every second and when time expires.
        /// </summary>
        private IEnumerator TimerCoroutine()
        {
            while (_timeRemaining > 0)
            {
                _timeRemaining -= Time.deltaTime;

                // Invoke timer updated event
                OnTimerUpdated?.Invoke(_timeRemaining);

                yield return null;
            }

            // Timer expired
            _timeRemaining = 0;
            _timerActive = false;
            OnTimerExpired?.Invoke();

            Debug.Log("Turn timer expired!");

            // Auto-advance to next phase when timer expires
            AdvancePhase();
        }

        #endregion

        #region Projectile Resolution

        /// <summary>
        /// Notifies the turn manager that a projectile has been fired.
        /// Increments the projectile counter during the resolving phase.
        /// </summary>
        public void RegisterProjectile()
        {
            if (_currentPhase == TurnPhase.FirePhase)
            {
                _isWaitingForProjectileResolution = true;
                Debug.Log("Projectile registered");
            }
        }

        /// <summary>
        /// Notifies the turn manager that a projectile has finished resolving.
        /// When all projectiles are resolved, the resolving phase can complete.
        /// </summary>
        public void ProjectileResolved()
        {
            Debug.Log("Projectile resolved");

            // In a full implementation, you would track multiple projectiles
            // For now, we'll mark resolution as complete after the first projectile resolves
            if (_currentPhase == TurnPhase.ResolvingPhase)
            {
                _isWaitingForProjectileResolution = false;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gets the active character for the current turn.
        /// Alias for the ActiveCharacter property.
        /// </summary>
        /// <returns>The currently active character controller, or null if none.</returns>
        public CharacterController2D GetActiveCharacter()
        {
            return ActiveCharacter;
        }

        /// <summary>
        /// Checks if a specific team has any alive characters.
        /// </summary>
        /// <param name="teamIndex">The team index (0 or 1).</param>
        /// <returns>True if the team has alive characters, false otherwise.</returns>
        public bool HasAliveCharacters(int teamIndex)
        {
            var team = teamIndex == 0 ? _gameManager.TeamA : _gameManager.TeamB;
            foreach (CharacterController2D character in team)
            {
                if (character != null && !character.IsEliminated)
                    return true;
            }
            return false;
        }

        #endregion
    }
}
