using UnityEngine;
using UnityEditor;
using System.IO; // Required for Path operations

// Place this script in a folder named "Editor" anywhere in your Assets folder.
public class ExtractAnimations : EditorWindow
{
    private const string MenuPath = "Assets/Extract Animations from FBX";

    // Add a menu item under "Assets"
    [MenuItem(MenuPath)]
    private static void ExtractSelectedFbxAnimations()
    {
        // Get all selected GameObjects in the Project window
        GameObject[] selectedObjects = Selection.GetFiltered<GameObject>(SelectionMode.Assets);

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("No FBX file selected in the Project window.");
            EditorUtility.DisplayDialog("No Selection", "Please select one or more FBX files in the Project window first.", "OK");
            return;
        }

        int extractedCount = 0;

        foreach (GameObject fbxObject in selectedObjects)
        {
            string assetPath = AssetDatabase.GetAssetPath(fbxObject);

            // Ensure it's actually an FBX file (basic check)
            if (!assetPath.ToLower().EndsWith(".fbx"))
            {
                Debug.LogWarning($"Skipping '{fbxObject.name}' because it doesn't appear to be an FBX file ({assetPath}).");
                continue;
            }

            Debug.Log($"Processing FBX: {assetPath}");

            // Load all assets contained within the FBX file
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

            string extractionFolder = Path.GetDirectoryName(assetPath);

            foreach (Object asset in assets)
            {
                // Check if the asset is an AnimationClip
                if (asset is AnimationClip clip)
                {
                    // Skip the internal __preview__ clips Unity generates
                    if (clip.name.StartsWith("__preview__"))
                    {
                        continue;
                    }

                    // Create a *copy* of the animation clip to make it a standalone asset
                    AnimationClip newClip = Object.Instantiate(clip);

                    // Define the path for the new .anim file
                    string animFilePath = Path.Combine(extractionFolder, $"{fbxObject.name}_{clip.name}.anim");
                    // Ensure the path is unique to avoid overwriting
                    string uniqueAnimFilePath = AssetDatabase.GenerateUniqueAssetPath(animFilePath);

                    // Create the .anim asset file
                    AssetDatabase.CreateAsset(newClip, uniqueAnimFilePath);

                    Debug.Log($"Extracted animation '{clip.name}' to '{uniqueAnimFilePath}'");
                    extractedCount++;
                }
            }
        }

        // Refresh the Asset Database to show the newly created files
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (extractedCount > 0)
        {
            Debug.Log($"Successfully extracted {extractedCount} animation clip(s).");
            EditorUtility.DisplayDialog("Extraction Complete", $"Successfully extracted {extractedCount} animation clip(s).\nCheck the console for details.", "OK");
        }
        else
        {
             Debug.LogWarning("No animation clips found in the selected FBX files (or only internal clips were found).");
             EditorUtility.DisplayDialog("No Animations Found", "No extractable animation clips were found in the selected FBX file(s).", "OK");
        }
    }

    // Optional: Add a validation function to enable the menu item only when an asset is selected
    [MenuItem(MenuPath, true)]
    private static bool ValidateExtractSelectedFbxAnimations()
    {
        // Enable the menu item only if at least one asset is selected in the Project view
        return Selection.activeObject != null;
    }
}