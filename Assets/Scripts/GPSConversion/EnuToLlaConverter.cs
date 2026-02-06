using System;
using UnityEngine;

public class EnuToLlaConverter
{
    // WGS84 ellipsoid constants
    private const double WGS84_A = 6378137.0; // semi-major axis in meters
    private const double WGS84_F = 1.0 / 298.257223563; // flattening
    private const double WGS84_B = WGS84_A * (1.0 - WGS84_F); // semi-minor axis
    private const double WGS84_E2 = 2.0 * WGS84_F - WGS84_F * WGS84_F; // eccentricity squared

    /// <summary>
    /// Converts ENU coordinates to Latitude, Longitude, and Altitude (LLA).
    /// </summary>
    /// <param name="e">East coordinate in meters</param>
    /// <param name="n">North coordinate in meters</param>
    /// <param name="u">Up coordinate in meters</param>
    /// <param name="refLat">Reference latitude in degrees</param>
    /// <param name="refLon">Reference longitude in degrees</param>
    /// <param name="refAlt">Reference altitude in meters</param>
    /// <returns>Tuple containing (latitude, longitude, altitude) in (degrees, degrees, meters)</returns>
    public static (double latitude, double longitude, double altitude) EnuToLla(
        double e, double n, double u, 
        double refLat, double refLon, double refAlt)
    {
        // Convert degrees to radians
        double refLatRad = DegreesToRadians(refLat);
        double refLonRad = DegreesToRadians(refLon);

        // Convert reference LLA to ECEF
        var (xRef, yRef, zRef) = LlaToEcef(refLatRad, refLonRad, refAlt);

        // Calculate rotation matrix for ENU to ECEF
        double sinLat = Math.Sin(refLatRad);
        double cosLat = Math.Cos(refLatRad);
        double sinLon = Math.Sin(refLonRad);
        double cosLon = Math.Cos(refLonRad);

        // Rotation matrix from ENU to ECEF
        double[,] rotation = new double[3, 3]
        {
            { -sinLon, cosLon, 0 },
            { -sinLat * cosLon, -sinLat * sinLon, cosLat },
            { cosLat * cosLon, cosLat * sinLon, sinLat }
        };

        // Transform ENU to ECEF
        double deltaX = rotation[0, 0] * e + rotation[0, 1] * n + rotation[0, 2] * u;
        double deltaY = rotation[1, 0] * e + rotation[1, 1] * n + rotation[1, 2] * u;
        double deltaZ = rotation[2, 0] * e + rotation[2, 1] * n + rotation[2, 2] * u;

        double xEcef = xRef + deltaX;
        double yEcef = yRef + deltaY;
        double zEcef = zRef + deltaZ;

        // Convert ECEF to LLA
        return EcefToLla(xEcef, yEcef, zEcef);
    }

    /// <summary>
    /// Converts LLA to ECEF coordinates.
    /// </summary>
    private static (double x, double y, double z) LlaToEcef(double latRad, double lonRad, double alt)
    {
        double sinLat = Math.Sin(latRad);
        double cosLat = Math.Cos(latRad);
        double sinLon = Math.Sin(lonRad);
        double cosLon = Math.Cos(lonRad);

        // Radius of curvature in the prime vertical
        double N = WGS84_A / Math.Sqrt(1.0 - WGS84_E2 * sinLat * sinLat);

        double x = (N + alt) * cosLat * cosLon;
        double y = (N + alt) * cosLat * sinLon;
        double z = ((1.0 - WGS84_E2) * N + alt) * sinLat;

        return (x, y, z);
    }

    /// <summary>
    /// Converts ECEF to LLA coordinates using iterative method.
    /// </summary>
    private static (double latitude, double longitude, double altitude) EcefToLla(double x, double y, double z)
    {
        double lon = Math.Atan2(y, x);
        
        // Iterative method for latitude and altitude
        double p = Math.Sqrt(x * x + y * y);
        double lat = Math.Atan2(z, p * (1.0 - WGS84_E2));
        double alt = 0.0;
        const int MAX_ITERATIONS = 100;
        const double EPSILON = 1e-12;

        for (int i = 0; i < MAX_ITERATIONS; i++)
        {
            double sinLat = Math.Sin(lat);
            double N = WGS84_A / Math.Sqrt(1.0 - WGS84_E2 * sinLat * sinLat);
            double prevAlt = alt;
            alt = p / Math.Cos(lat) - N;
            double prevLat = lat;
            lat = Math.Atan2(z, p * (1.0 - WGS84_E2 * N / (N + alt)));

            if (Math.Abs(lat - prevLat) < EPSILON && Math.Abs(alt - prevAlt) < EPSILON)
                break;
        }

        return (RadiansToDegrees(lat), RadiansToDegrees(lon), alt);
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    private static double RadiansToDegrees(double radians)
    {
        return radians * 180.0 / Math.PI;
    }

    // Example usage
    // public static void Main()
    // {
    //     // Example ENU coordinates (meters) and reference point (degrees, meters)
    //     double east = 100.0;
    //     double north = 100.0;
    //     double up = 10.0;
    //     double refLat = 40.0; // Example reference latitude
    //     double refLon = -74.0; // Example reference longitude
    //     double refAlt = 0.0; // Example reference altitude
    //     37.08650396057173, -76.38087990000001
    //
    //     var (lat, lon, alt) = EnuToLla(east, north, up, refLat, refLon, refAlt);
    //     Console.WriteLine($"Latitude: {lat:F8} degrees");
    //     Console.WriteLine($"Longitude: {lon:F8} degrees");
    //     Console.WriteLine($"Altitude: {alt:F2} meters");
    // }
}