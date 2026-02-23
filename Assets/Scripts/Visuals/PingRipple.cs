using UnityEngine;

namespace BlindSignal.Visuals
{
    /// <summary>
    /// Attached to the Ping prefab.  Animates the ripple effect over its
    /// lifetime: scales the Transform up and fades the material alpha to zero,
    /// then destroys the GameObject.
    ///
    /// SETUP:
    ///  1. Create a Quad GameObject (or Sprite) and apply a Material that uses
    ///     the BlindSignal/PingRipple shader.
    ///  2. Attach this script.
    ///  3. Save as a Prefab and reference it in PingSpawner.
    /// </summary>
    public class PingRipple : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Inspector
        // -------------------------------------------------------------------------
        [Header("Animation")]
        [Tooltip("Total duration of the ripple animation in seconds.")]
        [SerializeField] private float _lifetime = 1.5f;

        [Tooltip("World-space scale the ring grows to over its lifetime.")]
        [SerializeField] private float _maxScale = 10f;

        [Tooltip("Starting scale of the ring.")]
        [SerializeField] private float _startScale = 0.1f;

        [Header("Appearance")]
        [Tooltip("Colour of the ring (alpha is overridden by the animation).")]
        [SerializeField] private Color _ringColor = new Color(0.2f, 0.8f, 1f, 1f);

        // -------------------------------------------------------------------------
        // Private state
        // -------------------------------------------------------------------------
        private float   _elapsed;
        private Material _material;

        // Shader property IDs (cached for performance)
        private static readonly int PropAlpha  = Shader.PropertyToID("_Alpha");
        private static readonly int PropColor  = Shader.PropertyToID("_Color");
        private static readonly int PropRadius = Shader.PropertyToID("_Radius");

        // -------------------------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------------------------
        private void Awake()
        {
            // Use an instance-specific material so multiple ripples don't share state.
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                _material = renderer.material; // creates a per-instance copy
                _material.SetColor(PropColor, _ringColor);
            }

            transform.localScale = Vector3.one * _startScale;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _lifetime);

            // Scale up linearly over the lifetime.
            float currentScale = Mathf.Lerp(_startScale, _maxScale, t);
            transform.localScale = Vector3.one * currentScale;

            // Fade out using an ease-in curve so it's visible at the start.
            float alpha = 1f - Mathf.Pow(t, 2f);
            if (_material != null)
            {
                _material.SetFloat(PropAlpha, alpha);
                // Optionally keep radius constant; the scale drives apparent size.
                _material.SetFloat(PropRadius, 0.45f);
            }

            if (_elapsed >= _lifetime)
                Destroy(gameObject);
        }

        // -------------------------------------------------------------------------
        // Public factory helper (called by PingSpawner)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Configures the ripple colour tint based on the ping intensity.
        /// Higher intensity → more saturated / brighter ring.
        /// </summary>
        /// <param name="intensity">Noise Value (V_n). Expected range 1–15.</param>
        public void SetIntensity(float intensity)
        {
            // Map intensity [1,15] to a colour shift: low = blue, high = red.
            float t = Mathf.InverseLerp(1f, 15f, intensity);
            _ringColor = Color.Lerp(new Color(0.2f, 0.8f, 1f, 1f),
                                    new Color(1f,   0.3f, 0.1f, 1f),
                                    t);
            if (_material != null)
                _material.SetColor(PropColor, _ringColor);
        }
    }
}
