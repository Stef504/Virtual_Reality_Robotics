#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Networking;
using RealisticStreetDropIns;

public static class StreetPackInstaller
{
    private const string Root = "Assets/RealisticStreetDropIns";
    private const string Models = Root + "/Models";
    private const string Prefabs = Root + "/Prefabs";
    private const string Controllers = Root + "/AnimationControllers";
    private const string Materials = Root + "/Materials";
    private const string Coupe = "Assets/DriveReadyCoupe/Prefabs/DriveReadyCoupe.prefab";
    private const string HumanFbx = Models + "/OldMan_Walk.fbx";
    private const string HumanGlb = Models + "/OldMan_AllAnimations.glb";
    private const string SpectralGlb = Models + "/SpectralGT_CC0.glb";

    private struct Target
    {
        public string path, url;
        public Target(string p, string u) { path = p; url = u; }
    }
    private static readonly Target[] targets = {
        new Target(HumanFbx, "https://bake3d.ai/samples/free/bake3d-person-walk.fbx"),
        new Target(HumanGlb, "https://bake3d.ai/samples/free/bake3d-person-animated.glb"),
        new Target(SpectralGlb, "https://raw.githubusercontent.com/JaronKBragg7337/spectral-gt-rs/main/spectral_gt_rs_game_ready.glb")
    };
    private static Queue<Target> pending;
    private static UnityWebRequest activeRequest;
    private static Target current;
    private static readonly List<string> completed = new List<string>();
    private static readonly List<string> errors = new List<string>();
    private static bool downloading;

    [MenuItem("Tools/Realistic Street Drop-Ins/1 - Download High-Detail Models")]
    public static void DownloadModels()
    {
        if (downloading) { Debug.LogWarning("Realistic Street: download already in progress."); return; }
        EnsureDirs();
        pending = new Queue<Target>(targets.Where(t => !File.Exists(Path.GetFullPath(t.path))));
        completed.Clear(); errors.Clear();
        if (pending.Count == 0)
        {
            Debug.Log("Realistic Street: the three source models are already downloaded. Building prefabs.");
            BuildPrefabs();
            return;
        }
        downloading = true;
        EditorApplication.update += Poll;
        NextDownload();
    }

    private static void NextDownload()
    {
        if (pending.Count == 0)
        {
            EditorApplication.update -= Poll;
            EditorUtility.ClearProgressBar();
            downloading = false;
            if (completed.Count > 0)
            {
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (string path in completed)
                    {
                        string cachedFile = CachePath(path);
                        string destination = Path.GetFullPath(path);
                        Directory.CreateDirectory(Path.GetDirectoryName(destination));
                        File.Copy(cachedFile, destination, true);
                    }
                }
                finally { AssetDatabase.StopAssetEditing(); }
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            if (errors.Count > 0)
                Debug.LogWarning("Realistic Street: some downloads failed. Check Internet access or download files manually to Assets/RealisticStreetDropIns/Models.\n" + string.Join("\n", errors.ToArray()));
            EditorApplication.delayCall += BuildPrefabs;
            return;
        }
        current = pending.Dequeue();
        string cache = CachePath(current.path);
        Directory.CreateDirectory(Path.GetDirectoryName(cache));
        activeRequest = UnityWebRequest.Get(current.url);
        activeRequest.timeout = 120;
        activeRequest.SendWebRequest();
    }

    private static string CachePath(string assetPath)
    {
        string folder = Path.Combine(Directory.GetCurrentDirectory(), "Library/RealisticStreetDropInsCache");
        return Path.Combine(folder, Path.GetFileName(assetPath));
    }

    private static void Poll()
    {
        if (!downloading || activeRequest == null) return;
        EditorUtility.DisplayProgressBar("Downloading detailed street models", Path.GetFileName(current.path), Mathf.Clamp01(activeRequest.downloadProgress));
        if (!activeRequest.isDone) return;
        if (activeRequest.result == UnityWebRequest.Result.Success && activeRequest.downloadHandler.data.Length > 10000)
        {
            File.WriteAllBytes(CachePath(current.path), activeRequest.downloadHandler.data);
            completed.Add(current.path);
            Debug.Log("Downloaded: " + current.path);
        }
        else
            errors.Add(Path.GetFileName(current.path) + ": " + activeRequest.error + " (" + current.url + ")");
        activeRequest.Dispose();
        activeRequest = null;
        NextDownload();
    }

    private static void EnsureDirs()
    {
        foreach (string d in new[] { Models, Prefabs, Controllers, Materials })
            Directory.CreateDirectory(Path.GetFullPath(d));
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Realistic Street Drop-Ins/2 - Build or Rebuild Prefabs")]
    public static void BuildPrefabs()
    {
        if (downloading) return;
        EnsureDirs();
        int built = 0;
        // Existing WASD car from your previous package: convert copies to purely ambient props.
        GameObject coupe = AssetDatabase.LoadAssetAtPath<GameObject>(Coupe);
        if (coupe != null)
        {
            built += MakeCoupe(coupe, "Traffic_Coupe_Silver", true, new Color(.55f, .63f, .72f));
            built += MakeCoupe(coupe, "Traffic_Coupe_Red", true, new Color(.8f, .08f, .055f));
            built += MakeCoupe(coupe, "Parked_Coupe_Black", false, new Color(.055f, .06f, .07f));
            built += MakeCoupe(coupe, "Parked_Coupe_White", false, new Color(.82f, .86f, .91f));
        }
        else Debug.LogWarning("Realistic Street: import the earlier DriveReadyCoupe_Unity2022.unitypackage to also generate four coupe traffic/parked prefabs.");

        GameObject spectral = AssetDatabase.LoadAssetAtPath<GameObject>(SpectralGlb);
        if (spectral != null)
        {
            built += MakeModel(spectral, SpectralGlb, "Traffic_SpectralGT", false, true, 4.5f, -90f);
            built += MakeModel(spectral, SpectralGlb, "Parked_SpectralGT", false, false, 4.5f, -90f);
        }
        else if (File.Exists(Path.GetFullPath(SpectralGlb)))
            Debug.LogWarning("Realistic Street: SpectralGT_CC0.glb exists but Unity has not imported it. Install glTFast via Package Manager ('com.unity.cloud.gltfast'), then use menu 2 again.");

        // Prefer the richer GLB with Idle+Walk+Run. The FBX works with Unity without any glTF package.
        string humanPath = AssetDatabase.LoadAssetAtPath<GameObject>(HumanGlb) != null ? HumanGlb : HumanFbx;
        GameObject human = AssetDatabase.LoadAssetAtPath<GameObject>(humanPath);
        if (human != null)
        {
            built += MakeModel(human, humanPath, "Pedestrian_OldMan_Walking", true, true, 1.75f, 0f);
            built += MakeModel(human, humanPath, "Pedestrian_OldMan_Standing", true, false, 1.75f, 0f);
        }
        else Debug.LogWarning("Realistic Street: no walking human imported yet. Use menu 1 to download the 150k-triangle character, then menu 2.");

        AssetDatabase.SaveAssets();
        Debug.Log("Realistic Street: " + built + " prefabs ready under " + Prefabs + ". Drag the ones you want into any scene. No NavMesh needed.");

    }

    // Allow the user to import ANY other photorealistic rigged FBX/GLB and create drop-ins from it.
    [MenuItem("Tools/Realistic Street Drop-Ins/3 - Make Prefabs from Selected 3D Character")]
    public static void BuildSelected()
    {
        GameObject model = Selection.activeObject as GameObject;
        string sourcePath = AssetDatabase.GetAssetPath(model);
        if (model == null || string.IsNullOrEmpty(sourcePath) ||
            !(sourcePath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) || sourcePath.EndsWith(".glb", StringComparison.OrdinalIgnoreCase)))
        {
            EditorUtility.DisplayDialog("Select a model first", "Select an imported, rigged .FBX or .GLB MODEL ASSET in the Project window, then click this menu again.", "OK");
            return;
        }
        EnsureDirs();
        string label = Path.GetFileNameWithoutExtension(sourcePath).Replace(' ', '_');
        int count = MakeModel(model, sourcePath, "Pedestrian_" + label + "_Walking", true, true, 1.75f, 0f)
                  + MakeModel(model, sourcePath, "Pedestrian_" + label + "_Standing", true, false, 1.75f, 0f);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Street prefabs", "Created " + count + " prefab(s) in " + Prefabs + ". Walking motion requires an imported walk AnimationClip.", "OK");
    }

    [MenuItem("Tools/Realistic Street Drop-Ins/3 - Make Prefabs from Selected 3D Character", true)]
    private static bool BuildSelectedValidate() { return Selection.activeObject is GameObject; }

    private static int MakeCoupe(GameObject source, string name, bool moving, Color paint)
    {
        GameObject root = new GameObject(name);
        try
        {
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(source);
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            visual.name = "Detailed_Coupe_Visuals";
            // Strip the player-driving Rigidbody/controller; NPCs must never react to WASD.
            foreach (MonoBehaviour m in visual.GetComponents<MonoBehaviour>())
                if (m && m.GetType().Name == "DriveReadyCarController") UnityEngine.Object.DestroyImmediate(m);
            foreach (Rigidbody rb in visual.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(rb);
            foreach (Collider col in visual.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(col);
            Material material = PaintMaterial(name, paint);
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
                if (renderer.gameObject.name.StartsWith("01_PaintBody"))
                {
                    Material[] mats = renderer.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = material;
                    renderer.sharedMaterials = mats;
                }
            SetCollider(root, false, moving, 1.7f, 4.05f, 1.4f);
            if (moving)
            {
                var motion = root.AddComponent<StreetRouteMover>();
                motion.speed = 4.6f; motion.distance = 28f; motion.loopToStart = true;
                var spinner = root.AddComponent<StreetWheelSpinner>();
                spinner.rollingPivots = visual.GetComponentsInChildren<Transform>(true)
                    .Where(t => t.name.EndsWith("_RollingPivot")).ToArray();
            }
            return Save(root, name);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static int MakeModel(GameObject source, string sourcePath, string name, bool person, bool moving, float targetSize, float modelYaw)
    {
        GameObject root = new GameObject(name);
        try
        {
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(source);
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0f, modelYaw, 0f);
            model.transform.localScale = Vector3.one;
            model.name = "HighDetail_Model";
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) { Debug.LogWarning("Skipping " + sourcePath + ": no renderers found."); return 0; }
            // Normalize the physical scale and place feet/wheels on local Y=0.
            Bounds b = FindBounds(renderers);
            float size = person ? b.size.y : Mathf.Max(b.size.x, b.size.z);
            if (size > 0.0001f) model.transform.localScale = Vector3.one * (targetSize / size);
            b = FindBounds(model.GetComponentsInChildren<Renderer>(true));
            model.transform.position += new Vector3(-b.center.x, -b.min.y, -b.center.z);
            if (person)
            {
                AnimationClip clip = GetClip(sourcePath, moving ? "walk" : "idle");
                if (!clip && moving) clip = GetClip(sourcePath, "");
                if (!clip && !moving) clip = GetClip(sourcePath, "look");
                if (clip)
                {
                    Animator anim = model.GetComponent<Animator>();
                    if (!anim) anim = model.AddComponent<Animator>();
                    anim.runtimeAnimatorController = GetController(name, clip);
                    anim.applyRootMotion = false;
                    root.AddComponent<StreetMotionAnimator>();
                }
                else if (!moving)
                {
                    // FBX fallback has only Walk, not Idle. Freeze a frame rather than show a T-pose.
                    AnimationClip fallback = GetClip(sourcePath, "walk");
                    if (fallback != null)
                    {
                        try { fallback.SampleAnimation(model, Mathf.Min(.2f, fallback.length * .25f)); }
                        catch (Exception e) { Debug.LogWarning("Cannot pose " + name + ": " + e.Message); }
                    }
                }
                else Debug.LogWarning(name + ": no suitable walk animation found inside " + sourcePath + ". The route movement works, but leg animation needs a Walk clip.");
                SetCollider(root, true, moving, .50f, .50f, 1.75f);
                if (moving)
                {
                    var motion = root.AddComponent<StreetRouteMover>();
                    motion.speed = 1.3f; motion.distance = 9f; motion.loopToStart = false;
                }
            }
            else
            {
                SetCollider(root, false, moving, 1.9f, 4.5f, 1.35f);
                if (moving)
                {
                    var motion = root.AddComponent<StreetRouteMover>();
                    motion.speed = 5f; motion.distance = 30f; motion.loopToStart = true;
                }
            }
            return Save(root, name);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static Bounds FindBounds(Renderer[] renderers)
    {
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

    private static void SetCollider(GameObject root, bool person, bool moving, float width, float length, float height)
    {
        if (person)
        {
            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, height / 2f, 0);
            col.height = height;
            col.radius = width / 2f;
        }
        else
        {
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0, height / 2f, 0);
            col.size = new Vector3(width, height, length);
        }
        if (moving)
        {
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    private static AnimationClip GetClip(string sourcePath, string prefer)
    {
        AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<AnimationClip>()
            .Where(c => c && !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase) && c.length > .1f).ToArray();
        if (clips.Length == 0) return null;
        if (string.IsNullOrEmpty(prefer)) return clips[0];
        return clips.FirstOrDefault(c => c.name.IndexOf(prefer, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static RuntimeAnimatorController GetController(string name, AnimationClip sourceClip)
    {
        string animationPath = Controllers + "/" + name + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animationPath);
        if (!clip)
        {
            clip = UnityEngine.Object.Instantiate(sourceClip);
            clip.name = name + "_Loop";
            AnimationClipSettings s = AnimationUtility.GetAnimationClipSettings(clip);
            s.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, s);
            AssetDatabase.CreateAsset(clip, animationPath);
        }
        string path = Controllers + "/" + name + ".controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var state = controller.layers[0].stateMachine.defaultState;
        if (!state) state = controller.layers[0].stateMachine.AddState("Play");
        state.motion = clip;
        controller.layers[0].stateMachine.defaultState = state;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static Material PaintMaterial(string name, Color color)
    {
        string path = Materials + "/" + name + "_Paint.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) shader = Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .82f);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", .47f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static int Save(GameObject root, string name)
    {
        string path = Prefabs + "/" + name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        if (!prefab) { Debug.LogError("Failed to save " + name); return 0; }
        return 1;
    }
}
#endif
