using UnityEngine;

namespace BlindSignal.Player
{
    /// <summary>
    /// Stores and exposes derived player stats calculated from rank values.
    ///
    /// Ranks are integers that grow through gameplay.  The formulas apply
    /// diminishing-returns curves so that high ranks still improve stats but
    /// never allow unbounded growth.
    ///
    /// SETUP: Attach to the Player root GameObject alongside PlayerMovement.
    /// </summary>
    public class PlayerAttributes : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Inspector – raw rank values (set defaults or load from a save system)
        // -------------------------------------------------------------------------
        [Header("Rank Values (0 = unranked)")]
        [Tooltip("Reduces how much noise the player generates when moving.")]
        [SerializeField] private int _dampeningRank = 0;

        [Tooltip("Increases the radius within which the player can hear remote pings.")]
        [SerializeField] private int _acousticRangeRank = 0;

        // -------------------------------------------------------------------------
        // Derived properties – recalculated whenever a rank changes
        // -------------------------------------------------------------------------

        /// <summary>
        /// Multiplicative dampening factor applied to the raw Noise Value.
        /// Rank 0  → factor 1.0  (no dampening).
        /// Higher ranks → factor approaches 0.2 asymptotically (80 % max reduction).
        ///
        /// Formula: Dampening = 1 - 0.8 * (1 - e^(-0.3 * rank))
        /// </summary>
        public float DampeningFactor => 1f - 0.8f * (1f - Mathf.Exp(-0.3f * _dampeningRank));

        /// <summary>
        /// Maximum distance (Unity units) at which this player can hear remote pings.
        /// Rank 0  → 200 units base range.
        /// Higher ranks → asymptotically approaches 200 + 150 = 350 units.
        ///
        /// Formula: AcousticRange = 200 + 150 * (1 - e^(-0.25 * rank))
        /// </summary>
        public float AcousticRange => 200f + 150f * (1f - Mathf.Exp(-0.25f * _acousticRangeRank));

        // -------------------------------------------------------------------------
        // Public rank accessors (used by upgrade/progression systems)
        // -------------------------------------------------------------------------
        public int DampeningRank
        {
            get => _dampeningRank;
            set
            {
                _dampeningRank = Mathf.Max(0, value);
                LogStats();
            }
        }

        public int AcousticRangeRank
        {
            get => _acousticRangeRank;
            set
            {
                _acousticRangeRank = Mathf.Max(0, value);
                LogStats();
            }
        }

        // -------------------------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------------------------
        private void Start()
        {
            LogStats();
        }

        // -------------------------------------------------------------------------
        // Debug helper
        // -------------------------------------------------------------------------
        private void LogStats()
        {
            Debug.Log($"[PlayerAttributes] DampeningFactor={DampeningFactor:F3} " +
                      $"(rank {_dampeningRank})  |  " +
                      $"AcousticRange={AcousticRange:F1} units (rank {_acousticRangeRank})");
        }
    }
}
