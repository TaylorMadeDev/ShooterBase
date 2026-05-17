using UnityEngine;
using UnityEditor;
using System.IO;

public class AssetOrganizer : EditorWindow
{
    [MenuItem("Tools/Organize Assets")]
    public static void OrganizeProject()
    {
        // First, ensure all target directories exist
        EnsureFolderExists("Assets", "Materials");
        EnsureFolderExists("Assets/Materials", "VFX");
        EnsureFolderExists("Assets", "Textures");
        EnsureFolderExists("Assets/Textures", "VFX");
        EnsureFolderExists("Assets", "Prefabs");
        EnsureFolderExists("Assets/Prefabs", "VFX");
        EnsureFolderExists("Assets", "UI");
        EnsureFolderExists("Assets/UI", "Sprites");

        // Grab ALL files in Assets (excluding hidden packages/folders)
        string[] assetPaths = AssetDatabase.GetAllAssetPaths();

        foreach (string p in assetPaths)
        {
            string path = p;
            
            // Ignore things we shouldn't touch
            if (!path.StartsWith("Assets/") || path.Contains("/Editor/") || path.Contains("/Packages/") || path.Contains("TextMesh Pro")) continue;
            if (AssetDatabase.IsValidFolder(path)) continue;

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path).ToLower();

            string newName = fileNameWithoutExtension;
            string newFolder = "";

            // 1. MATERIALS (.mat)
            if (extension == ".mat")
            {
                if (fileNameWithoutExtension.Contains("VFX") || fileNameWithoutExtension == "Circle" || fileNameWithoutExtension == "Star")
                {
                    newFolder = "Assets/Materials/VFX";
                }
                else
                {
                    newFolder = "Assets/Materials";
                }

                if (!newName.StartsWith("MAT_")) newName = "MAT_" + newName;
                
                // Extra formatting safety 
                if (newName == "MAT_New Material") newName = "MAT_NewMaterial";
            }
            // 2. PREFABS (.prefab)
            else if (extension == ".prefab")
            {
                if (fileNameWithoutExtension == "BulletHole")
                {
                    newFolder = "Assets/Prefabs/VFX";
                    if (!newName.StartsWith("VFX_")) newName = "VFX_" + newName;
                }
                else
                {
                    // Ignore other prefabs for now (RustyBarrel, etc.)
                    continue;
                }
            }
            // 3. TEXTURES (.png, .jpg, .jpeg)
            else if (extension == ".png" || extension == ".jpg" || extension == ".jpeg")
            {
                string lower = fileNameWithoutExtension.ToLower();
                
                bool isUI = lower.Contains("hologram") || lower.Contains("part");
                bool isVFX = lower.Contains("circle") || lower.Contains("dirt") || lower.Contains("fire") || 
                             lower.Contains("flame") || lower.Contains("flare") || lower.Contains("light") || 
                             lower.Contains("magic") || lower.Contains("muzzle") || lower.Contains("scorch") || 
                             lower.Contains("scratch") || lower.Contains("slash") || lower.Contains("smoke") || 
                             lower.Contains("spark") || lower.Contains("star") || lower.Contains("symbol") || 
                             lower.Contains("trace") || lower.Contains("twirl") || lower.Contains("window");

                if (isUI)
                {
                    newFolder = "Assets/UI/Sprites";
                    if (!newName.StartsWith("SPR_") && !newName.StartsWith("UI_")) newName = "UI_" + newName;
                }
                else if (isVFX)
                {
                    newFolder = "Assets/Textures/VFX";
                    if (!newName.StartsWith("TEX_")) newName = "TEX_" + newName;
                }
                else
                {
                    newFolder = "Assets/Textures";
                    if (!newName.StartsWith("TEX_")) newName = "TEX_" + newName;
                }
            }
            else
            {
                // Not a material, prefab, or texture this script handles
                continue;
            }

            string currentFolder = Path.GetDirectoryName(path).Replace("\\", "/");
            string expectedNewPath = $"{newFolder}/{newName}{extension}";

            // Already handled / organized perfectly
            if (path == expectedNewPath) continue;

            // Rename first
            if (fileNameWithoutExtension != newName)
            {
                string err = AssetDatabase.RenameAsset(path, newName);
                if (string.IsNullOrEmpty(err))
                {
                    path = $"{currentFolder}/{newName}{extension}"; // Update tracked path for moving logic
                }
                else 
                { 
                    Debug.LogWarning($"[AssetOrganizer] Failed to rename {path} to {newName}: {err}"); 
                }
            }

            // Move final
            currentFolder = Path.GetDirectoryName(path).Replace("\\", "/");
            if (currentFolder != newFolder)
            {
                string err = AssetDatabase.MoveAsset(path, $"{newFolder}/{newName}{extension}");
                if (!string.IsNullOrEmpty(err)) 
                {
                    Debug.LogWarning($"[AssetOrganizer] Failed to move {path} to {newFolder}: {err}");
                }
            }
        }

        // Apply saves immediately
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[AssetOrganizer] Assets Organized Successfully!");
    }

    private static void EnsureFolderExists(string parentFolder, string newFolderName)
    {
        if (!AssetDatabase.IsValidFolder(parentFolder + "/" + newFolderName))
        {
            AssetDatabase.CreateFolder(parentFolder, newFolderName);
        }
    }
}
