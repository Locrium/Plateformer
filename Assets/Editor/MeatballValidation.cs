using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class MeatballValidation
{
    static object Call(object obj, string method, params object[] args) => obj.GetType().GetMethod(method,
        BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, args);
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
            Check((bool)Call(enemy, "SafeAhead"), "Following must be allowed on a broad floor");
            enemyObject.transform.position = origin + new Vector3(4.1f, 0.55f, 0);
            Physics.SyncTransforms();
            Check((bool)Call(enemy, "Grounded"), "Enemy must still be grounded before the edge");
            Check(!(bool)Call(enemy, "SafeAhead"), "Following must stop BEFORE reaching the edge");
            Check(!(bool)Call(enemy, "SafeRetreat", origin + new Vector3(6f, 0.55f, 0)),
                "Retreat past the platform edge must be rejected");
            enemyObject.transform.position = origin + Vector3.up * 0.55f;
            Physics.SyncTransforms();
            Check((bool)Call(enemy, "SafeRetreat", origin + new Vector3(-1.5f, 0.55f, 0)),
                "Retreat over continuous floor must be allowed");
            Call(enemy, "BeginHopBack");
            Check(enemyObject.GetComponent<Rigidbody>().linearVelocity.x == 0f,
                "No recorded path must not invent a backward jump");
            var trail = (System.Collections.Generic.List<Vector3>)typeof(MeatballEnemy)
                .GetField("safeTrail", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(enemy);
            trail.Add(origin + new Vector3(-1.5f, 0.55f, 0));
            trail.Add(origin + new Vector3(-0.5f, 0.55f, 0));
            trail.Add(enemyObject.transform.position);
            Call(enemy, "BeginHopBack");
            var hopVelocity = enemyObject.GetComponent<Rigidbody>().linearVelocity;
            Check(hopVelocity.x < 0 && hopVelocity.y > 0,
                "Contact retreat must jump upwards towards the previously visited position");
            floor.GetComponent<Collider>().enabled = false;
            Physics.SyncTransforms();
            Check(!(bool)Call(enemy, "Grounded"), "Own collider must not count as ground");
            Check(!(bool)Call(enemy, "SafeAhead"), "No floor must reject movement");
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
            Debug.Log("MEATBALL VALIDATION PASSED: floor support, predictive ledge stop, self filtering, missing floor, safe retreat selection, missing retreat history, hop direction, player damage, invulnerability, death, respawn.");
        }
        finally
        {
            if (enemyObject != null) UnityEngine.Object.DestroyImmediate(enemyObject);
            if (floor != null) UnityEngine.Object.DestroyImmediate(floor);
            if (playerObject != null) UnityEngine.Object.DestroyImmediate(playerObject);
        }
    }
}
