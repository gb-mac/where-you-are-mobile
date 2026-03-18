using UnityEngine;
using UnityEngine.XR.ARFoundation;
using WhereYouAre.Location;
using WhereYouAre.Network;

namespace WhereYouAre.AR
{
    /// <summary>
    /// Placed on each AR world item prefab.
    /// Positions the object in AR space based on its GPS coordinate,
    /// relative to the session origin tracked by WYALocationService.
    /// </summary>
    [RequireComponent(typeof(ARAnchor))]
    public class WYAARItemAnchor : MonoBehaviour
    {
        public WorldItem Item { get; private set; }

        private ARAnchor   _anchor;
        private bool       _positionSet;

        private void Awake() => _anchor = GetComponent<ARAnchor>();

        /// <summary>Initialise this anchor with item data. Call immediately after Instantiate.</summary>
        public void Initialise(WorldItem item)
        {
            Item = item;
            UpdateWorldPosition();
        }

        private void Update()
        {
            if (!_positionSet) UpdateWorldPosition();
        }

        private void UpdateWorldPosition()
        {
            var locationService = WYALocationService.Instance;
            if (locationService == null || !locationService.IsResolved) return;

            var itemCoord = new GeoCoord(Item.Latitude, Item.Longitude, Item.Altitude);
            Vector3 worldPos = locationService.GeoToWorld(itemCoord);

            transform.position = worldPos;
            _positionSet = true;
        }

        private void OnMouseDown() => WYAARItemManager.Instance?.TryClaimItem(this);
    }
}
