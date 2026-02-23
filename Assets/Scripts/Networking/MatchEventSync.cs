using System;
using System.Collections;
using UnityEngine;

namespace BlindSignal.Networking
{
    /// <summary>
    /// Central hub for sending and receiving ping (acoustic signal) events.
    ///
    /// In Phase 1 the channel subscription is simulated via mock coroutines so
    /// the rest of the codebase can be developed and tested without a live
    /// Supabase connection.  When the Supabase C# Realtime SDK is added, replace
    /// the mock region with the real subscription code.
    ///
    /// SETUP:
    ///  1. Add this component to the same persistent GameObject as SupabaseManager.
    ///  2. Reference the SupabaseManager in the Inspector (or resolve via Instance).
    /// </summary>
    public class MatchEventSync : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Public event – subscribe to receive remote pings.
        // Signature: (world-space position, intensity/noise value, sender player ID)
        // -------------------------------------------------------------------------
        public static event Action<Vector2, float, string> OnRemotePingReceived;

        // -------------------------------------------------------------------------
        // Inspector references
        // -------------------------------------------------------------------------
        [Header("Dependencies")]
        [Tooltip("The SupabaseManager singleton (can be left empty; resolved automatically).")]
        [SerializeField] private SupabaseManager _supabaseManager;

        [Header("Mock / Debug Settings")]
        [Tooltip("When true, incoming pings are simulated locally for testing.")]
        [SerializeField] private bool _useMockChannel = true;

        [Tooltip("How often (seconds) the mock channel emits a fake remote ping.")]
        [SerializeField] private float _mockPingInterval = 3f;

        // Supabase Realtime channel placeholder
        // private Supabase.Realtime.RealtimeChannel _pingChannel;

        // -------------------------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------------------------
        private void Awake()
        {
            if (_supabaseManager == null)
                _supabaseManager = SupabaseManager.Instance;
        }

        private void Start()
        {
            if (_useMockChannel)
                StartCoroutine(MockChannelCoroutine());
            else
                SubscribeToChannel();
        }

        private void OnDestroy()
        {
            UnsubscribeFromChannel();
        }

        // -------------------------------------------------------------------------
        // Sending a ping (local player → server → other clients)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Broadcast a ping originating from the local player.
        /// </summary>
        /// <param name="worldPosition">2D world position of the source.</param>
        /// <param name="noiseValue">Acoustic intensity (V_n). Movement ~1-5, gunshot 15.</param>
        /// <param name="playerId">Unique identifier of the local player.</param>
        public void SendPing(Vector2 worldPosition, float noiseValue, string playerId)
        {
            Debug.Log($"[MatchEventSync] Sending ping – pos:{worldPosition}, V_n:{noiseValue}, player:{playerId}");

            // ------------------------------------------------------------------
            // REAL IMPLEMENTATION
            // ------------------------------------------------------------------
            // var payload = new
            // {
            //     x         = worldPosition.x,
            //     y         = worldPosition.y,
            //     intensity = noiseValue,
            //     player_id = playerId
            // };
            // await _pingChannel.Send("broadcast", "ping", payload);
            // ------------------------------------------------------------------

            // In mock mode, echo back as a remote ping so local UI can be tested.
            if (_useMockChannel)
                RaiseRemotePing(worldPosition, noiseValue, "mock-remote-player");
        }

        // -------------------------------------------------------------------------
        // Receiving pings (server → this client)
        // -------------------------------------------------------------------------
        private void SubscribeToChannel()
        {
            // ------------------------------------------------------------------
            // REAL IMPLEMENTATION
            // ------------------------------------------------------------------
            // var client = _supabaseManager.Client;
            // _pingChannel = client.Realtime.Channel("realtime", "public", "pings");
            //
            // _pingChannel.AddBroadcastEventHandler("ping", (sender, baseBroadcast) =>
            // {
            //     var payload = baseBroadcast.Payload<PingPayload>();
            //     var pos      = new Vector2(payload.X, payload.Y);
            //     RaiseRemotePing(pos, payload.Intensity, payload.PlayerId);
            // });
            //
            // await _pingChannel.Subscribe();
            // Debug.Log("[MatchEventSync] Subscribed to realtime ping channel.");
            // ------------------------------------------------------------------

            Debug.Log("[MatchEventSync] (Placeholder) Channel subscription skipped – no SDK.");
        }

        private void UnsubscribeFromChannel()
        {
            // ------------------------------------------------------------------
            // REAL IMPLEMENTATION
            // ------------------------------------------------------------------
            // if (_pingChannel != null)
            //     await _pingChannel.Unsubscribe();
            // ------------------------------------------------------------------
        }

        // -------------------------------------------------------------------------
        // Mock channel – emits fake remote pings for local testing
        // -------------------------------------------------------------------------
        private IEnumerator MockChannelCoroutine()
        {
            Debug.Log("[MatchEventSync] Mock channel active – emitting test pings.");
            var rng = new System.Random();

            while (true)
            {
                yield return new WaitForSeconds(_mockPingInterval);

                // Random position within a 30-unit radius of the origin.
                var fakePos = new Vector2(
                    (float)(rng.NextDouble() * 60 - 30),
                    (float)(rng.NextDouble() * 60 - 30));

                float fakeIntensity = (float)(rng.NextDouble() * 4 + 1); // 1-5
                RaiseRemotePing(fakePos, fakeIntensity, "mock-remote-player");
            }
        }

        // -------------------------------------------------------------------------
        // Helper – fires the public event on the Unity main thread
        // -------------------------------------------------------------------------
        private void RaiseRemotePing(Vector2 position, float intensity, string playerId)
        {
            Debug.Log($"[MatchEventSync] Remote ping received – pos:{position}, V_n:{intensity}, player:{playerId}");
            OnRemotePingReceived?.Invoke(position, intensity, playerId);
        }

        // -------------------------------------------------------------------------
        // Payload DTO (used with the real SDK deserialization)
        // -------------------------------------------------------------------------
        [Serializable]
        private class PingPayload
        {
            public float X;
            public float Y;
            public float Intensity;
            public string PlayerId;
        }
    }
}
