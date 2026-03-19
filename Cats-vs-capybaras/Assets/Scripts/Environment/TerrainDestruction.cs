using UnityEngine;

namespace CatsVsCapybaras
{
    /// <summary>
    /// Pixel-level destructible terrain system.
    /// Clones the sprite texture at runtime, clears pixels in destruction zones,
    /// and rebuilds the PolygonCollider2D from the modified sprite.
    ///
    /// Setup:
    ///   1. Attach to a GameObject with SpriteRenderer + PolygonCollider2D
    ///   2. Assign a terrain sprite (PNG with alpha for empty areas)
    ///   3. Sprite must have Read/Write enabled in import settings
    ///   4. Set layer to Terrain
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(PolygonCollider2D))]
    public class TerrainDestruction : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer terrainRenderer;
        [SerializeField] private PolygonCollider2D terrainCollider;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("Alpha threshold for a pixel to count as solid")]
        private float solidThreshold = 0.1f;

        private Texture2D terrainTexture;
        private Texture2D originalTexture;
        private Sprite terrainSprite;
        private int textureWidth;
        private int textureHeight;
        private float pixelsPerUnit;
        private bool colliderDirty;

        private void Awake()
        {
            if (terrainRenderer == null) terrainRenderer = GetComponent<SpriteRenderer>();
            if (terrainCollider == null) terrainCollider = GetComponent<PolygonCollider2D>();

            if (terrainRenderer.sprite == null)
            {
                Debug.LogError("[TerrainDestruction] SpriteRenderer has no sprite assigned.", this);
                return;
            }

            terrainSprite = terrainRenderer.sprite;
            pixelsPerUnit = terrainSprite.pixelsPerUnit;
            CloneTexture();
        }

        private void LateUpdate()
        {
            if (colliderDirty)
            {
                RebuildColliderInternal();
                colliderDirty = false;
            }
        }

        private void CloneTexture()
        {
            Texture2D source = terrainSprite.texture;
            textureWidth = source.width;
            textureHeight = source.height;

            terrainTexture = new Texture2D(textureWidth, textureHeight, source.format, false);
            terrainTexture.filterMode = source.filterMode;
            Graphics.CopyTexture(source, terrainTexture);

            originalTexture = new Texture2D(textureWidth, textureHeight, source.format, false);
            Graphics.CopyTexture(source, originalTexture);

            ApplyTextureToSprite();
        }

        /// <summary>
        /// Destroys a circular area of terrain at the given world position.
        /// Collider is rebuilt automatically next LateUpdate.
        /// </summary>
        public void DestroyCircle(Vector2 worldPosition, float radius)
        {
            Vector2Int center = WorldToPixel(worldPosition);
            int radiusPx = Mathf.CeilToInt(radius * pixelsPerUnit);

            int minX = Mathf.Max(0, center.x - radiusPx);
            int maxX = Mathf.Min(textureWidth - 1, center.x + radiusPx);
            int minY = Mathf.Max(0, center.y - radiusPx);
            int maxY = Mathf.Min(textureHeight - 1, center.y + radiusPx);

            Color[] pixels = terrainTexture.GetPixels(minX, minY, maxX - minX + 1, maxY - minY + 1);
            int blockWidth = maxX - minX + 1;
            float radiusSq = radiusPx * radiusPx;
            bool modified = false;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - center.x;
                    float dy = y - center.y;
                    if (dx * dx + dy * dy <= radiusSq)
                    {
                        int idx = (y - minY) * blockWidth + (x - minX);
                        if (pixels[idx].a > 0f)
                        {
                            pixels[idx] = Color.clear;
                            modified = true;
                        }
                    }
                }
            }

            if (modified)
            {
                terrainTexture.SetPixels(minX, minY, blockWidth, maxY - minY + 1, pixels);
                terrainTexture.Apply();
                colliderDirty = true;
            }
        }

        /// <summary>
        /// Checks whether a world position has solid terrain.
        /// </summary>
        public bool IsSolid(Vector2 worldPosition)
        {
            Vector2Int px = WorldToPixel(worldPosition);
            if (px.x < 0 || px.x >= textureWidth || px.y < 0 || px.y >= textureHeight)
                return false;

            return terrainTexture.GetPixel(px.x, px.y).a > solidThreshold;
        }

        /// <summary>
        /// Returns the highest solid Y coordinate at a given world X, or null if none.
        /// </summary>
        public float? GetTerrainHeightAt(float worldX)
        {
            Vector2Int px = WorldToPixel(new Vector2(worldX, 0));
            if (px.x < 0 || px.x >= textureWidth) return null;

            for (int y = textureHeight - 1; y >= 0; y--)
            {
                if (terrainTexture.GetPixel(px.x, y).a > solidThreshold)
                    return PixelToWorld(new Vector2Int(px.x, y)).y;
            }
            return null;
        }

        /// <summary>
        /// Resets terrain to its original unmodified state.
        /// </summary>
        public void ResetTerrain()
        {
            if (originalTexture == null) return;
            Graphics.CopyTexture(originalTexture, terrainTexture);
            terrainTexture.Apply();
            colliderDirty = true;
        }

        private void RebuildColliderInternal()
        {
            ApplyTextureToSprite();

            // Recompute the collider path by scanning the updated texture for surface heights.
            // We do NOT destroy/recreate the component — AddComponent at runtime creates a
            // blank collider, not one traced from the sprite. SetPath in-place is correct.
            BuildColliderPath();
        }

        /// <summary>
        /// Scans the live terrain texture column by column to find the highest opaque pixel
        /// (the surface), then writes a closed polygon path into the PolygonCollider2D.
        /// Called once after every explosion so craters create real holes characters fall into.
        /// </summary>
        private void BuildColliderPath()
        {
            if (terrainCollider == null) return;

            const int stepPx = 12;   // sample every N px; keeps vertices under Unity's 252 limit

            // One GetPixels32 call is far cheaper than thousands of GetPixel calls.
            Color32[] allPixels = terrainTexture.GetPixels32();
            byte alphaCutoff    = (byte)(solidThreshold * 255f);

            int W = textureWidth;
            int H = textureHeight;

            float invPpu      = 1f / pixelsPerUnit;
            float localLeft   = -W * 0.5f * invPpu;
            float localBottom = -H * 0.5f * invPpu;

            int sampleCount = Mathf.CeilToInt((float)W / stepPx) + 1;
            var path        = new Vector2[sampleCount + 2];

            path[0] = new Vector2(localLeft, localBottom);   // bottom-left

            for (int i = 0; i < sampleCount; i++)
            {
                int px = Mathf.Min(i * stepPx, W - 1);

                // Scan from the top of this column downward to find the surface pixel.
                int surfacePy = 0;
                for (int py = H - 1; py >= 0; py--)
                {
                    if (allPixels[py * W + px].a > alphaCutoff)
                    {
                        surfacePy = py;
                        break;
                    }
                }

                float localX = (px   - W * 0.5f) * invPpu;
                float localY = (surfacePy - H * 0.5f) * invPpu;
                path[i + 1] = new Vector2(localX, localY);
            }

            path[sampleCount + 1] = new Vector2(-localLeft, localBottom);  // bottom-right

            terrainCollider.pathCount = 1;
            terrainCollider.SetPath(0, path);
        }

        private void ApplyTextureToSprite()
        {
            Sprite newSprite = Sprite.Create(
                terrainTexture,
                new Rect(0, 0, textureWidth, textureHeight),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit
            );
            terrainRenderer.sprite = newSprite;
        }

        private Vector2Int WorldToPixel(Vector2 worldPos)
        {
            Bounds bounds = terrainRenderer.sprite.bounds;
            Vector2 localPos = worldPos - (Vector2)(transform.position + (Vector3)bounds.min);
            return new Vector2Int(
                Mathf.RoundToInt(localPos.x * pixelsPerUnit),
                Mathf.RoundToInt(localPos.y * pixelsPerUnit)
            );
        }

        private Vector2 PixelToWorld(Vector2Int pixelPos)
        {
            Bounds bounds = terrainRenderer.sprite.bounds;
            Vector2 localPos = new Vector2(pixelPos.x / pixelsPerUnit, pixelPos.y / pixelsPerUnit);
            return (Vector2)(transform.position + (Vector3)bounds.min) + localPos;
        }

        private void OnDrawGizmosSelected()
        {
            if (terrainRenderer != null && terrainRenderer.sprite != null)
            {
                Bounds bounds = terrainRenderer.sprite.bounds;
                Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
                Gizmos.DrawWireCube(transform.position + (Vector3)bounds.center, bounds.size);
            }
        }
    }
}
