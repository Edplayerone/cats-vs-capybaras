using UnityEngine;
using System;

namespace CatsVsCapybaras
{
    /// <summary>
    /// Abstract base class for all projectile types in the turn-based combat system.
    /// Handles physics-based movement, wind effects, collision detection, and explosion mechanics.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class ProjectileBase : MonoBehaviour
    {
        #region Fields

        /// <summary>
        /// Reference to the character that fired this projectile.
        /// Used to prevent self-damage on launch and identify ownership.
        /// </summary>
        [SerializeField]
        private Character owner;

        /// <summary>
        /// Maximum lifetime of the projectile in seconds. Auto-destroys after this duration.
        /// </summary>
        [SerializeField]
        private float maxLifetime = 10f;

        /// <summary>
        /// Reference to the trail renderer for visual feedback during flight.
        /// Can be null if trail is not desired.
        /// </summary>
        [SerializeField]
        private TrailRenderer trailRenderer;

        /// <summary>
        /// Reference to the terrain destruction system.
        /// Used to create circular destruction zones on impact/explosion.
        /// </summary>
        [SerializeField]
        private TerrainDestruction terrainDestruction;

        /// <summary>
        /// Prefab reference for explosion VFX instantiation.
        /// Spawned at explosion position for visual feedback.
        /// </summary>
        [SerializeField]
        private GameObject explosionVFXPrefab;

        /// <summary>
        /// Reference to the TurnManager for signaling when projectile has resolved.
        /// </summary>
        [SerializeField]
        private TurnManager turnManager;

        /// <summary>
        /// Reference to the main camera for tracking projectile in flight.
        /// </summary>
        [SerializeField]
        private Camera mainCamera;

        private Rigidbody2D rigidBody;
        private float elapsedTime = 0f;
        private float currentWindForce = 0f;
        private bool hasLaunched = false;
        private bool hasResolved = false;

        #endregion

        #region Events

        /// <summary>
        /// Invoked when the projectile is launched.
        /// Passes the launch direction and power applied.
        /// </summary>
        public event Action<Vector2, float> OnLaunched;

        /// <summary>
        /// Invoked when the projectile impacts something (without explosion).
        /// Passes the impact position.
        /// </summary>
        public event Action<Vector2> OnImpact;

        /// <summary>
        /// Invoked when the projectile explodes.
        /// Passes the explosion position and radius.
        /// </summary>
        public event Action<Vector2, float> OnExploded;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            rigidBody = GetComponent<Rigidbody2D>();

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        private void Start()
        {
            if (trailRenderer != null)
            {
                trailRenderer.Clear();
            }
        }

        private void FixedUpdate()
        {
            if (!hasLaunched || hasResolved)
                return;

            // Apply wind force horizontally
            ApplyWindForce();

            // Track elapsed time for auto-destruction
            elapsedTime += Time.fixedDeltaTime;
            if (elapsedTime >= maxLifetime)
            {
                ResolveProjectile(transform.position);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!hasLaunched || hasResolved)
                return;

            // Don't damage the owner on launch
            Character hitCharacter = collision.gameObject.GetComponent<Character>();
            if (hitCharacter == owner)
                return;

            HandleCollision(collision.GetContact(0).point);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Launches the projectile with the specified direction, power, and wind force.
        /// This method must be called to activate the projectile.
        /// </summary>
        /// <param name="direction">Normalized direction vector for launch angle</param>
        /// <param name="power">Launch force multiplier (typically 0-100)</param>
        /// <param name="windForce">Horizontal wind force to apply continuously</param>
        public void Launch(Vector2 direction, float power, float windForce = 0f)
        {
            if (hasLaunched)
            {
                Debug.LogWarning($"Projectile {gameObject.name} is already launched!", gameObject);
                return;
            }

            hasLaunched = true;
            currentWindForce = windForce;

            // Apply initial velocity
            Vector2 launchVelocity = direction.normalized * power;
            rigidBody.velocity = launchVelocity;

            // Notify listeners
            OnLaunched?.Invoke(direction, power);

            // Enable trail if present
            if (trailRenderer != null)
            {
                trailRenderer.enabled = true;
            }
        }

        /// <summary>
        /// Triggers an explosion at the specified position.
        /// Damages characters in radius, destroys terrain, and spawns VFX.
        /// </summary>
        /// <param name="position">World position of the explosion center</param>
        /// <param name="radius">Radius of the explosion damage/destruction zone</param>
        /// <param name="damage">Base damage dealt at explosion center</param>
        public void Explode(Vector2 position, float radius, int damage)
        {
            if (hasResolved)
                return;

            // Spawn explosion VFX
            if (explosionVFXPrefab != null)
            {
                Instantiate(explosionVFXPrefab, position, Quaternion.identity);
            }

            // Damage characters in radius with falloff
            DamageCharactersInRadius(position, radius, damage);

            // Destroy terrain in radius
            if (terrainDestruction != null)
            {
                terrainDestruction.DestroyCircle(position, radius);
            }

            // Notify listeners
            OnExploded?.Invoke(position, radius);

            // Mark as resolved and signal turn manager
            ResolveProjectile(position);
        }

        /// <summary>
        /// Called by subclasses to set the owner character.
        /// </summary>
        public void SetOwner(Character ownerCharacter)
        {
            owner = ownerCharacter;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Abstract method for subclasses to define impact behavior.
        /// Called when the projectile collides with something.
        /// </summary>
        /// <param name="position">World position of the impact</param>
        protected abstract void OnImpact(Vector2 position);

        /// <summary>
        /// Called when the projectile collides with something.
        /// Invokes OnImpact event and calls the abstract OnImpact method.
        /// </summary>
        protected virtual void HandleCollision(Vector2 contactPoint)
        {
            OnImpact?.Invoke(contactPoint);
            OnImpact(contactPoint);
        }

        /// <summary>
        /// Applies the current wind force as horizontal acceleration.
        /// Called every FixedUpdate while the projectile is in flight.
        /// </summary>
        protected virtual void ApplyWindForce()
        {
            if (Mathf.Abs(currentWindForce) > 0.01f)
            {
                rigidBody.velocity += new Vector2(currentWindForce * Time.fixedDeltaTime, 0f);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Finds all characters in the explosion radius and applies damage with falloff.
        /// Damage decreases linearly from center to edge of radius.
        /// </summary>
        private void DamageCharactersInRadius(Vector2 position, float radius, int damage)
        {
            // Physics2D.OverlapCircleAll would be used here with actual Character array filtering
            Collider2D[] colliders = Physics2D.OverlapCircleAll(position, radius);

            foreach (Collider2D collider in colliders)
            {
                Character character = collider.GetComponent<Character>();
                if (character != null && character != owner)
                {
                    float distanceToCharacter = Vector2.Distance(position, character.transform.position);
                    float falloffFactor = Mathf.Max(0f, 1f - (distanceToCharacter / radius));
                    int damageDealt = Mathf.RoundToInt(damage * falloffFactor);

                    character.TakeDamage(damageDealt, owner);
                }
            }
        }

        /// <summary>
        /// Resolves the projectile, preventing further updates and signaling the turn manager.
        /// </summary>
        private void ResolveProjectile(Vector2 finalPosition)
        {
            if (hasResolved)
                return;

            hasResolved = true;

            // Disable trail
            if (trailRenderer != null)
            {
                trailRenderer.enabled = false;
            }

            // Signal turn manager that this projectile is done
            if (turnManager != null)
            {
                turnManager.ProjectileResolved(this);
            }

            // Destroy the projectile after a short delay for animation/effects
            Destroy(gameObject, 0.5f);
        }

        #endregion
    }
}
