using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class MeatballDiagnostics
{
    [MenuItem("Tools/Diagnose Selected Meatball")]
    public static void Run()
    {
        var enemy = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<MeatballEnemy>() : null;
        if (enemy == null || !Application.isPlaying) { Debug.Log("Select a meatball while playing."); return; }
        var bounds = enemy.GetComponent<SphereCollider>().bounds;
        string result = "MEATBALL DIAGNOSTICS: center=" + bounds.center + " bounds=" + bounds.size;
        if (enemy.target != null) result += " target=" + enemy.target.position + " distance=" + Vector3.Distance(enemy.transform.position, enemy.target.position);
        foreach (var method in new[] { "Grounded", "SafeAhead" })
            result += " " + method + "=" + typeof(MeatballEnemy).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(enemy, null);
        var direction = (Vector3)typeof(MeatballEnemy).GetField("direction", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enemy);
        result += " direction=" + direction;
        foreach (var hit in Physics.RaycastAll(bounds.center + Vector3.up * 2f, Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore))
            result += " BELOW=" + hit.collider.name + " distance=" + hit.distance + " normal=" + hit.normal;
        Debug.Log(result, enemy);
    }
}
