using UnityEngine;
using System.Collections.Generic;

// Required for Unity's official Splines package
using UnityEngine.Splines;


public class SplinePointExtractor : MonoBehaviour
{
    [Header("Target Spline GameObject")]
    [Tooltip("If null, will try to get SplineContainer from this GameObject.")]
    public GameObject targetSplineObject;

    [Header("Extraction Options")]
    [Tooltip("If true, extracts the control points (knots) of the spline. If false, samples the spline at a given resolution.")]
    public bool extractControlPoints = true;

    [Tooltip("Number of samples to take along the spline if not extracting control points. Higher values mean more points and a more detailed representation.")]
    public int samplesPerSpline = 100; // Only used if extractControlPoints is false

    [Header("Extracted Points (Read-Only)")]
    public List<Vector3> extractedWorldPoints = new List<Vector3>();

    // --- For Unity's Official Splines Package ---
    private SplineContainer _splineContainer;


    // --- Placeholder for Third-Party Spline Assets ---
    // Example: public DreamTeck.Splines.SplineComputer dreamteckSpline;
    // Example: public CurvySpline curvySpline;

    void Awake()
    {
        if (targetSplineObject == null)
        {
            targetSplineObject = this.gameObject; // Default to this GameObject
        }
        
        // Try to get the SplineContainer from the target GameObject
        _splineContainer = targetSplineObject.GetComponent<SplineContainer>();

        if (_splineContainer == null)
        {
            Debug.LogWarning($"SplineContainer not found on '{targetSplineObject.name}'. Make sure the Splines package is installed and a SplineContainer component is attached.", this);
            return;
        }

        Debug.LogWarning("Unity Splines package not detected. Install it via Package Manager for SplineContainer support. For other spline assets, you'll need to modify this script.", this);

        return; // Exit if no Unity Spline and no other handler

    }

    [ContextMenu("Extract Spline Points Now")] // Allows triggering from Inspector
    public void ExtractPoints()
    {
        extractedWorldPoints.Clear();


        if (_splineContainer != null)
        {
            ExtractUnitySplinePoints();
            return;
        }
    }


    private void ExtractUnitySplinePoints()
    {
        if (_splineContainer.Spline == null)
        {
            Debug.LogWarning("SplineContainer has no active Spline.", this);
            return;
        }

        Spline spline = _splineContainer.Spline;

        if (extractControlPoints)
        {
            // Extracting Knot (Control Point) positions
            // Knots are in local space of the SplineContainer's transform
            foreach (BezierKnot knot in spline.Knots)
            {
                // Convert local knot position to world position
                Vector3 worldPosition = _splineContainer.transform.TransformPoint(knot.Position);
                extractedWorldPoints.Add(worldPosition);
            }
            Debug.Log($"Extracted {extractedWorldPoints.Count} control points from Unity Spline.", this);
        }
        else
        {
            // Sampling the spline at a resolution
            if (samplesPerSpline <= 1)
            {
                Debug.LogWarning("Samples per spline must be greater than 1 for sampling.", this);
                samplesPerSpline = 2; // Ensure at least start and end
            }

            for (int i = 0; i < samplesPerSpline; i++)
            {
                float normalizedTime = (float)i / (samplesPerSpline - 1); // t from 0 to 1
                // EvaluatePosition expects local space t, and returns local space position
                Vector3 localPosition = spline.EvaluatePosition(normalizedTime);
                // Convert local spline point to world position
                Vector3 worldPosition = _splineContainer.transform.TransformPoint(localPosition);
                extractedWorldPoints.Add(worldPosition);
            }
            Debug.Log($"Extracted {extractedWorldPoints.Count} sampled points from Unity Spline.", this);
        }
    }

    // Optional: Visualize extracted points in the editor
    void OnDrawGizmosSelected()
    {
        if (extractedWorldPoints.Count == 0) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < extractedWorldPoints.Count; i++)
        {
            Gizmos.DrawSphere(extractedWorldPoints[i], 0.1f); // Adjust radius as needed
            if (i < extractedWorldPoints.Count - 1)
            {
                Gizmos.DrawLine(extractedWorldPoints[i], extractedWorldPoints[i + 1]);
            }
        }
    }
}