using UnityEngine;

namespace CatsVsCapybaras
{
    /// <summary>
    /// Carrot: direct-hit weapon. Explodes immediately on first contact.
    /// Good for precise aiming against exposed targets.
    /// </summary>
    public class CarrotProjectile : ProjectileBase
    {
        protected override void HandleImpact(Vector2 contactPoint)
        {
            Explode(contactPoint);
        }
    }

    /// <summary>
    /// Bomb: bouncing weapon with a fuse timer.
    /// Bounces off terrain/characters up to maxBounces times, then detonates.
    /// Also detonates automatically when fuseTime expires.
    /// Requires a PhysicsMaterial2D with bounciness ~0.6 on the Rigidbody2D.
    /// </summary>
    public class BombProjectile : ProjectileBase
    {
        [Header("Bomb Settings")]
        [SerializeField] private int maxBounces = 3;
        [SerializeField] private float fuseTime = 3f;

        private int bounceCount;
        private float fuseElapsed;
        private bool fuseStarted;

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!HasLaunched || HasResolved) return;

            if (!fuseStarted) return;
            fuseElapsed += Time.fixedDeltaTime;
            if (fuseElapsed >= fuseTime)
                Explode(transform.position);
        }

        protected override void OnCollisionEnter2D(Collision2D collision)
        {
            if (!HasLaunched || HasResolved) return;

            fuseStarted = true;
            bounceCount++;

            if (bounceCount >= maxBounces)
            {
                Explode(collision.GetContact(0).point);
                return;
            }

            // Let physics handle the bounce via PhysicsMaterial2D
        }

        protected override void HandleImpact(Vector2 contactPoint)
        {
            // Handled in OnCollisionEnter2D for bounce logic
        }
    }

    /// <summary>
    /// Banana: arcing weapon with a visual spin. Explodes on first contact.
    /// Slightly modified gravity for a more pronounced arc.
    /// Future: cluster split into mini-bananas on impact.
    /// </summary>
    public class BananaProjectile : ProjectileBase
    {
        [Header("Banana Settings")]
        [SerializeField] private float spinSpeed = 540f;
        [SerializeField] private float gravityMultiplier = 1.2f;

        protected override void Awake()
        {
            base.Awake();
            if (rb != null)
                rb.gravityScale = gravityMultiplier;
        }

        private void Update()
        {
            if (HasLaunched && !HasResolved)
                transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
        }

        protected override void HandleImpact(Vector2 contactPoint)
        {
            Explode(contactPoint);
        }
    }
}
