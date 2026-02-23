using UnityEngine;
using BlindSignal.Networking;
using BlindSignal.Player;

namespace BlindSignal.Visuals
{
    /// <summary>
    /// Listens for remote pings from MatchEventSync and instantiates the Ping
    /// ripple prefab when the ping falls within the local player's acoustic range
    /// (Fog of War mechanic).
    ///
    /// SETUP:
    ///  1. Attach this script to a persistent Manager GameObject in the scene.
    ///  2. Assign the PingRipplePrefab (a Quad with PingRipple.cs and the
    ///     PingRipple material).
    ///  3. Assign the LocalPlayer Transform and its PlayerAttributes component.
    /// </summary>
    public class PingSpawner : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Inspector
        // -------------------------------------------------------------------------
        [Header("Prefabs")]
        [Tooltip("The Ping ripple prefab (contains PingRipple.cs).")]
        [SerializeField] private GameObject _pingRipplePrefab;

        [Header("Local Player References")]
        [Tooltip("Transform of the local player (used to check hearing range).")]
        [SerializeField] private Transform _localPlayerTransform;

        [Tooltip("PlayerAttributes of the local player (provides AcousticRange).")]
        [SerializeField] private PlayerAttributes _localPlayerAttributes;

        [Header("Visuals")]
        [Tooltip("Y-offset (height) at which ping ripples are spawned in world space.")]
        [SerializeField] private float _spawnHeight = 0.01f;

        // -------------------------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------------------------
        private void OnEnable()
        {
            MatchEventSync.OnRemotePingReceived += HandleRemotePing;
        }

        private void OnDisable()
        {
            MatchEventSync.OnRemotePingReceived -= HandleRemotePing;
        }

        private void Start()
        {
            // Auto-resolve references if not set in Inspector.
            if (_localPlayerTransform == null)
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    _localPlayerTransform  = playerObj.transform;
                    _localPlayerAttributes = playerObj.GetComponent<PlayerAttributes>();
                }
            }

            if (_pingRipplePrefab == null)
                Debug.LogWarning("[PingSpawner] PingRipplePrefab is not assigned.");

            if (_localPlayerTransform == null)
                Debug.LogWarning("[PingSpawner] LocalPlayer transform is not assigned " +
                                 "and could not be auto-resolved. Tag your Player 'Player'.");
        }

        // -------------------------------------------------------------------------
        // Event handler
        // -------------------------------------------------------------------------
        private void HandleRemotePing(Vector2 pingOrigin, float intensity, string senderId)
        {
            if (_localPlayerTransform == null || _localPlayerAttributes == null)
                return;

            // Fog of War check: is the ping within the player's acoustic range?
            Vector2 localPos2D = new Vector2(
                _localPlayerTransform.position.x,
                _localPlayerTransform.position.z);

            float distance = Vector2.Distance(localPos2D, pingOrigin);

            if (distance > _localPlayerAttributes.AcousticRange)
            {
                Debug.Log($"[PingSpawner] Ping from {senderId} is out of range " +
                          $"({distance:F1} > {_localPlayerAttributes.AcousticRange:F1}). Suppressed.");
                return;
            }

            SpawnRipple(pingOrigin, intensity);
        }

        // -------------------------------------------------------------------------
        // Spawning
        // -------------------------------------------------------------------------
        private void SpawnRipple(Vector2 worldPos2D, float intensity)
        {
            if (_pingRipplePrefab == null) return;

            Vector3 spawnPos = new Vector3(worldPos2D.x, _spawnHeight, worldPos2D.y);
            GameObject rippleGO = Instantiate(_pingRipplePrefab, spawnPos, Quaternion.identity);

            // Configure intensity-based colour if the prefab has a PingRipple component.
            var ripple = rippleGO.GetComponent<PingRipple>();
            if (ripple != null)
                ripple.SetIntensity(intensity);
        }
    }
}
