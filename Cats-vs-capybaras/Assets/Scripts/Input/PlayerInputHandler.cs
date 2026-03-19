using System;
using UnityEngine;

namespace CatsVsCapybaras
{
    /// <summary>
    /// Handles player input for iOS touch and editor mouse/keyboard.
    /// Fires events consumed by TurnManager to drive gameplay.
    ///
    /// Touch layout:
    ///   Left 1/3 of screen  → D-pad movement (left/right/jump)
    ///   Right 2/3 of screen → Swipe to aim and fire
    ///                         Swipe direction = aim angle
    ///                         Swipe distance  = power (minSwipePixels → maxSwipePixels = 0 → 1)
    ///                         Release          = fire
    ///
    /// Editor / mouse fallback:
    ///   Arrow keys / WASD   → Movement
    ///   Space                → Jump
    ///   Left click + drag    → Swipe aim (same as touch)
    ///   Left click release   → Fire
    ///   1 / 2 / 3            → Weapon select
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        public event Action<float>        OnMoveInput;
        public event Action<float, float> OnFireRequested;
        public event Action<int>          OnWeaponSelected;
        public event Action               OnJumpRequested;

        [Header("Swipe Configuration")]
        [SerializeField] private float maxSwipePixels  = 180f;  // distance for 100% power
        [SerializeField] private float minSwipePixels  = 12f;   // minimum before direction registers
        [SerializeField] private float dpadScreenFraction = 0.33f;

        // Public state read by AimLine and TurnManager
        public float MoveHorizontal { get; private set; }
        public float AimAngle       { get; private set; }
        public float ChargePower    { get; private set; }
        public bool  IsCharging     { get; private set; }
        public bool  InputEnabled   { get; private set; }

        // Swipe tracking
        private Vector2 swipeStartScreen;
        private bool    swipeHasDirection;   // true once swipe > minSwipePixels

        // ── Enable / Disable ─────────────────────────────────────

        public void EnableInput(bool allowMove, bool allowAim)
        {
            InputEnabled    = true;
            _moveAllowed    = allowMove;
            _aimAllowed     = allowAim;
            MoveHorizontal  = 0f;
            ChargePower     = 0f;
            IsCharging      = false;
            swipeHasDirection = false;
        }

        public void DisableInput()
        {
            InputEnabled    = false;
            _moveAllowed    = false;
            _aimAllowed     = false;
            MoveHorizontal  = 0f;
            ChargePower     = 0f;
            IsCharging      = false;
            swipeHasDirection = false;
        }

        private bool _moveAllowed;
        private bool _aimAllowed;

        // ── Compatibility (called by TurnManager, unused in swipe mode) ──────

        /// <summary>
        /// Legacy: TurnManager passes the active character's world position.
        /// The swipe system derives aim from gesture delta, not world coordinates,
        /// so this is intentionally a no-op.
        /// </summary>
        public void SetAimOrigin(Vector2 worldPosition) { /* no-op for swipe system */ }

        // ── UI callbacks (called by on-screen buttons) ────────────

        /// <summary>Called by the on-screen jump button.</summary>
        public void RequestJump()
        {
            if (InputEnabled && _moveAllowed)
                OnJumpRequested?.Invoke();
        }

        /// <summary>Called by weapon-selection UI buttons.</summary>
        public void SelectWeapon(int index) => OnWeaponSelected?.Invoke(index);

        // ── Unity loop ────────────────────────────────────────────

        private void Update()
        {
            if (!InputEnabled) return;

            if (_moveAllowed) HandleMovement();
            if (_aimAllowed)  HandleSwipeFire();
            HandleWeaponKeys();
        }

        // ── Movement ──────────────────────────────────────────────

        private void HandleMovement()
        {
            MoveHorizontal = 0f;

            // Touch D-pad: left third of the screen
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.position.x < Screen.width * dpadScreenFraction)
                {
                    float center  = Screen.width * dpadScreenFraction * 0.5f;
                    MoveHorizontal = Mathf.Clamp((t.position.x - center) / center, -1f, 1f);
                    break;
                }
            }

            // Keyboard fallback
            if (Mathf.Approximately(MoveHorizontal, 0f))
            {
                if (Input.GetKey(KeyCode.LeftArrow)  || Input.GetKey(KeyCode.A)) MoveHorizontal = -1f;
                if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) MoveHorizontal =  1f;
            }

            if (Input.GetKeyDown(KeyCode.Space))
                OnJumpRequested?.Invoke();

            OnMoveInput?.Invoke(MoveHorizontal);
        }

        // ── Swipe-to-fire ─────────────────────────────────────────

        private void HandleSwipeFire()
        {
            bool inputBegan   = false;
            bool inputHeld    = false;
            bool inputEnded   = false;
            Vector2 currentScreen = Vector2.zero;

            // ── Touch input ───────────────────────────────────────
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                // Only handle swipes in the right portion of the screen
                if (t.position.x < Screen.width * dpadScreenFraction) continue;

                currentScreen = t.position;

                switch (t.phase)
                {
                    case TouchPhase.Began:
                        inputBegan = true;
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        inputHeld = true;
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        inputEnded = true;
                        break;
                }
                break;
            }

            // ── Mouse fallback (editor / desktop) ─────────────────
            if (Input.touchCount == 0)
            {
                currentScreen = Input.mousePosition;

                if (Input.GetMouseButtonDown(0)) inputBegan = true;
                if (Input.GetMouseButton(0))     inputHeld  = true;
                if (Input.GetMouseButtonUp(0))   inputEnded = true;
            }

            // ── State machine ─────────────────────────────────────

            // Began: record swipe origin
            if (inputBegan)
            {
                swipeStartScreen  = currentScreen;
                swipeHasDirection = false;
                IsCharging        = true;
                ChargePower       = 0f;
            }

            // Holding: update direction and power from swipe delta
            if (IsCharging && (inputHeld || inputBegan))
            {
                Vector2 delta = currentScreen - swipeStartScreen;
                float   dist  = delta.magnitude;

                if (dist >= minSwipePixels)
                {
                    swipeHasDirection = true;
                    AimAngle   = Mathf.Atan2(delta.y, delta.x);
                    ChargePower = Mathf.Clamp01((dist - minSwipePixels)
                                               / (maxSwipePixels - minSwipePixels));
                }
                else
                {
                    // Too small: keep previous aim angle, no power yet
                    ChargePower = 0f;
                }
            }

            // Released: fire if swipe was meaningful
            if (inputEnded && IsCharging)
            {
                if (swipeHasDirection && ChargePower > 0.05f)
                    OnFireRequested?.Invoke(AimAngle, ChargePower);

                IsCharging        = false;
                ChargePower       = 0f;
                swipeHasDirection = false;
            }
        }

        // ── Weapon hotkeys ────────────────────────────────────────

        private void HandleWeaponKeys()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) OnWeaponSelected?.Invoke(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) OnWeaponSelected?.Invoke(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) OnWeaponSelected?.Invoke(2);
        }
    }
}
