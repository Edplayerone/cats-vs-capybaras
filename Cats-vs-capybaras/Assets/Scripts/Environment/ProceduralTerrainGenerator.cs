using UnityEngine;

namespace CatsVsCapybaras
{
    /// <summary>
    /// Procedurally generates the terrain texture at runtime and sets an accurate
    /// PolygonCollider2D path so characters walk along the actual terrain surface.
    ///
    /// Key insight: AddComponent<PolygonCollider2D>() at runtime creates a BLANK collider,
    /// not one auto-traced from the sprite. We must manually compute and set the path
    /// from the heights[] array we already have from texture generation.
    ///
    /// Run order -100 fires BEFORE TerrainDestruction, so the collider path is set
    /// correctly before any game logic queries it.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(SpriteRenderer))]
    public class ProceduralTerrainGenerator : MonoBehaviour
    {
        [Header("Canvas")]
        [SerializeField] private int   textureWidth  = 2814;
        [SerializeField] private int   textureHeight = 1536;
        [SerializeField] private float pixelsPerUnit = 100f;

        [Header("Terrain Profile")]
        [SerializeField, Range(0.40f, 0.75f)]
        private float baseHeightFraction = 0.58f;

        [Header("Canyon (separates teams)")]
        [SerializeField, Range(0f, 1f)]
        private float canyonCenterFraction    = 0.50f;
        [SerializeField, Range(0f, 0.3f)]
        private float canyonHalfWidthFraction = 0.107f;

        // ── Palette ──────────────────────────────────────────────────────────
        static readonly Color32 GrassLight  = new Color32( 98, 155,  48, 255);
        static readonly Color32 GrassDark   = new Color32( 72, 118,  30, 255);
        static readonly Color32 DirtTop     = new Color32(138,  96,  24, 255);
        static readonly Color32 DirtDeep    = new Color32( 55,  36,   8, 255);
        static readonly Color32 Transparent = new Color32(  0,   0,   0,   0);

        /// <summary>Step size (pixels) between collider path samples.
        /// Keeps total vertices under Unity's 252-per-path limit.</summary>
        private const int ColliderStepPx = 12;   // 2814/12 ≈ 235 points + 2 corners = 237

        private void Awake() => Generate();

        [ContextMenu("Generate Terrain Now")]
        public void Generate()
        {
            var sr = GetComponent<SpriteRenderer>();
            sr.enabled = true;

            int W = textureWidth;
            int H = textureHeight;

            // ── 1. Compute per-column terrain heights ─────────────────────────
            int baseH    = Mathf.RoundToInt(H * baseHeightFraction);
            int canyonCx = Mathf.RoundToInt(W * canyonCenterFraction);
            int canyonHW = Mathf.RoundToInt(W * canyonHalfWidthFraction);

            int[] heights = new int[W];
            for (int x = 0; x < W; x++)
            {
                float h = baseH;
                h += Mathf.Sin(x * 0.005f)         * 44f;
                h += Mathf.Sin(x * 0.014f + 1.20f) * 24f;
                h += Mathf.Sin(x * 0.033f + 0.70f) * 12f;
                h += Mathf.Sin(x * 0.078f + 2.00f) *  6f;

                int dist = Mathf.Abs(x - canyonCx);
                if (dist < canyonHW)
                {
                    float t    = (float)dist / canyonHW;
                    float wall = Mathf.Pow(t, 0.65f);
                    h = h * wall;
                }

                heights[x] = Mathf.Max(0, Mathf.RoundToInt(h));
            }

            // ── 2. Fill pixel array ───────────────────────────────────────────
            Color32[] pixels = new Color32[W * H];
            for (int x = 0; x < W; x++)
            {
                int gndY   = heights[x];
                int grassH = Mathf.Max(4, gndY / 55);

                for (int y = 0; y < H; y++)
                {
                    int idx = y * W + x;
                    if (y > gndY)
                    {
                        pixels[idx] = Transparent;
                    }
                    else if (y > gndY - grassH)
                    {
                        float t = 1f - (float)(gndY - y) / grassH;
                        pixels[idx] = Color32.Lerp(GrassDark, GrassLight, t);
                    }
                    else
                    {
                        float depth = (gndY > 0) ? (float)(gndY - y) / gndY : 1f;
                        pixels[idx] = Color32.Lerp(DirtTop, DirtDeep, Mathf.Sqrt(depth));
                    }
                }
            }

            // ── 3. Build Texture2D + Sprite ───────────────────────────────────
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode   = TextureWrapMode.Clamp
            };
            tex.SetPixels32(pixels);
            tex.Apply();

            sr.sprite = Sprite.Create(tex,
                new Rect(0, 0, W, H),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);

            // ── 4. Set PolygonCollider2D path from heights[] ──────────────────
            // We already have exact per-column heights from step 1 — use them directly
            // rather than trying to re-scan the texture. Sampled every ColliderStepPx
            // pixels to stay under Unity's 252-vertex-per-path limit.
            var polyCol = GetComponent<PolygonCollider2D>();
            if (polyCol != null)
            {
                polyCol.isTrigger = false;
                polyCol.pathCount = 1;
                polyCol.SetPath(0, BuildPathFromHeights(heights, W, H, pixelsPerUnit));
                Debug.Log("[ProceduralTerrain] Collider path set from height data.");
            }

            // ── 5. Disable the flat GroundCollider ────────────────────────────
            var groundCol = GameObject.Find("GroundCollider");
            if (groundCol != null)
            {
                groundCol.SetActive(false);
                Debug.Log("[ProceduralTerrain] GroundCollider disabled — terrain collider is solid.");
            }
        }

        /// <summary>
        /// Builds a PolygonCollider2D path in the GameObject's local space from
        /// a per-column height array. The polygon traces the terrain surface left→right,
        /// then closes along the bottom of the texture.
        /// </summary>
        public static Vector2[] BuildPathFromHeights(int[] heights, int W, int H, float ppu)
        {
            float invPpu      = 1f / ppu;
            float localLeft   = -W * 0.5f * invPpu;
            float localBottom = -H * 0.5f * invPpu;

            // Sample count (left-to-right surface points)
            int sampleCount = Mathf.CeilToInt((float)W / ColliderStepPx) + 1;

            // path = [bottom-left] + [surface points L→R] + [bottom-right]
            var path = new Vector2[sampleCount + 2];

            path[0] = new Vector2(localLeft, localBottom);  // bottom-left corner

            for (int i = 0; i < sampleCount; i++)
            {
                int px = Mathf.Min(i * ColliderStepPx, W - 1);
                float localX = (px - W * 0.5f) * invPpu;
                float localY = (heights[px] - H * 0.5f) * invPpu;
                path[i + 1] = new Vector2(localX, localY);
            }

            path[sampleCount + 1] = new Vector2(-localLeft, localBottom);  // bottom-right corner

            return path;
        }
    }
}
