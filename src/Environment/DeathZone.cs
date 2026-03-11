using UnityEngine;

namespace CatsVsCapybaras
{
    /// <summary>
    /// Trigger volume that instantly kills characters entering it.
    /// Place at the bottom of the map (water/void) as an invisible trigger collider.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DeathZone : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            var character = other.GetComponent<CharacterController2D>();
            if (character != null && character.IsAlive)
                character.TakeDamage(character.MaxHealth * 10f);
        }

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }
    }
}
