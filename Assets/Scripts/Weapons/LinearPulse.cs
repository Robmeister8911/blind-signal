using UnityEngine;
using BlindSignal.Networking;

namespace BlindSignal.Weapons
{
    /// <summary>
    /// Handles firing a straight-line projectile and immediately broadcasting a
    /// large "Muzzle Flash" ping (Noise Value = 15) to all other players.
    ///
    /// SETUP:
    ///  1. Attach to the Player's weapon GameObject (or the Player root).
    ///  2. Assign a ProjectilePrefab (a small Sphere/Capsule Rigidbody).
    ///  3. Assign the MuzzlePoint Transform (tip of the weapon barrel).
    ///  4. Assign (or auto-resolve) the MatchEventSync reference.
    ///  5. Wire up FireWeapon() to a UI button or input event.
    /// </summary>
    public class LinearPulse : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Constants
        // -------------------------------------------------------------------------
        /// <summary>Noise Value emitted by a gunshot / pulse fire.</summary>
        public const float MuzzleFlashNoiseValue = 15f;

        // -------------------------------------------------------------------------
        // Inspector
        // -------------------------------------------------------------------------
        [Header("Projectile")]
        [Tooltip("Prefab instantiated when firing (should have a Rigidbody).")]
        [SerializeField] private GameObject _projectilePrefab;

        [Tooltip("Transform at the tip of the barrel – projectile spawn origin.")]
        [SerializeField] private Transform _muzzlePoint;

        [Tooltip("Speed of the projectile in world units per second.")]
        [SerializeField] private float _projectileSpeed = 30f;

        [Tooltip("Seconds before the projectile is automatically destroyed.")]
        [SerializeField] private float _projectileLifetime = 3f;

        [Header("Fire Rate")]
        [Tooltip("Minimum seconds between shots (cooldown).")]
        [SerializeField] private float _fireCooldown = 0.5f;

        [Header("Dependencies")]
        [SerializeField] private MatchEventSync _matchEventSync;

        [Header("Player Identity")]
        [Tooltip("Unique player ID – must match the one on PlayerMovement.")]
        public string PlayerId = "local-player";

        // -------------------------------------------------------------------------
        // Private state
        // -------------------------------------------------------------------------
        private float _cooldownTimer;

        // -------------------------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------------------------
        private void Awake()
        {
            if (_matchEventSync == null)
                _matchEventSync = FindFirstObjectByType<MatchEventSync>();

            if (_muzzlePoint == null)
                _muzzlePoint = transform;
        }

        private void Update()
        {
            if (_cooldownTimer > 0f)
                _cooldownTimer -= Time.deltaTime;
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Attempts to fire the weapon.  Call this from a UI button or input handler.
        /// Returns true if the shot was fired, false if still on cooldown.
        /// </summary>
        public bool FireWeapon()
        {
            if (_cooldownTimer > 0f)
            {
                Debug.Log("[LinearPulse] On cooldown.");
                return false;
            }

            SpawnProjectile();
            BroadcastMuzzleFlash();

            _cooldownTimer = _fireCooldown;
            return true;
        }

        // -------------------------------------------------------------------------
        // Private helpers
        // -------------------------------------------------------------------------
        private void SpawnProjectile()
        {
            if (_projectilePrefab == null)
            {
                Debug.LogWarning("[LinearPulse] ProjectilePrefab is not assigned.");
                return;
            }

            GameObject proj = Instantiate(_projectilePrefab,
                                          _muzzlePoint.position,
                                          _muzzlePoint.rotation);

            // Apply velocity via Rigidbody if present.
            var rb = proj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = _muzzlePoint.forward * _projectileSpeed;
            }
            else
            {
                // Fallback: attach a simple mover component at runtime.
                var mover = proj.AddComponent<ProjectileMover>();
                mover.Initialise(_muzzlePoint.forward, _projectileSpeed);
            }

            Destroy(proj, _projectileLifetime);
            Debug.Log($"[LinearPulse] Projectile fired from {_muzzlePoint.position}.");
        }

        private void BroadcastMuzzleFlash()
        {
            if (_matchEventSync == null) return;

            Vector2 pos2D = new Vector2(_muzzlePoint.position.x, _muzzlePoint.position.z);
            _matchEventSync.SendPing(pos2D, MuzzleFlashNoiseValue, PlayerId);
            Debug.Log($"[LinearPulse] Muzzle flash ping sent (V_n={MuzzleFlashNoiseValue}).");
        }
    }

    // -------------------------------------------------------------------------
    // Minimal runtime component to move projectiles that lack a Rigidbody
    // -------------------------------------------------------------------------
    internal class ProjectileMover : MonoBehaviour
    {
        private Vector3 _direction;
        private float   _speed;

        public void Initialise(Vector3 direction, float speed)
        {
            _direction = direction.normalized;
            _speed     = speed;
        }

        private void Update()
        {
            transform.Translate(_direction * (_speed * Time.deltaTime), Space.World);
        }
    }
}
