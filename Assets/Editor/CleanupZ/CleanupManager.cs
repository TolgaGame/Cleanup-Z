using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class CleanupManager : EditorWindow {
    [MenuItem("Tools/Cleanup-Z/Comment DebugLog", false, 1)]
    static void DisableDebugLogStatementsMenuItem() {
        string scriptsPath = Application.dataPath + "/Scripts"; // Target for scripts Asset Folder
        string[] scriptFiles = Directory.GetFiles(scriptsPath, "*.cs", SearchOption.AllDirectories); // Get all .cs file in Project

        foreach (string scriptFile in scriptFiles) {
            string scriptContent = File.ReadAllText(scriptFile);
            string pattern = @"\bDebug\.Log\b"; // Find Debug.Log with Regex
            string replacement = "// Debug.Log";
            string newScriptContent = Regex.Replace(scriptContent, pattern, replacement);

            File.WriteAllText(scriptFile, newScriptContent);
        }

        Debug.Log("Debug.Log statements disabled in scripts.");
    }

    [MenuItem("Tools/Cleanup-Z/Clean Missing References", false, 2)]
    static void CleanMissingReferencesMenuItem() {
        CleanMissingReferencesInScene();
    }

    static void CleanMissingReferencesInScene() {
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject rootObject in rootObjects) {
            CleanMissingReferencesInGameObject(rootObject);
        }
        Debug.Log("Missing references cleaned in the scene.");
    }

    static void CleanMissingReferencesInGameObject(GameObject gameObject) {
        Component[] components = gameObject.GetComponents<Component>();
        foreach (Component component in components) {
            // Find missing Component
            if (component == null) {
                Debug.Log("Cleaning missing component in " + gameObject.name);
                // Remove Task
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
            }
        }
        int childCount = gameObject.transform.childCount;
        for (int i = 0; i < childCount; i++) {
            Transform child = gameObject.transform.GetChild(i);
            CleanMissingReferencesInGameObject(child.gameObject);
        }
    }

    [MenuItem("Tools/Cleanup-Z/Documentation")]
    static void DocumanOpen() {
        Application.OpenURL("https://github.com/TolgaGame"); // Documentation Link
    }
}

////////////////////////////////////////////////////////-- FIND All Components In Scene
class ListComponentsIn : EditorWindow {
    private static List<Component> AllComponents = new List<Component>();

    [MenuItem("Tools/Cleanup-Z/All Components in Scene", false, 3)]
    static void ListComponentsInSceneMenuItem() {
        ListComponentsInScene(); // Show all components on GameObject in current Scene
    }

    static void ListComponentsInScene() {
        AllComponents.Clear();
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject rootObject in rootObjects) {
            CollectComponentsInGameObject(rootObject);
        }
        // Show List on GUI
        ShowComponentListGUI();
    }

    static void CollectComponentsInGameObject(GameObject gameObject) {
        // Get Component Data
        Component[] components = gameObject.GetComponents<Component>();
        foreach (Component component in components) {
            if (component != null && !AllComponents.Contains(component)) {
                AllComponents.Add(component);
            }
        }

        int childCount = gameObject.transform.childCount;
        for (int i = 0; i < childCount; i++) {
            Transform child = gameObject.transform.GetChild(i);
            CollectComponentsInGameObject(child.gameObject);
        }
    }

    static void ShowComponentListGUI() {
        EditorWindow window = EditorWindow.GetWindow<ListComponentsIn>();
        window.titleContent = new GUIContent("Component List");
        window.Show();
    }

    void OnGUI() {
        GUILayout.Label("Components in Scene", EditorStyles.boldLabel);
        foreach (Component component in AllComponents) {
            EditorGUILayout.ObjectField(component.GetType().Name, component, typeof(Component), true);
        }
    }
}

//////////////////////////////////////////// Find Not Apply Prefabs
class UnappliedPrefabsList : EditorWindow {
    private static List<GameObject> UnappliedPrefabs = new List<GameObject>();

    [MenuItem("Tools/Cleanup-Z/List Unapplied Prefabs", false, 4)]
    static void ListUnappliedPrefabsMenuItem() {
        UnappliedPrefabs.Clear();
        ListUnappliedPrefabsInScene();
        ShowUnappliedPrefabListGUI();
    }

    static void ListUnappliedPrefabsInScene() {
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject rootObject in rootObjects) {
            ListUnappliedPrefabsInGameObject(rootObject);
        }
    }

    static void ListUnappliedPrefabsInGameObject(GameObject gameObject) {
        if (PrefabUtility.GetCorrespondingObjectFromSource(gameObject) != null && PrefabUtility.HasPrefabInstanceAnyOverrides(gameObject, false)) {
            UnappliedPrefabs.Add(gameObject);
        }

        int childCount = gameObject.transform.childCount;
        for (int i = 0; i < childCount; i++) {
            Transform child = gameObject.transform.GetChild(i);
            ListUnappliedPrefabsInGameObject(child.gameObject);
        }
    }

    static void ShowUnappliedPrefabListGUI() {
        UnappliedPrefabsList window = EditorWindow.GetWindow<UnappliedPrefabsList>();
        window.titleContent = new GUIContent("Unapplied Prefabs List");
        window.Show();
    }

    void OnGUI() {
        GUILayout.Label("Unapplied Prefabs in Scene", EditorStyles.boldLabel);
        foreach (GameObject prefabObject in UnappliedPrefabs) {
            EditorGUILayout.ObjectField("Prefab", prefabObject, typeof(GameObject), true);
        }
    }
}

/////////////////////////////////////// Find Duplicate Assets

class DuplicateFilesList : EditorWindow {
    private static List<string> DuplicateFiles = new List<string>();

    [MenuItem("Tools/Cleanup-Z/List Duplicate Files", false, 6)]
    static void ListDuplicateFilesMenuItem() {
        DuplicateFiles.Clear();
        ListDuplicateFilesInAssets();
        ShowDuplicateFilesListGUI();
    }

    static void ListDuplicateFilesInAssets() {
        string[] allFiles = Directory.GetFiles(Application.dataPath, "*.*", SearchOption.AllDirectories);
        HashSet<string> fileNamesSet = new HashSet<string>();

        foreach (string filePath in allFiles) {
            string fileName = Path.GetFileName(filePath);

            // Dosya ismi zaten sette varsa, bu bir duplicate dosya (meta dosyaları hariç)
            if (!fileName.EndsWith(".meta") && fileNamesSet.Contains(fileName)) {
                DuplicateFiles.Add(filePath);
            } else {
                fileNamesSet.Add(fileName);
            }
        }
    }

    static void ShowDuplicateFilesListGUI() {
        DuplicateFilesList window = EditorWindow.GetWindow<DuplicateFilesList>();
        window.titleContent = new GUIContent("Duplicate Files List");
        window.Show();
    }

    void OnGUI() {
        GUILayout.Label("Duplicate Files in Assets", EditorStyles.boldLabel);

        if (DuplicateFiles.Count == 0) {
            EditorGUILayout.LabelField("No duplicate files found.");
        } else {
            foreach (string filePath in DuplicateFiles) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(filePath);
                if (GUILayout.Button("Show in Project", GUILayout.Width(120))) {
                    ShowInProject(filePath);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }

    static void ShowInProject(string filePath) {
        string relativePath = "Assets" + filePath.Substring(Application.dataPath.Length);
        UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativePath);
        EditorGUIUtility.PingObject(asset);
    }

}

////////////////////////////////////////////////////////-- FIND Uncompressed Images

class UncompressedImagesList : EditorWindow {
    private static HashSet<string> imageExtensions = new HashSet<string> { ".png", ".jpg", ".jpeg", ".tga", ".gif" };
    private static List<string> nonCompliantImages = new List<string>();

    [MenuItem("Tools/Cleanup-Z/Find Uncompressed Images", false, 7)]
    static void ListNonCompliantImagesMenuItem() {
        nonCompliantImages.Clear();
        ListNonCompliantImagesInAssets();
        ShowNonCompliantImagesListGUI();
    }

    static void ListNonCompliantImagesInAssets() {
        string[] allFiles = Directory.GetFiles(Application.dataPath, "*.*", SearchOption.AllDirectories);

        foreach (string filePath in allFiles) {
            if (IsImage(filePath)) {
                string relativePath = "Assets" + filePath.Substring(Application.dataPath.Length);
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(relativePath);

                if (texture != null) {
                    // Texture'nin genişliği ve yüksekliği alınıyor
                    int width = texture.width;
                    int height = texture.height;

                    // Eğer texture 2'nin veya 4'ün katı değilse, listeye ekleyelim
                    if (!IsPowerOfTwo(width) || !IsPowerOfTwo(height)) {
                        // Texture boyutunu Debug.Log ile kontrol edelim
                        Debug.Log(string.Format("Texture: {0}, Width: {1}, Height: {2}", relativePath, width, height));
                        nonCompliantImages.Add(filePath);
                    }
                }
            }
        }
    }

    static bool IsImage(string filePath) {
        string extension = Path.GetExtension(filePath).ToLower();
        return imageExtensions.Contains(extension);
    }

    static void ShowNonCompliantImagesListGUI() {
        UncompressedImagesList window = EditorWindow.GetWindow<UncompressedImagesList>();
        window.titleContent = new GUIContent("UncompressedImagesList Images List");
        window.Show();
    }

    void OnGUI() {
        GUILayout.Label("Uncompressed Images in Assets", EditorStyles.boldLabel);

        if (nonCompliantImages.Count == 0) {
            EditorGUILayout.LabelField("No uncompressed images found.");
        } else {
            foreach (string filePath in nonCompliantImages) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(filePath);
                if (GUILayout.Button("Show in Project", GUILayout.Width(120))) {
                    ShowInProject(filePath);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }

    static void ShowInProject(string filePath) {
        string assetsPath = "Assets" + filePath.Substring(Application.dataPath.Length);
        UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetsPath);
        EditorGUIUtility.PingObject(asset);
        Selection.activeObject = asset;
    }

    static bool IsPowerOfTwo(int number) {
        return (number & (number - 1)) == 0;
    }
}
