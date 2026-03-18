using System;

namespace WhereYouAre.Location
{
    public enum LocationSource
    {
        None,
        DeviceGPS,
        Cached,
        Manual,
        Default
    }

    [Serializable]
    public struct GeoCoord
    {
        public double Latitude;
        public double Longitude;
        public double Altitude; // metres above sea level
        public LocationSource Source;

        public bool IsValid => Latitude != 0.0 || Longitude != 0.0;

        public GeoCoord(double lat, double lon, double alt = 0.0, LocationSource source = LocationSource.None)
        {
            Latitude  = lat;
            Longitude = lon;
            Altitude  = alt;
            Source    = source;
        }

        public override string ToString() => $"({Latitude:F6}, {Longitude:F6})";
    }
}
