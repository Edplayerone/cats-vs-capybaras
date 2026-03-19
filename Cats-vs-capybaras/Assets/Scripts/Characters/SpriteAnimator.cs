using UnityEngine;

namespace CatsVsCapybaras
{
    /// <summary>
    /// Code-driven sprite animator for characters.
    /// Reads CharacterController2D.CurrentAnimState each frame and swaps
    /// SpriteRenderer.sprite to play the matching animation clip.
    ///
    /// Setup:
    ///   1. Add this component to each character prefab alongside
    ///      CharacterController2D and SpriteRenderer.
    ///   2. In the Inspector, populate the Clips array — one entry per
    ///      AnimState you want to animate.
    ///   3. Assign sprites from mochi-character-board (sliced) to each clip.
    ///
    /// Walk cycle uses Walk_R and Walk_R2 alternating (the sheet has two
    /// walk poses). Idle loops on the single Idle frame.
    /// CharacterController2D already handles flipX for facing direction.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(CharacterController2D))]
    public class SpriteAnimator : MonoBehaviour
    {
        [System.Serializable]
        public class AnimClip
        {
            public CharacterController2D.AnimState state;
            [Tooltip("Frames to cycle through. Single frame = static pose.")]
            public Sprite[] frames;
            [Tooltip("Frames per second for multi-frame clips.")]
            public float fps = 8f;
            [Tooltip("Loop the clip, or hold on the last frame.")]
            public bool loop = true;
        }

        [Header("Animation Clips")]
        [SerializeField] private AnimClip[] clips;

        [Header("Fallback")]
        [Tooltip("Displayed when no clip matches the current state.")]
        [SerializeField] private Sprite fallbackSprite;

        private SpriteRenderer sr;
        private CharacterController2D cc;

        private AnimClip currentClip;
        private int frameIndex;
        private float frameTimer;
        private CharacterController2D.AnimState lastState;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            cc = GetComponent<CharacterController2D>();
        }

        private void Update()
        {
            CharacterController2D.AnimState state = cc.CurrentAnimState;

            // Detect state change → reset clip
            if (state != lastState)
            {
                lastState = state;
                currentClip = FindClip(state);
                frameIndex = 0;
                frameTimer = 0f;
            }

            if (currentClip == null || currentClip.frames == null || currentClip.frames.Length == 0)
            {
                if (fallbackSprite != null)
                    sr.sprite = fallbackSprite;
                return;
            }

            // Single-frame clip: no timer needed
            if (currentClip.frames.Length == 1)
            {
                sr.sprite = currentClip.frames[0];
                return;
            }

            // Advance frame timer
            float frameDuration = currentClip.fps > 0f ? 1f / currentClip.fps : 0.125f;
            frameTimer += Time.deltaTime;

            while (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                int next = frameIndex + 1;

                if (next >= currentClip.frames.Length)
                    frameIndex = currentClip.loop ? 0 : currentClip.frames.Length - 1;
                else
                    frameIndex = next;
            }

            Sprite frame = currentClip.frames[frameIndex];
            if (frame != null)
                sr.sprite = frame;
        }

        private AnimClip FindClip(CharacterController2D.AnimState state)
        {
            if (clips == null) return null;
            foreach (var clip in clips)
            {
                if (clip.state == state)
                    return clip;
            }
            return null;
        }

        /// <summary>
        /// Force-plays a clip by state from external code (e.g. after taking damage).
        /// </summary>
        public void ForceState(CharacterController2D.AnimState state)
        {
            lastState = state;
            currentClip = FindClip(state);
            frameIndex = 0;
            frameTimer = 0f;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Warn about missing clips for important states in the editor
            if (clips == null || clips.Length == 0) return;
            var defined = new System.Collections.Generic.HashSet<CharacterController2D.AnimState>();
            foreach (var c in clips) defined.Add(c.state);

            var important = new[]
            {
                CharacterController2D.AnimState.Idle,
                CharacterController2D.AnimState.Walking,
                CharacterController2D.AnimState.Jumping,
                CharacterController2D.AnimState.Dead
            };
            foreach (var s in important)
            {
                if (!defined.Contains(s))
                    Debug.LogWarning($"[SpriteAnimator] No clip defined for state: {s}", this);
            }
        }
#endif
    }
}
