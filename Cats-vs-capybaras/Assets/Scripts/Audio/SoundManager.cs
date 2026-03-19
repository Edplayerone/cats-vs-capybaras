using UnityEngine;

namespace CatsVsCapybaras
{
    /// <summary>
    /// Singleton audio manager. Plays one-shot SFX via a shared AudioSource.
    /// Clips can be assigned in the Inspector OR auto-loaded from Resources/Audio/SFX/.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("Source")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource flySource;  // dedicated source so fly can be stopped on impact

        [Header("Projectile SFX")]
        [SerializeField] private AudioClip carrotFlyClip;
        [SerializeField] private AudioClip carrotImpactClip;

        [Header("Character SFX")]
        [SerializeField] private AudioClip jumpClip;
        [SerializeField] private AudioClip landClip;
        [SerializeField] private AudioClip[] footstepClips;

        [Header("Volume")]
        [SerializeField, Range(0f, 1f)] private float flyVolume      = 0.6f;
        [SerializeField, Range(0f, 1f)] private float impactVolume   = 0.8f;
        [SerializeField, Range(0f, 1f)] private float jumpVolume     = 0.5f;
        [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.35f;

        [Header("Footstep Timing")]
        [SerializeField] private float footstepInterval = 0.25f;

        private float lastFootstepTime;
        private int lastFootstepIndex = -1;

        // Public references so SceneBuilder can assign clips at edit time
        public AudioClip CarrotFlyClip   { get => carrotFlyClip;   set => carrotFlyClip = value; }
        public AudioClip CarrotImpactClip { get => carrotImpactClip; set => carrotImpactClip = value; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Auto-create AudioSource if not assigned
            if (sfxSource == null)
            {
                sfxSource = GetComponent<AudioSource>();
                if (sfxSource == null)
                    sfxSource = gameObject.AddComponent<AudioSource>();
            }

            sfxSource.playOnAwake = false;

            // Auto-create a second source dedicated to the fly sound (so it can be stopped)
            if (flySource == null)
            {
                flySource = gameObject.AddComponent<AudioSource>();
                flySource.playOnAwake = false;
            }

            // Auto-load clips from Resources if not assigned in Inspector
            if (carrotFlyClip == null)
                carrotFlyClip = Resources.Load<AudioClip>("Audio/SFX/Carrot_Fly");
            if (carrotImpactClip == null)
                carrotImpactClip = Resources.Load<AudioClip>("Audio/SFX/Carrot_Impact");

            if (footstepClips == null || footstepClips.Length == 0)
            {
                footstepClips = new[]
                {
                    Resources.Load<AudioClip>("Audio/SFX/footstep_grass_000"),
                    Resources.Load<AudioClip>("Audio/SFX/footstep_grass_001"),
                    Resources.Load<AudioClip>("Audio/SFX/footstep_grass_002"),
                    Resources.Load<AudioClip>("Audio/SFX/footstep_grass_003"),
                    Resources.Load<AudioClip>("Audio/SFX/footstep_grass_004"),
                };
            }

            Debug.Log($"[SoundManager] Fly clip: {(carrotFlyClip != null ? carrotFlyClip.name : "MISSING")} | Impact clip: {(carrotImpactClip != null ? carrotImpactClip.name : "MISSING")} | Footsteps: {footstepClips.Length}");
        }

        // ── Projectile ───────────────────────────────────────────────

        public void PlayCarrotFly()
        {
            if (carrotFlyClip == null) return;
            flySource.clip   = carrotFlyClip;
            flySource.volume = flyVolume;
            flySource.loop   = true;   // loop so long shots don't go silent
            flySource.Play();
        }

        public void StopCarrotFly()
        {
            if (flySource.isPlaying)
                flySource.Stop();
        }

        public void PlayCarrotImpact()
        {
            StopCarrotFly();   // cut fly sound immediately on impact
            if (carrotImpactClip != null)
                sfxSource.PlayOneShot(carrotImpactClip, impactVolume);
        }

        // ── Character ─────────────────────────────────────────────────

        public void PlayJump()
        {
            if (jumpClip != null)
                sfxSource.PlayOneShot(jumpClip, jumpVolume);
        }

        public void PlayLand()
        {
            if (landClip != null)
                sfxSource.PlayOneShot(landClip, jumpVolume);
        }

        /// <summary>
        /// Plays a random footstep clip, rate-limited by footstepInterval.
        /// Avoids repeating the same clip twice in a row.
        /// </summary>
        public void PlayFootstep()
        {
            if (footstepClips == null || footstepClips.Length == 0) return;
            if (Time.time - lastFootstepTime < footstepInterval) return;

            int index = Random.Range(0, footstepClips.Length);
            if (footstepClips.Length > 1 && index == lastFootstepIndex)
                index = (index + 1) % footstepClips.Length;

            if (footstepClips[index] != null)
                sfxSource.PlayOneShot(footstepClips[index], footstepVolume);

            lastFootstepIndex = index;
            lastFootstepTime = Time.time;
        }
    }
}
