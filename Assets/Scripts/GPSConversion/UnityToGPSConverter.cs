using UnityEngine;
using System;

public class UnityToGPSConverter : MonoBehaviour
{
    [Header("Reference GPS Coordinate")]
    public double refLatitude = 37.08650396057173;   // degrees
    public double refLongitude = -76.38087990000001;  // degrees
    public double refAltitude = 10.0;   // meters

    [Header("Reference Unity Position")]
    public Vector3 refUnityPosition;

    // WGS84 ellipsoid constants
    const double a = 6378137.0;         // semi-major axis in meters
    const double f = 1 / 298.257223563; // flattening
    const double b = a * (1 - f);       // semi-minor axis
    const double e2 = 1 - (b * b) / (a * a); // eccentricity squared

    /// <summary>
    /// Converts a Unity world position to GPS coordinates (lat, lon, alt).
    /// </summary>
    public (double lat, double lon, double alt) UnityToGPS(Vector3 unityPosition)
    {
        // Step 1: Convert reference GPS to ECEF
        Vector3d refECEF = GeodeticToECEF(refLatitude, refLongitude, refAltitude);

        // Step 2: Compute local ENU offset from Unity position
        Vector3 offset = unityPosition - refUnityPosition;
        Vector3d enuOffset = new Vector3d(offset.x, offset.y, offset.z); // Unity: x=East, y=Up, z=North

        // Step 3: Convert ENU offset to ECEF
        Vector3d ecef = ENUToECEF(enuOffset, refLatitude, refLongitude, refECEF);

        // Step 4: Convert ECEF to GPS
        return ECEFToGeodetic(ecef);
    }

    Vector3d GeodeticToECEF(double latDeg, double lonDeg, double alt)
    {
        double lat = Mathf.Deg2Rad * latDeg;
        double lon = Mathf.Deg2Rad * lonDeg;

        double N = a / Math.Sqrt(1 - e2 * Math.Sin(lat) * Math.Sin(lat));

        double x = (N + alt) * Math.Cos(lat) * Math.Cos(lon);
        double y = (N + alt) * Math.Cos(lat) * Math.Sin(lon);
        double z = ((1 - e2) * N + alt) * Math.Sin(lat);

        return new Vector3d(x, y, z);
    }

    Vector3d ENUToECEF(Vector3d enu, double latDeg, double lonDeg, Vector3d refECEF)
    {
        double lat = Mathf.Deg2Rad * latDeg;
        double lon = Mathf.Deg2Rad * lonDeg;

        double sinLat = Math.Sin(lat);
        double cosLat = Math.Cos(lat);
        double sinLon = Math.Sin(lon);
        double cosLon = Math.Cos(lon);

        double x = -sinLon * enu.x - sinLat * cosLon * enu.z + cosLat * cosLon * enu.y;
        double y = cosLon * enu.x - sinLat * sinLon * enu.z + cosLat * sinLon * enu.y;
        double z = cosLat * enu.z + sinLat * enu.y;

        return new Vector3d(refECEF.x + x, refECEF.y + y, refECEF.z + z);
    }

    (double lat, double lon, double alt) ECEFToGeodetic(Vector3d ecef)
    {
        double x = ecef.x;
        double y = ecef.y;
        double z = ecef.z;

        double lon = Math.Atan2(y, x);
        double p = Math.Sqrt(x * x + y * y);
        double theta = Math.Atan2(z * a, p * b);
        double sinTheta = Math.Sin(theta);
        double cosTheta = Math.Cos(theta);

        double lat = Math.Atan2(z + e2 * b * sinTheta * sinTheta * sinTheta,
                                p - e2 * a * cosTheta * cosTheta * cosTheta);

        double N = a / Math.Sqrt(1 - e2 * Math.Sin(lat) * Math.Sin(lat));
        double alt = p / Math.Cos(lat) - N;

        return (Mathf.Rad2Deg * lat, Mathf.Rad2Deg * lon, alt);
    }
}

/// <summary>
/// Double precision vector3 for geodetic calculations.
/// </summary>
public struct Vector3d
{
    public double x, y, z;

    public Vector3d(double x, double y, double z)
    {
        this.x = x; this.y = y; this.z = z;
    }

    public static Vector3d operator +(Vector3d a, Vector3d b)
        => new Vector3d(a.x + b.x, a.y + b.y, a.z + b.z);

    public override string ToString()
        => $"({x:F3}, {y:F3}, {z:F3})";
}
