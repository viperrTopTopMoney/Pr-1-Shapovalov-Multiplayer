using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FishNet.Component.Spawning;
using FishNet.Component.Transforming;
using FishNet.Managing;
using FishNet.Object;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class LastLockProjectSetup
{
    private const int AutoBuildRevision = 6;
    private const string AutoBuildRevisionKey = "THE_LAST_LOCK_AUTO_BUILD_REVISION";
    private const string MainScenePath = "Assets/Scenes/Main.unity";
    private const string GameScenePath = "Assets/Scenes/SampleScene.unity";
    private const string PlayerPrefabPath = "Assets/PLAYER 1.prefab";
    private const string PlayerVisualPrefabPath = "Assets/Survivalist/Prefab/Survivalist (1).prefab";
    private const string CabinPrefabPath = "Assets/Flooded_Grounds/Prefabs/Buildings/Cabins/Cabin1.prefab";

    private const string GeneratedFolder = "Assets/Generated";
    private const string GeneratedAnimationsFolder = "Assets/Generated/Animations";
    private const string GeneratedPrefabsFolder = "Assets/Generated/Prefabs";
    private const string PlayerControllerPath = "Assets/Generated/Animations/Player.controller";
    private const string ZombieNormalPath = "Assets/Generated/Prefabs/Zombie_Normal_PLACEHOLDER.prefab";
    private const string ZombieRunnerPath = "Assets/Generated/Prefabs/Zombie_Runner_PLACEHOLDER.prefab";
    private const string ZombieBossPath = "Assets/Generated/Prefabs/Zombie_Boss_PLACEHOLDER.prefab";

    [MenuItem("THE LAST LOCK/Build Playable Prototype")]
    public static void BuildPlayablePrototype()
    {
        EnsureFolder(GeneratedFolder);
        EnsureFolder(GeneratedAnimationsFolder);
        EnsureFolder(GeneratedPrefabsFolder);

        AnimationControllerBootstrap.BuildPlayerController();
        NormalizePlayerPrefab();
        BuildZombiePrefab(ZombieNormalPath, "Zombie_Normal_PLACEHOLDER", new Color(0.25f, 0.55f, 0.25f), 1f);
        BuildZombiePrefab(ZombieRunnerPath, "Zombie_Runner_PLACEHOLDER", new Color(0.55f, 0.45f, 0.12f), 0.9f);
        BuildZombiePrefab(ZombieBossPath, "Zombie_Boss_PLACEHOLDER", new Color(0.45f, 0.12f, 0.12f), 1.4f);

        BuildMainScene();
        BuildGameScene();
        UpdateBuildSettings();
        EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[THE LAST LOCK] Project layout rebuilt.");
    }

    [InitializeOnLoadMethod]
    private static void AutoBuildLatestRevision()
    {
        if (Application.isBatchMode || EditorPrefs.GetInt(AutoBuildRevisionKey, 0) >= AutoBuildRevision)
            return;

        EditorApplication.update -= TryAutoBuildLatestRevision;
        EditorApplication.update += TryAutoBuildLatestRevision;
    }

    private static void TryAutoBuildLatestRevision()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;

        EditorApplication.update -= TryAutoBuildLatestRevision;
        try
        {
            BuildPlayablePrototype();
            EditorPrefs.SetInt(AutoBuildRevisionKey, AutoBuildRevision);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static void BuildMainScene()
    {
        Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

        RemoveGameplayObjects(scene);

        EnsureManagerFallbacks();
        RemoveMenuPlayerSpawner();
        EnsurePersistentSessionManager();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void BuildGameScene()
    {
        Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        RemoveRootObjects(scene, _ => true);

        GameObject root = new GameObject("THE_LAST_LOCK_GAMEPLAY");
        GameSceneBootstrap bootstrap = root.AddComponent<GameSceneBootstrap>();

        GameObject environment = Child(root, "01_ENVIRONMENT_FLOODED_GROUNDS");
        GameObject gameplay = Child(root, "02_GAMEPLAY");
        GameObject systems = Child(gameplay, "Systems_NETWORKED");
        GameObject markers = Child(gameplay, "Markers");
        GameObject barricades = Child(gameplay, "Barricades_NETWORKED");

        Bounds houseBounds = CreateEnvironment(environment.transform);
        Marker(markers.transform, "HouseCenter_TARGET", houseBounds.center);
        CreateIntrusionZone(gameplay.transform, houseBounds);
        CreateBarricades(barricades.transform, houseBounds);
        CreatePlayerSpawns(markers.transform, houseBounds);
        CreateZombieSpawns(markers.transform, houseBounds);
        CreateGameplaySystems(systems, bootstrap);
        CreateNavigation(root);
        ReserializeSceneNetworkObjects(scene);

        EditorUtility.SetDirty(bootstrap);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void NormalizePlayerPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) == null)
            return;

        GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);

        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        Transform pointFire = FindDeepChild(root.transform, "pointFire");
        if (pointFire != null)
            pointFire.localScale = Vector3.one;

        Transform visual = FindDeepChild(root.transform, "Visual_Survivalist");
        if (visual != null)
        {
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            visual.localScale = Vector3.one;

            Animator animator = visual.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                PlayerView playerView = root.GetComponentInChildren<PlayerView>(true);
                if (playerView != null)
                    SetObject(playerView, "_animator", animator);
            }
        }

        PlayerNetwork playerNetwork = root.GetComponent<PlayerNetwork>();
        if (playerNetwork != null)
            SetArray(playerNetwork, "_spawnPoints", Array.Empty<Transform>());

        NetworkObject networkObject = root.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            SerializedObject so = new SerializedObject(networkObject);
            SerializedProperty transformData = so.FindProperty("SerializedTransformProperties");
            if (transformData != null)
            {
                transformData.FindPropertyRelative("Position").vector3Value = Vector3.zero;
                transformData.FindPropertyRelative("Rotation").quaternionValue = Quaternion.identity;
                transformData.FindPropertyRelative("Scale").vector3Value = Vector3.one;
                transformData.FindPropertyRelative("IsValid").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(networkObject);
            }
        }

        PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void EnsureManagerFallbacks()
    {
        GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
        if (gameManager == null)
            return;

        SetObject(gameManager, "_houseCenterFallback", null);

        ZombieManager zombieManager = Object.FindAnyObjectByType<ZombieManager>();
        if (zombieManager != null)
        {
            SetObject(zombieManager, "_houseCenterFallback", null);
            SetArray(zombieManager, "_spawnPoints", Array.Empty<Transform>());
        }

        NetworkManager networkManager = Object.FindAnyObjectByType<NetworkManager>();
        if (networkManager != null)
        {
            PlayerSpawner spawner = networkManager.GetComponent<PlayerSpawner>() ?? networkManager.gameObject.AddComponent<PlayerSpawner>();
            spawner.Spawns = Array.Empty<Transform>();
            EditorUtility.SetDirty(spawner);
        }

        EditorUtility.SetDirty(gameManager);
        if (zombieManager != null)
            EditorUtility.SetDirty(zombieManager);
    }

    private static void RemoveMenuPlayerSpawner()
    {
        NetworkManager networkManager = Object.FindAnyObjectByType<NetworkManager>();
        if (networkManager == null)
            return;

        PlayerSpawner spawner = networkManager.GetComponent<PlayerSpawner>();
        if (spawner != null)
            UnityEngine.Object.DestroyImmediate(spawner);
    }

    private static void EnsurePersistentSessionManager()
    {
        NetworkManager networkManager = Object.FindAnyObjectByType<NetworkManager>();
        if (networkManager == null)
            return;

        SessionManager persistent = networkManager.GetComponent<SessionManager>();
        if (persistent == null)
            persistent = networkManager.gameObject.AddComponent<SessionManager>();
        if (networkManager.GetComponent<LobbyRuntimeHUD>() == null)
            networkManager.gameObject.AddComponent<LobbyRuntimeHUD>();

        SessionManager[] sessions = Object.FindObjectsByType<SessionManager>(FindObjectsSortMode.None);
        foreach (SessionManager session in sessions)
        {
            if (session != null && session != persistent)
                UnityEngine.Object.DestroyImmediate(session);
        }

        EditorUtility.SetDirty(persistent);
    }

    private static void CreateGameplaySystems(GameObject systems, GameSceneBootstrap bootstrap)
    {
        systems.AddComponent<NetworkObject>();
        systems.AddComponent<GameManager>();
        systems.AddComponent<WaveManager>();
        ZombieManager zombieManager = systems.AddComponent<ZombieManager>();

        SetObject(zombieManager, "_normalZombiePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(ZombieNormalPath));
        SetObject(zombieManager, "_runnerZombiePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(ZombieRunnerPath));
        SetObject(zombieManager, "_bossZombiePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(ZombieBossPath));

        GamePlayerSpawner playerSpawner = systems.AddComponent<GamePlayerSpawner>();
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        SetObject(playerSpawner, "_playerPrefab", playerPrefab != null ? playerPrefab.GetComponent<NetworkObject>() : null);
        SetObject(bootstrap, "_playerSpawner", playerSpawner);
    }

    private static Bounds CreateEnvironment(Transform parent)
    {
        GameObject cabinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CabinPrefabPath);
        GameObject cabin = cabinPrefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(cabinPrefab, parent) : null;

        if (cabin != null)
        {
            cabin.name = "Cabin1_FLOODED_GROUNDS";
            cabin.transform.localPosition = Vector3.zero;
            cabin.transform.localRotation = Quaternion.identity;
        }

        Bounds bounds = CalculateBounds(cabin);
        if (bounds.size.sqrMagnitude < 1f)
            bounds = new Bounds(new Vector3(0f, 1.5f, 0f), new Vector3(12f, 3f, 10f));

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "GameplayGround_NavMesh";
        floor.transform.SetParent(parent, false);
        float size = Mathf.Max(90f, Mathf.Max(bounds.size.x, bounds.size.z) + 70f);
        floor.transform.position = new Vector3(bounds.center.x, bounds.min.y - 0.3f, bounds.center.z);
        floor.transform.localScale = new Vector3(size, 0.5f, size);
        floor.isStatic = true;

        return bounds;
    }

    private static Bounds CalculateBounds(GameObject go)
    {
        if (go == null)
            return default;

        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return default;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static void CreateIntrusionZone(Transform parent, Bounds bounds)
    {
        GameObject zone = new GameObject("HouseIntrusionZone_TRIGGER");
        zone.transform.SetParent(parent, false);
        zone.transform.position = bounds.center;
        BoxCollider collider = zone.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(
            Mathf.Max(6f, bounds.size.x * 0.75f),
            Mathf.Max(3f, bounds.size.y),
            Mathf.Max(6f, bounds.size.z * 0.75f));
        zone.AddComponent<HouseIntrusionZone>();
    }

    private static void CreateBarricades(Transform parent, Bounds bounds)
    {
        float y = bounds.min.y + Mathf.Max(1.1f, bounds.size.y * 0.35f);
        float frontZ = bounds.min.z - 0.3f;
        float backZ = bounds.max.z + 0.3f;
        float leftX = bounds.min.x - 0.3f;
        float rightX = bounds.max.x + 0.3f;

        CreateBarricade<DoorController>(parent, "MainDoor_NETWORKED", new Vector3(bounds.center.x, y, frontZ), new Vector3(2f, 2.5f, 0.35f));
        CreateBarricade<WindowController>(parent, "Window_Left_NETWORKED", new Vector3(leftX, y + 0.4f, bounds.center.z), new Vector3(0.35f, 1.4f, 2.2f));
        CreateBarricade<WindowController>(parent, "Window_Right_NETWORKED", new Vector3(rightX, y + 0.4f, bounds.center.z), new Vector3(0.35f, 1.4f, 2.2f));
        CreateBarricade<WindowController>(parent, "Window_Back_NETWORKED", new Vector3(bounds.center.x, y + 0.4f, backZ), new Vector3(2.2f, 1.4f, 0.35f));
    }

    private static void CreateBarricade<T>(Transform parent, string name, Vector3 position, Vector3 scale)
        where T : BarricadeController
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.layer = 2;
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        go.transform.localScale = scale;
        go.AddComponent<NetworkObject>();
        go.AddComponent<T>();
    }

    private static void CreatePlayerSpawns(Transform parent, Bounds bounds)
    {
        Vector3 center = bounds.center;
        center.y = bounds.min.y + 1f;
        Marker(parent, "PlayerSpawn_1", center + new Vector3(-1.5f, 0f, 0f));
        Marker(parent, "PlayerSpawn_2", center + new Vector3(1.5f, 0f, 0f));
    }

    private static void CreateZombieSpawns(Transform parent, Bounds bounds)
    {
        float radius = Mathf.Max(34f, Mathf.Max(bounds.size.x, bounds.size.z) + 24f);
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 2f / 8f;
            Vector3 position = bounds.center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            position.y = bounds.min.y + 0.2f;
            Transform spawn = Marker(parent, $"ZombieSpawn_{i + 1}", position);
            spawn.LookAt(bounds.center);
        }
    }

    private static void CreateNavigation(GameObject root)
    {
        NavMeshSurface surface = root.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~(1 << 2);
        surface.BuildNavMesh();
        EditorUtility.SetDirty(surface);
    }

    private static void ReserializeSceneNetworkObjects(Scene scene)
    {
        MethodInfo createSceneId = typeof(NetworkObject).GetMethod(
            "CreateSceneId",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        MethodInfo reserialize = typeof(NetworkObject).GetMethod(
            "ReserializeEditorSetValues",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (createSceneId == null || reserialize == null)
        {
            Debug.LogWarning("[THE LAST LOCK] FishNet scene reserialization API was not found.");
            return;
        }

        object[] arguments = { scene, true, 0 };
        object result = createSceneId.Invoke(null, arguments);
        if (result is not IEnumerable<NetworkObject> networkObjects)
            return;

        foreach (NetworkObject networkObject in networkObjects)
        {
            reserialize.Invoke(networkObject, new object[] { true, false });
            EditorUtility.SetDirty(networkObject);
        }
    }

    private static void BuildZombiePrefab(string path, string objectName, Color color, float visualScale)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            AssetDatabase.DeleteAsset(path);

        GameObject root = new GameObject(objectName);
        root.AddComponent<NetworkObject>();
        NetworkTransform networkTransform = root.AddComponent<NetworkTransform>();
        SetBool(networkTransform, "_clientAuthoritative", false);

        NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
        agent.radius = 0.4f;
        agent.height = 1.8f;
        agent.baseOffset = 0.9f;

        root.AddComponent<ZombieNetwork>();
        ZombieView view = root.AddComponent<ZombieView>();

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Visual_REPLACE_WITH_ZOMBIE_ASSET";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = Vector3.up;
        visual.transform.localScale = Vector3.one * visualScale;
        UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());

        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader);
            material.color = color;
            renderer.sharedMaterial = material;
        }

        SetObject(view, "_visualRoot", visual.transform);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void UpdateBuildSettings()
    {
        EditorBuildSettingsScene main = new EditorBuildSettingsScene(MainScenePath, true);
        EditorBuildSettingsScene game = new EditorBuildSettingsScene(GameScenePath, true);
        EditorBuildSettings.scenes = new[] { main, game };
    }

    private static void RemoveRootObjects(Scene scene, Func<GameObject, bool> shouldRemove)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (shouldRemove(root))
                UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void RemoveGameplayObjects(Scene scene)
    {
        List<GameObject> targets = new List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
            CollectGameplayTargets(root.transform, targets);

        for (int i = targets.Count - 1; i >= 0; i--)
        {
            if (targets[i] != null)
                UnityEngine.Object.DestroyImmediate(targets[i]);
        }
    }

    private static void CollectGameplayTargets(Transform current, List<GameObject> targets)
    {
        if (current == null)
            return;

        if (IsGameplayPrototype(current.gameObject))
        {
            targets.Add(current.gameObject);
            return;
        }

        foreach (Transform child in current)
            CollectGameplayTargets(child, targets);
    }

    private static bool IsGameplayPrototype(GameObject go)
    {
        if (go == null)
            return false;

        string name = go.name;
        return name == "PLAYER 1" ||
               name.StartsWith("THE_LAST_LOCK_GAMEPLAY", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("01_ENVIRONMENT_FLOODED_GROUNDS", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("02_GAMEPLAY", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("Cabin1_FLOODED_GROUNDS", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("HouseIntrusionZone", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("ZombieSpawn_", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("PlayerSpawn_", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("MainDoor_", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("Window_", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("GameplayGround_NavMesh", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("HouseCenter_TARGET", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("GameSceneBootstrap", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("LastLockRuntimeHUD", StringComparison.OrdinalIgnoreCase) ||
               go.GetComponent<BarricadeController>() != null ||
               go.GetComponent<DoorController>() != null ||
               go.GetComponent<WindowController>() != null ||
               go.GetComponent<ZombieNetwork>() != null ||
               go.GetComponent<ZombieView>() != null ||
               go.GetComponent<HouseIntrusionZone>() != null ||
               go.GetComponent<NavMeshSurface>() != null ||
               go.GetComponent<GameSceneBootstrap>() != null ||
               go.GetComponent<LastLockRuntimeHUD>() != null ||
               go.GetComponent<ZombieManager>() != null ||
               go.GetComponent<WaveManager>() != null ||
               go.GetComponent<PickupManager>() != null ||
               go.GetComponent<PlayerNetwork>() != null ||
               go.GetComponent<PlayerMovement>() != null ||
               go.GetComponent<PlayerView>() != null ||
               go.GetComponent<PlayerCamera>() != null ||
               go.GetComponent<PlayerShooting>() != null ||
               go.GetComponent<PlayerInteraction>() != null;
    }

    private static bool HasAnyComponent(GameObject go, params Type[] components)
    {
        foreach (Type type in components)
        {
            if (go.GetComponent(type) != null)
                return true;
        }

        return false;
    }

    private static Transform Marker(Transform parent, string name, Vector3 position)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        return go.transform;
    }

    private static GameObject Child(GameObject parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform nested = FindDeepChild(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private static void SetObject(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
    {
        if (target == null)
            return;

        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property != null)
        {
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetBool(UnityEngine.Object target, string fieldName, bool value)
    {
        if (target == null)
            return;

        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property != null)
        {
            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetArray(UnityEngine.Object target, string fieldName, Transform[] values)
    {
        if (target == null)
            return;

        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property != null)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string parent = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
        string leaf = Path.GetFileName(folderPath);

        if (!string.IsNullOrWhiteSpace(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        if (string.IsNullOrWhiteSpace(parent))
            AssetDatabase.CreateFolder("Assets", leaf);
        else
            AssetDatabase.CreateFolder(parent, leaf);
    }
}
