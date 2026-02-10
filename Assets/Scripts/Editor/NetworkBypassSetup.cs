using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor utility to create NetworkBypassManager prefab and setup
/// </summary>
public class NetworkBypassSetup
{
    [MenuItem("Tools/Network Bypass/Create NetworkBypassManager Prefab")]
    public static void CreateNetworkBypassPrefab()
    {
        // Create GameObject
        GameObject go = new GameObject("NetworkBypassManager");
        
        // Add component
        var manager = go.AddComponent<NetworkBypass.NetworkBypassManager>();
        
        // Configure default settings
        manager.primaryProvider = NetworkBypass.NetworkBypassManager.DoHProvider.Cloudflare;
        manager.fallbackProvider = NetworkBypass.NetworkBypassManager.DoHProvider.Google;
        manager.enableCache = true;
        manager.cacheExpirationSeconds = 300;
        manager.queryTimeoutSeconds = 5;
        manager.enableDebugLogs = true;
        
        // Create Prefabs folder if it doesn't exist
        string prefabFolder = "Assets/Prefabs";
        if (!Directory.Exists(prefabFolder))
        {
            Directory.CreateDirectory(prefabFolder);
            AssetDatabase.Refresh();
        }
        
        // Save as prefab
        string prefabPath = $"{prefabFolder}/NetworkBypassManager.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        
        Debug.Log($"✅ NetworkBypassManager prefab created at: {prefabPath}");
        Debug.Log("💡 Drag this prefab into your scene to enable network bypass!");
        
        // Clean up scene object
        Object.DestroyImmediate(go);
        
        // Select the prefab
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
    }
    
    [MenuItem("Tools/Network Bypass/Add to Current Scene")]
    public static void AddToCurrentScene()
    {
        // Check if already exists
        var existing = GameObject.FindObjectOfType<NetworkBypass.NetworkBypassManager>();
        if (existing != null)
        {
            Debug.LogWarning("⚠️ NetworkBypassManager already exists in scene!");
            Selection.activeGameObject = existing.gameObject;
            return;
        }
        
        // Create GameObject
        GameObject go = new GameObject("NetworkBypassManager");
        
        // Add component
        var manager = go.AddComponent<NetworkBypass.NetworkBypassManager>();
        
        // Configure default settings
        manager.primaryProvider = NetworkBypass.NetworkBypassManager.DoHProvider.Cloudflare;
        manager.fallbackProvider = NetworkBypass.NetworkBypassManager.DoHProvider.Google;
        manager.enableCache = true;
        manager.cacheExpirationSeconds = 300;
        manager.queryTimeoutSeconds = 5;
        manager.enableDebugLogs = true;
        
        Debug.Log("✅ NetworkBypassManager added to scene!");
        
        // Select the object
        Selection.activeGameObject = go;
    }
    
    [MenuItem("Tools/Network Bypass/Create Example Scene")]
    public static void CreateExampleScene()
    {
        // Add NetworkBypassManager
        AddToCurrentScene();
        
        // Create example GameObject
        GameObject exampleGo = new GameObject("NetworkBypassExample");
        exampleGo.AddComponent<NetworkBypassExample>();
        
        Debug.Log("✅ Example scene setup complete!");
        Debug.Log("💡 Press Play to see DNS resolution and bypassed requests in action!");
    }
    
    [MenuItem("Tools/Network Bypass/Open Documentation")]
    public static void OpenDocumentation()
    {
        string readmePath = Path.Combine(Application.dataPath, "..", "README.md");
        if (File.Exists(readmePath))
        {
            Application.OpenURL($"file:///{readmePath}");
        }
        else
        {
            Debug.LogError("README.md not found!");
        }
    }
}
