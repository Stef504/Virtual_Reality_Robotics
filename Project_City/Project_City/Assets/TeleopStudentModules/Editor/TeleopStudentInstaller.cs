#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TeleopStudentModules.Editor
{
    // Adds UI ONLY. Does not replace the current scene, move the robot, or modify cameras/wheel physics.
    public static class TeleopStudentInstaller
    {
        const string RootName = "TeleopStudentModules_DROPIN";
        static Font font;
        static readonly Color panelBg = new Color(.075f,.12f,.19f,.92f);
        static readonly Color midBg = new Color(.12f,.19f,.29f,.98f);
        static readonly Color lightText = new Color(.89f,.94f,1f,1f);
        static readonly Color secondaryText = new Color(.68f,.79f,.88f,1f);

        [MenuItem("Tools/Teleop Students/Add 4-Module UI to CURRENT Scene")]
        public static void CreateDropIn()
        {
            if (FindExistingDropIn() != null)
            {
                EditorUtility.DisplayDialog("Already installed",
                    "A TeleopStudentModules_DROPIN object already exists in the open scene. " +
                    "The installer will not overwrite it. Select that object in the Hierarchy.", "OK");
                return;
            }
            if (!EditorUtility.DisplayDialog("Add student modules",
                "This ADDS a separate data source and overlay Canvas to the CURRENT open scene. " +
                "It does not move/modify your robot, wheels, cameras or existing UI. Continue?", "Add", "Cancel")) return;

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) { Debug.LogError("Could not load Unity LegacyRuntime.ttf"); return; }

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Add modular teleoperation UI");
            GameObject backend = Child(root.transform, "SharedData");
            var sceneSource = backend.AddComponent<SceneTeleopDataSource>();
            var sampleSource = backend.AddComponent<SampleTeleopDataSource>();
            sampleSource.enabled = false; // Switch context to this for independent mock tests.
            var externalSource = backend.AddComponent<ExternalTeleopDataSource>();
            externalSource.enabled = false; // An actual ROS adapter must push external messages.
            var context = backend.AddComponent<TeleopStudentContext>();
            context.dataSource = sceneSource;
            TryAutoWire(sceneSource);

            GameObject canvasGO = new GameObject("StudentUI_Canvas", typeof(RectTransform));
            canvasGO.transform.SetParent(root.transform, false);
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600,900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            var canvasRect = canvasGO.GetComponent<RectTransform>();
            Stretch(canvasRect);
            var backdrop = MakePanel(canvasGO.transform, "StudentOverlay", 0,0,1,1,
                new Color(.03f,.065f,.095f,.30f));
            var header = MakePanel(backdrop.transform,"Header",.015f,.92f,.985f,.99f,panelBg);
            MakeText(header.transform,"Title",.02f,.06f,.55f,.94f,"XR TELEOPERATION  /  STUDENT MODULES",22,TextAnchor.MiddleLeft,lightText);
            var sourceLabel = MakeText(header.transform,"DataSourceStatus",.58f,.05f,.985f,.94f,
                "UNITY SIMULATION",14,TextAnchor.MiddleRight,secondaryText);
            var headerStatus = header.AddComponent<ModuleStatusHeader>();
            headerStatus.context = context;
            headerStatus.status = sourceLabel;

            BuildCamera(backdrop.transform, context);
            BuildMap(backdrop.transform, context);
            BuildProximity(backdrop.transform, context);
            BuildSemantic(backdrop.transform, context);

            if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null)
            {
                GameObject events = Child(root.transform, "UI_EventSystem");
                events.AddComponent<EventSystem>();
                events.AddComponent<StandaloneInputModule>(); // Old input manager; see README for Input System note.
            }
            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Student modules added",
                "Added TeleopStudentModules_DROPIN. The existing scene and Husky are unchanged.\n\n" +
                "1) Inspect SharedData > Scene Teleop Data Source references.\n" +
                "2) Assign camera RenderTextures if needed.\n" +
                "3) Disable the old BaselineUI_Canvas ONLY if you want to see the new overlay alone.\n" +
                "4) Press Play. See QUICK_START.md.","OK");
        }

        [MenuItem("Tools/Teleop Students/Add OPTIONAL Manual Sample Markers")]
        public static void AddDemoLabels()
        {
            GameObject root = GameObject.Find(RootName);
            if (!root) { EditorUtility.DisplayDialog("Install modules first", "Add the modular UI before creating optional sample markers.", "OK"); return; }
            if (GameObject.Find("TeleopManualLabelExamples") != null) return;
            var source = root.GetComponentInChildren<SceneTeleopDataSource>(true);
            Vector3 origin = source != null && source.huskyRoot != null ? source.huskyRoot.position : Vector3.zero;
            GameObject group = new GameObject("TeleopManualLabelExamples");
            Undo.RegisterCreatedObjectUndo(group, "Create optional annotation examples");
            string[] names = { "Example pallet", "Example moving agent", "Example obstacle" };
            Vector3[] offsets = { new Vector3(-2f,.5f,3f), new Vector3(1.5f,.5f,2.2f), new Vector3(3f,.5f,-1.5f) };
            for (int i=0;i<names.Length;i++)
            {
                GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                g.name = names[i];
                g.transform.SetParent(group.transform);
                g.transform.position = origin + offsets[i];
                g.transform.localScale = Vector3.one * .32f;
                Collider c = g.GetComponent<Collider>();
                if (c) UnityEngine.Object.DestroyImmediate(c);
                var tag = g.AddComponent<SemanticSceneLabel>();
                tag.objectLabel = names[i];
                tag.manuallyMarkedMoving = i == 1;
            }
            Selection.activeGameObject = group;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Optional sample annotations",
                "Created three small NON-COLLIDING spheres with manual labels. Their 'moving' tag is NOT detected; it is assigned in the Inspector. Reposition or delete freely.","OK");
        }

        static GameObject FindExistingDropIn()
        {
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
                if (go.name == RootName && go.scene.IsValid()) return go;
            return null;
        }
        static void TryAutoWire(SceneTeleopDataSource s)
        {
            GameObject robot = GameObject.Find("Husky_a200");
            if (robot != null && robot.activeInHierarchy)
            {
                s.huskyRoot = robot.transform;
                Transform baseLink = FindDescendant(robot.transform, "base_link");
                s.robotPoseFrame = baseLink != null ? baseLink : robot.transform;
            }
            Transform fc = FindActiveNamed("FrontCamera");
            Transform rc = FindActiveNamed("RearCamera");
            Transform mc = FindActiveNamed("DemoGroundTruthMapCamera");
            if (fc) s.frontCamera = fc.GetComponent<Camera>();
            if (rc) s.rearCamera = rc.GetComponent<Camera>();
            if (mc) s.topDownCamera = mc.GetComponent<Camera>();
        }
        static Transform FindActiveNamed(string name)
        {
            foreach (Transform t in UnityEngine.Object.FindObjectsOfType<Transform>())
                if (t.name == name && t.gameObject.activeInHierarchy) return t;
            return null;
        }
        static Transform FindDescendant(Transform t, string name)
        {
            foreach (Transform child in t)
            {
                if (child.name == name) return child;
                Transform found = FindDescendant(child, name);
                if (found != null) return found;
            }
            return null;
        }
        static void BuildCamera(Transform parent, TeleopStudentContext ctx)
        {
            var p=CreateModule(parent,"01_CameraModule", "01 / CAMERA & VISUAL AWARENESS", .015f,.51f,.635f,.91f);
            var main=MakePanel(p.transform,"PrimaryFrame",.025f,.15f,.70f,.84f,midBg);
            var mainImg=MakeRaw(main.transform,"PrimaryCamera",.015f,.04f,.985f,.91f);
            var mainLabel=MakeText(main.transform,"PrimaryLabel",.025f,.90f,.98f,.995f,"FRONT CAMERA",13,TextAnchor.MiddleLeft,lightText);
            var second=MakePanel(p.transform,"SecondaryFrame",.715f,.31f,.98f,.84f,midBg);
            var secondImg=MakeRaw(second.transform,"SecondaryCamera",.04f,.04f,.96f,.88f);
            var secondLabel=MakeText(second.transform,"SecondaryLabel",.04f,.87f,.96f,.99f,"REAR CAMERA",12,TextAnchor.MiddleLeft,lightText);
            var status=MakeText(p.transform,"Status",.025f,.04f,.7f,.14f,"Connect camera RenderTextures",12,TextAnchor.MiddleLeft,secondaryText);
            Button swap=MakeButton(p.transform,"SwapFrontRear",.72f,.17f,.84f,.28f,"SWAP");
            Button hide=MakeButton(p.transform,"ToggleRear",.85f,.17f,.98f,.28f,"REAR ON/OFF");
            var module=p.AddComponent<CameraModule>();
            module.context=ctx; module.primaryImage=mainImg; module.secondaryImage=secondImg;
            module.primaryLabel=mainLabel; module.secondaryLabel=secondLabel; module.statusText=status;
            module.swapButton=swap; module.secondaryButton=hide; module.secondaryView=second;
        }
        static void BuildMap(Transform parent, TeleopStudentContext ctx)
        {
            var p=CreateModule(parent,"02_SpatialMapModule", "02 / SPATIAL MAP", .65f,.51f,.985f,.91f);
            var mapFrame=MakePanel(p.transform,"MapFrame",.035f,.19f,.97f,.83f,midBg);
            var raw=MakeRaw(mapFrame.transform,"MapTexture",0f,0f,1f,1f);
            var marker=MakeText(mapFrame.transform,"RobotMarker",.48f,.48f,.52f,.52f,"▲",27,TextAnchor.MiddleCenter,new Color(.24f,1f,.76f));
            marker.rectTransform.sizeDelta=new Vector2(24,24);
            var status=MakeText(p.transform,"Status",.035f,.08f,.9f,.18f,"No top-down RenderTexture",11,TextAnchor.MiddleLeft,secondaryText);
            var coordinates=MakeText(p.transform,"RobotPose",.035f,.015f,.98f,.08f,"Robot pose unavailable",11,TextAnchor.MiddleLeft,lightText);
            Button plus=MakeButton(p.transform,"ZoomIn",.69f,.85f,.78f,.98f,"+");
            Button minus=MakeButton(p.transform,"ZoomOut",.79f,.85f,.88f,.98f,"-");
            Button reset=MakeButton(p.transform,"ZoomReset",.89f,.85f,.98f,.98f,"1:1");
            var m=p.AddComponent<SpatialMapModule>();
            m.context=ctx; m.mapImage=raw; m.marker=marker.rectTransform;
            m.coordinateText=coordinates; m.statusText=status;
            m.zoomInButton=plus; m.zoomOutButton=minus; m.resetZoomButton=reset;
        }
        static void BuildProximity(Transform parent, TeleopStudentContext ctx)
        {
            var p=CreateModule(parent,"03_ObstacleAwarenessModule", "03 / PROXIMITY & WARNINGS", .015f,.04f,.485f,.49f);
            var lblL=MakeText(p.transform,"LeftDistance",.04f,.64f,.47f,.77f,"LEFT  N/A",19,TextAnchor.MiddleLeft,lightText);
            var lblR=MakeText(p.transform,"RightDistance",.52f,.64f,.96f,.77f,"RIGHT  N/A",19,TextAnchor.MiddleLeft,lightText);
            var leftBg=MakePanel(p.transform,"LeftBarBg",.04f,.51f,.46f,.60f,midBg);
            var rightBg=MakePanel(p.transform,"RightBarBg",.54f,.51f,.96f,.60f,midBg);
            var leftFill=MakePanel(leftBg.transform,"LeftUrgency",0,0,1,1,new Color(1f,.72f,.3f));
            var rightFill=MakePanel(rightBg.transform,"RightUrgency",0,0,1,1,new Color(1f,.72f,.3f));
            Image leftImage=leftFill.GetComponent<Image>(), rightImage=rightFill.GetComponent<Image>();
            // Bar lengths are set through RectTransform anchors (no sprite required).
            var info=MakeText(p.transform,"WarningInfo",.04f,.27f,.96f,.42f,"VISUAL PREVIEW — NO HAPTIC HARDWARE",13,TextAnchor.MiddleLeft,secondaryText);
            var threshold=MakeText(p.transform,"Threshold",.04f,.07f,.53f,.22f,"WARN 0.85 m",16,TextAnchor.MiddleLeft,lightText);
            Button plus=MakeButton(p.transform,"IncreaseThreshold",.67f,.08f,.80f,.23f,"WARN +");
            Button minus=MakeButton(p.transform,"DecreaseThreshold",.82f,.08f,.96f,.23f,"WARN -");
            var m=p.AddComponent<ObstacleAwarenessModule>();
            m.context=ctx; m.leftText=lblL; m.rightText=lblR; m.statusText=info;
            m.thresholdText=threshold; m.leftBar=leftImage; m.rightBar=rightImage;
            m.increaseWarningButton=plus; m.decreaseWarningButton=minus;
        }
        static void BuildSemantic(Transform parent, TeleopStudentContext ctx)
        {
            var p=CreateModule(parent,"04_SemanticPerceptionModule", "04 / STATIC & MOVING SCENE LABELS", .50f,.04f,.985f,.49f);
            var status=MakeText(p.transform,"LabelStatus",.035f,.79f,.67f,.9f,"No scene labels",12,TextAnchor.MiddleLeft,secondaryText);
            Button stat=MakeButton(p.transform,"ToggleStatic",.68f,.83f,.82f,.98f,"STATIC");
            Button moving=MakeButton(p.transform,"ToggleMoving",.83f,.83f,.98f,.98f,"MOVING");
            var filters=MakeText(p.transform,"Filters",.035f,.7f,.96f,.78f,"STATIC ON   MOVING ON",12,TextAnchor.MiddleLeft,lightText);
            Text[] rows=new Text[6];
            for (int i=0;i<rows.Length;i++)
            {
                float top=.69f-i*.092f;
                rows[i]=MakeText(p.transform,"ObjectRow_"+(i+1),.045f,top-.087f,.97f,top,
                    "",14,TextAnchor.MiddleLeft,lightText);
            }
            var m=p.AddComponent<SemanticPerceptionModule>();
            m.context=ctx; m.statusText=status; m.filterText=filters; m.objectRows=rows;
            m.staticFilterButton=stat; m.movingFilterButton=moving;
        }
        static GameObject CreateModule(Transform parent,string name,string title,float x1,float y1,float x2,float y2)
        {
            GameObject g=MakePanel(parent,name,x1,y1,x2,y2,panelBg);
            MakeText(g.transform,"ModuleTitle",.025f,.85f,.96f,.99f,title,17,TextAnchor.MiddleLeft,lightText);
            return g;
        }
        static GameObject Child(Transform parent,string name)
        {
            GameObject g=new GameObject(name);
            g.transform.SetParent(parent,false);
            return g;
        }
        static void Stretch(RectTransform r)
        {
            r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one;
            r.offsetMin=Vector2.zero; r.offsetMax=Vector2.zero;
        }
        static RectTransform Rect(Transform parent,string name,float x1,float y1,float x2,float y2)
        {
            GameObject go=new GameObject(name,typeof(RectTransform));
            go.transform.SetParent(parent,false);
            RectTransform rect=go.GetComponent<RectTransform>();
            rect.anchorMin=new Vector2(x1,y1);rect.anchorMax=new Vector2(x2,y2);
            rect.offsetMin=Vector2.zero;rect.offsetMax=Vector2.zero;
            return rect;
        }
        static GameObject MakePanel(Transform parent,string name,float x1,float y1,float x2,float y2,Color color)
        {
            RectTransform rect=Rect(parent,name,x1,y1,x2,y2);
            Image image=rect.gameObject.AddComponent<Image>();
            image.color=color;
            return rect.gameObject;
        }
        static RawImage MakeRaw(Transform parent,string name,float x1,float y1,float x2,float y2)
        {
            var rect=Rect(parent,name,x1,y1,x2,y2);
            RawImage raw=rect.gameObject.AddComponent<RawImage>();
            raw.color=Color.black;
            return raw;
        }
        static Text MakeText(Transform parent,string name,float x1,float y1,float x2,float y2,
            string content,int size,TextAnchor align,Color color)
        {
            var rect=Rect(parent,name,x1,y1,x2,y2);
            Text t=rect.gameObject.AddComponent<Text>();
            t.font=font; t.text=content; t.fontSize=size; t.alignment=align;
            t.color=color; t.raycastTarget=false;
            t.horizontalOverflow=HorizontalWrapMode.Wrap;
            t.verticalOverflow=VerticalWrapMode.Truncate;
            return t;
        }
        static Button MakeButton(Transform parent,string name,float x1,float y1,float x2,float y2,string label)
        {
            GameObject p=MakePanel(parent,name,x1,y1,x2,y2,new Color(.22f,.37f,.52f,1f));
            Button b=p.AddComponent<Button>();
            b.targetGraphic=p.GetComponent<Image>();
            MakeText(p.transform,"Label",.02f,.02f,.98f,.98f,label,12,TextAnchor.MiddleCenter,lightText);
            return b;
        }
    }
}
#endif
