#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace TeleopLab.Editor
{
    // Run once from Tools > XR Teleop Lab > Build Demo Scene.
    // Overwrites the three generated RT assets, baseline UI prefab and sample scene.
    public static class TeleopSceneBuilder
    {
        const string Root = "Assets/TeleopTemplate";
        static Font font;

        [MenuItem("Tools/XR Teleop Lab/Build Demo Scene")]
        public static void Build()
        {
            if (!EditorUtility.DisplayDialog("Build reference teleoperation scene",
                "This overwrites TeleopDemo.unity, BaselineUI.prefab and generated render textures. Student copies should be saved under new names. Continue?", "Build", "Cancel")) return;

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.1f;
            sun.transform.rotation = Quaternion.Euler(52, -40, 0);
            RenderSettings.ambientLight = new Color(.48f,.53f,.59f);
            RenderSettings.fog = false;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(2.2f, 1, 2.2f);
            SetMaterial(ground, new Color(.49f,.55f,.56f));

            BuildWall("Boundary_Left",  -5.2f,0,0, .25f,2,19);
            BuildWall("Boundary_Right",  5.2f,0,0, .25f,2,19);
            BuildWall("Boundary_Far",  0,0,9.2f, 10.6f,2,.25f);
            BuildWall("Boundary_Near", 0,0,-9.2f, 10.6f,2,.25f);
            BuildWall("Divider_A", -2.3f,0,-3.5f, .35f,1.2f,6.3f);
            BuildWall("Divider_B", 2.2f,0,2.5f, .35f,1.2f,5.0f);
            BuildBox("Obstacle_Crate_A", new Vector3(1.0f,.50f,-3.1f), new Vector3(1.3f,1,1.0f), new Color(.83f,.57f,.29f));
            BuildBox("Obstacle_Crate_B", new Vector3(-1.1f,.50f,3.5f), new Vector3(1.0f,1,1.4f), new Color(.83f,.57f,.29f));
            BuildBox("Obstacle_Low", new Vector3(3.3f,.22f,5.0f), new Vector3(.9f,.44f,1.0f), new Color(.83f,.57f,.29f));

            var goal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            goal.name = "Finish_Zone";
            goal.transform.position = new Vector3(3, .03f, 7.1f);
            goal.transform.localScale = new Vector3(.8f,.04f,.8f);
            SetMaterial(goal, new Color(.23f,.84f,.52f));
            goal.GetComponent<Collider>().isTrigger = true;
            var start = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            start.name = "Start_Zone";
            start.transform.position = new Vector3(0, .025f, -7f);
            start.transform.localScale = new Vector3(.8f,.03f,.8f);
            start.GetComponent<Collider>().isTrigger = true;
            SetMaterial(start, new Color(.22f,.57f,.89f));

            GameObject root = new GameObject("RobotRoot__Replace_Visual_Child_With_URDF");
            root.transform.position = new Vector3(0, 0, -7);
            var character = root.AddComponent<CharacterController>();
            character.height = .93f; character.radius = .44f;
            character.center = new Vector3(0, .47f, 0);
            character.stepOffset = .1f;
            character.slopeLimit = 40;
            DemoRobotDriver driver = root.AddComponent<DemoRobotDriver>();
            driver.goal = goal.transform;
            driver.startPosition = root.transform.position;
            var robotVisual = new GameObject("RobotVisual__Replace_With_Imported_URDF");
            robotVisual.transform.SetParent(root.transform, false);
            driver.visualRoot = robotVisual.transform;
            BuildBody(robotVisual.transform);
            DemoProximity proximity = root.AddComponent<DemoProximity>();
            proximity.obstacleMask = 1 << 0; // Default layer only, robot visuals are Ignore Raycast

            RenderTexture frontRT = MakeRT("FrontView",640,480);
            RenderTexture rearRT = MakeRT("RearView",640,480);
            RenderTexture mapRT = MakeRT("TopDownDemoMap",512,512);
            MakeRobotCamera("FrontCamera", root.transform, new Vector3(0,.86f,.46f), Quaternion.identity, frontRT);
            MakeRobotCamera("RearCamera", root.transform, new Vector3(0,.86f,-.46f), Quaternion.Euler(0,180,0), rearRT);
            Camera top = MakeRobotCamera("DemoGroundTruthMapCamera", null, new Vector3(0,18,0), Quaternion.Euler(90,0,0), mapRT);
            top.orthographic = true; top.orthographicSize = 10.6f;
            top.clearFlags = CameraClearFlags.SolidColor; top.backgroundColor = new Color(.25f,.31f,.39f);

            Camera editorPreview = MakeRobotCamera("EditorPreviewCamera", null, new Vector3(0,16,-19), Quaternion.identity, null);
            editorPreview.transform.LookAt(new Vector3(0,0,0));
            editorPreview.tag = "MainCamera";
            editorPreview.gameObject.AddComponent<AudioListener>();

            GameObject control = new GameObject("TeleopApp__Mode_Demo_or_ROS2");
            var core = control.AddComponent<TeleopCore>();
            var trial = control.AddComponent<TrialRecorder>();
            trial.robot = driver; driver.trials = trial;
            core.demoDriver = driver; core.demoProximity = proximity; core.trials = trial;

            GameObject canvasObject = BuildUI(frontRT, rearRT, mapRT);
            TeleopDashboard ui = canvasObject.GetComponent<TeleopDashboard>();
            core.dashboard = ui;

            string prefabPath = Root + "/GeneratedAssets/BaselineUI.prefab";
            AssetDatabase.DeleteAsset(prefabPath);
            // Save UI separately so students can duplicate & refine it as their deliverable.
            PrefabUtility.SaveAsPrefabAssetAndConnect(canvasObject, prefabPath, InteractionMode.AutomatedAction);
            // Scene references are instance overrides, not dependencies of the reusable prefab.
            ui.demoProximity = proximity; ui.trials = trial;

            string scenePath = Root + "/Scenes/TeleopDemo.unity";
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorUtility.DisplayDialog("Teleoperation template created",
                "Open Assets/TeleopTemplate/Scenes/TeleopDemo.unity and press Play.\n\nUse WASD, SPACE, R, F1/F2. See README.md for ROS setup and caveats.", "OK");
        }

        static RenderTexture MakeRT(string name, int width, int height)
        {
            string p = Root + "/GeneratedAssets/" + name + ".renderTexture";
            AssetDatabase.DeleteAsset(p);
            var rt = new RenderTexture(width,height,16) { name=name, antiAliasing=1 };
            AssetDatabase.CreateAsset(rt,p);
            return rt;
        }

        static Camera MakeRobotCamera(string name, Transform parent, Vector3 position, Quaternion rotation, RenderTexture target)
        {
            var go = new GameObject(name);
            if (parent) { go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localRotation=rotation; }
            else { go.transform.position=position; go.transform.rotation=rotation; }
            var camera = go.AddComponent<Camera>();
            camera.targetTexture = target;
            camera.fieldOfView = 85f;
            camera.nearClipPlane = .02f;
            camera.farClipPlane = 80f;
            if (target) camera.depth = -2;
            return camera;
        }

        static void BuildBody(Transform parent)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Placeholder_Chassis";
            body.transform.SetParent(parent,false);
            body.transform.localPosition = new Vector3(0,.49f,0);
            body.transform.localScale = new Vector3(.75f,.33f,1.0f);
            SetMaterial(body,new Color(.21f,.39f,.66f));
            MakeVisualOnly(body);
            for (int side=-1; side<=1; side+=2)
            for (int front=-1; front<=1; front+=2)
            {
                var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = "VisualWheel_"+side+"_"+front;
                wheel.transform.SetParent(parent,false);
                wheel.transform.localPosition = new Vector3(side*.43f,.29f,front*.32f);
                wheel.transform.localRotation = Quaternion.Euler(0,0,90);
                wheel.transform.localScale = new Vector3(.28f,.10f,.28f);
                SetMaterial(wheel, new Color(.12f,.15f,.18f)); MakeVisualOnly(wheel);
            }
            BuildBoxChild("FrontIndicator", parent, new Vector3(0,.73f,.43f), new Vector3(.25f,.06f,.10f),new Color(.23f,.94f,.68f));
        }
        static void MakeVisualOnly(GameObject go)
        {
            go.layer = 2; // Ignore Raycast
            Collider c = go.GetComponent<Collider>();
            if (c) Object.DestroyImmediate(c);
        }
        static void BuildBoxChild(string name, Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name; cube.transform.SetParent(parent,false);
            cube.transform.localPosition = pos; cube.transform.localScale = scale;
            MakeVisualOnly(cube); SetMaterial(cube,color);
        }
        static void BuildWall(string name, float x, float y, float z, float width, float height, float depth)
        {
            BuildBox(name,new Vector3(x,y+height*.5f,z),new Vector3(width,height,depth),new Color(.35f,.44f,.56f));
        }
        static void BuildBox(string name, Vector3 pos, Vector3 scale, Color color)
        {
            var box=GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name=name; box.transform.position=pos; box.transform.localScale=scale;
            SetMaterial(box,color);
        }
        static void SetMaterial(GameObject go, Color c)
        {
            var renderer=go.GetComponent<Renderer>();
            if (!renderer) return;
            string path = Root + "/GeneratedAssets/Mat_" + go.name + ".mat";
            // Each material is a project asset, not an unsaved transient editor object.
            AssetDatabase.DeleteAsset(path);
            Material m = new Material(Shader.Find("Standard"));
            m.color=c;
            AssetDatabase.CreateAsset(m,path);
            renderer.sharedMaterial=m;
        }

        static GameObject BuildUI(RenderTexture frontRT, RenderTexture rearRT, RenderTexture mapRT)
        {
            var root=new GameObject("BaselineUI_Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1600,900); scaler.matchWidthOrHeight=.5f;
            var dashboard=root.AddComponent<TeleopDashboard>();
            dashboard.demoFront=frontRT; dashboard.demoRear=rearRT; dashboard.demoMap=mapRT;

            Panel(root.transform,"Header",.025f,.91f,.975f,.985f,new Color(.08f,.13f,.19f,.98f));
            dashboard.modeText=Text(root.transform,"ModeText","STANDALONE  |  DEMO DATA",.05f,.927f,.61f,.97f,24,TextAnchor.MiddleLeft);
            dashboard.statusText=Text(root.transform,"StatusText","SIMULATED / LOCAL",.60f,.927f,.95f,.97f,17,TextAnchor.MiddleRight);

            var frontPanel=Panel(root.transform,"FrontPanel",.025f,.375f,.64f,.895f, new Color(.07f,.11f,.16f,.99f));
            Text(frontPanel,"FrontTitle","PRIMARY  /  FRONT VIEW",0,.9f,1,1,19,TextAnchor.MiddleCenter);
            dashboard.frontImage=Raw(frontPanel,"FrontFeed",.018f,.025f,.982f,.89f,frontRT);

            var rearPanel=Panel(root.transform,"RearPanel",.66f,.61f,.975f,.895f,new Color(.07f,.11f,.16f,.99f));
            Text(rearPanel,"RearTitle","SECONDARY  /  REAR VIEW",0,.85f,1,1,17,TextAnchor.MiddleCenter);
            dashboard.rearImage=Raw(rearPanel,"RearFeed",.025f,.03f,.975f,.84f,rearRT);

            var mapPanel=Panel(root.transform,"MapPanel",.025f,.045f,.64f,.36f,new Color(.07f,.11f,.16f,.99f));
            dashboard.mapLabel=Text(mapPanel,"MapTitle","TOP-DOWN CAMERA  |  NOT SLAM",0,.83f,1,1,16,TextAnchor.MiddleCenter);
            dashboard.mapImage=Raw(mapPanel,"MapFeed",.02f,.04f,.98f,.82f,mapRT);
            var marker=Text(dashboard.mapImage.transform,"RobotMarker","^",0,0,0,0,30,TextAnchor.MiddleCenter);
            marker.color=new Color(.04f,.85f,.95f);
            var markerRect=marker.GetComponent<RectTransform>();
            markerRect.anchorMin=markerRect.anchorMax=new Vector2(.5f,.5f);
            markerRect.pivot=new Vector2(.5f,.5f); markerRect.sizeDelta=new Vector2(36,36);
            markerRect.gameObject.SetActive(false);
            dashboard.mapRobotMarker=markerRect;

            var proxPanel=Panel(root.transform,"AwarenessPanel",.66f,.375f,.975f,.595f,new Color(.07f,.11f,.16f,.99f));
            Text(proxPanel,"AwarenessTitle","PROXIMITY / AWARENESS",.05f,.79f,.95f,.98f,16,TextAnchor.MiddleLeft);
            dashboard.leftText=Text(proxPanel,"LeftDistance","LEFT  N/A",.05f,.58f,.95f,.76f,17,TextAnchor.MiddleLeft);
            dashboard.leftBar=Bar(proxPanel,"LeftUrgency",.06f,.50f,.94f,.56f);
            dashboard.rightText=Text(proxPanel,"RightDistance","RIGHT N/A",.05f,.27f,.95f,.46f,17,TextAnchor.MiddleLeft);
            dashboard.rightBar=Bar(proxPanel,"RightUrgency",.06f,.18f,.94f,.24f);

            var helpPanel=Panel(root.transform,"ControlsAndResults",.66f,.045f,.975f,.36f,new Color(.07f,.11f,.16f,.99f));
            Text(helpPanel,"ControlsTitle","CONTROL & TRIAL STATUS",.05f,.80f,.95f,.98f,17,TextAnchor.MiddleLeft);
            dashboard.metricText=Text(helpPanel,"Metrics","TIME  0.0 s    CONTACTS  0",.05f,.54f,.95f,.78f,18,TextAnchor.MiddleLeft);
            dashboard.helpText=Text(helpPanel,"Instructions","W/S drive   A/D turn  |  SPACE stop  |  R reset  |  F2 ROS",.05f,.32f,.95f,.57f,15,TextAnchor.MiddleLeft);
            dashboard.swapButton=MakeButton(helpPanel,"SwapCamerasButton","SWAP CAMERAS",.045f,.06f,.355f,.27f);
            dashboard.mapButton=MakeButton(helpPanel,"ToggleMapButton","TOGGLE MAP",.365f,.06f,.665f,.27f);
            dashboard.resetButton=MakeButton(helpPanel,"ResetButton","RESET",.675f,.06f,.955f,.27f);
            return root;
        }

        static Transform Panel(Transform parent,string name,float xmin,float ymin,float xmax,float ymax,Color c)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));
            go.transform.SetParent(parent,false);
            Rect(go.GetComponent<RectTransform>(),xmin,ymin,xmax,ymax);
            var image=go.GetComponent<Image>(); image.color=c;
            return go.transform;
        }
        static RawImage Raw(Transform parent,string name,float xmin,float ymin,float xmax,float ymax,Texture texture)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(RawImage));
            go.transform.SetParent(parent,false);
            Rect(go.GetComponent<RectTransform>(),xmin,ymin,xmax,ymax);
            var image=go.GetComponent<RawImage>(); image.texture=texture; image.color=Color.white;
            // Prevent the 4:3 camera feed being stretched to fill a wide HUD panel.
            if (name == "FrontFeed" || name == "RearFeed" || name == "MapFeed")
            {
                var fitter = go.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = name == "MapFeed" ? 1f : 4f / 3f;
            }
            return image;
        }
        static Text Text(Transform parent,string name,string value,float xmin,float ymin,float xmax,float ymax,int size,TextAnchor alignment)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));
            go.transform.SetParent(parent,false);
            Rect(go.GetComponent<RectTransform>(),xmin,ymin,xmax,ymax);
            var txt=go.GetComponent<Text>();
            txt.text=value; txt.font=font; txt.fontSize=size; txt.alignment=alignment;
            txt.color=new Color(.91f,.95f,.98f); txt.raycastTarget=false;
            txt.horizontalOverflow=HorizontalWrapMode.Wrap;
            txt.verticalOverflow=VerticalWrapMode.Truncate;
            return txt;
        }
        static Button MakeButton(Transform parent,string name,string caption,float xmin,float ymin,float xmax,float ymax)
        {
            var g=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));
            g.transform.SetParent(parent,false);
            Rect(g.GetComponent<RectTransform>(),xmin,ymin,xmax,ymax);
            g.GetComponent<Image>().color=new Color(.19f,.36f,.52f);
            var button=g.GetComponent<Button>();
            Text(g.transform,"Label",caption,.03f,0,.97f,1,13,TextAnchor.MiddleCenter);
            return button;
        }
        static Image Bar(Transform parent,string name,float xmin,float ymin,float xmax,float ymax)
        {
            var under=Panel(parent,name+"Track",xmin,ymin,xmax,ymax,new Color(.27f,.35f,.40f));
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));
            go.transform.SetParent(under,false);
            Rect(go.GetComponent<RectTransform>(),0,0,1,1);
            var img=go.GetComponent<Image>(); img.color=new Color(.95f,.65f,.30f);
            img.type=Image.Type.Filled; img.fillMethod=Image.FillMethod.Horizontal;
            img.fillAmount=0;
            return img;
        }
        static void Rect(RectTransform rect,float xmin,float ymin,float xmax,float ymax)
        {
            rect.anchorMin=new Vector2(xmin,ymin);
            rect.anchorMax=new Vector2(xmax,ymax);
            rect.offsetMin=Vector2.zero; rect.offsetMax=Vector2.zero;
        }
    }
}
#endif
