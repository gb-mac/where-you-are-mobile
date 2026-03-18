using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Android;

namespace WhereYouAre.Location
{
    /// <summary>
    /// Singleton MonoBehaviour that acquires and maintains the device GPS position.
    /// Raises OnLocationResolved once with the session origin, then continuously
    /// updates CurrentPosition.
    ///
    /// Fallback chain:
    ///   1. Cached PlayerPrefs coordinate (< 30 days old)
    ///   2. Device GPS
    ///   3. Manual / hardcoded default
    /// </summary>
    public class WYALocationService : MonoBehaviour
    {
        public static WYALocationService Instance { get; private set; }

        public event Action<GeoCoord, bool> OnLocationResolved;

        public GeoCoord SessionOrigin  { get; private set; }
        public GeoCoord CurrentPosition { get; private set; }
        public bool IsResolved { get; private set; }

        [Header("Settings")]
        [SerializeField] private float desiredAccuracyMetres  = 10f;
        [SerializeField] private float updateDistanceMetres   = 5f;
        [SerializeField] private float gpsTimeoutSeconds      = 15f;
        [SerializeField] private double defaultLatitude       = 51.5074;
        [SerializeField] private double defaultLongitude      = -0.1278;

        private const string CacheKeyLat       = "WYA_LastLat";
        private const string CacheKeyLon       = "WYA_LastLon";
        private const string CacheKeyTimestamp = "WYA_LastLocTime";
        private const int    CacheMaxAgeDays   = 30;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start() => StartCoroutine(ResolveLocation());

        private IEnumerator ResolveLocation()
        {
            // 1. Try cache
            if (TryLoadCachedCoord(out var cached))
            {
                Debug.Log($"[WYALocation] Using cached coord {cached}");
                ResolveWith(cached);
                StartCoroutine(StartLiveTracking());
                yield break;
            }

            // 2. Request GPS permission (Android)
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                Permission.RequestUserPermission(Permission.FineLocation);
                float waited = 0f;
                while (!Permission.HasUserAuthorizedPermission(Permission.FineLocation) && waited < 10f)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }
            }
#endif

            // 3. Start GPS
            if (!Input.location.isEnabledByUser)
            {
                Debug.LogWarning("[WYALocation] GPS disabled by user, using default");
                ResolveWith(GetDefault());
                yield break;
            }

            Input.location.Start(desiredAccuracyMetres, updateDistanceMetres);

            float timeout = gpsTimeoutSeconds;
            while (Input.location.status == LocationServiceStatus.Initializing && timeout > 0)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (Input.location.status != LocationServiceStatus.Running)
            {
                Debug.LogWarning("[WYALocation] GPS failed to start, using default");
                ResolveWith(GetDefault());
                yield break;
            }

            var loc = Input.location.lastData;
            var coord = new GeoCoord(loc.latitude, loc.longitude, loc.altitude, LocationSource.DeviceGPS);
            SaveCachedCoord(coord);
            ResolveWith(coord);
            StartCoroutine(StartLiveTracking());
        }

        private IEnumerator StartLiveTracking()
        {
            while (true)
            {
                yield return new WaitForSeconds(3f);
                if (Input.location.status == LocationServiceStatus.Running)
                {
                    var loc = Input.location.lastData;
                    CurrentPosition = new GeoCoord(loc.latitude, loc.longitude, loc.altitude, LocationSource.DeviceGPS);
                }
            }
        }

        private void ResolveWith(GeoCoord coord)
        {
            SessionOrigin   = coord;
            CurrentPosition = coord;
            IsResolved      = true;
            OnLocationResolved?.Invoke(coord, coord.Source != LocationSource.Default);
        }

        public Vector3 GeoToWorld(GeoCoord coord) => WYAGeoMath.GeoToWorld(coord, SessionOrigin);
        public GeoCoord WorldToGeo(Vector3 worldPos) => WYAGeoMath.WorldToGeo(worldPos, SessionOrigin);

        private void SaveCachedCoord(GeoCoord coord)
        {
            PlayerPrefs.SetFloat(CacheKeyLat, (float)coord.Latitude);
            PlayerPrefs.SetFloat(CacheKeyLon, (float)coord.Longitude);
            PlayerPrefs.SetString(CacheKeyTimestamp, DateTime.UtcNow.ToString("O"));
            PlayerPrefs.Save();
        }

        private bool TryLoadCachedCoord(out GeoCoord coord)
        {
            coord = default;
            if (!PlayerPrefs.HasKey(CacheKeyTimestamp)) return false;

            if (!DateTime.TryParse(PlayerPrefs.GetString(CacheKeyTimestamp), out var timestamp)) return false;
            if ((DateTime.UtcNow - timestamp).TotalDays > CacheMaxAgeDays) return false;

            coord = new GeoCoord(
                PlayerPrefs.GetFloat(CacheKeyLat),
                PlayerPrefs.GetFloat(CacheKeyLon),
                0.0,
                LocationSource.Cached
            );
            return coord.IsValid;
        }

        private GeoCoord GetDefault() => new GeoCoord(defaultLatitude, defaultLongitude, 0.0, LocationSource.Default);
    }
}
