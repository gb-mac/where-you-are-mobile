using UnityEngine;

namespace WhereYouAre.Location
{
    /// <summary>
    /// Pure static math: geodetic (WGS84) ↔ Unity world space via ENU local tangent plane.
    ///
    /// Unity axis mapping (matches UE5 desktop conversion):
    ///   X = East  (Right)
    ///   Y = Up
    ///   Z = North (Forward)
    ///
    /// Scale: 1 Unity unit = 1 metre (unlike UE5 where 1 UU = 1 cm).
    /// </summary>
    public static class WYAGeoMath
    {
        // WGS84 ellipsoid
        private const double A  = 6378137.0;
        private const double E2 = 0.00669437999014;

        /// <summary>Convert GPS coordinate to Unity world space relative to origin.</summary>
        public static Vector3 GeoToWorld(GeoCoord target, GeoCoord origin)
        {
            var tECEF = GeoToECEF(target);
            var oECEF = GeoToECEF(origin);

            var delta = new ECEF
            {
                X = tECEF.X - oECEF.X,
                Y = tECEF.Y - oECEF.Y,
                Z = tECEF.Z - oECEF.Z
            };

            return ECEFDeltaToUnity(delta, origin);
        }

        /// <summary>Convert Unity world position back to GPS coordinate given the session origin.</summary>
        public static GeoCoord WorldToGeo(Vector3 worldPos, GeoCoord origin)
        {
            // Unity: X=East, Y=Up, Z=North (metres)
            double east  = worldPos.x;
            double up    = worldPos.y;
            double north = worldPos.z;

            double latRad = origin.Latitude  * Mathf.Deg2Rad;
            double lonRad = origin.Longitude * Mathf.Deg2Rad;

            double sinLat = System.Math.Sin(latRad);
            double cosLat = System.Math.Cos(latRad);
            double sinLon = System.Math.Sin(lonRad);
            double cosLon = System.Math.Cos(lonRad);

            var oECEF = GeoToECEF(origin);
            var delta = new ECEF
            {
                X = -sinLon * east - sinLat * cosLon * north + cosLat * cosLon * up,
                Y =  cosLon * east - sinLat * sinLon * north + cosLat * sinLon * up,
                Z =  cosLat * north + sinLat * up
            };

            var tECEF = new ECEF { X = oECEF.X + delta.X, Y = oECEF.Y + delta.Y, Z = oECEF.Z + delta.Z };

            // ECEF → geodetic (iterative)
            double p   = System.Math.Sqrt(tECEF.X * tECEF.X + tECEF.Y * tECEF.Y);
            double lon = System.Math.Atan2(tECEF.Y, tECEF.X);
            double lat = System.Math.Atan2(tECEF.Z, p * (1.0 - E2));

            for (int i = 0; i < 5; i++)
            {
                double sinL = System.Math.Sin(lat);
                double n    = A / System.Math.Sqrt(1.0 - E2 * sinL * sinL);
                lat = System.Math.Atan2(tECEF.Z + E2 * n * sinL, p);
            }

            double sinLatF = System.Math.Sin(lat);
            double N   = A / System.Math.Sqrt(1.0 - E2 * sinLatF * sinLatF);
            double alt = p / System.Math.Cos(lat) - N;

            return new GeoCoord(lat * Mathf.Rad2Deg, lon * Mathf.Rad2Deg, alt);
        }

        /// <summary>Distance in metres between two GPS coordinates (ignores altitude).</summary>
        public static double HaversineDistance(GeoCoord a, GeoCoord b)
        {
            double dLat = (b.Latitude  - a.Latitude)  * Mathf.Deg2Rad;
            double dLon = (b.Longitude - a.Longitude) * Mathf.Deg2Rad;
            double v    = System.Math.Sin(dLat / 2) * System.Math.Sin(dLat / 2)
                        + System.Math.Cos(a.Latitude * Mathf.Deg2Rad)
                        * System.Math.Cos(b.Latitude * Mathf.Deg2Rad)
                        * System.Math.Sin(dLon / 2) * System.Math.Sin(dLon / 2);
            return A * 2.0 * System.Math.Atan2(System.Math.Sqrt(v), System.Math.Sqrt(1.0 - v));
        }

        // ---------------------------------------------------------------

        private struct ECEF { public double X, Y, Z; }

        private static ECEF GeoToECEF(GeoCoord c)
        {
            double latRad = c.Latitude  * Mathf.Deg2Rad;
            double lonRad = c.Longitude * Mathf.Deg2Rad;
            double sinLat = System.Math.Sin(latRad);
            double n      = A / System.Math.Sqrt(1.0 - E2 * sinLat * sinLat);
            double r      = n + c.Altitude;
            return new ECEF
            {
                X = r * System.Math.Cos(latRad) * System.Math.Cos(lonRad),
                Y = r * System.Math.Cos(latRad) * System.Math.Sin(lonRad),
                Z = (n * (1.0 - E2) + c.Altitude) * sinLat
            };
        }

        private static Vector3 ECEFDeltaToUnity(ECEF delta, GeoCoord origin)
        {
            double latRad = origin.Latitude  * Mathf.Deg2Rad;
            double lonRad = origin.Longitude * Mathf.Deg2Rad;
            double sinLat = System.Math.Sin(latRad);
            double cosLat = System.Math.Cos(latRad);
            double sinLon = System.Math.Sin(lonRad);
            double cosLon = System.Math.Cos(lonRad);

            double east  = -sinLon * delta.X + cosLon * delta.Y;
            double north = -sinLat * cosLon * delta.X - sinLat * sinLon * delta.Y + cosLat * delta.Z;
            double up    =  cosLat * cosLon * delta.X + cosLat * sinLon * delta.Y + sinLat * delta.Z;

            // Unity: X=East, Y=Up, Z=North (1 unit = 1 metre)
            return new Vector3((float)east, (float)up, (float)north);
        }
    }
}
