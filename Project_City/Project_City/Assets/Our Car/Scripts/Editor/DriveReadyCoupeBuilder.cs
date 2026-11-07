#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using DriveReadyCoupe;

// This auto-generates the .prefab ON IMPORT after Unity has imported the OBJ meshes.
[InitializeOnLoad]
public static class DriveReadyCoupeBuilder
{
    private const string Base = "Assets/DriveReadyCoupe";
    private const string MeshRoot = Base + "/Meshes/";
    private const string PrefabPath = Base + "/Prefabs/DriveReadyCoupe.prefab";

    private static readonly string[] AllMeshes = {
        "01_PaintBody", "02_TintedGlass", "03_BlackTrim", "04_ChromeTrim",
        "05_Headlights", "06_Taillights", "07_AccentDetails", "08_Mirrors",
        "Wheel_01_Tire", "Wheel_02_TreadGrooves", "Wheel_03_AluminumRim",
        "Wheel_04_BrakeDisc", "Wheel_05_Caliper"
    };

    static DriveReadyCoupeBuilder()
    {
        EditorApplication.delayCall += AutoBuild;
    }

    private static void AutoBuild()
    {
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) && AllModelsPresent())
            BuildPrefab();
    }

    private static bool AllModelsPresent()
    {
        foreach (string name in AllMeshes)
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(MeshRoot + name + ".obj"))
                return false;
        return true;
    }

    [MenuItem("Tools/Drive Ready Coupe/Build or Rebuild Prefab")]
    public static void BuildPrefab()
    {
        if (!AllModelsPresent())
        {
            Debug.LogError("Drive Ready Coupe: OBJ meshes still importing or missing. Wait for import to finish, then use Tools > Drive Ready Coupe > Build or Rebuild Prefab.");
            return;
        }
        if (!AssetDatabase.IsValidFolder(Base + "/Materials"))
            AssetDatabase.CreateFolder(Base, "Materials");
        if (!AssetDatabase.IsValidFolder(Base + "/Prefabs"))
            AssetDatabase.CreateFolder(Base, "Prefabs");

        // Use URP Lit in URP projects and Standard in built-in renderer projects.
        Material paint = MakeMat("Gloss Yellow", new Color(.98f, .76f, .07f), .42f, .84f);
        Material glass = MakeMat("Dark Tinted Glass", new Color(.035f, .105f, .145f), .25f, .86f);
        Material black = MakeMat("Matte Black", new Color(.035f, .04f, .05f), .06f, .4f);
        Material chrome = MakeMat("Polished Chrome", new Color(.70f, .75f, .81f), .83f, .92f);
        Material head = MakeMat("Headlight White", new Color(.88f,.91f,1f), .19f, .83f);
        Material tail = MakeMat("Brake Light Red", new Color(.78f,.038f,.03f), .10f, .78f);
        Material accent = MakeMat("Body Seams", new Color(.17f,.12f,.04f), .07f, .46f);
        Material tread = MakeMat("Tread Detail", new Color(.055f,.06f,.063f), .01f, .23f);
        Material aluminum = MakeMat("Forged Aluminum", new Color(.63f,.68f,.71f), .65f, .83f);
        Material rotor = MakeMat("Brake Rotor", new Color(.34f,.37f,.41f), .55f, .55f);
        Material caliper = MakeMat("Red Calipers", new Color(.90f,.07f,.04f), .28f, .70f);

        var root = new GameObject("DriveReadyCoupe_WASD");
        try
        {
            root.transform.position = Vector3.zero;
            root.transform.localScale = Vector3.one;
            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 1200f;
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.centerOfMass = new Vector3(0f, -.18f, 0f);
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, .69f, 0);
            collider.size = new Vector3(1.66f, 1.28f, 4.05f);
            var carScript = root.AddComponent<DriveReadyCarController>();

            GameObject visuals = new GameObject("Paintwork and Details");
            visuals.transform.SetParent(root.transform, false);
            AttachModel("01_PaintBody", paint, visuals.transform);
            AttachModel("02_TintedGlass", glass, visuals.transform);
            AttachModel("03_BlackTrim", black, visuals.transform);
            AttachModel("04_ChromeTrim", chrome, visuals.transform);
            AttachModel("05_Headlights", head, visuals.transform);
            AttachModel("06_Taillights", tail, visuals.transform);
            AttachModel("07_AccentDetails", accent, visuals.transform);
            AttachModel("08_Mirrors", paint, visuals.transform);

            GameObject wheels = new GameObject("Wheels (independent pivots)");
            wheels.transform.SetParent(root.transform, false);
            var steerPivots = new Transform[2];
            var spinPivots = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                bool front = i < 2;
                bool left = (i % 2 == 0);
                string label = (front ? "Front" : "Rear") + "_" + (left ? "Left" : "Right");
                GameObject steering = new GameObject(label + "_SteeringPivot");
                steering.transform.SetParent(wheels.transform, false);
                steering.transform.localPosition = new Vector3(left ? -.845f : .845f,
                    .375f, front ? 1.36f : -1.38f);
                GameObject spin = new GameObject(label + "_RollingPivot");
                spin.transform.SetParent(steering.transform, false);
                AttachModel("Wheel_01_Tire", black, spin.transform);
                AttachModel("Wheel_02_TreadGrooves", tread, spin.transform);
                AttachModel("Wheel_03_AluminumRim", aluminum, spin.transform);
                AttachModel("Wheel_04_BrakeDisc", rotor, spin.transform);
                AttachModel("Wheel_05_Caliper", caliper, spin.transform);
                if (front) steerPivots[i] = steering.transform;
                spinPivots[i] = spin.transform;
            }
            carScript.steerPivots = steerPivots;
            carScript.spinPivots = spinPivots;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            if (!prefab) throw new InvalidOperationException("Prefab save failed");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Drive Ready Coupe: ready! Drag Assets/DriveReadyCoupe/Prefabs/DriveReadyCoupe into your scene. W/S/A/D, Space brake, R reset.");
        }
        catch (Exception e)
        {
            Debug.LogError("Drive Ready Coupe: prefab build failed: " + e);
            throw;
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static Material MakeMat(string name, Color color, float metallic, float smooth)
    {
        string path = Base + "/Materials/" + name.Replace(" ", "_") + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) shader = Shader.Find("Standard");
            if (!shader) throw new InvalidOperationException("Neither URP Lit nor Standard shader found");
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f); // Handles mirrored side surfaces.
        m.doubleSidedGI = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    private static GameObject AttachModel(string modelName, Material material, Transform parent)
    {
        string path = MeshRoot + modelName + ".obj";
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (!source) throw new InvalidOperationException("Missing imported mesh: " + path);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(source);
        go.name = modelName;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            r.sharedMaterials = new[] { material };
        return go;
    }
}
#endif
