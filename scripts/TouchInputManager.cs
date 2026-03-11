using UnityEngine;
using UnityEngine.Events;
using System;

namespace CatsVsCapybaras
{
    /// <summary>
    /// Manages all touch input for iOS gameplay including virtual D-pad movement,
    /// drag-to-aim aiming mechanics, and weapon selection events.
    /// Handles both Touch (iOS) and Mouse input (Editor testing).
    /// </summary>
    public class TouchInputManager : MonoBehaviour
    {
        #region Events

        /// <summary>
        /// Fired when fire/shoot is requested. Provides angle (radians) and power (0-1 normalized).
        /// </summary>
        public event Action<float, float> OnFireRequested;

        /// <summary>
        /// Fired during move phase with horizontal movement input (-1 to 1).
        /// </summary>
        public event Action<float> OnMoveInput;

        /// <summary>
        /// Fired when a weapon is selected via UI button.
        /// </summary>
        public event Action<int> OnWeaponSelected;

        #endregion

        #region Input State Properties

        /// <summary>
        /// Current aim angle in radians. Updated during AimPhase while dragging.
        /// </summary>
        [field: SerializeField]
        public float AimAngle { get; private set; } = 0f;

        /// <summary>
        /// Current aim power as normalized value (0-1). Updated during AimPhase while dragging.
        /// </summary>
        [field: SerializeField]
        public float AimPower { get; private set; } = 0f;

        /// <summary>
        /// Current horizontal movement input (-1 to 1). Only valid during MovePhase.
        /// </summary>
        [field: SerializeField]
        public float MoveInput { get; private set; } = 0f;

        /// <summary>
        /// True when actively aiming (finger/mouse held down in aim zone).
        /// </summary>
        [field: SerializeField]
        public bool IsAiming { get; private set; } = false;

        #endregion

        #region Configuration

        [SerializeField]
        [Tooltip("Fraction of screen width (0-1) that constitutes the D-pad zone on the left side")]
        private float dpadZoneWidth = 0.33f;

        [SerializeField]
        [Tooltip("Minimum drag distance in pixels before aim is registered")]
        private float aimDeadZone = 30f;

        [SerializeField]
        [Tooltip("Maximum drag distance in pixels before power reaches 1.0")]
        private float maxAimDragDistance = 250f;

        [SerializeField]
        [Tooltip("Minimum drag distance to register valid aim (in pixels)")]
        private float minAimDragDistance = 20f;

        #endregion

        #region Private Fields

        private bool isDuringMovePhase = false;
        private bool isDuringAimPhase = false;

        private Vector2 dpadTouchStartPos = Vector2.zero;
        private int dpadTouchId = -1;
        private bool isDpadActive = false;

        private Vector2 aimTouchStartPos = Vector2.zero;
        private int aimTouchId = -1;
        private Vector2 characterAimOrigin = Vector2.zero;

        private bool isMouseAiming = false;

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            // Could subscribe to GameManager or TurnManager events here
        }

        private void OnDisable()
        {
            // Unsubscribe from events
        }

        private void Update()
        {
            if (isDuringMovePhase)
            {
                HandleMoveInput();
            }

            if (isDuringAimPhase)
            {
                HandleAimInput();
            }
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Called by GameManager/TurnManager when entering MovePhase.
        /// </summary>
        public void EnableMovePhase()
        {
            isDuringMovePhase = true;
            isDuringAimPhase = false;
            MoveInput = 0f;
        }

        /// <summary>
        /// Called by GameManager/TurnManager when entering AimPhase.
        /// Requires the world position of the active character for slingshot calculations.
        /// </summary>
        public void EnableAimPhase(Vector3 characterWorldPos)
        {
            isDuringMovePhase = false;
            isDuringAimPhase = true;
            characterAimOrigin = characterWorldPos;
            AimAngle = 0f;
            AimPower = 0f;
            IsAiming = false;
        }

        /// <summary>
        /// Called by GameManager/TurnManager when exiting input phases (e.g., FirePhase).
        /// </summary>
        public void DisableInput()
        {
            isDuringMovePhase = false;
            isDuringAimPhase = false;
            IsAiming = false;
            MoveInput = 0f;
            aimTouchId = -1;
            dpadTouchId = -1;
        }

        /// <summary>
        /// Returns the current aim indicator data (used by UI to draw aim line).
        /// </summary>
        public (Vector3 startPos, Vector3 direction, float power) GetAimIndicatorData()
        {
            Vector3 direction = new Vector3(Mathf.Cos(AimAngle), Mathf.Sin(AimAngle), 0f);
            return (characterAimOrigin, direction, AimPower);
        }

        #endregion

        #region Move Phase Input

        private void HandleMoveInput()
        {
            MoveInput = 0f;

            // Handle touch input
            if (Input.touchCount > 0)
            {
                foreach (Touch touch in Input.touches)
                {
                    if (IsPointInDpadZone(touch.position))
                    {
                        if (touch.phase == TouchPhase.Began)
                        {
                            dpadTouchId = touch.fingerId;
                            dpadTouchStartPos = touch.position;
                            isDpadActive = true;
                        }

                        if (touch.fingerId == dpadTouchId)
                        {
                            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                            {
                                float dragX = touch.position.x - dpadTouchStartPos.x;
                                float dpadWidth = Screen.width * dpadZoneWidth;
                                MoveInput = Mathf.Clamp01(dragX / (dpadWidth * 0.5f));
                                MoveInput = Mathf.Clamp(MoveInput, -1f, 1f);
                            }

                            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                            {
                                dpadTouchId = -1;
                                isDpadActive = false;
                                MoveInput = 0f;
                            }
                        }
                    }
                }
            }
            // Handle mouse input for editor testing
            else if (Input.GetMouseButton(0))
            {
                Vector2 mousePos = Input.mousePosition;
                if (IsPointInDpadZone(mousePos))
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        dpadTouchStartPos = mousePos;
                    }

                    float dragX = mousePos.x - dpadTouchStartPos.x;
                    float dpadWidth = Screen.width * dpadZoneWidth;
                    MoveInput = Mathf.Clamp(dragX / (dpadWidth * 0.5f), -1f, 1f);
                }
            }
            else
            {
                MoveInput = 0f;
            }

            OnMoveInput?.Invoke(MoveInput);
        }

        private bool IsPointInDpadZone(Vector2 screenPos)
        {
            float dpadWidth = Screen.width * dpadZoneWidth;
            return screenPos.x < dpadWidth;
        }

        #endregion

        #region Aim Phase Input

        private void HandleAimInput()
        {
            // Handle touch input
            if (Input.touchCount > 0)
            {
                foreach (Touch touch in Input.touches)
                {
                    if (!IsPointInDpadZone(touch.position))
                    {
                        if (touch.phase == TouchPhase.Began)
                        {
                            aimTouchId = touch.fingerId;
                            aimTouchStartPos = touch.position;
                            IsAiming = true;
                        }

                        if (touch.fingerId == aimTouchId)
                        {
                            if (touch.phase == TouchPhase.Moved)
                            {
                                UpdateAimFromDrag(touch.position);
                            }

                            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                            {
                                if (IsAiming && AimPower > 0f)
                                {
                                    OnFireRequested?.Invoke(AimAngle, AimPower);
                                }

                                aimTouchId = -1;
                                IsAiming = false;
                                AimAngle = 0f;
                                AimPower = 0f;
                            }
                        }
                    }
                }
            }
            // Handle mouse input for editor testing
            else if (Input.GetMouseButton(0))
            {
                Vector2 mousePos = Input.mousePosition;

                if (!IsPointInDpadZone(mousePos))
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        aimTouchStartPos = mousePos;
                        isMouseAiming = true;
                        IsAiming = true;
                    }

                    if (isMouseAiming)
                    {
                        UpdateAimFromDrag(mousePos);
                    }
                }
            }
            else if (Input.GetMouseButtonUp(0) && isMouseAiming)
            {
                if (IsAiming && AimPower > 0f)
                {
                    OnFireRequested?.Invoke(AimAngle, AimPower);
                }

                isMouseAiming = false;
                IsAiming = false;
                AimAngle = 0f;
                AimPower = 0f;
            }
        }

        private void UpdateAimFromDrag(Vector2 currentTouchPos)
        {
            Vector2 dragVector = currentTouchPos - aimTouchStartPos;
            float dragDistance = dragVector.magnitude;

            // Apply dead zone
            if (dragDistance < aimDeadZone)
            {
                AimPower = 0f;
                AimAngle = 0f;
                return;
            }

            // Calculate aim angle (drag away from character = opposite direction fired)
            // Angle is negated to invert slingshot feel
            float rawAngle = Mathf.Atan2(dragVector.y, dragVector.x);
            AimAngle = rawAngle + Mathf.PI; // Invert for slingshot effect

            // Clamp angle to valid firing range (typically -90 to 90 degrees from horizontal)
            AimAngle = Mathf.Clamp(AimAngle, 0f, Mathf.PI);

            // Calculate power based on drag distance
            float normalizedDistance = Mathf.Clamp01(dragDistance / maxAimDragDistance);
            AimPower = normalizedDistance;
        }

        #endregion

        #region Weapon Selection

        /// <summary>
        /// Called by weapon selection UI buttons.
        /// </summary>
        public void SelectWeapon(int weaponIndex)
        {
            OnWeaponSelected?.Invoke(weaponIndex);
        }

        #endregion
    }
}
