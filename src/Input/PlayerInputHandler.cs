using System;
using UnityEngine;

namespace CatsVsCapybaras
{
    /// <summary>
    /// Handles player input for iOS touch and editor mouse/keyboard.
    /// Fires events consumed by TurnManager to drive gameplay.
    ///
    /// Touch layout:
    ///   Left 1/3 of screen  → D-pad movement
    ///   Right 2/3 of screen → Aim (touch position) + charge (hold) + fire (release)
    ///
    /// Editor:
    ///   Arrow keys / WASD   → Movement
    ///   Mouse position       → Aim direction
    ///   Left click hold      → Charge power
    ///   Left click release   → Fire
    ///   Space                → Jump
    ///   1 / 2 / 3            → Weapon select
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        public event Action<float> OnMoveInput;
        public event Action<float, float> OnFireRequested;
        public event Action<int> OnWeaponSelected;
        public event Action OnJumpRequested;

        [Header("Configuration")]
        [SerializeField] private float maxChargeTime = 2.5f;
        [SerializeField] private float dpadScreenFraction = 0.33f;
        [SerializeField] private Camera gameCamera;

        public float MoveHorizontal { get; private set; }
        public float AimAngle { get; private set; }
        public float ChargePower { get; private set; }
        public bool IsCharging { get; private set; }
        public bool InputEnabled { get; private set; }

        private bool moveAllowed;
        private bool aimAllowed;
        private Vector2 aimOriginWorld;

        private void Awake()
        {
            if (gameCamera == null)
                gameCamera = Camera.main;
        }

        public void EnableInput(bool allowMove, bool allowAim)
        {
            InputEnabled = true;
            moveAllowed = allowMove;
            aimAllowed = allowAim;
            MoveHorizontal = 0f;
            ChargePower = 0f;
            IsCharging = false;
        }

        public void DisableInput()
        {
            InputEnabled = false;
            moveAllowed = false;
            aimAllowed = false;
            MoveHorizontal = 0f;
            ChargePower = 0f;
            IsCharging = false;
        }

        public void SetAimOrigin(Vector2 worldPosition)
        {
            aimOriginWorld = worldPosition;
        }

        /// <summary>
        /// Called by UI jump button on iOS.
        /// </summary>
        public void RequestJump()
        {
            if (InputEnabled && moveAllowed)
                OnJumpRequested?.Invoke();
        }

        /// <summary>
        /// Called by weapon selection UI buttons on iOS.
        /// </summary>
        public void SelectWeapon(int index)
        {
            OnWeaponSelected?.Invoke(index);
        }

        private void Update()
        {
            if (!InputEnabled) return;

            if (moveAllowed) HandleMovement();
            if (aimAllowed) HandleAiming();
            HandleWeaponKeys();
        }

        private void HandleMovement()
        {
            MoveHorizontal = 0f;

            // Touch D-pad: left portion of screen
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.position.x < Screen.width * dpadScreenFraction)
                {
                    float center = Screen.width * dpadScreenFraction * 0.5f;
                    MoveHorizontal = Mathf.Clamp((touch.position.x - center) / center, -1f, 1f);
                    break;
                }
            }

            // Keyboard fallback (editor + desktop testing)
            if (Mathf.Approximately(MoveHorizontal, 0f))
            {
                if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) MoveHorizontal = -1f;
                if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) MoveHorizontal = 1f;
            }

            // Jump
            if (Input.GetKeyDown(KeyCode.Space))
                OnJumpRequested?.Invoke();

            OnMoveInput?.Invoke(MoveHorizontal);
        }

        private void HandleAiming()
        {
            Vector2 inputScreenPos = Vector2.zero;
            bool hasInput = false;
            bool inputBegan = false;
            bool inputEnded = false;

            // Touch: right portion of screen
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.position.x >= Screen.width * dpadScreenFraction)
                {
                    inputScreenPos = touch.position;
                    hasInput = true;
                    if (touch.phase == TouchPhase.Began) inputBegan = true;
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) inputEnded = true;
                    break;
                }
            }

            // Mouse fallback
            if (!hasInput)
            {
                inputScreenPos = Input.mousePosition;
                hasInput = true;
                if (Input.GetMouseButtonDown(0)) inputBegan = true;
                if (Input.GetMouseButtonUp(0)) inputEnded = true;
            }

            // Update aim angle from screen position → world position
            if (hasInput && gameCamera != null)
            {
                Vector2 worldPos = gameCamera.ScreenToWorldPoint(inputScreenPos);
                Vector2 dir = worldPos - aimOriginWorld;
                if (dir.sqrMagnitude > 0.01f)
                    AimAngle = Mathf.Atan2(dir.y, dir.x);
            }

            // Charge: begin
            if (inputBegan)
            {
                IsCharging = true;
                ChargePower = 0f;
            }

            // Charge: hold
            if (IsCharging)
                ChargePower = Mathf.Clamp01(ChargePower + Time.deltaTime / maxChargeTime);

            // Fire: release with sufficient charge
            if (inputEnded && IsCharging)
            {
                if (ChargePower > 0.05f)
                    OnFireRequested?.Invoke(AimAngle, ChargePower);

                IsCharging = false;
                ChargePower = 0f;
            }
        }

        private void HandleWeaponKeys()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) OnWeaponSelected?.Invoke(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) OnWeaponSelected?.Invoke(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) OnWeaponSelected?.Invoke(2);
        }
    }
}
