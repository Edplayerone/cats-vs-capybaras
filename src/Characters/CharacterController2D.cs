using System;
using UnityEngine;

namespace CatsVsCapybaras
{
    /// <summary>
    /// Core character controller for the turn-based combat game.
    /// Handles movement, health, ground detection, fall damage, knockback,
    /// weapon firing, and animation state. Input-agnostic: receives commands
    /// from TurnManager (which may come from player input or future AI).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class CharacterController2D : MonoBehaviour
    {
        public enum AnimState { Idle, Walking, Jumping, Falling, Hurt, Dead }

        public event Action<CharacterController2D, float> OnDamaged;
        public event Action<CharacterController2D> OnEliminated;
        public event Action<CharacterController2D> OnHealed;

        [Header("Identity")]
        [SerializeField] private string characterName = "Character";
        [SerializeField] private int teamIndex;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 3f;
        [SerializeField] private float jumpForce = 9f;

        [Header("Ground Detection")]
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private float groundCheckDistance = 0.15f;
        [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.5f);

        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;

        [Header("Fall Damage")]
        [SerializeField] private float fallDamageThreshold = 3f;
        [SerializeField] private float fallDamageMultiplier = 10f;

        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;

        private float currentHealth;
        private bool isEliminated;
        private bool isActive;
        private bool isGrounded;
        private float moveInput;
        private int facingDirection = 1;
        private float highestYSinceGrounded;
        private Vector2 spawnPosition;
        private AnimState currentAnimState;

        // -- Public properties --
        public string CharacterName => characterName;
        public int TeamIndex => teamIndex;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float HealthNormalized => maxHealth > 0 ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
        public bool IsAlive => !isEliminated && currentHealth > 0f;
        public bool IsEliminated => isEliminated;
        public bool IsActive => isActive;
        public bool IsGrounded => isGrounded;
        public int FacingDirection => facingDirection;
        public AnimState CurrentAnimState => currentAnimState;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            rb.freezeRotation = true;

            currentHealth = maxHealth;
            spawnPosition = transform.position;
            highestYSinceGrounded = transform.position.y;
        }

        private void FixedUpdate()
        {
            if (isEliminated) return;

            UpdateGrounded();
            ApplyMovement();
            TrackFallHeight();
        }

        private void LateUpdate()
        {
            UpdateSprite();
            UpdateAnimState();
        }

        // ── Input (called by TurnManager) ──────────────────────────

        public void SetMoveInput(float horizontal)
        {
            moveInput = Mathf.Clamp(horizontal, -1f, 1f);
        }

        public void Jump()
        {
            if (!isActive || !IsAlive || !isGrounded) return;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        public void ActivateForTurn()
        {
            isActive = true;
            moveInput = 0f;
            highestYSinceGrounded = transform.position.y;
        }

        public void DeactivateAfterTurn()
        {
            isActive = false;
            moveInput = 0f;
        }

        // ── Weapon ─────────────────────────────────────────────────

        /// <summary>
        /// Spawns and launches a projectile based on the given weapon data.
        /// Returns the spawned ProjectileBase, or null on failure.
        /// </summary>
        public ProjectileBase FireWeapon(WeaponData weapon, float aimAngle, float normalizedPower, float wind)
        {
            if (weapon == null || weapon.projectilePrefab == null) return null;

            Vector2 dir = new Vector2(Mathf.Cos(aimAngle), Mathf.Sin(aimAngle));
            Vector2 spawnPos = (Vector2)transform.position + Vector2.up * 0.5f + dir * 1.2f;

            var projObj = Instantiate(weapon.projectilePrefab, spawnPos, Quaternion.identity);
            var projectile = projObj.GetComponent<ProjectileBase>();

            if (projectile == null)
            {
                Debug.LogError($"[CharacterController2D] Weapon '{weapon.weaponName}' prefab missing ProjectileBase.", this);
                Destroy(projObj);
                return null;
            }

            float power = Mathf.Lerp(weapon.minPower, weapon.maxPower, normalizedPower);
            projectile.Initialize(this, weapon.damage, weapon.explosionRadius);
            projectile.Launch(dir, power, wind);

            if (dir.x != 0f) facingDirection = dir.x > 0 ? 1 : -1;

            return projectile;
        }

        // ── Health ─────────────────────────────────────────────────

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f) return;

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            OnDamaged?.Invoke(this, amount);

            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                isEliminated = true;
                OnEliminated?.Invoke(this);
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            OnHealed?.Invoke(this);
        }

        public void ApplyKnockback(Vector2 force)
        {
            if (!IsAlive) return;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(force, ForceMode2D.Impulse);
        }

        // ── Round Reset ────────────────────────────────────────────

        public void ResetForNewRound()
        {
            currentHealth = maxHealth;
            isEliminated = false;
            isActive = false;
            moveInput = 0f;
            transform.position = spawnPosition;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // ── Physics ────────────────────────────────────────────────

        private void UpdateGrounded()
        {
            Vector2 origin = (Vector2)transform.position + groundCheckOffset;
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayers);

            bool wasGrounded = isGrounded;
            isGrounded = hit.collider != null;

            // Landed: check fall damage
            if (isGrounded && !wasGrounded)
            {
                float fallDistance = highestYSinceGrounded - transform.position.y;
                if (fallDistance > fallDamageThreshold)
                {
                    float dmg = (fallDistance - fallDamageThreshold) * fallDamageMultiplier;
                    TakeDamage(dmg);
                }
                highestYSinceGrounded = transform.position.y;
            }
        }

        private void ApplyMovement()
        {
            if (!isActive || !IsAlive)
            {
                if (isGrounded && Mathf.Abs(rb.linearVelocity.x) > 0.05f)
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.85f, rb.linearVelocity.y);
                return;
            }

            if (isGrounded && Mathf.Abs(moveInput) > 0.01f)
            {
                rb.linearVelocity = new Vector2(moveInput * walkSpeed, rb.linearVelocity.y);
                facingDirection = moveInput > 0 ? 1 : -1;
            }
            else if (isGrounded && Mathf.Abs(moveInput) < 0.01f)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }

        private void TrackFallHeight()
        {
            if (!isGrounded && transform.position.y > highestYSinceGrounded)
                highestYSinceGrounded = transform.position.y;
        }

        // ── Visuals ────────────────────────────────────────────────

        private void UpdateSprite()
        {
            spriteRenderer.flipX = facingDirection < 0;
        }

        private void UpdateAnimState()
        {
            if (!IsAlive) { currentAnimState = AnimState.Dead; return; }
            if (!isGrounded && rb.linearVelocity.y > 0.5f) { currentAnimState = AnimState.Jumping; return; }
            if (!isGrounded) { currentAnimState = AnimState.Falling; return; }
            if (Mathf.Abs(rb.linearVelocity.x) > 0.1f) { currentAnimState = AnimState.Walking; return; }
            currentAnimState = AnimState.Idle;
        }

        // ── Gizmos ─────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            Vector2 origin = (Vector2)transform.position + groundCheckOffset;
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(origin, origin + Vector2.down * groundCheckDistance);
        }
    }
}
