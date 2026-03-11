using UnityEngine;

namespace CatsVsCapybaras
{
    /// <summary>
    /// ScriptableObject containing the definition and configuration of a weapon.
    /// Used to create weapon instances with consistent data across the game.
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon_", menuName = "Cats vs Capybaras/Weapon Definition")]
    public class WeaponData : ScriptableObject
    {
        /// <summary>
        /// Display name of the weapon.
        /// </summary>
        [SerializeField]
        public string weaponName = "Unnamed Weapon";

        /// <summary>
        /// Detailed description of the weapon's behavior and characteristics.
        /// </summary>
        [SerializeField]
        [TextArea(2, 4)]
        public string description = "";

        /// <summary>
        /// Icon sprite used in UI to represent this weapon.
        /// </summary>
        [SerializeField]
        public Sprite icon;

        /// <summary>
        /// Prefab of the projectile to instantiate when this weapon is fired.
        /// Must have a component inheriting from ProjectileBase.
        /// </summary>
        [SerializeField]
        public GameObject projectilePrefab;

        /// <summary>
        /// Base damage dealt at the explosion center.
        /// </summary>
        [SerializeField]
        public int damage = 30;

        /// <summary>
        /// Radius of the explosion damage and terrain destruction zone.
        /// </summary>
        [SerializeField]
        public float explosionRadius = 2f;

        /// <summary>
        /// Maximum power multiplier that can be applied to this weapon.
        /// </summary>
        [SerializeField]
        public float maxPower = 100f;

        /// <summary>
        /// Minimum power multiplier that can be applied to this weapon.
        /// </summary>
        [SerializeField]
        public float minPower = 10f;

        /// <summary>
        /// Number of ammo available for this weapon. -1 means infinite ammo.
        /// </summary>
        [SerializeField]
        public int ammoCount = -1;
    }

    /// <summary>
    /// Carrot projectile: A straight-shot weapon that explodes on first contact.
    /// Ideal for direct targeting without arc considerations.
    /// </summary>
    public class CarrotProjectile : ProjectileBase
    {
        /// <summary>
        /// Damage dealt by this projectile.
        /// </summary>
        [SerializeField]
        private int damage = 32;

        /// <summary>
        /// Radius of the explosion zone.
        /// </summary>
        [SerializeField]
        private float explosionRadius = 1.9f;

        /// <summary>
        /// Called when the projectile impacts an object.
        /// The carrot explodes immediately on first contact.
        /// </summary>
        protected override void OnImpact(Vector2 position)
        {
            Explode(position, explosionRadius, damage);
        }
    }

    /// <summary>
    /// Bomb projectile: A bouncing weapon with a timed fuse.
    /// Bounces up to 3 times before exploding, or detonates after 3 seconds.
    /// </summary>
    public class BombProjectile : ProjectileBase
    {
        /// <summary>
        /// Damage dealt by the explosion.
        /// </summary>
        [SerializeField]
        private int damage = 55;

        /// <summary>
        /// Radius of the explosion zone.
        /// </summary>
        [SerializeField]
        private float explosionRadius = 3.25f;

        /// <summary>
        /// Maximum number of bounces before forced explosion.
        /// </summary>
        [SerializeField]
        private int maxBounces = 3;

        /// <summary>
        /// Fuse timer duration in seconds.
        /// Bomb explodes automatically after this time regardless of bounces.
        /// </summary>
        [SerializeField]
        private float fuseTimer = 3f;

        private int bounceCount = 0;
        private float fuseElapsedTime = 0f;

        /// <summary>
        /// Initializes the bomb by starting the fuse timer.
        /// </summary>
        private void Start()
        {
            // Physics material with bounciness is expected on the Rigidbody2D
            // Set up in inspector with a PhysicsMaterial2D that has bounciness ~0.8
        }

        /// <summary>
        /// Updates the fuse timer each frame.
        /// Bomb explodes when the timer expires.
        /// </summary>
        private void Update()
        {
            if (bounceCount < maxBounces)
            {
                fuseElapsedTime += Time.deltaTime;
                if (fuseElapsedTime >= fuseTimer)
                {
                    Explode(transform.position, explosionRadius, damage);
                }
            }
        }

        /// <summary>
        /// Called when the projectile impacts an object.
        /// Decrements bounce count and explodes if bounces are exhausted.
        /// </summary>
        protected override void OnImpact(Vector2 position)
        {
            bounceCount++;

            // Explode if out of bounces
            if (bounceCount >= maxBounces)
            {
                Explode(position, explosionRadius, damage);
            }
        }
    }

    /// <summary>
    /// Banana projectile: An arcing weapon that explodes on first contact.
    /// Features a visual spin effect and slightly modified gravity for arc trajectory.
    /// Future enhancement: cluster splitting into mini-bananas.
    /// </summary>
    public class BananaProjectile : ProjectileBase
    {
        /// <summary>
        /// Damage dealt by the explosion.
        /// </summary>
        [SerializeField]
        private int damage = 38;

        /// <summary>
        /// Radius of the explosion zone.
        /// </summary>
        [SerializeField]
        private float explosionRadius = 2.4f;

        /// <summary>
        /// Rotation speed for the spinning visual effect (degrees per second).
        /// </summary>
        [SerializeField]
        private float spinSpeed = 360f;

        /// <summary>
        /// Gravity scale modifier to create a more pronounced arc.
        /// Values > 1 will drop faster, < 1 will arc higher.
        /// </summary>
        [SerializeField]
        private float gravityScaleModifier = 1f;

        /// <summary>
        /// TODO: Future enhancement for clustered bananas.
        /// When enabled, this projectile will split into 3 mini-bananas on impact.
        /// </summary>
        private bool clusterEnabled = false;

        private Rigidbody2D rigidBody;

        private void Awake()
        {
            rigidBody = GetComponent<Rigidbody2D>();

            // Apply gravity scale modifier for arcing behavior
            if (rigidBody != null && gravityScaleModifier > 0f)
            {
                rigidBody.gravityScale = gravityScaleModifier;
            }
        }

        private void Update()
        {
            // Apply spinning visual effect
            if (spinSpeed > 0f)
            {
                transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// Called when the projectile impacts an object.
        /// The banana explodes immediately on first contact.
        /// </summary>
        protected override void OnImpact(Vector2 position)
        {
            // TODO: Implement cluster splitting if clusterEnabled is true
            // Would instantiate 3 mini-banana projectiles at the impact point
            // with reduced damage and spreads out in a cone pattern.

            Explode(position, explosionRadius, damage);
        }
    }
}
