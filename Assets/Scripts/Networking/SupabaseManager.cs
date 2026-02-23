using UnityEngine;

namespace BlindSignal.Networking
{
    /// <summary>
    /// Singleton MonoBehaviour that holds Supabase connection credentials and
    /// provides a central point for initialising the Supabase client.
    ///
    /// SETUP:
    ///  1. Add this component to a persistent GameObject in your bootstrap scene.
    ///  2. Replace the placeholder strings below with your actual project values,
    ///     or populate them via the Inspector (mark [SerializeField] if preferred).
    ///  3. When you add the Supabase C# SDK (supabase-csharp), uncomment and
    ///     complete the initialisation block inside InitialiseClient().
    /// </summary>
    public class SupabaseManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static SupabaseManager Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Configuration – replace with real values or load from a config file.
        // -------------------------------------------------------------------------
        [Header("Supabase Credentials")]
        [Tooltip("Your Supabase project URL, e.g. https://xyzabc.supabase.co")]
        public string SupabaseUrl = "https://your-project.supabase.co";

        [Tooltip("Your Supabase anonymous (public) API key")]
        public string SupabaseAnonKey = "your-anon-key-here";

        // -------------------------------------------------------------------------
        // Supabase client reference (uncomment once the SDK is imported)
        // -------------------------------------------------------------------------
        // private Supabase.Client _supabaseClient;
        // public Supabase.Client Client => _supabaseClient;

        // -------------------------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------------------------
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitialiseClient();
        }

        // -------------------------------------------------------------------------
        // Initialisation
        // -------------------------------------------------------------------------
        private void InitialiseClient()
        {
            if (string.IsNullOrEmpty(SupabaseUrl) || string.IsNullOrEmpty(SupabaseAnonKey))
            {
                Debug.LogError("[SupabaseManager] Supabase URL or Anon Key is not set. " +
                               "Please populate the credentials in the Inspector.");
                return;
            }

            Debug.Log($"[SupabaseManager] Initialising Supabase client for project: {SupabaseUrl}");

            // ------------------------------------------------------------------
            // REAL IMPLEMENTATION (uncomment after adding the supabase-csharp SDK)
            // ------------------------------------------------------------------
            // var options = new Supabase.SupabaseOptions
            // {
            //     AutoRefreshToken = true,
            //     AutoConnectRealtime = true
            // };
            // _supabaseClient = new Supabase.Client(SupabaseUrl, SupabaseAnonKey, options);
            // await _supabaseClient.InitializeAsync();
            // Debug.Log("[SupabaseManager] Supabase client initialised successfully.");
            // ------------------------------------------------------------------

            // Placeholder log so the game can boot without the SDK installed.
            Debug.Log("[SupabaseManager] (Placeholder) Supabase client ready.");
        }
    }
}
