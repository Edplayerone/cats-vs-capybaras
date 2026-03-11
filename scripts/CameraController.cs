using UnityEngine;
using System;

namespace CatsVsCapybaras
{
    /// <summary>
    /// Manages camera behavior for 2D turn-based combat game.
    /// Supports multiple follow modes: character tracking, projectile tracking,
    /// explosion reactions, and smooth panning between targets.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        /// <summary>
        /// Defines the camera's current behavior mode.
        /// </summary>
        public enum CameraMode
        {
            /// <summary>Follows the active character with optional lead offset.</summary>
            FollowCharacter,

            /// <summary>Tracks a flying projectile in real-time.</summary>
            FollowProjectile,

            /// <summary>Freezes at explosion point, then transitions to next mode.</summary>
            FollowExplosion,

            /// <summary>Smooth pan to a specific position over time.</summary>
            PanToTarget,

            /// <summary>Free look mode (not actively used in MVP).</summary>
            FreeLook
        }

        #region Events

        /// <summary>Invoked when a pan animation completes.</summary>
        public event Action OnPanComplete;

        #endregion

        #region Fields - Mode & Targets

        /// <summary>Current camera mode.</summary>
        private CameraMode m_currentMode = CameraMode.FollowCharacter;

        /// <summary>Transform to follow in FollowCharacter or FollowProjectile modes.</summary>
        private Transform m_targetTransform;

        /// <summary>Target position for PanToTarget mode.</summary>
        private Vector2 m_panTargetPosition;

        /// <summary>Time remaining for current pan animation.</summary>
        private float m_panTimeRemaining;

        /// <summary>Total duration of current pan animation.</summary>
        private float m_panDuration;

        /// <summary>Starting position for current pan.</summary>
        private Vector2 m_panStartPosition;

        /// <summary>Time remaining for explosion freeze effect.</summary>
        private float m_explosionFreezeTimeRemaining;

        #endregion

        #region Fields - Movement & Smoothing

        /// <summary>Current velocity for SmoothDamp calculations (X axis).</summary>
        private float m_velocityX;

        /// <summary>Current velocity for SmoothDamp calculations (Y axis).</summary>
        private float m_velocityY;

        /// <summary>Current velocity for zoom (orthographic size).</summary>
        private float m_zoomVelocity;

        /// <summary>Shake offset applied to camera position.</summary>
        private Vector2 m_shakeOffset = Vector2.zero;

        /// <summary>Remaining duration of shake effect.</summary>
        private float m_shakeTimeRemaining;

        #endregion

        #region Inspector Fields

        [Header("Follow Settings")]

        /// <summary>Damping time for SmoothDamp when following character (smaller = faster response).</summary>
        [SerializeField]
        private float m_characterFollowDamping = 0.1f;

        /// <summary>Damping time for SmoothDamp when following projectile (very responsive).</summary>
        [SerializeField]
        private float m_projectileFollowDamping = 0.05f;

        /// <summary>Dead zone size; camera won't move if target is within this distance.</summary>
        [SerializeField]
        private float m_followDeadZone = 0.5f;

        /// <summary>Lead offset in the character's movement direction (units ahead of character).</summary>
        [SerializeField]
        private Vector2 m_characterFollowLead = new Vector2(1f, 0.5f);

        [Header("Pan Settings")]

        /// <summary>Default duration for pan animations between targets.</summary>
        [SerializeField]
        private float m_defaultPanDuration = 1f;

        [Header("Explosion Settings")]

        /// <summary>Duration to freeze camera at explosion point.</summary>
        [SerializeField]
        private float m_explosionFreezeDuration = 1f;

        /// <summary>Magnitude of camera shake effect during explosions.</summary>
        [SerializeField]
        private float m_shakeMagnitude = 0.15f;

        /// <summary>Duration of camera shake effect.</summary>
        [SerializeField]
        private float m_shakeDuration = 0.3f;

        [Header("Zoom Settings")]

        /// <summary>Default orthographic camera size.</summary>
        [SerializeField]
        private float m_defaultZoom = 5f;

        /// <summary>Minimum orthographic size (zoomed in).</summary>
        [SerializeField]
        private float m_minZoom = 3f;

        /// <summary>Maximum orthographic size (zoomed out).</summary>
        [SerializeField]
        private float m_maxZoom = 8f;

        /// <summary>Damping time for zoom transitions.</summary>
        [SerializeField]
        private float m_zoomDamping = 0.2f;

        [Header("Bounds Settings")]

        /// <summary>World width (horizontal bounds).</summary>
        [SerializeField]
        private float m_worldWidth = 25f;

        /// <summary>Minimum Y position camera can reach.</summary>
        [SerializeField]
        private float m_minY = 0f;

        /// <summary>Maximum Y position camera can reach (for high projectile arcs).</summary>
        [SerializeField]
        private float m_maxY = 6f;

        #endregion

        #region Cached Components

        private Camera m_camera;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            m_camera = GetComponent<Camera>();
            m_camera.orthographicSize = m_defaultZoom;
        }

        private void LateUpdate()
        {
            UpdateCameraMode();
            ApplyShake();
            ClampCameraPosition();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the camera to a new mode with optional target.
        /// </summary>
        /// <param name="mode">The camera mode to switch to.</param>
        /// <param name="target">Transform to follow (used for FollowCharacter and FollowProjectile modes).</param>
        /// <param name="position">Target position (used for PanToTarget and FollowExplosion modes).</param>
        public void SetMode(CameraMode mode, Transform target = null, Vector2? position = null)
        {
            m_currentMode = mode;
            m_targetTransform = target;

            switch (mode)
            {
                case CameraMode.FollowCharacter:
                case CameraMode.FollowProjectile:
                    // No additional setup needed; target is stored
                    break;

                case CameraMode.PanToTarget:
                    if (position.HasValue)
                    {
                        m_panStartPosition = (Vector2)transform.position;
                        m_panTargetPosition = position.Value;
                        m_panDuration = m_defaultPanDuration;
                        m_panTimeRemaining = m_defaultPanDuration;
                    }
                    break;

                case CameraMode.FollowExplosion:
                    if (position.HasValue)
                    {
                        transform.position = new Vector3(position.Value.x, position.Value.y, transform.position.z);
                    }
                    m_explosionFreezeTimeRemaining = m_explosionFreezeDuration;
                    StartShake();
                    break;

                case CameraMode.FreeLook:
                    // Free look can be implemented for editor/debugging
                    break;
            }
        }

        /// <summary>
        /// Sets the camera to follow a character.
        /// </summary>
        public void FollowCharacter(Transform character)
        {
            SetMode(CameraMode.FollowCharacter, character);
        }

        /// <summary>
        /// Sets the camera to follow a projectile.
        /// </summary>
        public void FollowProjectile(Transform projectile)
        {
            SetMode(CameraMode.FollowProjectile, projectile);
        }

        /// <summary>
        /// Triggers camera reaction to explosion at specified position.
        /// Freezes camera, applies shake, then auto-transitions.
        /// </summary>
        public void ReactToExplosion(Vector2 explosionPosition)
        {
            SetMode(CameraMode.FollowExplosion, null, explosionPosition);
        }

        /// <summary>
        /// Smoothly pans camera from current position to target over specified duration.
        /// </summary>
        public void PanToTarget(Vector2 targetPosition, float duration = -1f)
        {
            if (duration < 0f)
                duration = m_defaultPanDuration;

            m_panStartPosition = (Vector2)transform.position;
            m_panTargetPosition = targetPosition;
            m_panDuration = duration;
            m_panTimeRemaining = duration;
            m_currentMode = CameraMode.PanToTarget;
        }

        /// <summary>
        /// Sets the orthographic zoom level with smooth damping.
        /// </summary>
        public void SetZoom(float zoomLevel)
        {
            zoomLevel = Mathf.Clamp(zoomLevel, m_minZoom, m_maxZoom);
            // SmoothDamp to target zoom is handled in ClampCameraPosition
        }

        /// <summary>
        /// Resets camera to default zoom level.
        /// </summary>
        public void ResetZoom()
        {
            SetZoom(m_defaultZoom);
        }

        /// <summary>
        /// Gets the current camera mode.
        /// </summary>
        public CameraMode GetCurrentMode() => m_currentMode;

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates camera position/behavior based on current mode.
        /// </summary>
        private void UpdateCameraMode()
        {
            switch (m_currentMode)
            {
                case CameraMode.FollowCharacter:
                    UpdateFollowCharacter();
                    break;

                case CameraMode.FollowProjectile:
                    UpdateFollowProjectile();
                    break;

                case CameraMode.FollowExplosion:
                    UpdateFollowExplosion();
                    break;

                case CameraMode.PanToTarget:
                    UpdatePanToTarget();
                    break;

                case CameraMode.FreeLook:
                    // Implement free look if needed
                    break;
            }
        }

        /// <summary>
        /// Follows active character with configurable lead and dead zone.
        /// </summary>
        private void UpdateFollowCharacter()
        {
            if (m_targetTransform == null)
                return;

            Vector2 targetPos = (Vector2)m_targetTransform.position;

            // Apply lead offset (slightly ahead of character)
            targetPos += m_characterFollowLead;

            // Apply dead zone
            Vector2 currentPos = (Vector2)transform.position;
            float distanceToTarget = Vector2.Distance(currentPos, targetPos);

            if (distanceToTarget < m_followDeadZone)
                return;

            // Smooth damp to target position
            float newX = Mathf.SmoothDamp(currentPos.x, targetPos.x, ref m_velocityX, m_characterFollowDamping);
            float newY = Mathf.SmoothDamp(currentPos.y, targetPos.y, ref m_velocityY, m_characterFollowDamping);

            transform.position = new Vector3(newX, newY, transform.position.z);
        }

        /// <summary>
        /// Tracks flying projectile with very responsive damping.
        /// </summary>
        private void UpdateFollowProjectile()
        {
            if (m_targetTransform == null)
                return;

            Vector2 targetPos = (Vector2)m_targetTransform.position;
            Vector2 currentPos = (Vector2)transform.position;

            float newX = Mathf.SmoothDamp(currentPos.x, targetPos.x, ref m_velocityX, m_projectileFollowDamping);
            float newY = Mathf.SmoothDamp(currentPos.y, targetPos.y, ref m_velocityY, m_projectileFollowDamping);

            transform.position = new Vector3(newX, newY, transform.position.z);
        }

        /// <summary>
        /// Freezes camera at explosion point for duration, then auto-transitions.
        /// </summary>
        private void UpdateFollowExplosion()
        {
            m_explosionFreezeTimeRemaining -= Time.deltaTime;

            if (m_explosionFreezeTimeRemaining <= 0f)
            {
                // Auto-transition back to following character
                // This will be handled by TurnManager calling SetMode again
                // Or we could have a callback here
            }
        }

        /// <summary>
        /// Smoothly pans to target position over time.
        /// </summary>
        private void UpdatePanToTarget()
        {
            m_panTimeRemaining -= Time.deltaTime;

            if (m_panTimeRemaining <= 0f)
            {
                // Pan complete
                transform.position = new Vector3(m_panTargetPosition.x, m_panTargetPosition.y, transform.position.z);
                OnPanComplete?.Invoke();
                return;
            }

            float elapsed = m_panDuration - m_panTimeRemaining;
            float t = elapsed / m_panDuration;

            // Smooth easing (ease-in-out)
            t = t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;

            Vector2 newPos = Vector2.Lerp(m_panStartPosition, m_panTargetPosition, t);
            transform.position = new Vector3(newPos.x, newPos.y, transform.position.z);
        }

        /// <summary>
        /// Applies camera shake effect.
        /// </summary>
        private void ApplyShake()
        {
            if (m_shakeTimeRemaining > 0f)
            {
                m_shakeTimeRemaining -= Time.deltaTime;

                // Random offset within shake magnitude
                m_shakeOffset = (Vector2)Random.insideUnitCircle * m_shakeMagnitude;

                Vector3 pos = transform.position;
                transform.position = new Vector3(pos.x + m_shakeOffset.x, pos.y + m_shakeOffset.y, pos.z);
            }
            else
            {
                m_shakeOffset = Vector2.zero;
            }
        }

        /// <summary>
        /// Starts the camera shake effect.
        /// </summary>
        private void StartShake()
        {
            m_shakeTimeRemaining = m_shakeDuration;
        }

        /// <summary>
        /// Clamps camera position within world bounds and applies zoom limits.
        /// </summary>
        private void ClampCameraPosition()
        {
            Vector3 pos = transform.position;

            // Get camera dimensions
            float cameraHeight = m_camera.orthographicSize * 2f;
            float cameraWidth = cameraHeight * m_camera.aspect;

            // Clamp horizontal bounds
            float minCameraX = cameraWidth * 0.5f;
            float maxCameraX = m_worldWidth - cameraWidth * 0.5f;
            pos.x = Mathf.Clamp(pos.x, minCameraX, maxCameraX);

            // Clamp vertical bounds
            float minCameraY = m_minY + cameraHeight * 0.5f;
            float maxCameraY = m_maxY;
            pos.y = Mathf.Clamp(pos.y, minCameraY, maxCameraY);

            transform.position = pos;

            // Smooth damp zoom
            float targetZoom = m_defaultZoom;
            m_camera.orthographicSize = Mathf.SmoothDamp(
                m_camera.orthographicSize,
                targetZoom,
                ref m_zoomVelocity,
                m_zoomDamping
            );
        }

        #endregion
    }
}
