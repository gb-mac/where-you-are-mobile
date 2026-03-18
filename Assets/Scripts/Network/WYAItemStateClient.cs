using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace WhereYouAre.Network
{
    /// <summary>
    /// HTTP client for the shared item state backend.
    /// Both the mobile app and desktop game read/write from this API.
    /// Base URL is set in Resources/WYAConfig (created at runtime).
    /// </summary>
    public class WYAItemStateClient : MonoBehaviour
    {
        public static WYAItemStateClient Instance { get; private set; }

        // TODO: move to config / environment variable
        private string BaseUrl => "https://api.whereyouare.game/v1";

        public event Action<WorldItem> OnItemPlaced;
        public event Action<WorldItem> OnItemClaimed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Fetch items within radius metres of the given GPS coordinate.
        /// Calls back on main thread.
        /// </summary>
        public void FetchNearbyItems(double lat, double lon, float radiusMetres,
            Action<NearbyItemsResponse> onSuccess, Action<string> onError = null)
        {
            string url = $"{BaseUrl}/items/nearby?lat={lat}&lon={lon}&radius={radiusMetres}";
            StartCoroutine(GetRequest<NearbyItemsResponse>(url, onSuccess, onError));
        }

        /// <summary>Place an item at a GPS coordinate.</summary>
        public void PlaceItem(PlaceItemRequest request,
            Action<WorldItem> onSuccess, Action<string> onError = null)
        {
            StartCoroutine(PostRequest<WorldItem>($"{BaseUrl}/items", request, result =>
            {
                OnItemPlaced?.Invoke(result);
                onSuccess?.Invoke(result);
            }, onError));
        }

        /// <summary>Claim an item. First-come-first-served — server rejects if already claimed.</summary>
        public void ClaimItem(ClaimItemRequest request,
            Action<WorldItem> onSuccess, Action<string> onError = null)
        {
            StartCoroutine(PostRequest<WorldItem>($"{BaseUrl}/items/claim", request, result =>
            {
                OnItemClaimed?.Invoke(result);
                onSuccess?.Invoke(result);
            }, onError));
        }

        /// <summary>Fetch faction territory polygons near a location.</summary>
        public void FetchFactionTerritory(double lat, double lon, float radiusMetres,
            Action<FactionTerritory[]> onSuccess, Action<string> onError = null)
        {
            string url = $"{BaseUrl}/territory?lat={lat}&lon={lon}&radius={radiusMetres}";
            StartCoroutine(GetRequest<FactionTerritory[]>(url, onSuccess, onError));
        }

        // -------------------------------------------------------------------

        private IEnumerator GetRequest<T>(string url, Action<T> onSuccess, Action<string> onError)
        {
            using var req = UnityWebRequest.Get(url);
            SetAuthHeaders(req);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[WYAApi] GET {url} failed: {req.error}");
                onError?.Invoke(req.error);
                yield break;
            }

            T result = JsonConvert.DeserializeObject<T>(req.downloadHandler.text);
            onSuccess?.Invoke(result);
        }

        private IEnumerator PostRequest<T>(string url, object body, Action<T> onSuccess, Action<string> onError)
        {
            string json = JsonConvert.SerializeObject(body);
            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            SetAuthHeaders(req);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[WYAApi] POST {url} failed: {req.error} — {req.downloadHandler.text}");
                onError?.Invoke(req.error);
                yield break;
            }

            T result = JsonConvert.DeserializeObject<T>(req.downloadHandler.text);
            onSuccess?.Invoke(result);
        }

        private void SetAuthHeaders(UnityWebRequest req)
        {
            // TODO: inject auth token from session manager
            // req.SetRequestHeader("Authorization", $"Bearer {SessionManager.Instance.Token}");
        }
    }
}
