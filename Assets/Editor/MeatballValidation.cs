using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class MeatballValidation
{
    static object Call(object obj, string method) => obj.GetType().GetMethod(method,
        BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, null);
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    [MenuItem("Tools/Validate Meatball Ground Safety")]
    public static void Run()
    {
        GameObject floor = null, enemyObject = null, playerObject = null;
        try
        {
            // Far from level geometry; no scene assets are opened or saved.
            Vector3 origin = new Vector3(10000, 10000, 10000);
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = origin + Vector3.down * 0.5f;
            floor.transform.localScale = new Vector3(10, 1, 10);
            enemyObject = new GameObject("Ground safety validation");
            enemyObject.transform.position = origin + Vector3.up * 0.55f;
            var enemy = enemyObject.AddComponent<MeatballEnemy>();
            Call(enemy, "Awake");
            typeof(MeatballEnemy).GetField("direction", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(enemy, Vector3.right);
            Physics.SyncTransforms();
            Check((bool)Call(enemy, "Grounded"), "Flat floor must count as ground");
            Check((bool)Call(enemy, "SafeAhead"), "Charge must be allowed on a broad floor");
            enemyObject.transform.position = origin + new Vector3(4.1f, 0.55f, 0);
            Physics.SyncTransforms();
            Check((bool)Call(enemy, "Grounded"), "Enemy must still be grounded before the edge");
            Check(!(bool)Call(enemy, "SafeAhead"), "Charge must stop BEFORE reaching the edge");
            floor.GetComponent<Collider>().enabled = false;
            Physics.SyncTransforms();
            Check(!(bool)Call(enemy, "Grounded"), "Own collider must not count as ground");
            Check(!(bool)Call(enemy, "SafeAhead"), "No floor must reject a charge");
            playerObject = new GameObject("Health validation");
            var health = playerObject.AddComponent<PlayerHealth>();
            Call(health, "Awake");
            health.TakeDamage(1);
            Check(health.Health == 4, "Player must take damage");
            health.TakeDamage(1);
            Check(health.Health == 4, "Invulnerability must prevent repeated hits");
            health.invulnerabilityTime = 0;
            health.Respawn();
            health.respawnOnDeath = false;
            health.TakeDamage(100);
            Check(health.IsDead, "Lethal damage must mark player dead");
            health.Respawn();
            Check(health.Health == health.maxHealth, "Respawn must restore health");
            Debug.Log("MEATBALL VALIDATION PASSED: floor support, predictive ledge stop, self filtering, missing floor, player damage, invulnerability, death, respawn.");
        }
        finally
        {
            if (enemyObject != null) UnityEngine.Object.DestroyImmediate(enemyObject);
            if (floor != null) UnityEngine.Object.DestroyImmediate(floor);
            if (playerObject != null) UnityEngine.Object.DestroyImmediate(playerObject);
        }
    }
}
