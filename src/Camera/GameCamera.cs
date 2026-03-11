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

        [Header("Bounds")]
        [SerializeField] private float worldWidth = 25f;
        [SerializeField] private float worldMinY = -2f;
        [SerializeField] private float worldMaxY = 8f;

        [Header("Zoom")]
        [SerializeField] private float defaultOrthoSize = 5f;

        private Camera cam;
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
            cam.orthographicSize = defaultOrthoSize;
        }

        private void LateUpdate()
        {
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

        private void ClampToBounds()
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;

            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, halfWidth, worldWidth - halfWidth);
            pos.y = Mathf.Clamp(pos.y, worldMinY + halfHeight, worldMaxY);
            transform.position = pos;
        }
    }
}
