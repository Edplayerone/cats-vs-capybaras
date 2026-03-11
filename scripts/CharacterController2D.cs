using UnityEngine;
using System;

namespace CatsVsCapybaras
{
    /// <summary>
    /// Manages a character's physics, movement, health, and animation states in the 2D turn-based combat game.
    /// Handles team assignment, fall damage, ground detection, and integrates with the turn management system.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class CharacterController2D : MonoBehaviour
    {
        #region Animation State
        /// <summary>
        /// Enumeration of animation states for the character.
        /// </summary>
        public enum AnimationState
        {
            Idle,
            Walking,
            Falling,
            Hurt,
            Dead
        }
        #endregion

        #region Events
        /// <summary>
        /// Fired when the character takes damage. Passes the damage amount.
        /// </summary>
        public event Action<float> OnDamaged;

        /// <summary>
        /// Fired when the character is healed. Passes the heal amount.
        /// </summary>
        public event Action<float> OnHealed;

        /// <summary>
        /// Fired when the character is eliminated (health <= 0).
        /// </summary>
        public event Action OnEliminated;

        /// <summary>
        /// Fired when the grounded state changes. Passes the new grounded state.
        /// </summary>
        public event Action<bool> OnGroundedChanged;
        #endregion

        #region Serialized Fields
        [Header("Team Configuration")]
        [SerializeField]
        [Tooltip("Team index: 0 = Cats, 1 = Capybaras")]
        private int teamIndex = 0;

        [SerializeField]
        [Tooltip("Character index within the team (0 or 1)")]
        private int characterIndex = 0;

        [SerializeField]
        [Tooltip("Display name for the character")]
        private string characterName = "Cat";

        [Header("Movement")]
        [SerializeField]
        [Tooltip("Walk speed in units per second")]
        private float walkSpeed = 3f;

        [SerializeField]
        [Tooltip("Layer mask for ground detection")]
        private LayerMask groundLayerMask;

        [SerializeField]
        [Tooltip("Offset from character center for ground check raycast")]
        private float groundCheckOffsetY = -0.5f;

        [SerializeField]
        [Tooltip("Distance to raycast downward for ground detection")]
        private float groundCheckDistance = 0.1f;

        [Header("Health")]
        [SerializeField]
        [Tooltip("Maximum health for the character")]
        private float maxHealth = 100f;

        [Header("Fall Damage")]
        [SerializeField]
        [Tooltip("Minimum fall distance in units before taking damage")]
        private float fallDamageThreshold = 3f;

        [SerializeField]
        [Tooltip("Damage multiplier per unit fallen (damage = (fallDistance - threshold) * multiplier)")]
        private float fallDamageMultiplier = 10f;
        #endregion

        #region Private Fields
        private Rigidbody2D rigidbody2D;
        private SpriteRenderer spriteRenderer;

        private float currentHealth;
        private bool isGrounded;
        private bool wasGroundedLastFrame;
        private float lastGroundedYPosition;

        private bool isActive;
        private float moveInputHorizontal;

        private AnimationState currentAnimationState = AnimationState.Idle;
        private int facingDirection = 1; // 1 for right, -1 for left
        #endregion

        #region Properties
        /// <summary>
        /// Gets the team index for this character (0 = Cats, 1 = Capybaras).
        /// </summary>
        public int TeamIndex => teamIndex;

        /// <summary>
        /// Gets the character index within the team.
        /// </summary>
        public int CharacterIndex => characterIndex;

        /// <summary>
        /// Gets the display name of the character.
        /// </summary>
        public string CharacterName => characterName;

        /// <summary>
        /// Gets the current health of the character.
        /// </summary>
        public float CurrentHealth => currentHealth;

        /// <summary>
        /// Gets the maximum health of the character.
        /// </summary>
        public float MaxHealth => maxHealth;

        /// <summary>
        /// Gets whether the character is alive (health > 0).
        /// </summary>
        public bool IsAlive => currentHealth > 0;

        /// <summary>
        /// Gets whether the character is currently grounded.
        /// </summary>
        public bool IsGrounded => isGrounded;

        /// <summary>
        /// Gets whether this character is the active turn character.
        /// </summary>
        public bool IsActive => isActive;

        /// <summary>
        /// Gets the current animation state of the character.
        /// </summary>
        public AnimationState CurrentAnimationState => currentAnimationState;

        /// <summary>
        /// Gets the facing direction (-1 = left, 1 = right).
        /// </summary>
        public int FacingDirection => facingDirection;

        /// <summary>
        /// Gets the health as a normalized value (0 to 1).
        /// </summary>
        public float HealthNormalized => Mathf.Clamp01(currentHealth / maxHealth);
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            rigidbody2D = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            currentHealth = maxHealth;
            wasGroundedLastFrame = false;
            lastGroundedYPosition = transform.position.y;
        }

        private void FixedUpdate()
        {
            UpdateGroundedState();
            ApplyMovement();
            UpdateAnimationState();
        }

        private void LateUpdate()
        {
            UpdateSpriteFlip();
        }
        #endregion

        #region Ground Detection
        /// <summary>
        /// Updates the grounded state by raycasting downward from the character's position.
        /// </summary>
        private void UpdateGroundedState()
        {
            Vector2 raycastOrigin = new Vector2(transform.position.x, transform.position.y + groundCheckOffsetY);
            RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, Vector2.down, groundCheckDistance, groundLayerMask);

            bool wasGrounded = isGrounded;
            isGrounded = hit.collider != null;

            // Track grounded position for fall damage calculation
            if (isGrounded && !wasGrounded)
            {
                lastGroundedYPosition = transform.position.y;
            }

            // Fire event if grounded state changed
            if (isGrounded != wasGrounded)
            {
                OnGroundedChanged?.Invoke(isGrounded);
            }

            wasGroundedLastFrame = wasGrounded;
        }
        #endregion

        #region Movement
        /// <summary>
        /// Applies horizontal movement to the character if active and during a move phase.
        /// Only moves if grounded to prevent mid-air adjustments in this MVP.
        /// </summary>
        private void ApplyMovement()
        {
            if (!isActive || !IsAlive || moveInputHorizontal == 0)
            {
                // Stop horizontal movement if not active or no input
                rigidbody2D.velocity = new Vector2(0, rigidbody2D.velocity.y);
                return;
            }

            if (isGrounded)
            {
                // Apply movement only while grounded
                float targetVelocityX = moveInputHorizontal * walkSpeed;
                rigidbody2D.velocity = new Vector2(targetVelocityX, rigidbody2D.velocity.y);

                // Update facing direction based on movement
                if (moveInputHorizontal > 0)
                    facingDirection = 1;
                else if (moveInputHorizontal < 0)
                    facingDirection = -1;
            }
            else
            {
                // Stop horizontal movement while falling (no air control in MVP)
                rigidbody2D.velocity = new Vector2(0, rigidbody2D.velocity.y);
            }
        }

        /// <summary>
        /// Receives horizontal move input from the input manager.
        /// </summary>
        /// <param name="horizontal">Horizontal input value (-1 to 1)</param>
        public void SetMoveInput(float horizontal)
        {
            moveInputHorizontal = Mathf.Clamp(horizontal, -1f, 1f);
        }
        #endregion

        #region Animation State Management
        /// <summary>
        /// Updates the current animation state based on character conditions.
        /// </summary>
        private void UpdateAnimationState()
        {
            if (!IsAlive)
            {
                SetAnimationState(AnimationState.Dead);
                return;
            }

            if (!isGrounded)
            {
                SetAnimationState(AnimationState.Falling);
                return;
            }

            // Check if currently moving
            if (Mathf.Abs(rigidbody2D.velocity.x) > 0.1f)
            {
                SetAnimationState(AnimationState.Walking);
            }
            else
            {
                SetAnimationState(AnimationState.Idle);
            }
        }

        /// <summary>
        /// Sets the animation state and triggers state-specific logic.
        /// </summary>
        /// <param name="newState">The new animation state</param>
        private void SetAnimationState(AnimationState newState)
        {
            if (currentAnimationState == newState)
                return;

            currentAnimationState = newState;

            // Additional state-specific logic can be added here (e.g., animation triggers)
        }
        #endregion

        #region Sprite Management
        /// <summary>
        /// Flips the sprite to match the facing direction.
        /// </summary>
        private void UpdateSpriteFlip()
        {
            spriteRenderer.flipX = (facingDirection == -1);
        }

        /// <summary>
        /// Applies a team color tint to the character's sprite.
        /// </summary>
        /// <param name="teamColor">Color to apply</param>
        public void SetTeamColor(Color teamColor)
        {
            spriteRenderer.color = teamColor;
        }
        #endregion

        #region Health System
        /// <summary>
        /// Applies damage to the character and fires the OnDamaged event.
        /// </summary>
        /// <param name="damageAmount">Amount of damage to apply</param>
        public void TakeDamage(float damageAmount)
        {
            if (!IsAlive)
                return;

            damageAmount = Mathf.Max(0, damageAmount);
            currentHealth -= damageAmount;

            OnDamaged?.Invoke(damageAmount);

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                OnEliminated?.Invoke();
            }
        }

        /// <summary>
        /// Heals the character and fires the OnHealed event.
        /// </summary>
        /// <param name="healAmount">Amount of health to restore</param>
        public void Heal(float healAmount)
        {
            if (!IsAlive)
                return;

            healAmount = Mathf.Max(0, healAmount);
            float oldHealth = currentHealth;
            currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);

            float actualHeal = currentHealth - oldHealth;
            if (actualHeal > 0)
            {
                OnHealed?.Invoke(actualHeal);
            }
        }

        /// <summary>
        /// Fully restores the character's health.
        /// </summary>
        public void FullHeal()
        {
            Heal(maxHealth - currentHealth);
        }

        /// <summary>
        /// Calculates and applies fall damage based on the distance fallen.
        /// Called when the character lands after falling.
        /// </summary>
        private void ApplyFallDamage()
        {
            float fallDistance = lastGroundedYPosition - transform.position.y;

            if (fallDistance > fallDamageThreshold)
            {
                float damage = (fallDistance - fallDamageThreshold) * fallDamageMultiplier;
                TakeDamage(damage);
            }
        }
        #endregion

        #region Turn Management
        /// <summary>
        /// Sets whether this character is the active turn character.
        /// Called by TurnManager at the start of this character's turn.
        /// </summary>
        /// <param name="active">True if this character's turn is active</param>
        public void SetActive(bool active)
        {
            isActive = active;
            moveInputHorizontal = 0;

            if (active && isGrounded)
            {
                lastGroundedYPosition = transform.position.y;
            }
        }

        /// <summary>
        /// Called by TurnManager when this character's turn ends to finalize the turn state.
        /// Clears movement input and applies any pending effects.
        /// </summary>
        public void EndTurn()
        {
            SetActive(false);
            moveInputHorizontal = 0;

            // Apply fall damage if applicable
            if (wasGroundedLastFrame && !isGrounded)
            {
                // Will apply on next ground contact
            }
            else if (isGrounded && lastGroundedYPosition > transform.position.y)
            {
                ApplyFallDamage();
            }
        }
        #endregion

        #region Debug Visualization
        /// <summary>
        /// Draws debug visualization for ground detection in the Scene view.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Vector2 raycastOrigin = new Vector2(transform.position.x, transform.position.y + groundCheckOffsetY);

            // Draw ground check raycast
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(raycastOrigin, raycastOrigin + Vector2.down * groundCheckDistance);

            // Draw character bounds
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, Vector3.one);
        }
        #endregion
    }
}
