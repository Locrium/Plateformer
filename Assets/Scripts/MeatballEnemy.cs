using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class MeatballEnemy : MonoBehaviour
{
    public Transform target;
    [Min(1)] public int maxHealth = 3;
    [Min(0.1f)] public float detectionRange = 12f;
    [Min(0.1f)] public float chargeSpeed = 9f;
    [Min(0.1f)] public float windupTime = 0.8f;
    [Min(0.1f)] public float chargeDuration = 1.2f;
    [Min(0.1f)] public float recoveryTime = 1.5f;
    public bool splitOnDefeat = true;
    [Range(2, 6)] public int miniCount = 3;
    public Transform visual;
    public UnityEvent onPlayerHit;
    public int Health { get; private set; }

    enum State { Waiting, Windup, Charging, Recovering }
    State state;
    Rigidbody body;
    SphereCollider hitbox;
    Vector3 direction;
    Vector3 visualScale;
    float timer;
    float searchTimer;
    bool dead;
    bool hitPlayer;
    readonly HashSet<int> hitPlates = new HashSet<int>();

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        hitbox = GetComponent<SphereCollider>();
        body.constraints |= RigidbodyConstraints.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        Health = maxHealth;
        if (visual != null) visualScale = visual.localScale;
    }

    void FixedUpdate()
    {
        if (dead) return;
        if (target == null && (searchTimer -= Time.fixedDeltaTime) <= 0f)
        {
            searchTimer = 1f;
            var player = FindFirstObjectByType<SimplePlayerController>();
            if (player != null) target = player.transform;
            else
            {
                var legacyPlayer = FindFirstObjectByType<Player>();
                if (legacyPlayer != null) target = legacyPlayer.transform;
                else
                {
                    // Some scenes use PlayerInput without either movement script.
                    var inputPlayer = FindFirstObjectByType<UnityEngine.InputSystem.PlayerInput>();
                    if (inputPlayer != null) target = inputPlayer.transform;
                    else
                    {
                        var taggedPlayer = GameObject.FindGameObjectWithTag("Player");
                        if (taggedPlayer != null) target = taggedPlayer.transform;
                    }
                }
            }
        }
        timer -= Time.fixedDeltaTime;
        if (state == State.Waiting)
        {
            StopHorizontal();
            if (target != null && Vector3.Distance(transform.position, target.position) <= detectionRange && Grounded())
            {
                direction = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up).normalized;
                if (direction.sqrMagnitude > 0.01f) { state = State.Windup; timer = windupTime; }
            }
        }
        else if (state == State.Windup)
        {
            StopHorizontal();
            if (visual != null) visual.localScale = Vector3.Scale(visualScale, new Vector3(1.15f, 0.75f, 1.15f));
            if (timer <= 0f) { state = State.Charging; timer = chargeDuration; hitPlayer = false; ResetVisual(); }
        }
        else if (state == State.Charging)
        {
            if (timer <= 0f || !Grounded()) { Recover(); return; }
            body.linearVelocity = direction * chargeSpeed + Vector3.up * body.linearVelocity.y;
            if (visual != null) visual.Rotate(Vector3.Cross(Vector3.up, direction), chargeSpeed * Time.fixedDeltaTime * 90f, Space.World);
        }
        else
        {
            StopHorizontal();
            if (timer <= 0f) state = State.Waiting;
        }
    }

    bool Grounded()
    {
        var bounds = hitbox.bounds;
        return Physics.Raycast(bounds.center, Vector3.down, bounds.extents.y + 0.2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }

    void StopHorizontal() { body.linearVelocity = Vector3.up * body.linearVelocity.y; }
    void ResetVisual() { if (visual != null) visual.localScale = visualScale; }
    void Recover() { state = State.Recovering; timer = recoveryTime; StopHorizontal(); ResetVisual(); }

    void OnCollisionEnter(Collision collision) { HandleContact(collision.collider); CheckWall(collision); }
    void OnCollisionStay(Collision collision) { HandleContact(collision.collider); CheckWall(collision); }
    void OnTriggerEnter(Collider other) { HandleContact(other); }

    void CheckWall(Collision collision)
    {
        if (state != State.Charging || collision.collider.GetComponentInParent<MeatballEnemy>() != null) return;
        foreach (var contact in collision.contacts)
            if (Vector3.Dot(contact.normal, direction) < -0.6f) { Recover(); break; }
    }

    void HandleContact(Collider other)
    {
        if (dead) return;
        var plate = other.GetComponentInParent<Plate>();
        var oldPlate = other.GetComponentInParent<Plate4>();
        Component projectile = plate != null ? (Component)plate : oldPlate;
        if (projectile != null)
        {
            // Remember each plate so resting or multi-collider plates cannot deal repeated damage.
            if (hitPlates.Add(projectile.GetInstanceID())) TakeDamage(1);
            return;
        }
        if (state == State.Charging && !hitPlayer && target != null &&
            (other.transform == target || other.transform.IsChildOf(target)))
        {
            hitPlayer = true;
            onPlayerHit.Invoke();
            Recover();
        }
    }

    public void TakeDamage(int amount)
    {
        if (dead || amount <= 0) return;
        Health = Mathf.Max(0, Health - amount);
        if (Health > 0) return;
        dead = true;
        hitbox.enabled = false;
        if (splitOnDefeat)
        {
            float radius;
            // Use scale because disabling the collider clears its bounds.
            radius = hitbox.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
            for (int i = 0; i < miniCount; i++)
            {
                float angle = i * Mathf.PI * 2f / miniCount;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius * 0.65f;
                var mini = Instantiate(this, transform.position + offset + Vector3.up * radius * 0.2f, Quaternion.identity);
                mini.name = "Mini Meatball";
                mini.transform.localScale = transform.lossyScale * 0.4f;
                mini.splitOnDefeat = false;
                mini.maxHealth = mini.Health = 1;
                mini.dead = false;
                mini.hitbox.enabled = true;
                mini.target = target;
                mini.chargeSpeed = chargeSpeed * 1.2f;
                mini.windupTime = windupTime * 0.65f;
                mini.Recover();
                mini.timer = recoveryTime + i * 0.2f;
                mini.hitPlates.UnionWith(hitPlates);
            }
        }
        Destroy(gameObject);
    }

    [ContextMenu("Defeat (test splitting)")]
    void TestDefeat() { if (Application.isPlaying) TakeDamage(Health); }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        if (target != null) Gizmos.DrawLine(transform.position, target.position);
    }
}

