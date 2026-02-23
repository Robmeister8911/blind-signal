using UnityEngine;
using BlindSignal.Networking;
using BlindSignal.UI;

namespace BlindSignal.Player
{
    /// <summary>
    /// Moves the player character using input from a VirtualJoystick and
    /// periodically broadcasts acoustic pings via MatchEventSync.
    ///
    /// Noise Value (V_n) mapping:
    ///   Joystick magnitude ≤ 0.2  → crouch/idle  V_n = 1
    ///   Joystick magnitude ≤ 0.6  → walk          V_n = 3
    ///   Joystick magnitude  > 0.6  → sprint        V_n = 5
    /// The V_n is then multiplied by the PlayerAttributes.DampeningFactor.
    ///
    /// SETUP:
    ///  1. Attach to the Player root GameObject.
    ///  2. Assign the VirtualJoystick reference in the Inspector.
    ///  3. Assign (or auto-resolve) the MatchEventSync reference.
    ///  4. Give the player a unique ID (e.g. from auth).
    /// </summary>
    [RequireComponent(typeof(PlayerAttributes))]
    public class PlayerMovement : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Inspector
        // -------------------------------------------------------------------------
        [Header("Dependencies")]
        [SerializeField] private VirtualJoystick _joystick;
        [SerializeField] private MatchEventSync _matchEventSync;

        [Header("Movement")]
        [Tooltip("World units per second at full joystick deflection.")]
        [SerializeField] private float _maxSpeed = 8f;

        [Header("Ping Broadcasting")]
        [Tooltip("How often (seconds) a movement ping is sent while the player is moving.")]
        [SerializeField] private float _pingInterval = 0.5f;

        [Tooltip("Minimum joystick magnitude required before movement pings are sent.")]
        [SerializeField] private float _movementThreshold = 0.05f;

        [Header("Player Identity")]
        [Tooltip("Unique player identifier (populate from auth system at runtime).")]
        public string PlayerId = "local-player";

        // -------------------------------------------------------------------------
        // Private state
        // -------------------------------------------------------------------------
        private PlayerAttributes _attributes;
        private float _pingTimer;

        // Noise thresholds
        private const float CrouchThreshold = 0.2f;
        private const float WalkThreshold   = 0.6f;
        private const float CrouchNoise     = 1f;
        private const float WalkNoise       = 3f;
        private const float SprintNoise     = 5f;

        // -------------------------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------------------------
        private void Awake()
        {
            _attributes = GetComponent<PlayerAttributes>();

            if (_matchEventSync == null)
                _matchEventSync = FindFirstObjectByType<MatchEventSync>();

            if (_joystick == null)
                Debug.LogWarning("[PlayerMovement] VirtualJoystick is not assigned. " +
                                 "Assign it in the Inspector.");
        }

        private void Update()
        {
            HandleMovement();
            HandlePingBroadcast();
        }

        // -------------------------------------------------------------------------
        // Movement
        // -------------------------------------------------------------------------
        private void HandleMovement()
        {
            if (_joystick == null) return;

            Vector2 input = _joystick.Direction;
            Vector3 move  = new Vector3(input.x, 0f, input.y) * (_maxSpeed * _joystick.Magnitude);
            transform.Translate(move * Time.deltaTime, Space.World);
        }

        // -------------------------------------------------------------------------
        // Ping broadcasting
        // -------------------------------------------------------------------------
        private void HandlePingBroadcast()
        {
            if (_joystick == null || _matchEventSync == null) return;

            bool isMoving = _joystick.Magnitude > _movementThreshold;
            if (!isMoving) return;

            _pingTimer -= Time.deltaTime;
            if (_pingTimer > 0f) return;

            _pingTimer = _pingInterval;

            float rawNoise    = CalculateNoiseValue(_joystick.Magnitude);
            float dampedNoise = rawNoise * _attributes.DampeningFactor;

            Vector2 worldPos2D = new Vector2(transform.position.x, transform.position.z);
            _matchEventSync.SendPing(worldPos2D, dampedNoise, PlayerId);
        }

        // -------------------------------------------------------------------------
        // Noise Value calculation
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns the base Noise Value (V_n) for a given joystick magnitude.
        /// </summary>
        public static float CalculateNoiseValue(float magnitude)
        {
            if (magnitude <= CrouchThreshold) return CrouchNoise;
            if (magnitude <= WalkThreshold)   return WalkNoise;
            return SprintNoise;
        }
    }
}
