using UnityEditor;
using UnityEngine;

public static class MeatballEnemyMenu
{
    [MenuItem("GameObject/Enemies/Meatball", false, 10)]
    static void CreateMeatball()
    {
        var root = new GameObject("Meatball");
        Undo.RegisterCreatedObjectUndo(root, "Create Meatball");
        root.transform.position = SceneView.lastActiveSceneView != null
            ? SceneView.lastActiveSceneView.pivot + Vector3.up : Vector3.up;
        root.AddComponent<SphereCollider>();
        root.AddComponent<Rigidbody>();
        var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "Visual";
        visual.transform.SetParent(root.transform, false);
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        const string path = "Assets/Meatball.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { color = new Color(0.32f, 0.09f, 0.035f) };
            AssetDatabase.CreateAsset(material, path);
        }
        visual.GetComponent<Renderer>().sharedMaterial = material;
        // Uneven surface makes the rolling motion visible with a plain material.
        for (int i = 0; i < 9; i++)
        {
            var lump = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lump.name = "Meatball lump";
            lump.transform.SetParent(visual.transform, false);
            float angle = i * 2.39996f;
            float y = 1f - (i + 0.5f) * 2f / 9f;
            float ring = Mathf.Sqrt(1f - y * y);
            lump.transform.localPosition = new Vector3(Mathf.Cos(angle) * ring, y, Mathf.Sin(angle) * ring) * 0.42f;
            lump.transform.localScale = Vector3.one * 0.25f;
            Object.DestroyImmediate(lump.GetComponent<Collider>());
            lump.GetComponent<Renderer>().sharedMaterial = material;
        }
        root.AddComponent<MeatballEnemy>().visual = visual.transform;
        Selection.activeGameObject = root;
    }
}
