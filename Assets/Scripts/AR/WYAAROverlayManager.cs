using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using WhereYouAre.Location;
using WhereYouAre.Network;

namespace WhereYouAre.AR
{
    /// <summary>
    /// Renders faction territory overlays and Displacement Zones in AR.
    /// Territory polygons are fetched from the backend and drawn as
    /// semi-transparent meshes anchored to the ground plane.
    /// </summary>
    public class WYAAROverlayManager : MonoBehaviour
    {
        [Header("Territory Materials")]
        [SerializeField] private Material machinesTerritoryMat;
        [SerializeField] private Material humansTerritoryMat;
        [SerializeField] private Material wardensTerritoryMat;
        [SerializeField] private Material excommunicadoTerritoryMat;
        [SerializeField] private Material displacementZoneMat;

        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private float          overlayHeight = 0.05f; // metres above ground
        [SerializeField] private float          fetchIntervalSecs = 60f;

        private readonly List<GameObject> _overlayObjects = new();
        private WYALocationService _locationService;
        private WYAItemStateClient _apiClient;

        private void Start()
        {
            _locationService = WYALocationService.Instance;
            _apiClient       = WYAItemStateClient.Instance;
            _locationService.OnLocationResolved += OnLocationResolved;
        }

        private void OnLocationResolved(GeoCoord origin, bool bSuccess)
        {
            FetchAndRenderTerritory();
            StartCoroutine(PeriodicFetch());
        }

        private IEnumerator PeriodicFetch()
        {
            while (true)
            {
                yield return new WaitForSeconds(fetchIntervalSecs);
                FetchAndRenderTerritory();
            }
        }

        private void FetchAndRenderTerritory()
        {
            var pos = _locationService.CurrentPosition;
            _apiClient.FetchFactionTerritory(pos.Latitude, pos.Longitude, 1000f, territories =>
            {
                ClearOverlays();
                foreach (var territory in territories)
                    RenderTerritory(territory);
            });
        }

        private void RenderTerritory(FactionTerritory territory)
        {
            if (territory.BoundaryPoints == null || territory.BoundaryPoints.Count < 3) return;

            var mat = territory.Faction switch
            {
                FactionId.Machines      => machinesTerritoryMat,
                FactionId.Humans        => humansTerritoryMat,
                FactionId.Wardens       => wardensTerritoryMat,
                FactionId.Excommunicado => excommunicadoTerritoryMat,
                _                      => null
            };
            if (mat == null) return;

            var go  = new GameObject($"Territory_{territory.Faction}");
            var mf  = go.AddComponent<MeshFilter>();
            var mr  = go.AddComponent<MeshRenderer>();
            mr.material = mat;

            // Build vertices from GPS boundary points
            var verts = new Vector3[territory.BoundaryPoints.Count];
            for (int i = 0; i < territory.BoundaryPoints.Count; i++)
            {
                var pt = territory.BoundaryPoints[i];
                var coord = new GeoCoord(pt.Lat, pt.Lon);
                Vector3 worldPos = _locationService.GeoToWorld(coord);
                verts[i] = new Vector3(worldPos.x, overlayHeight, worldPos.z);
            }

            mf.mesh = BuildPolygonMesh(verts);
            _overlayObjects.Add(go);
        }

        private void ClearOverlays()
        {
            foreach (var go in _overlayObjects) Destroy(go);
            _overlayObjects.Clear();
        }

        /// <summary>Simple fan triangulation for convex polygons.</summary>
        private static Mesh BuildPolygonMesh(Vector3[] verts)
        {
            var mesh = new Mesh();
            mesh.vertices = verts;

            int triCount = verts.Length - 2;
            int[] tris   = new int[triCount * 3];
            for (int i = 0; i < triCount; i++)
            {
                tris[i * 3]     = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = i + 2;
            }
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
