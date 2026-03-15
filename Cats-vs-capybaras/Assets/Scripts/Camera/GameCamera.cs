using System;
using UnityEngine;

namespace CatsVsCapybaras
{
    /// <summary>
    /// Camera controller for 2D turn-based combat.
    /// Supports smooth follow (character/projectile), pan-to-target with easing,
    /// hold-position for explosions, and screen shake.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class GameCamera : MonoBehaviour
    {
        public enum CameraMode { FollowTarget, HoldPosition, PanToTarget }

        public event Action OnPanComplete;

        [Header("Follow")]
        [SerializeField] private float characterFollowDamping = 0.15f;
        [SerializeField] private float projectileFollowDamping = 0.05f;
        [SerializeField] private Vector2 followOffset = new Vector2(0f, 1f);

        [Header("Pan")]
        [SerializeField] private float defaultPanDuration = 1f;

        [Header("Shake")]
        [SerializeField] private float defaultShakeMagnitude = 0.2f;
        [SerializeField] private float defaultShakeDuration = 0.3f;

        [Header("Bounds (auto-detected from terrain at startup)")]
        [SerializeField] private float worldMinX =  0f;
        [SerializeField] private float worldMaxX = 28.14f;
        [SerializeField] private float worldMinY = -3f;
        [SerializeField] private float worldMaxY = 10f;

        [Header("Zoom")]
        [Tooltip("How many world-units wide the viewport should show regardless of device/orientation.")]
        [SerializeField] private float targetVisibleWidth = 14.22f;
        [Tooltip("Fallback ortho size used only if aspect ratio is unavailable.")]
        [SerializeField] private float defaultOrthoSize = 4f;

        private Camera cam;
        private int lastScreenW;
        private int lastScreenH;
        private CameraMode currentMode = CameraMode.HoldPosition;
        private Transform followTarget;
        private float currentDamping;

        private Vector2 panStart;
        private Vector2 panEnd;
        private float panDuration;
        private float panElapsed;

        private float shakeTimeRemaining;
        private float shakeMagnitude;
        private Vector2 shakeOffset;

        private float velX;
        private float velY;

        public CameraMode CurrentMode => currentMode;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            AutoDetectWorldBounds();
            RecalculateOrthoSize();
        }

        private void LateUpdate()
        {
            // Recalculate ortho size if screen dimensions changed
            // (simulator switch, device rotation, window resize in editor).
            if (Screen.width != lastScreenW || Screen.height != lastScreenH)
                RecalculateOrthoSize();

            switch (currentMode)
            {
                case CameraMode.FollowTarget:
                    UpdateFollow();
                    break;
                case CameraMode.PanToTarget:
                    UpdatePan();
                    break;
                case CameraMode.HoldPosition:
                    break;
            }

            ApplyShake();
            ClampToBounds();
        }

        public void FollowTarget(Transform target, float damping = -1f)
        {
            followTarget = target;
            currentDamping = damping > 0 ? damping : characterFollowDamping;
            currentMode = CameraMode.FollowTarget;
            velX = 0f;
            velY = 0f;
        }

        public void PanTo(Vector2 targetPosition, float duration = -1f)
        {
            panStart = transform.position;
            panEnd = targetPosition;
            panDuration = duration > 0 ? duration : defaultPanDuration;
            panElapsed = 0f;
            currentMode = CameraMode.PanToTarget;
        }

        public void HoldPosition()
        {
            currentMode = CameraMode.HoldPosition;
        }

        public void SnapTo(Vector2 position)
        {
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            velX = 0f;
            velY = 0f;
        }

        public void TriggerShake(float magnitude = -1f, float duration = -1f)
        {
            shakeMagnitude = magnitude > 0 ? magnitude : defaultShakeMagnitude;
            shakeTimeRemaining = duration > 0 ? duration : defaultShakeDuration;
        }

        private void UpdateFollow()
        {
            if (followTarget == null)
            {
                currentMode = CameraMode.HoldPosition;
                return;
            }

            Vector2 targetPos = (Vector2)followTarget.position + followOffset;
            Vector3 pos = transform.position;

            pos.x = Mathf.SmoothDamp(pos.x, targetPos.x, ref velX, currentDamping);
            pos.y = Mathf.SmoothDamp(pos.y, targetPos.y, ref velY, currentDamping);
            transform.position = pos;
        }

        private void UpdatePan()
        {
            panElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(panElapsed / panDuration);
            t = t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;

            Vector2 pos = Vector2.Lerp(panStart, panEnd, t);
            transform.position = new Vector3(pos.x, pos.y, transform.position.z);

            if (panElapsed >= panDuration)
            {
                currentMode = CameraMode.HoldPosition;
                OnPanComplete?.Invoke();
            }
        }

        private void ApplyShake()
        {
            // Undo previous shake offset
            transform.position -= (Vector3)shakeOffset;
            shakeOffset = Vector2.zero;

            if (shakeTimeRemaining <= 0f) return;

            shakeTimeRemaining -= Time.deltaTime;
            float intensity = Mathf.Lerp(0f, shakeMagnitude, shakeTimeRemaining / defaultShakeDuration);
            shakeOffset = UnityEngine.Random.insideUnitCircle * intensity;
            transform.position += (Vector3)shakeOffset;
        }

        /// <summary>
        /// Reads the terrain sprite's actual bounds so camera clamps match
        /// the real world geometry, regardless of where the object was placed.
        /// ProceduralTerrainGenerator runs at ExecutionOrder -100,
        /// so its sprite already exists by the time GameCamera.Awake() fires.
        /// </summary>
        private void AutoDetectWorldBounds()
        {
            var terrain = FindAnyObjectByType<ProceduralTerrainGenerator>();
            if (terrain == null) return;

            var sr = terrain.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) return;

            Bounds b = sr.bounds;
            worldMinX = b.min.x;
            worldMaxX = b.max.x;
            worldMinY = b.min.y;
            worldMaxY = b.max.y;

            Debug.Log($"[GameCamera] Auto-detected world bounds: X[{worldMinX:F2}..{worldMaxX:F2}] Y[{worldMinY:F2}..{worldMaxY:F2}]");
        }

        /// <summary>
        /// Sets orthographicSize so the viewport always shows <targetVisibleWidth> world-units
        /// horizontally, regardless of device or orientation.
        /// </summary>
        private void RecalculateOrthoSize()
        {
            lastScreenW = Screen.width;
            lastScreenH = Screen.height;

            float aspect = (lastScreenH > 0) ? (float)lastScreenW / lastScreenH : 0f;
            cam.orthographicSize = (aspect > 0f)
                ? (targetVisibleWidth * 0.5f) / aspect
                : defaultOrthoSize;
        }

        private void ClampToBounds()
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth  = halfHeight * cam.aspect;

            Vector3 pos = transform.position;

            // Camera centre must stay far enough from each world edge
            // so the viewport never reveals the void beyond the terrain.
            float leftLimit  = worldMinX + halfWidth;
            float rightLimit = worldMaxX - halfWidth;

            if (leftLimit > rightLimit)
                pos.x = (worldMinX + worldMaxX) * 0.5f;   // world narrower than viewport → centre
            else
                pos.x = Mathf.Clamp(pos.x, leftLimit, rightLimit);

            float bottomLimit = worldMinY + halfHeight;
            float topLimit    = worldMaxY - halfHeight;

            if (bottomLimit > topLimit)
                pos.y = (worldMinY + worldMaxY) * 0.5f;
            else
                pos.y = Mathf.Clamp(pos.y, bottomLimit, topLimit);

            transform.position = pos;
        }
    }
}
