using UnityEngine;

/// <summary>
/// A simple struct to hold latitude, longitude, and altitude.
/// </summary>
[System.Serializable]
public struct GpsData
{
    public double latitude;
    public double longitude;
    public double altitude;

    public override string ToString()
    {
        return $"Lat: {latitude:F6}, Lon: {longitude:F6}, Alt: {altitude:F2}m";
    }
}


/// <summary>
/// Converts Unity world coordinates to GPS coordinates based on a reference point.
/// This script uses a flat-earth approximation and is suitable for local-scale applications.
/// </summary>
public class GpsConverter : MonoBehaviour
{
    // WGS 84 ellipsoid radius in meters. This is a standard value for GPS calculations.
    private const double EarthRadius = 6378137.0;

    [Header("GPS Reference Point")]
    [Tooltip("Latitude of the real-world origin point.")]
    public double referenceLatitude = 37.08650396057173; // Default: New York City

    [Tooltip("Longitude of the real-world origin point.")]
    public double referenceLongitude = -76.38087990000001; // Default: New York City

    [Tooltip("Altitude of the real-world origin point in meters.")]
    public double referenceAltitude = 10.0;

    // The Unity position that corresponds to the reference GPS coordinate.
    // By default, this is the position of the GameObject this script is attached to.
    private Vector3 referenceUnityPosition;

    // Cached values for conversion calculations to improve performance.
    private double metersPerDegreeLat;
    private double metersPerDegreeLon;


    private void Awake()
    {
        // Set the Unity reference position to this object's starting position.
        referenceUnityPosition = transform.position;
        
        // Pre-calculate the meters-per-degree values for this specific latitude.
        CalculateMetersPerDegree();
    }

    /// <summary>
    /// Calculates and caches the number of meters per degree of longitude and latitude
    /// at the reference latitude.
    /// </summary>
    private void CalculateMetersPerDegree()
    {
        // Convert reference latitude to radians for trigonometric functions.
        double refLatRad = referenceLatitude * Mathf.Deg2Rad;

        // The distance for one degree of latitude is relatively constant.
        metersPerDegreeLat = (System.Math.PI / 180.0) * EarthRadius;
        
        // The distance for one degree of longitude depends on the latitude.
        metersPerDegreeLon = metersPerDegreeLat * System.Math.Cos(refLatRad);
    }
    
    /// <summary>
    /// Converts a Unity world coordinate to GPS data.
    /// </summary>
    /// <param name="unityPosition">The Unity world position to convert.</param>
    /// <returns>The corresponding GPS data (Lat, Lon, Alt).</returns>
    public GpsData ConvertUnityToGps(Vector3 unityPosition)
    {
        // Calculate the vector offset from the Unity reference point.
        // Assumes standard mapping: +X=East, +Y=Up, +Z=North
        Vector3 offset = unityPosition - referenceUnityPosition;
        float northOffset = offset.z;
        float eastOffset = offset.x;
        float upOffset = offset.y;

        // Calculate the change in latitude and longitude in degrees.
        double latOffsetDegrees = northOffset / metersPerDegreeLat;
        double lonOffsetDegrees = eastOffset / metersPerDegreeLon;

        // Apply the offsets to the reference GPS coordinate.
        GpsData newGpsData = new GpsData
        {
            latitude = referenceLatitude + latOffsetDegrees,
            longitude = referenceLongitude + lonOffsetDegrees,
            altitude = referenceAltitude + upOffset
        };

        return newGpsData;
    }

    #region Example Usage
    [Header("Testing")]
    [Tooltip("Assign a Transform here to see its calculated GPS coordinates in the console.")]
    public Transform targetObject;

    // In the editor, you can see the updates if you move the targetObject.
    private void Update()
    {
        if (targetObject != null)
        {
            GpsData targetGps = ConvertUnityToGps(targetObject.position);
            Debug.Log($"{targetObject.name}'s GPS Coordinate: {targetGps}");
        }
    }
    #endregion
}