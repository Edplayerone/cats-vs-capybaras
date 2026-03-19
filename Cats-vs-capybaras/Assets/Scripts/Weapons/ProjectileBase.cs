using System;
using UnityEngine;

namespace CatsVsCapybaras
{
    /// <summary>
    /// Abstract base class for all projectiles. Handles launch physics, wind,
    /// collision detection, explosion damage/knockback, and terrain destruction.
    ///
    /// Subclasses override HandleImpact to define per-weapon collision behavior
    /// (e.g. explode on contact, bounce, fuse timer).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class ProjectileBase : MonoBehaviour
    {
        public event Action<ProjectileBase, Vector2> OnExploded;
        public event Action<ProjectileBase> OnResolved;

        [Header("Lifetime")]
        [SerializeField] private float maxLifetime = 10f;

        [Header("Bounds (auto-destroy if exceeded)")]
        [SerializeField] private Vector2 worldBoundsMin = new Vector2(-10f, -30f);
        [SerializeField] private Vector2 worldBoundsMax = new Vector2(35f, 40f);

        [Header("Visuals")]
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private GameObject explosionVFXPrefab;

        protected Rigidbody2D rb;

        private TerrainDestruction terrain;
        private CharacterController2D owner;
        private int damage;
        private float explosionRadius;
        private float windForce;
        private float elapsedTime;
        private bool launched;
        private bool resolved;

        public bool HasLaunched => launched;
        public bool HasResolved => resolved;
        public CharacterController2D Owner => owner;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            terrain = FindAnyObjectByType<TerrainDestruction>();
        }

        protected virtual void Start()
        {
            if (trailRenderer != null)
                trailRenderer.Clear();
        }

        protected virtual void FixedUpdate()
        {
            if (!launched || resolved) return;

            // Horizontal wind
            if (Mathf.Abs(windForce) > 0.001f)
                rb.AddForce(new Vector2(windForce, 0f));

            elapsedTime += Time.fixedDeltaTime;
            if (elapsedTime >= maxLifetime)
            {
                Resolve();
                return;
            }

            // Out-of-bounds safety net
            Vector2 pos = transform.position;
            if (pos.x < worldBoundsMin.x || pos.x > worldBoundsMax.x ||
                pos.y < worldBoundsMin.y || pos.y > worldBoundsMax.y)
            {
                Resolve();
            }
        }

        /// <summary>
        /// Called by CharacterController2D after instantiation to wire up ownership and weapon stats.
        /// </summary>
        public void Initialize(CharacterController2D projectileOwner, int weaponDamage, float weaponExplosionRadius)
        {
            owner = projectileOwner;
            damage = weaponDamage;
            explosionRadius = weaponExplosionRadius;
        }

        /// <summary>
        /// Launches the projectile with a direction, speed, and wind force.
        /// </summary>
        public void Launch(Vector2 direction, float power, float wind)
        {
            if (launched) return;

            launched = true;
            windForce = wind;
            rb.gravityScale = 1f;
            rb.linearVelocity = direction.normalized * power;

            if (trailRenderer != null)
                trailRenderer.enabled = true;

            // SFX: fly sound on launch
            SoundManager.Instance?.PlayCarrotFly();
        }

        protected virtual void OnCollisionEnter2D(Collision2D collision)
        {
            if (!launched || resolved) return;
            HandleImpact(collision.GetContact(0).point);
        }

        /// <summary>
        /// Subclass hook: define what happens when the projectile hits something.
        /// Call Explode() for immediate detonation or let it bounce/fuse.
        /// </summary>
        protected abstract void HandleImpact(Vector2 contactPoint);

        /// <summary>
        /// Triggers an explosion at the given position. Destroys terrain, damages
        /// all characters in radius with distance falloff, applies knockback, and spawns VFX.
        /// </summary>
        protected void Explode(Vector2 position)
        {
            if (resolved) return;

            // Terrain destruction
            if (terrain != null)
                terrain.DestroyCircle(position, explosionRadius);

            // Damage and knockback — affects all characters including the owner (self-damage like Worms)
            float effectRadius = explosionRadius * 1.8f;
            Collider2D[] hits = Physics2D.OverlapCircleAll(position, effectRadius);
            foreach (var hit in hits)
            {
                var character = hit.GetComponent<CharacterController2D>();
                if (character == null || !character.IsAlive) continue;

                float dist = Vector2.Distance(position, character.transform.position);
                float falloff = Mathf.Max(0f, 1f - dist / effectRadius);

                character.TakeDamage(Mathf.RoundToInt(damage * falloff));

                Vector2 knockDir = ((Vector2)character.transform.position - position).normalized;
                float knockStrength = falloff * 14f;
                character.ApplyKnockback(knockDir * knockStrength + Vector2.up * knockStrength * 0.5f);
            }

            // SFX: impact sound on explosion
            SoundManager.Instance?.PlayCarrotImpact();

            // VFX
            if (explosionVFXPrefab != null)
                Instantiate(explosionVFXPrefab, position, Quaternion.identity);

            OnExploded?.Invoke(this, position);
            Resolve();
        }

        protected void Resolve()
        {
            if (resolved) return;
            resolved = true;

            if (trailRenderer != null)
                trailRenderer.enabled = false;

            // Ensure fly sound stops even if projectile expired without hitting anything
            SoundManager.Instance?.StopCarrotFly();

            OnResolved?.Invoke(this);
            Destroy(gameObject, 0.1f);
        }
    }
}
