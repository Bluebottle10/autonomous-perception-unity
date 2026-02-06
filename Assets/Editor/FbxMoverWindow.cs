using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

// Place this script in a folder named "Editor" anywhere in your Assets folder.
public class FbxMoverWindow : EditorWindow
{
    private string sourceFolderPath = "Assets"; // Default or last selected
    private string fileExtension = "asset";
    private string destinationRootName = "AnimationConfigs"; // Default name for the new root
    private Object sourceFolderObject = null;

    // Add a menu item to open this window
    [MenuItem("Tools/Move files with extensions")]
    private static void ShowWindow()
    {
        // Get existing open window or if none, make a new one docked next to Inspector
        FbxMoverWindow window = GetWindow<FbxMoverWindow>("Move files", true, typeof(SceneView));
        window.minSize = new Vector2(350, 200); // Set a minimum size
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Move Files to New Structure", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select a source root folder in your Project window containing files (possibly in subfolders). Enter a name for the new root folder where the structure will be replicated and files moved.", MessageType.Info);

        EditorGUILayout.Space(10);

        // --- Source Folder Selection ---
        GUILayout.Label("1. Source Root Folder", EditorStyles.label);
        sourceFolderObject = EditorGUILayout.ObjectField("Select Folder:", sourceFolderObject, typeof(DefaultAsset), false);

        // Validate if the selected object is a folder
        if (sourceFolderObject != null)
        {
            string path = AssetDatabase.GetAssetPath(sourceFolderObject);
            if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path))
            {
                sourceFolderPath = path;
                GUI.color = Color.white; // Reset color if valid
                EditorGUILayout.LabelField("Selected Path:", sourceFolderPath);
            }
            else
            {
                GUI.color = Color.red; // Indicate invalid selection
                EditorGUILayout.LabelField("Selected Path:", "Selection is not a valid folder!");
                sourceFolderObject = null; // Reset if not a folder
                sourceFolderPath = string.Empty;
                GUI.color = Color.white; // Reset color
            }
        }
        else
        {
             EditorGUILayout.LabelField("Selected Path:", "No folder selected.");
             sourceFolderPath = string.Empty;
        }
        
        EditorGUILayout.Space(10);

        // --- Destination Folder Name ---
        GUILayout.Label("2. File extension (file type)", EditorStyles.label);
        fileExtension = EditorGUILayout.TextField("File extension:", fileExtension);


        EditorGUILayout.Space(10);

        // --- Destination Folder Name ---
        GUILayout.Label("3. New Root Folder Name", EditorStyles.label);
        destinationRootName = EditorGUILayout.TextField("Destination Name:", destinationRootName);

        // Basic validation for destination name
        if (string.IsNullOrWhiteSpace(destinationRootName) || destinationRootName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            GUI.color = Color.red;
            EditorGUILayout.LabelField("Status:", "Invalid destination folder name.");
            GUI.color = Color.white;
        } else {
             EditorGUILayout.LabelField("Status:", $"Will create/use 'Assets/{destinationRootName}'");
        }


        EditorGUILayout.Space(20);

        // --- Process Button ---
        GUI.enabled = !string.IsNullOrEmpty(sourceFolderPath) &&
                      !string.IsNullOrWhiteSpace(destinationRootName) &&
                       destinationRootName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0; // Enable button only if inputs are valid

        if (GUILayout.Button("Process and Move Files", GUILayout.Height(40)))
        {
            // Construct full destination path
            string destinationRootPath = Path.Combine("Assets", destinationRootName).Replace("\\", "/"); // Ensure forward slashes

            // Prevent processing if source and destination overlap dangerously
            if (destinationRootPath.Equals(sourceFolderPath, System.StringComparison.OrdinalIgnoreCase) ||
                destinationRootPath.StartsWith(sourceFolderPath + "/", System.StringComparison.OrdinalIgnoreCase) ||
                sourceFolderPath.StartsWith(destinationRootPath + "/", System.StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("Error", "Source and Destination folders cannot be the same or nested within each other.", "OK");
            }
            else
            {
                 // Confirmation Dialog
                if (EditorUtility.DisplayDialog("Confirm Move Operation",
                    $"This will:\n" +
                    $"1. Scan for files in:\n   '{sourceFolderPath}'\n" +
                    $"2. Create a new structure under:\n   '{destinationRootPath}'\n" +
                    $"3. Move all found files.\n\n" +
                    $"Original files will be MOVED from the source folder.\n" +
                    $"This operation cannot be easily undone. Are you sure?",
                    "Yes, Move Files", "Cancel"))
                {
                    ProcessMoveOperation(sourceFolderPath, destinationRootPath);
                }
            }
        }
        GUI.enabled = true; // Re-enable GUI elements

        EditorGUILayout.Space(10);
    }


    private void ProcessMoveOperation(string sourceRoot, string destinationRoot)
    {
        Debug.Log($"Starting move process: Source='{sourceRoot}', Destination='{destinationRoot}'");
        int fbxMovedCount = 0;
        int dirsCreatedCount = 0;
        List<string> errors = new List<string>();

        try
        {
            // Ensure the root destination directory exists
            if (!AssetDatabase.IsValidFolder(destinationRoot))
            {
                // Need to potentially create intermediate folders if destinationRoot is nested
                 string parent = Path.GetDirectoryName(destinationRoot);
                 string newFolderName = Path.GetFileName(destinationRoot);
                 if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(newFolderName)) {
                     AssetDatabase.CreateFolder(parent, newFolderName);
                     Debug.Log($"Created root destination folder: {destinationRoot}");
                     dirsCreatedCount++;
                 } else if (parent == "Assets") { // Root level folder
                     AssetDatabase.CreateFolder("", newFolderName); // Create folder in Assets root
                     Debug.Log($"Created root destination folder: {destinationRoot}");
                     dirsCreatedCount++;
                 }
                 else {
                     errors.Add($"Failed to create root destination folder: {destinationRoot}. Invalid path structure?");
                 }
            }

            // Find all files recursively within the source directory
            // Use Directory.GetFiles with full path, then convert back to Asset path
            string sourceRootFullPath = Path.GetFullPath(sourceRoot);
            string[] fbxFilesFullPaths = Directory.GetFiles(sourceRootFullPath, $"*.{fileExtension}", SearchOption.AllDirectories);

            if (fbxFilesFullPaths.Length == 0)
            {
                Debug.LogWarning($"No files found within '{sourceRoot}'.");
            }

            // Process each found file
            foreach (string fbxFullPath in fbxFilesFullPaths)
            {
                // Convert full path back to Unity Asset Path (relative to project root)
                string sourceFbxPath = "Assets" + fbxFullPath.Substring(Application.dataPath.Length);
                sourceFbxPath = sourceFbxPath.Replace("\\", "/"); // Ensure forward slashes

                // Calculate the relative path within the source structure
                string relativePath = sourceFbxPath.Substring(sourceRoot.Length); // Includes leading '/' if not root
                if (relativePath.StartsWith("/"))
                {
                    relativePath = relativePath.Substring(1);
                }

                // Construct the destination path for the file
                string destinationFbxPath = Path.Combine(destinationRoot, relativePath).Replace("\\", "/");

                // Ensure the target directory structure exists within the destination
                string destinationDirectoryPath = Path.GetDirectoryName(destinationFbxPath).Replace("\\", "/");
                if (!AssetDatabase.IsValidFolder(destinationDirectoryPath))
                {
                    // We need to create potentially multiple nested folders
                    string currentPath = "Assets";
                    string[] folders = destinationDirectoryPath.Substring("Assets/".Length).Split('/');
                    foreach (string folder in folders)
                    {
                        string nextPath = Path.Combine(currentPath, folder).Replace("\\", "/");
                        if (!AssetDatabase.IsValidFolder(nextPath))
                        {
                             string result = AssetDatabase.CreateFolder(currentPath, folder);
                             if(string.IsNullOrEmpty(result)) {
                                Debug.Log($"Created directory: {nextPath}");
                                dirsCreatedCount++;
                             } else {
                                 string errorMsg = $"Failed to create directory '{nextPath}'. Error: {result}";
                                 errors.Add(errorMsg);
                                 Debug.LogError(errorMsg);
                                 goto NextFile; // Skip this file if folder creation fails
                             }
                        }
                        currentPath = nextPath;
                    }
                }

                // Move the asset using AssetDatabase (handles meta files etc.)
                string moveResult = AssetDatabase.MoveAsset(sourceFbxPath, destinationFbxPath);

                if (string.IsNullOrEmpty(moveResult))
                {
                    Debug.Log($"Moved '{sourceFbxPath}' -> '{destinationFbxPath}'");
                    fbxMovedCount++;
                }
                else
                {
                    string errorMsg = $"Failed to move '{sourceFbxPath}' to '{destinationFbxPath}'. Error: {moveResult}";
                    errors.Add(errorMsg);
                    Debug.LogError(errorMsg);
                }

                NextFile:; // Label to jump to for next iteration on error
            }

             // Refresh Asset Database to show changes
             Debug.Log("Refreshing Asset Database...");
             AssetDatabase.Refresh();

             // --- Final Report ---
             string summaryMessage = $"Operation Complete.\n\n" +
                                     $"Files Moved: {fbxMovedCount}\n" +
                                     $"Directories Created: {dirsCreatedCount}\n" +
                                     $"Errors: {errors.Count}";

             if (errors.Count > 0)
             {
                 summaryMessage += "\n\nSome errors occurred. Please check the Console for details.";
                 Debug.LogError("Move Operation completed with errors. See logs above.");
                 EditorUtility.DisplayDialog("Operation Finished with Errors", summaryMessage, "OK");
             }
             else
             {
                 Debug.Log($"Move Operation completed successfully. Moved {fbxMovedCount} files.");
                  EditorUtility.DisplayDialog("Operation Successful", summaryMessage, "OK");
             }

        }
        catch (System.Exception e)
        {
            Debug.LogError($"An unexpected error occurred during the move operation: {e.Message}\n{e.StackTrace}");
            EditorUtility.DisplayDialog("Critical Error", $"An unexpected error occurred: {e.Message}\n\nPlease check the Console for details.", "OK");
            AssetDatabase.Refresh(); // Refresh even on error
        }
    }
}