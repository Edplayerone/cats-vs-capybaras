using UnityEngine;

namespace CatsVsCapybaras
{
    /// <summary>
    /// Self-destroying explosion VFX. Attach to a prefab with ParticleSystems.
    /// Spawned by ProjectileBase on detonation.
    /// </summary>
    public class ExplosionEffect : MonoBehaviour
    {
        [SerializeField] private float lifetime = 2f;
        [SerializeField] private ParticleSystem[] particleSystems;

        private void Start()
        {
            if (particleSystems != null)
            {
                foreach (var ps in particleSystems)
                    ps.Play();
            }
            Destroy(gameObject, lifetime);
        }

        public static ExplosionEffect Spawn(Vector2 position, float radius, GameObject prefab)
        {
            if (prefab == null) return null;

            var obj = Instantiate(prefab, position, Quaternion.identity);
            obj.transform.localScale = Vector3.one * Mathf.Max(0.5f, radius / 2f);
            return obj.GetComponent<ExplosionEffect>();
        }
    }
}
