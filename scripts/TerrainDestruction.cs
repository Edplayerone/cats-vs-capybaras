using UnityEngine;
using System.Collections.Generic;

namespace CatsVsCapybaras
{
    /// <summary>
    /// Handles destructible terrain for the Cats vs Capybaras game.
    /// Manages pixel-level terrain destruction and dynamically rebuilds the physics collider.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(PolygonCollider2D))]
    public class TerrainDestruction : MonoBehaviour
    {
        #region Fields

        [SerializeField]
        private SpriteRenderer terrainRenderer;

        [SerializeField]
        private PolygonCollider2D terrainCollider;

        /// <summary>
        /// Threshold for determining if a pixel is solid (0-1 alpha value).
        /// </summary>
        [SerializeField]
        [Range(0f, 1f)]
        private float solidThreshold = 0.5f;

        /// <summary>
        /// When true, collider will be rebuilt on next opportunity.
        /// Allows batching multiple destruction calls before expensive rebuild.
        /// </summary>
        private bool colliderDirty = false;

        /// <summary>
        /// Working texture for modifications. Cloned from the original sprite texture.
        /// </summary>
        private Texture2D terrainTexture;

        /// <summary>
        /// Original texture dimensions in pixels.
        /// </summary>
        private int textureWidth;
        private int textureHeight;

        /// <summary>
        /// Original texture, stored for round reset functionality.
        /// </summary>
        private Texture2D originalTexture;

        /// <summary>
        /// Cached reference to the Sprite component for coordinate conversions.
        /// </summary>
        private Sprite terrainSprite;

        /// <summary>
        /// Last destruction point for debug visualization.
        /// </summary>
        private Vector2 lastDestructionPoint;

        /// <summary>
        /// Last destruction radius for debug visualization.
        /// </summary>
        private float lastDestructionRadius;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
            CloneTerrainTexture();
        }

        private void OnDrawGizmos()
        {
            // Visualize the last destruction point and radius
            if (Application.isPlaying && lastDestructionRadius > 0)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
                Gizmos.DrawWireSphere(lastDestructionPoint, lastDestructionRadius);
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes references to required components.
        /// </summary>
        private void InitializeComponents()
        {
            if (terrainRenderer == null)
                terrainRenderer = GetComponent<SpriteRenderer>();

            if (terrainCollider == null)
                terrainCollider = GetComponent<PolygonCollider2D>();

            if (terrainRenderer == null || terrainRenderer.sprite == null)
            {
                Debug.LogError("TerrainDestruction requires a SpriteRenderer with a sprite assigned.", gameObject);
                return;
            }

            terrainSprite = terrainRenderer.sprite;
        }

        /// <summary>
        /// Clones the terrain texture from the sprite so modifications don't affect the asset.
        /// </summary>
        private void CloneTerrainTexture()
        {
            Texture2D sourceTexture = terrainSprite.texture;

            textureWidth = sourceTexture.width;
            textureHeight = sourceTexture.height;

            // Create a read-write copy of the texture
            terrainTexture = new Texture2D(textureWidth, textureHeight, sourceTexture.format, false);
            terrainTexture.name = sourceTexture.name + "_Destructible";

            // Copy pixels from source
            Graphics.CopyTexture(sourceTexture, terrainTexture);

            // Store original for potential reset
            originalTexture = new Texture2D(textureWidth, textureHeight, sourceTexture.format, false);
            Graphics.CopyTexture(sourceTexture, originalTexture);

            // Apply the cloned texture to the renderer
            terrainRenderer.sprite = Sprite.Create(
                terrainTexture,
                terrainSprite.rect,
                terrainSprite.pivot,
                terrainSprite.pixelsPerUnit
            );
        }

        #endregion

        #region Public Destruction Methods

        /// <summary>
        /// Destroys terrain in a circular area, setting pixels to transparent.
        /// </summary>
        /// <param name="worldPosition">Center of the destruction circle in world space.</param>
        /// <param name="radius">Radius of the destruction area in world units.</param>
        public void DestroyCircle(Vector2 worldPosition, float radius)
        {
            Vector2Int centerPixel = WorldToPixel(worldPosition);
            float radiusPixels = radius * terrainSprite.pixelsPerUnit;

            Color[] pixels = terrainTexture.GetPixels();
            int pixelsModified = 0;

            // Iterate through the bounding box of the circle
            int minX = Mathf.Max(0, centerPixel.x - Mathf.CeilToInt(radiusPixels));
            int maxX = Mathf.Min(textureWidth - 1, centerPixel.x + Mathf.CeilToInt(radiusPixels));
            int minY = Mathf.Max(0, centerPixel.y - Mathf.CeilToInt(radiusPixels));
            int maxY = Mathf.Min(textureHeight - 1, centerPixel.y + Mathf.CeilToInt(radiusPixels));

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    float distanceSquared = (x - centerPixel.x) * (x - centerPixel.x) +
                                           (y - centerPixel.y) * (y - centerPixel.y);
                    float radiusSquared = radiusPixels * radiusPixels;

                    if (distanceSquared <= radiusSquared)
                    {
                        int pixelIndex = y * textureWidth + x;
                        pixels[pixelIndex] = Color.clear;
                        pixelsModified++;
                    }
                }
            }

            if (pixelsModified > 0)
            {
                terrainTexture.SetPixels(pixels);
                terrainTexture.Apply();
                colliderDirty = true;

                lastDestructionPoint = worldPosition;
                lastDestructionRadius = radius;
            }
        }

        /// <summary>
        /// Destroys terrain along a line with a specified width (for tunneling weapons).
        /// </summary>
        /// <param name="start">Start point in world space.</param>
        /// <param name="end">End point in world space.</param>
        /// <param name="width">Width of the destruction line in world units.</param>
        public void DestroyLine(Vector2 start, Vector2 end, float width)
        {
            Vector2Int startPixel = WorldToPixel(start);
            Vector2Int endPixel = WorldToPixel(end);
            float widthPixels = width * terrainSprite.pixelsPerUnit;

            Color[] pixels = terrainTexture.GetPixels();
            int pixelsModified = 0;

            // Use Bresenham-style line drawing
            Vector2 direction = (endPixel - (Vector2)startPixel).normalized;
            int steps = Mathf.Max(
                Mathf.Abs(endPixel.x - startPixel.x),
                Mathf.Abs(endPixel.y - startPixel.y)
            );

            for (int step = 0; step <= steps; step++)
            {
                Vector2 currentPoint = Vector2.Lerp(startPixel, endPixel, steps > 0 ? step / (float)steps : 0);

                // Destroy circle at this point along the line
                int cx = Mathf.RoundToInt(currentPoint.x);
                int cy = Mathf.RoundToInt(currentPoint.y);

                int minX = Mathf.Max(0, Mathf.RoundToInt(cx - widthPixels));
                int maxX = Mathf.Min(textureWidth - 1, Mathf.RoundToInt(cx + widthPixels));
                int minY = Mathf.Max(0, Mathf.RoundToInt(cy - widthPixels));
                int maxY = Mathf.Min(textureHeight - 1, Mathf.RoundToInt(cy + widthPixels));

                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        float distanceSquared = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                        float radiusSquared = widthPixels * widthPixels;

                        if (distanceSquared <= radiusSquared)
                        {
                            int pixelIndex = y * textureWidth + x;
                            if (pixels[pixelIndex].a > 0)
                            {
                                pixels[pixelIndex] = Color.clear;
                                pixelsModified++;
                            }
                        }
                    }
                }
            }

            if (pixelsModified > 0)
            {
                terrainTexture.SetPixels(pixels);
                terrainTexture.Apply();
                colliderDirty = true;

                lastDestructionPoint = (start + end) * 0.5f;
                lastDestructionRadius = width;
            }
        }

        /// <summary>
        /// Marks collider as dirty so it will be rebuilt before next physics update.
        /// Call this after batching multiple destruction operations.
        /// </summary>
        public void FinalizeDestructions()
        {
            if (colliderDirty)
            {
                RebuildCollider();
                colliderDirty = false;
            }
        }

        #endregion

        #region Collider Rebuilding

        /// <summary>
        /// Rebuilds the PolygonCollider2D based on the current terrain texture.
        /// Uses Unity's built-in sprite physics shape generation.
        /// </summary>
        private void RebuildCollider()
        {
            if (terrainCollider == null)
                return;

            try
            {
                // Method 1: Recreate sprite with physics shape from texture
                // This uses Unity's built-in physics shape generation
                Sprite newSprite = Sprite.Create(
                    terrainTexture,
                    terrainSprite.rect,
                    terrainSprite.pivot,
                    terrainSprite.pixelsPerUnit
                );

                terrainRenderer.sprite = newSprite;

                // Method 2 (Fallback): Remove and re-add collider to force auto-generation
                // Unity will generate collider paths from the sprite's alpha channel
                bool wasEnabled = terrainCollider.enabled;
                terrainCollider.enabled = false;

                // Clear existing paths
                terrainCollider.pathCount = 0;

                terrainCollider.enabled = wasEnabled;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Error rebuilding collider: " + ex.Message, gameObject);

                // Ultimate fallback: Simple box collider reconstruction
                SimplicityRebuildFallback();
            }
        }

        /// <summary>
        /// Fallback collider rebuild using a simple approach.
        /// Removes and re-adds the component to force Unity's auto-generation.
        /// </summary>
        private void SimplicityRebuildFallback()
        {
            try
            {
                PolygonCollider2D oldCollider = terrainCollider;
                Vector2 offset = oldCollider.offset;
                bool wasEnabled = oldCollider.enabled;

                DestroyImmediate(oldCollider);

                PolygonCollider2D newCollider = gameObject.AddComponent<PolygonCollider2D>();
                newCollider.offset = offset;
                newCollider.enabled = wasEnabled;

                terrainCollider = newCollider;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Fallback collider rebuild failed: " + ex.Message, gameObject);
            }
        }

        #endregion

        #region Terrain Query Methods

        /// <summary>
        /// Checks if a world position has solid terrain.
        /// </summary>
        /// <param name="worldPosition">Position in world space.</param>
        /// <returns>True if the pixel at this position is solid (alpha above threshold).</returns>
        public bool IsPixelSolid(Vector2 worldPosition)
        {
            Vector2Int pixelPos = WorldToPixel(worldPosition);

            if (pixelPos.x < 0 || pixelPos.x >= textureWidth ||
                pixelPos.y < 0 || pixelPos.y >= textureHeight)
            {
                return false;
            }

            Color pixel = terrainTexture.GetPixel(pixelPos.x, pixelPos.y);
            return pixel.a > solidThreshold;
        }

        /// <summary>
        /// Gets the terrain height at a specific world X position.
        /// Returns the topmost solid pixel in that column, or null if no solid pixels exist.
        /// </summary>
        /// <param name="worldX">X position in world space.</param>
        /// <returns>Y position in world space, or null if no solid terrain found.</returns>
        public float? GetTerrainHeightAt(float worldX)
        {
            Vector2Int pixelPos = WorldToPixel(new Vector2(worldX, 0));

            if (pixelPos.x < 0 || pixelPos.x >= textureWidth)
                return null;

            // Scan from top to bottom to find first solid pixel
            for (int y = textureHeight - 1; y >= 0; y--)
            {
                Color pixel = terrainTexture.GetPixel(pixelPos.x, y);
                if (pixel.a > solidThreshold)
                {
                    return PixelToWorld(new Vector2Int(pixelPos.x, y)).y;
                }
            }

            return null;
        }

        #endregion

        #region Coordinate Conversion

        /// <summary>
        /// Converts a world space position to texture pixel coordinates.
        /// </summary>
        private Vector2Int WorldToPixel(Vector2 worldPos)
        {
            // Get the sprite's local bounds
            Bounds spriteBounds = terrainSprite.bounds;
            Vector3 spriteMin = transform.position + (Vector3)spriteBounds.min;

            // Convert world position relative to sprite origin
            Vector2 relativePos = worldPos - (Vector2)spriteMin;

            // Scale to texture coordinates
            float pixelsPerUnit = terrainSprite.pixelsPerUnit;
            int pixelX = Mathf.RoundToInt(relativePos.x * pixelsPerUnit);
            int pixelY = Mathf.RoundToInt(relativePos.y * pixelsPerUnit);

            return new Vector2Int(pixelX, pixelY);
        }

        /// <summary>
        /// Converts texture pixel coordinates to world space position.
        /// </summary>
        private Vector2 PixelToWorld(Vector2Int pixelPos)
        {
            Bounds spriteBounds = terrainSprite.bounds;
            Vector3 spriteMin = transform.position + (Vector3)spriteBounds.min;

            float pixelsPerUnit = terrainSprite.pixelsPerUnit;
            Vector2 relativePos = new Vector2(pixelPos.x / pixelsPerUnit, pixelPos.y / pixelsPerUnit);

            return (Vector2)spriteMin + relativePos;
        }

        #endregion

        #region Reset and Utilities

        /// <summary>
        /// Resets the terrain to its original state (for round reset).
        /// </summary>
        public void ResetTerrain()
        {
            if (originalTexture != null)
            {
                Graphics.CopyTexture(originalTexture, terrainTexture);
                terrainTexture.Apply();

                colliderDirty = true;
                FinalizeDestructions();
            }
        }

        /// <summary>
        /// Gets the current destruction state as a texture (for saving/loading).
        /// </summary>
        /// <returns>A copy of the current terrain texture.</returns>
        public Texture2D GetTerrainSnapshot()
        {
            Texture2D snapshot = new Texture2D(textureWidth, textureHeight, terrainTexture.format, false);
            Graphics.CopyTexture(terrainTexture, snapshot);
            return snapshot;
        }

        /// <summary>
        /// Gets the dimensions of the terrain texture.
        /// </summary>
        public Vector2Int GetTextureDimensions()
        {
            return new Vector2Int(textureWidth, textureHeight);
        }

        #endregion
    }
}
