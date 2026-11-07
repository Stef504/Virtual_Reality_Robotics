using UnityEditor;
using UnityEngine;
using TeleopPedestrian;

namespace TeleopPedestrian.Editor
{
    public static class CreateWalkingPedestrian
    {
        [MenuItem("Tools/Teleop Students/Create Walking Person")]
        public static void Create()
        {
            // This generator only creates new objects in the open scene.
            GameObject root = new GameObject("WalkingPerson_01");
            Undo.RegisterCreatedObjectUndo(root, "Create walking pedestrian");

            // Place close to scene-view focus, if possible, but keep feet on Y=0.
            Vector3 position = Vector3.zero;
            if (SceneView.lastActiveSceneView != null)
            {
                position = SceneView.lastActiveSceneView.pivot;
                position.y = 0f;
            }
            root.transform.position = position;

            var walker = root.AddComponent<WalkingPedestrian>();
            walker.pathLength = 5f;
            walker.speed = 0.8f;

            Material clothes = MakeMaterial(new Color(0.16f, 0.42f, 0.74f));
            Material trousers = MakeMaterial(new Color(0.17f, 0.21f, 0.28f));
            Material skin = MakeMaterial(new Color(0.76f, 0.54f, 0.39f));
            Material shoes = MakeMaterial(new Color(0.09f, 0.1f, 0.12f));

            // Primitive colliders removed from body parts: these are animated visuals,
            // not physical ragdolls. This avoids accidental collisions with the Husky.
            Part("Torso", PrimitiveType.Capsule, root.transform,
                new Vector3(0f, 1.25f, 0f), new Vector3(0.43f, 0.52f, 0.28f), clothes);
            Part("Head", PrimitiveType.Sphere, root.transform,
                new Vector3(0f, 1.81f, 0f), new Vector3(0.27f, 0.31f, 0.27f), skin);

            Transform leftArm = Pivot("LeftArmPivot", root.transform, new Vector3(-0.28f, 1.56f, 0f));
            Transform rightArm = Pivot("RightArmPivot", root.transform, new Vector3(0.28f, 1.56f, 0f));
            Part("LeftArm", PrimitiveType.Capsule, leftArm, new Vector3(0f, -0.35f, 0f), new Vector3(0.16f, 0.38f, 0.16f), clothes);
            Part("RightArm", PrimitiveType.Capsule, rightArm, new Vector3(0f, -0.35f, 0f), new Vector3(0.16f, 0.38f, 0.16f), clothes);

            Transform leftLeg = Pivot("LeftLegPivot", root.transform, new Vector3(-0.14f, 0.91f, 0f));
            Transform rightLeg = Pivot("RightLegPivot", root.transform, new Vector3(0.14f, 0.91f, 0f));
            Part("LeftLeg", PrimitiveType.Capsule, leftLeg, new Vector3(0f, -0.39f, 0f), new Vector3(0.19f, 0.43f, 0.19f), trousers);
            Part("RightLeg", PrimitiveType.Capsule, rightLeg, new Vector3(0f, -0.39f, 0f), new Vector3(0.19f, 0.43f, 0.19f), trousers);
            Part("LeftShoe", PrimitiveType.Cube, leftLeg, new Vector3(0f, -0.78f, 0.09f), new Vector3(0.20f, 0.11f, 0.36f), shoes);
            Part("RightShoe", PrimitiveType.Cube, rightLeg, new Vector3(0f, -0.78f, 0.09f), new Vector3(0.20f, 0.11f, 0.36f), shoes);

            walker.leftArmPivot = leftArm;
            walker.rightArmPivot = rightArm;
            walker.leftLegPivot = leftLeg;
            walker.rightLegPivot = rightLeg;

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            Debug.Log("WalkingPerson_01 created. Move it onto the floor, set Path Length, then press Play.", root);
        }

        static Transform Pivot(string name, Transform parent, Vector3 position)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            return go.transform;
        }

        static GameObject Part(string name, PrimitiveType shape, Transform parent,
            Vector3 pos, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(shape);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            Collider c = go.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        static Material MakeMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Material material = new Material(shader);
            material.color = color;
            return material;
        }
    }
}
