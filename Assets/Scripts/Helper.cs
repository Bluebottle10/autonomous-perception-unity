using UnityEngine;

public static class Helper
{
    public static void PutOnTheGround(Transform transform, float offset = 0)
    {
        // reposition the transform 5m above the max height
        transform.position = new Vector3(transform.position.x, GetMaxElevation() + 5f, transform.position.z);
        
        // place on the ground + offset
        RaycastHit hit;
        // Does the ray intersect any objects excluding the player layer
        if (Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Infinity, -1))
        { 
            // Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.down) * hit.distance, Color.yellow); 
            transform.position = hit.point + new Vector3(0, offset, 0);
            // Debug.Log($"{hit.point.y} + {offset} = {transform.position.y}");
            // Debug.Log($"Did Hit {instance.transform.name}, {hit.distance}"); 
        }
        else
        { 
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.down) * 1000, Color.red); 
            Debug.Log($"Did not Hit {transform.name}");
        }
    }
    
    public static void PutOnTheGroundWithInitialHeight(Transform transform, float initialHeight, float offset = 0)
    {
        // reposition the transform 5m above the max height
        transform.position = new Vector3(transform.position.x, initialHeight, transform.position.z);
        
        // place on the ground + offset
        RaycastHit hit;
        // Does the ray intersect any objects excluding the player layer
        if (Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Infinity, -1))
        { 
            // Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.down) * hit.distance, Color.yellow); 
            transform.position = hit.point + new Vector3(0, offset, 0);
            // Debug.Log($"{hit.point.y} + {offset} = {transform.position.y}");
            // Debug.Log($"Did Hit {instance.transform.name}, {hit.distance}"); 
        }
        else
        { 
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.down) * 1000, Color.red); 
            Debug.Log($"Did not Hit {transform.name}");
        }
    }
    
    public static float CalculateElevationFromGround(Transform transform)
    {
        RaycastHit hit;
        // Does the ray intersect any objects excluding the player layer
        if (Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Infinity, -1))
        { 
            return hit.distance;
        }
        else
        { 
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.down) * 1000, Color.red); 
            Debug.Log($"Did not Hit {transform.name}");
        }

        return -1f;
    }

    public static float GetMaxElevation()
    {
        float maxHeight = 0f;
        
        // find all the terrains and enumerate each one of them
        Terrain[] terrains = Terrain.activeTerrains;
        if (terrains != null && terrains.Length > 0)
        {
            foreach (Terrain terrain in terrains)
            {
                // find max height of a terrain
                TerrainData terrainData = terrain.terrainData;
                int resolution = terrainData.heightmapResolution;
                float[,] heights = terrainData.GetHeights(0, 0, resolution, resolution);

                float maxNormalizedHeight = 0f;

                // Iterate through all points in the heightmap
                // Performance note: This can be slow for very large terrains if called frequently.
                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        if (heights[y, x] > maxNormalizedHeight)
                        {
                            maxNormalizedHeight = heights[y, x];
                        }
                    }
                }
                
                // actual height
                float terrainMaxHeight = maxNormalizedHeight * terrainData.size.y;
                if (terrainMaxHeight > maxHeight)
                    maxHeight = terrainMaxHeight;
            }

            return maxHeight;
        }
        else
        {
            Debug.LogError("No terrains found in the scene.");
            return 0f;
        }
    }
}
