using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhereYouAre.Location;
using WhereYouAre.Network;

namespace WhereYouAre.AR
{
    /// <summary>
    /// Manages the lifecycle of AR world items:
    /// - Fetches nearby items from the backend on location resolve and periodically
    /// - Spawns / despawns item prefabs as the player moves
    /// - Handles claim requests (first-come-first-served)
    /// - Triggers Snatch Warning notifications when an enemy nears your cache
    /// </summary>
    public class WYAARItemManager : MonoBehaviour
    {
        public static WYAARItemManager Instance { get; private set; }

        [Header("Prefabs")]
        [SerializeField] private GameObject supplyCachePrefab;
        [SerializeField] private GameObject deadDropPrefab;
        [SerializeField] private GameObject wardenMarkerPrefab;
        [SerializeField] private GameObject factionCachePrefab;

        [Header("Settings")]
        [SerializeField] private float fetchRadiusMetres  = 500f;
        [SerializeField] private float fetchIntervalSecs  = 30f;
        [SerializeField] private float claimRadiusMetres  = 20f;

        private readonly Dictionary<string, WYAARItemAnchor> _spawnedItems = new();
        private WYALocationService _locationService;
        private WYAItemStateClient _apiClient;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _locationService = WYALocationService.Instance;
            _apiClient       = WYAItemStateClient.Instance;

            _locationService.OnLocationResolved += OnLocationResolved;
        }

        private void OnLocationResolved(GeoCoord origin, bool bSuccess)
        {
            FetchNearbyItems();
            StartCoroutine(PeriodicFetch());
        }

        private IEnumerator PeriodicFetch()
        {
            while (true)
            {
                yield return new WaitForSeconds(fetchIntervalSecs);
                FetchNearbyItems();
            }
        }

        private void FetchNearbyItems()
        {
            var pos = _locationService.CurrentPosition;
            _apiClient.FetchNearbyItems(pos.Latitude, pos.Longitude, fetchRadiusMetres,
                response =>
                {
                    var activeIds = new HashSet<string>();
                    foreach (var item in response.Items)
                    {
                        activeIds.Add(item.Id);
                        if (!_spawnedItems.ContainsKey(item.Id))
                            SpawnItem(item);
                    }

                    // Despawn items no longer in range
                    var toRemove = new List<string>();
                    foreach (var kvp in _spawnedItems)
                        if (!activeIds.Contains(kvp.Key)) toRemove.Add(kvp.Key);
                    foreach (var id in toRemove)
                    {
                        Destroy(_spawnedItems[id].gameObject);
                        _spawnedItems.Remove(id);
                    }
                },
                error => Debug.LogWarning($"[WYAARItemManager] Fetch failed: {error}"));
        }

        private void SpawnItem(WorldItem item)
        {
            var prefab = item.Type switch
            {
                ItemType.SupplyCache   => supplyCachePrefab,
                ItemType.DeadDrop      => deadDropPrefab,
                ItemType.WardenMarker  => wardenMarkerPrefab,
                ItemType.FactionCache  => factionCachePrefab,
                _                     => deadDropPrefab
            };

            if (prefab == null)
            {
                Debug.LogWarning($"[WYAARItemManager] No prefab for ItemType {item.Type}");
                return;
            }

            var go     = Instantiate(prefab);
            var anchor = go.GetComponent<WYAARItemAnchor>();
            if (anchor == null) anchor = go.AddComponent<WYAARItemAnchor>();
            anchor.Initialise(item);
            _spawnedItems[item.Id] = anchor;
        }

        /// <summary>
        /// Attempt to claim an item. Validates player is within claimRadiusMetres.
        /// Called by WYAARItemAnchor on tap.
        /// </summary>
        public void TryClaimItem(WYAARItemAnchor anchor)
        {
            var item     = anchor.Item;
            var itemCoord = new GeoCoord(item.Latitude, item.Longitude);
            var playerPos = _locationService.CurrentPosition;
            double dist  = WYAGeoMath.HaversineDistance(playerPos, itemCoord);

            if (dist > claimRadiusMetres)
            {
                Debug.Log($"[WYAARItemManager] Too far to claim ({dist:F0}m away, need {claimRadiusMetres}m)");
                // TODO: show UI "Get closer to claim this item"
                return;
            }

            var request = new ClaimItemRequest
            {
                ItemId   = item.Id,
                PlayerId = "TODO_player_id" // replace with session player ID
            };

            _apiClient.ClaimItem(request,
                claimed =>
                {
                    Debug.Log($"[WYAARItemManager] Claimed item {claimed.Id}");
                    Destroy(anchor.gameObject);
                    _spawnedItems.Remove(claimed.Id);
                    // TODO: add items to player inventory
                },
                error => Debug.LogWarning($"[WYAARItemManager] Claim failed: {error}"));
        }
    }
}
