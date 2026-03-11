using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatsVsCapybaras
{
    /// <summary>
    /// Central game manager implementing the singleton pattern.
    /// Manages game states, team setup, win conditions, and overall game flow.
    /// Coordinates between TurnManager, CameraController, UIManager, and TerrainDestruction.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Enums

        /// <summary>
        /// Represents the current state of the game.
        /// </summary>
        public enum GameState
        {
            /// <summary>Main menu state</summary>
            MainMenu,
            /// <summary>Team setup and character placement state</summary>
            TeamSetup,
            /// <summary>Active gameplay state</summary>
            Playing,
            /// <summary>Round has ended, waiting for next round or game end</summary>
            RoundOver,
            /// <summary>Game has ended, displaying results</summary>
            GameOver
        }

        #endregion

        #region Singleton

        private static GameManager _instance;

        /// <summary>
        /// Gets the singleton instance of GameManager.
        /// </summary>
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameManager>();
                    if (_instance == null)
                    {
                        Debug.LogError("GameManager instance not found in scene!");
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Invoked when the game state changes.
        /// Provides the new GameState as parameter.
        /// </summary>
        public event Action<GameState> OnGameStateChanged;

        /// <summary>
        /// Invoked when a team's character is eliminated.
        /// Provides team index (0 or 1) and character index.
        /// </summary>
        public event Action<int, int> OnCharacterEliminated;

        /// <summary>
        /// Invoked when a round ends.
        /// Provides the winning team index (0 or 1), or -1 if tie.
        /// </summary>
        public event Action<int> OnRoundEnded;

        /// <summary>
        /// Invoked when the entire game ends.
        /// Provides the winning team index (0 or 1).
        /// </summary>
        public event Action<int> OnGameEnded;

        /// <summary>
        /// Invoked when team setup is complete and ready to play.
        /// </summary>
        public event Action OnTeamSetupComplete;

        #endregion

        #region Inspector Fields

        [SerializeField]
        private TurnManager _turnManager;

        [SerializeField]
        private CameraController _cameraController;

        [SerializeField]
        private UIManager _uiManager;

        [SerializeField]
        private TerrainDestruction _terrainDestruction;

        /// <summary>
        /// Number of rounds to win the entire match.
        /// </summary>
        [SerializeField]
        private int _roundsToWin = 3;

        #endregion

        #region Private Fields

        private GameState _currentState = GameState.MainMenu;
        private int _roundsWonTeamA = 0;
        private int _roundsWonTeamB = 0;
        private List<CharacterController2D> _teamA = new List<CharacterController2D>();
        private List<CharacterController2D> _teamB = new List<CharacterController2D>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current game state.
        /// </summary>
        public GameState CurrentState => _currentState;

        /// <summary>
        /// Gets the list of Team A (Cats) characters.
        /// </summary>
        public List<CharacterController2D> TeamA => _teamA;

        /// <summary>
        /// Gets the list of Team B (Capybaras) characters.
        /// </summary>
        public List<CharacterController2D> TeamB => _teamB;

        /// <summary>
        /// Gets the number of rounds Team A has won.
        /// </summary>
        public int RoundsWonTeamA => _roundsWonTeamA;

        /// <summary>
        /// Gets the number of rounds Team B has won.
        /// </summary>
        public int RoundsWonTeamB => _roundsWonTeamB;

        /// <summary>
        /// Gets the current round number (1-indexed).
        /// </summary>
        public int CurrentRound => _roundsWonTeamA + _roundsWonTeamB + 1;

        #endregion

        #region Lifecycle Methods

        private void Awake()
        {
            // Enforce singleton pattern
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Validate required components
            if (_turnManager == null)
                _turnManager = FindObjectOfType<TurnManager>();
            if (_cameraController == null)
                _cameraController = FindObjectOfType<CameraController>();
            if (_uiManager == null)
                _uiManager = FindObjectOfType<UIManager>();
            if (_terrainDestruction == null)
                _terrainDestruction = FindObjectOfType<TerrainDestruction>();

            // Subscribe to turn manager events
            if (_turnManager != null)
            {
                _turnManager.OnPhaseChanged += HandleTurnPhaseChanged;
                _turnManager.OnTurnEnded += HandleTurnEnded;
            }

            // Initialize game state
            SetGameState(GameState.MainMenu);
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_turnManager != null)
            {
                _turnManager.OnPhaseChanged -= HandleTurnPhaseChanged;
                _turnManager.OnTurnEnded -= HandleTurnEnded;
            }
        }

        #endregion

        #region Game State Management

        /// <summary>
        /// Sets the current game state and invokes the OnGameStateChanged event.
        /// </summary>
        /// <param name="newState">The new game state to transition to.</param>
        public void SetGameState(GameState newState)
        {
            if (_currentState == newState)
                return;

            _currentState = newState;
            Debug.Log($"Game state changed to: {newState}");

            OnGameStateChanged?.Invoke(_currentState);

            // Handle state-specific logic
            switch (_currentState)
            {
                case GameState.TeamSetup:
                    HandleTeamSetupStart();
                    break;
                case GameState.Playing:
                    HandlePlayingStart();
                    break;
                case GameState.RoundOver:
                    HandleRoundOverStart();
                    break;
                case GameState.GameOver:
                    HandleGameOverStart();
                    break;
            }
        }

        #endregion

        #region Team Setup

        /// <summary>
        /// Registers a character to a team.
        /// Should be called during team setup phase.
        /// </summary>
        /// <param name="character">The character controller to register.</param>
        /// <param name="teamIndex">Team index (0 for Team A, 1 for Team B).</param>
        public void RegisterCharacter(CharacterController2D character, int teamIndex)
        {
            if (character == null)
            {
                Debug.LogWarning("Attempting to register null character!");
                return;
            }

            if (teamIndex == 0)
            {
                _teamA.Add(character);
                character.SetTeam(0);
            }
            else if (teamIndex == 1)
            {
                _teamB.Add(character);
                character.SetTeam(1);
            }
            else
            {
                Debug.LogWarning($"Invalid team index: {teamIndex}");
            }
        }

        /// <summary>
        /// Completes team setup and transitions to Playing state.
        /// Should be called after all characters are placed and ready.
        /// </summary>
        public void CompleteTeamSetup()
        {
            if (_teamA.Count == 0 || _teamB.Count == 0)
            {
                Debug.LogError("Cannot complete team setup: both teams must have at least one character!");
                return;
            }

            OnTeamSetupComplete?.Invoke();
            SetGameState(GameState.Playing);
        }

        #endregion

        #region Character Management

        /// <summary>
        /// Marks a character as eliminated and checks win conditions.
        /// Called when a character's health reaches zero.
        /// </summary>
        /// <param name="teamIndex">The team index (0 or 1) of the eliminated character.</param>
        /// <param name="characterIndex">The character index within the team.</param>
        public void EliminateCharacter(int teamIndex, int characterIndex)
        {
            if (teamIndex == 0)
            {
                if (characterIndex < _teamA.Count)
                {
                    _teamA[characterIndex].SetEliminated(true);
                    OnCharacterEliminated?.Invoke(0, characterIndex);
                    Debug.Log($"Team A Character {characterIndex} eliminated!");
                }
            }
            else if (teamIndex == 1)
            {
                if (characterIndex < _teamB.Count)
                {
                    _teamB[characterIndex].SetEliminated(true);
                    OnCharacterEliminated?.Invoke(1, characterIndex);
                    Debug.Log($"Team B Character {characterIndex} eliminated!");
                }
            }

            CheckRoundWinCondition();
        }

        #endregion

        #region Win Condition Checking

        /// <summary>
        /// Checks if the current round has been won by any team.
        /// A team wins the round when all opposing team members are eliminated.
        /// </summary>
        private void CheckRoundWinCondition()
        {
            int teamAAlive = CountAliveCharacters(_teamA);
            int teamBAlive = CountAliveCharacters(_teamB);

            if (teamAAlive == 0)
            {
                EndRound(1); // Team B wins
            }
            else if (teamBAlive == 0)
            {
                EndRound(0); // Team A wins
            }
        }

        /// <summary>
        /// Counts the number of alive characters on a team.
        /// </summary>
        /// <param name="team">The team's character list.</param>
        /// <returns>Number of alive characters.</returns>
        private int CountAliveCharacters(List<CharacterController2D> team)
        {
            int count = 0;
            foreach (CharacterController2D character in team)
            {
                if (character != null && !character.IsEliminated)
                    count++;
            }
            return count;
        }

        #endregion

        #region Round Management

        /// <summary>
        /// Ends the current round with a winning team.
        /// Updates match score and determines next action (next round or game over).
        /// </summary>
        /// <param name="winningTeamIndex">The index of the winning team (0 or 1).</param>
        public void EndRound(int winningTeamIndex)
        {
            if (_turnManager != null)
                _turnManager.PauseTimer();

            if (winningTeamIndex == 0)
            {
                _roundsWonTeamA++;
                Debug.Log("Team A wins the round!");
            }
            else if (winningTeamIndex == 1)
            {
                _roundsWonTeamB++;
                Debug.Log("Team B wins the round!");
            }

            OnRoundEnded?.Invoke(winningTeamIndex);
            SetGameState(GameState.RoundOver);
        }

        /// <summary>
        /// Starts a new round, resetting teams and terrain.
        /// Called after the RoundOver state when transitioning to the next round.
        /// </summary>
        public void StartNewRound()
        {
            // Reset all characters
            ResetTeam(_teamA);
            ResetTeam(_teamB);

            // Reset terrain
            if (_terrainDestruction != null)
                _terrainDestruction.ResetTerrain();

            // Reset turn manager
            if (_turnManager != null)
                _turnManager.ResetTurn();

            // Transition to Playing state
            SetGameState(GameState.Playing);
        }

        /// <summary>
        /// Resets a team's characters for the next round.
        /// </summary>
        /// <param name="team">The team to reset.</param>
        private void ResetTeam(List<CharacterController2D> team)
        {
            foreach (CharacterController2D character in team)
            {
                if (character != null)
                {
                    character.ResetHealth();
                    character.SetEliminated(false);
                    character.ResetPosition();
                }
            }
        }

        #endregion

        #region Match Management

        /// <summary>
        /// Checks if the match has been won by any team.
        /// A team wins the match when they reach the required number of round wins.
        /// </summary>
        private void CheckMatchWinCondition()
        {
            if (_roundsWonTeamA >= _roundsToWin)
            {
                EndGame(0); // Team A wins match
            }
            else if (_roundsWonTeamB >= _roundsToWin)
            {
                EndGame(1); // Team B wins match
            }
        }

        /// <summary>
        /// Ends the game with a winning team.
        /// </summary>
        /// <param name="winningTeamIndex">The index of the winning team (0 or 1).</param>
        public void EndGame(int winningTeamIndex)
        {
            if (_turnManager != null)
                _turnManager.PauseTimer();

            Debug.Log($"Team {(winningTeamIndex == 0 ? "A (Cats)" : "B (Capybaras)")} wins the match!");
            OnGameEnded?.Invoke(winningTeamIndex);
            SetGameState(GameState.GameOver);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles turn phase changes from the TurnManager.
        /// Updates camera mode based on the new phase.
        /// </summary>
        private void HandleTurnPhaseChanged(TurnManager.TurnPhase newPhase)
        {
            if (_cameraController == null)
                return;

            switch (newPhase)
            {
                case TurnManager.TurnPhase.MovePhase:
                case TurnManager.TurnPhase.AimPhase:
                    _cameraController.SetCameraMode(CameraController.CameraMode.FollowCharacter);
                    break;
                case TurnManager.TurnPhase.FirePhase:
                    _cameraController.SetCameraMode(CameraController.CameraMode.FollowProjectile);
                    break;
                case TurnManager.TurnPhase.ResolvingPhase:
                    _cameraController.SetCameraMode(CameraController.CameraMode.FollowExplosion);
                    break;
                case TurnManager.TurnPhase.TransitionPhase:
                    _cameraController.SetCameraMode(CameraController.CameraMode.FollowCharacter);
                    break;
            }
        }

        /// <summary>
        /// Handles turn end events from the TurnManager.
        /// Advances to the next turn.
        /// </summary>
        private void HandleTurnEnded()
        {
            // Any necessary cleanup between turns can be done here
        }

        /// <summary>
        /// Handles team setup state initialization.
        /// </summary>
        private void HandleTeamSetupStart()
        {
            _roundsWonTeamA = 0;
            _roundsWonTeamB = 0;
            _teamA.Clear();
            _teamB.Clear();
        }

        /// <summary>
        /// Handles Playing state initialization.
        /// Starts the first turn if this is the first round.
        /// </summary>
        private void HandlePlayingStart()
        {
            if (_turnManager != null)
            {
                _turnManager.StartTurn();
            }
        }

        /// <summary>
        /// Handles RoundOver state initialization.
        /// Checks if match should continue or end.
        /// </summary>
        private void HandleRoundOverStart()
        {
            CheckMatchWinCondition();
        }

        /// <summary>
        /// Handles GameOver state initialization.
        /// Display final results and cleanup.
        /// </summary>
        private void HandleGameOverStart()
        {
            if (_uiManager != null)
                _uiManager.ShowGameOverScreen();
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gets a specific character from a team.
        /// </summary>
        /// <param name="teamIndex">The team index (0 or 1).</param>
        /// <param name="characterIndex">The character index within the team.</param>
        /// <returns>The character controller, or null if not found.</returns>
        public CharacterController2D GetCharacter(int teamIndex, int characterIndex)
        {
            if (teamIndex == 0 && characterIndex < _teamA.Count)
                return _teamA[characterIndex];
            else if (teamIndex == 1 && characterIndex < _teamB.Count)
                return _teamB[characterIndex];

            return null;
        }

        /// <summary>
        /// Pauses the game.
        /// </summary>
        public void PauseGame()
        {
            Time.timeScale = 0f;
        }

        /// <summary>
        /// Resumes the game.
        /// </summary>
        public void ResumeGame()
        {
            Time.timeScale = 1f;
        }

        #endregion
    }
}
