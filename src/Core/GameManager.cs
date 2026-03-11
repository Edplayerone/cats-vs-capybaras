using System;
using UnityEngine;

namespace CatsVsCapybaras
{
    /// <summary>
    /// Central game coordinator. Owns top-level state (Playing / GameOver),
    /// weapon definitions, wind, and references to all subsystems.
    ///
    /// Scene setup:
    ///   1. Create empty GameObject "GameManager"
    ///   2. Attach this script
    ///   3. Assign TurnManager, TeamManager, GameCamera, PlayerInputHandler,
    ///      HUDManager, TerrainDestruction references in Inspector
    ///   4. Populate the Weapons array with WeaponData ScriptableObjects
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public enum GameState { Setup, Playing, GameOver }

        public static GameManager Instance { get; private set; }

        public event Action<GameState> OnGameStateChanged;
        public event Action<int> OnGameEnded;

        [Header("Subsystem References")]
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private TeamManager teamManager;
        [SerializeField] private GameCamera gameCamera;
        [SerializeField] private PlayerInputHandler inputHandler;
        [SerializeField] private HUDManager hudManager;
        [SerializeField] private TerrainDestruction terrain;

        [Header("Weapons")]
        [SerializeField] private WeaponData[] weapons;

        [Header("Wind")]
        [SerializeField] private float maxWindStrength = 5f;

        public GameState CurrentState { get; private set; } = GameState.Setup;
        public WeaponData[] Weapons => weapons;
        public float Wind { get; private set; }
        public TurnManager TurnManager => turnManager;
        public TeamManager TeamManager => teamManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            FallbackResolve();
            StartGame();
        }

        public void StartGame()
        {
            teamManager.Initialize();
            teamManager.InitializeAmmo(weapons);
            turnManager.Initialize();

            if (hudManager != null)
                hudManager.Initialize(this, turnManager, teamManager);

            SetState(GameState.Playing);
            turnManager.StartFirstTurn();
        }

        public void RandomizeWind()
        {
            Wind = UnityEngine.Random.Range(-maxWindStrength, maxWindStrength);
        }

        public void EndGame(int winningTeamIndex)
        {
            if (CurrentState == GameState.GameOver) return;

            SetState(GameState.GameOver);
            OnGameEnded?.Invoke(winningTeamIndex);

            string winner = winningTeamIndex >= 0
                ? teamManager.GetTeamName(winningTeamIndex)
                : "Draw";
            Debug.Log($"[GameManager] Game Over — Winner: {winner}");
        }

        public void RestartGame()
        {
            if (terrain != null)
                terrain.ResetTerrain();

            teamManager.ResetAllCharacters();
            teamManager.InitializeAmmo(weapons);
            Wind = 0f;

            SetState(GameState.Playing);
            turnManager.StartFirstTurn();
        }

        private void SetState(GameState state)
        {
            if (CurrentState == state) return;
            CurrentState = state;
            OnGameStateChanged?.Invoke(state);
        }

        /// <summary>
        /// Attempts to find missing references automatically.
        /// Inspector assignment is preferred for reliability.
        /// </summary>
        private void FallbackResolve()
        {
            if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>();
            if (teamManager == null) teamManager = FindAnyObjectByType<TeamManager>();
            if (gameCamera == null) gameCamera = FindAnyObjectByType<GameCamera>();
            if (inputHandler == null) inputHandler = FindAnyObjectByType<PlayerInputHandler>();
            if (hudManager == null) hudManager = FindAnyObjectByType<HUDManager>();
            if (terrain == null) terrain = FindAnyObjectByType<TerrainDestruction>();
        }
    }
}
