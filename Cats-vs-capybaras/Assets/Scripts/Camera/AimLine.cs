using UnityEngine;

namespace CatsVsCapybaras
{
    /// <summary>
    /// Draws a fading aim line from the active character during a swipe gesture.
    /// The line is ONLY visible while the player is actively swiping (IsCharging = true).
    /// Line length scales from 0 to maxLineLength based on ChargePower,
    /// so the player gets live feedback on how far the projectile will travel.
    /// Attach to an empty GameObject — a LineRenderer is added automatically.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class AimLine : MonoBehaviour
    {
        [SerializeField] private float maxLineLength = 5f;
        [SerializeField] private int   pointCount    = 16;
        [SerializeField] private Color nearColor     = new Color(1f, 1f, 0.3f, 0.9f);
        [SerializeField] private Color farColor      = new Color(1f, 0.6f, 0.1f, 0f);

        private LineRenderer        lr;
        private TurnManager         turnManager;
        private PlayerInputHandler  inputHandler;

        private void Awake()
        {
            lr = GetComponent<LineRenderer>();
            lr.positionCount = pointCount;
            lr.startWidth    = 0.09f;
            lr.endWidth      = 0.02f;
            lr.startColor    = nearColor;
            lr.endColor      = farColor;
            lr.useWorldSpace = true;
            lr.sortingOrder  = 10;
            lr.textureMode   = LineTextureMode.Tile;

            var mat = new Material(Shader.Find("Sprites/Default"));
            lr.material = mat;
        }

        private void LateUpdate()
        {
            if (turnManager  == null) turnManager  = FindAnyObjectByType<TurnManager>();
            if (inputHandler == null) inputHandler = FindAnyObjectByType<PlayerInputHandler>();

            // Only show while the player is actively swiping in the Action phase
            bool show = turnManager   != null
                     && inputHandler  != null
                     && turnManager.CurrentPhase == TurnManager.TurnPhase.Action
                     && turnManager.ActiveCharacter != null
                     && inputHandler.IsCharging
                     && inputHandler.ChargePower > 0f;

            lr.enabled = show;
            if (!show) return;

            Vector2 origin = (Vector2)turnManager.ActiveCharacter.transform.position
                           + Vector2.up * 0.5f;

            float   angle   = inputHandler.AimAngle;
            Vector2 dir     = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float   length  = maxLineLength * inputHandler.ChargePower;

            for (int i = 0; i < pointCount; i++)
            {
                float t = (float)i / (pointCount - 1);
                lr.SetPosition(i, origin + dir * (length * t));
            }

            // Fade colours toward transparent at the tip
            lr.startColor = nearColor;
            lr.endColor   = farColor;
        }
    }
}
