using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class MeatballEnemy : MonoBehaviour
{
    public Transform target;
    [Min(1)] public int maxHealth = 3;
    [Min(0.1f)] public float detectionRange = 12f;
    [UnityEngine.Serialization.FormerlySerializedAs("chargeSpeed")]
    [Min(0.1f)] public float moveSpeed = 3f;
    [Header("Contact hop back")]
    [Min(0.1f)] public float retreatDistance = 2f;
    [Min(0.2f)] public float hopDuration = 0.5f;
    [Min(0.1f)] public float recoveryTime = 1.5f;
    public bool splitOnDefeat = true;
    [Range(2, 6)] public int miniCount = 3;
    public Transform visual;
    public UnityEvent onPlayerHit = new UnityEvent();
    public UnityEvent onDamaged = new UnityEvent();
    public UnityEvent onDefeated = new UnityEvent();
    [Header("Ground and ledge safety")]
    public LayerMask groundLayers = Physics.DefaultRaycastLayers;
    [Range(0f, 75f)] public float maxSlope = 45f;
    [Min(0.02f)] public float groundTolerance = 0.25f;
    [Min(0.05f)] public float ledgeLookAhead = 0.4f;
    [Header("Combat")]
    [Min(1)] public int contactDamage = 1;
    [Min(0.1f)] public float minimumPlateSpeed = 1f;
    [Min(0f)] public float hitStunTime = 0.35f;
    [SerializeField] string movementStatus;
    public int Health { get; private set; }

    enum State { Following, HoppingBack, Recovering }
    State state;
    Rigidbody body;
    SphereCollider hitbox;
    Vector3 direction;
    Vector3 visualScale;
    float timer;
    float searchTimer;
    bool dead;
    bool hitPlayer;
    string recoveryReason;
    readonly List<Vector3> safeTrail = new List<Vector3>();
    Vector3 hopStart;
    Vector3 hopDestination;
    float hopElapsed;
    readonly HashSet<int> hitPlates = new HashSet<int>();
    readonly RaycastHit[] groundHits = new RaycastHit[32];

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        hitbox = GetComponent<SphereCollider>();
        body.constraints |= RigidbodyConstraints.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        Health = maxHealth;
        if (body.isKinematic)
            Debug.LogWarning("Meatball requires a non-kinematic Rigidbody to move.", this);
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
        if (state == State.HoppingBack) { UpdateHop(); return; }
        if (state == State.Recovering)
        {
            movementStatus = "Recovering: " + recoveryReason;
            StopHorizontal();
            if (timer <= 0f) { state = State.Following; hitPlayer = false; }
            return;
        }
        StopHorizontal();
        if (target == null) { movementStatus = "No target"; return; }
        if (!Grounded()) { movementStatus = "Not grounded"; return; }
        RememberSafePosition();
        Vector3 delta = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
        if (delta.magnitude > detectionRange) { movementStatus = "Target outside detection range"; return; }
        direction = delta.normalized;
        if (direction.sqrMagnitude < 0.01f) return;
        if (!SafeAhead()) { movementStatus = "Stopping before ledge"; return; }
        movementStatus = "Following player";
        if (!body.isKinematic)
            body.linearVelocity = direction * moveSpeed + Vector3.up * body.linearVelocity.y;
        if (visual != null && visual != transform)
            visual.Rotate(Vector3.Cross(Vector3.up, direction), moveSpeed * Time.fixedDeltaTime * 90f, Space.World);
    }

    void RememberSafePosition()
    {
        if (!Supported(hitbox.bounds.center)) return;
        if (safeTrail.Count == 0 || Vector3.Distance(safeTrail[safeTrail.Count - 1], body.position) >= 0.15f)
        {
            safeTrail.Add(body.position);
            if (safeTrail.Count > 100) safeTrail.RemoveAt(0);
        }
    }

    bool SafeRetreat(Vector3 destination)
    {
        Vector3 start = body.position;
        float distance = Vector3.Distance(start, destination);
        int steps = Mathf.Max(1, Mathf.CeilToInt(distance / 0.15f));
        Vector3 side = Vector3.Cross(Vector3.up, (destination - start).normalized) * hitbox.bounds.extents.x * 0.5f;
        Vector3 centerOffset = hitbox.bounds.center - body.position;
        for (int i = 0; i <= steps; i++)
        {
            Vector3 sample = Vector3.Lerp(start, destination, (float)i / steps) + centerOffset;
            if (!Supported(sample) || !Supported(sample + side) || !Supported(sample - side)) return false;
        }
        return true;
    }

    void BeginHopBack()
    {
        // Walk backwards through actual visited positions. Never guess an away-from-player direction.
        Vector3 destination = body.position;
        int destinationIndex = -1;
        for (int i = safeTrail.Count - 1; i >= 0; i--)
        {
            float distance = Vector3.ProjectOnPlane(safeTrail[i] - body.position, Vector3.up).magnitude;
            if (distance > retreatDistance) break;
            if (distance < 0.25f) continue;
            if (!SafeRetreat(safeTrail[i])) break;
            destination = safeTrail[i];
            destinationIndex = i;
        }
        if (destinationIndex < 0 || body.isKinematic)
        {
            Recover("No safe space to hop back");
            return;
        }
        hopStart = body.position;
        hopDestination = destination;
        hopElapsed = 0f;
        safeTrail.RemoveRange(destinationIndex + 1, safeTrail.Count - destinationIndex - 1);
        state = State.HoppingBack;
        float verticalSpeed = (destination.y - hopStart.y) / hopDuration - 0.5f * Physics.gravity.y * hopDuration;
        body.linearVelocity = Vector3.ProjectOnPlane(destination - hopStart, Vector3.up) / hopDuration + Vector3.up * verticalSpeed;
    }

    void UpdateHop()
    {
        hopElapsed += Time.fixedDeltaTime;
        movementStatus = "Hopping back along safe path";
        // Probe the projected ground path, not the airborne body's height.
        Vector3 groundPosition = Vector3.Lerp(hopStart, hopDestination, Mathf.Clamp01(hopElapsed / hopDuration));
        Vector3 groundProbe = new Vector3(body.position.x, groundPosition.y, body.position.z) + transform.TransformVector(hitbox.center);
        if (!Supported(groundProbe)) { Recover("Retreat ground disappeared"); return; }
        Vector3 remaining = Vector3.ProjectOnPlane(hopDestination - body.position, Vector3.up);
        float remainingTime = Mathf.Max(Time.fixedDeltaTime, hopDuration - hopElapsed);
        Vector3 horizontal = Vector3.ClampMagnitude(remaining / remainingTime, retreatDistance / hopDuration);
        body.linearVelocity = horizontal + Vector3.up * body.linearVelocity.y;
        if ((hopElapsed >= hopDuration && Grounded() && body.linearVelocity.y <= 0.1f) || hopElapsed > hopDuration + 1f)
            Recover("Hop finished");
    }

    bool Grounded()
    {
        var bounds = hitbox.bounds;
        // A small sphere supports uneven surfaces better than a single ray.
        float probeRadius = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.5f;
        int count = Physics.SphereCastNonAlloc(bounds.center, probeRadius, Vector3.down, groundHits,
            Mathf.Max(0f, bounds.extents.y - probeRadius) + groundTolerance, groundLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
            if (ValidGround(groundHits[i])) return true;
        return false;
    }

    bool ValidGround(RaycastHit hit)
    {
        return hit.collider != null && hit.collider.attachedRigidbody != body &&
            hit.collider.GetComponentInParent<MeatballEnemy>() == null &&
            hit.collider.GetComponentInParent<UnityEngine.InputSystem.PlayerInput>() == null &&
            (target == null || (hit.transform != target && !hit.transform.IsChildOf(target))) &&
            Vector3.Angle(hit.normal, Vector3.up) <= maxSlope;
    }

    bool SafeAhead()
    {
        var bounds = hitbox.bounds;
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
        float look = radius + ledgeLookAhead + moveSpeed * Time.fixedDeltaTime * 2f;
        Vector3 ahead = bounds.center + direction * look;
        Vector3 side = Vector3.Cross(Vector3.up, direction) * radius * 0.65f;
        return Supported(ahead) && Supported(ahead + side) && Supported(ahead - side);
    }

    bool Supported(Vector3 origin)
    {
        int count = Physics.RaycastNonAlloc(origin, Vector3.down, groundHits,
            hitbox.radius * Mathf.Max(transform.lossyScale.x, Mathf.Max(transform.lossyScale.y, transform.lossyScale.z)) + groundTolerance,
            groundLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
            if (ValidGround(groundHits[i])) return true;
        return false;
    }

    void StopHorizontal() { if (!body.isKinematic) body.linearVelocity = Vector3.up * body.linearVelocity.y; }
    void ResetVisual() { if (visual != null) visual.localScale = visualScale; }
    void Recover(string reason = "Hit stun") { recoveryReason = reason; state = State.Recovering; timer = recoveryTime; StopHorizontal(); ResetVisual(); }

    void OnCollisionEnter(Collision collision) { HandleContact(collision.collider); CheckWall(collision); }
    void OnCollisionStay(Collision collision) { HandleContact(collision.collider); CheckWall(collision); }
    void OnTriggerEnter(Collider other) { HandleContact(other); }

    void CheckWall(Collision collision)
    {
        if (state != State.Following && state != State.HoppingBack || collision.collider.GetComponentInParent<MeatballEnemy>() != null) return;
        foreach (var contact in collision.contacts)
            if (Vector3.Dot(contact.normal, direction) < -0.6f) { Recover("Wall: " + collision.collider.name); break; }
    }

    void HandleContact(Collider other)
    {
        if (dead) return;
        var plate = other.GetComponentInParent<Plate>();
        var oldPlate = other.GetComponentInParent<Plate4>();
        Component projectile = plate != null ? (Component)plate : oldPlate;
        if (projectile != null)
        {
            var projectileBody = projectile.GetComponent<Rigidbody>();
            bool landed = plate != null ? plate.landed : oldPlate.landed;
            if (landed || projectileBody == null || projectileBody.isKinematic ||
                projectileBody.linearVelocity.sqrMagnitude < minimumPlateSpeed * minimumPlateSpeed) return;
            // Remember each plate so resting or multi-collider plates cannot deal repeated damage.
            if (hitPlates.Add(projectile.GetInstanceID())) TakeDamage(1);
            return;
        }
        TryHitPlayer(other);
    }

    // CharacterController contacts may be reported on the player rather than the Rigidbody.
    public void TryHitPlayer(Collider other)
    {
        if (!dead && state == State.Following && !hitPlayer && target != null &&
            (other.transform == target || other.transform.IsChildOf(target)))
        {
            hitPlayer = true;
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health != null) health.TakeDamage(contactDamage);
            onPlayerHit.Invoke();
            BeginHopBack();
        }
    }

    void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        retreatDistance = Mathf.Max(0.1f, retreatDistance);
        hopDuration = Mathf.Max(0.2f, hopDuration);
        recoveryTime = Mathf.Max(0.1f, recoveryTime);
        miniCount = Mathf.Clamp(miniCount, 2, 6);
    }

    public void TakeDamage(int amount)
    {
        if (dead || amount <= 0) return;
        Health = Mathf.Max(0, Health - amount);
        dead = Health == 0;
        onDamaged.Invoke();
        if (Health > 0) { Recover(); timer = hitStunTime; return; }
        ResetVisual();
        onDefeated.Invoke();
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
                // Keep fragments over the existing platform rather than scattering over an edge.
                if (!Supported(transform.position + offset)) offset = Vector3.zero;
                var mini = Instantiate(this, transform.position + offset + Vector3.up * radius * 0.2f, Quaternion.identity);
                mini.name = "Mini Meatball";
                mini.transform.localScale = transform.lossyScale * 0.4f;
                mini.splitOnDefeat = false;
                mini.maxHealth = mini.Health = 1;
                mini.dead = false;
                mini.hitbox.enabled = true;
                mini.target = target;
                mini.hitPlayer = false;
                mini.moveSpeed = moveSpeed * 1.2f;
                mini.safeTrail.Clear();
                mini.Recover();
                mini.timer = recoveryTime + i * 0.2f;
                mini.hitPlates.UnionWith(hitPlates);
                // Fragments can briefly overlap when spawned on a narrow platform.
                foreach (var sibling in FindObjectsByType<MeatballEnemy>(FindObjectsSortMode.None))
                    if (sibling != mini && sibling.hitbox != null)
                        Physics.IgnoreCollision(mini.hitbox, sibling.hitbox);
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
        if (Application.isPlaying && hitbox != null)
        {
            Gizmos.color = SafeAhead() ? Color.green : Color.red;
            float radius = Mathf.Max(hitbox.bounds.extents.x, hitbox.bounds.extents.z);
            Vector3 ahead = hitbox.bounds.center + direction * (radius + ledgeLookAhead + moveSpeed * Time.fixedDeltaTime * 2f);
            Gizmos.DrawLine(hitbox.bounds.center, ahead);
            Gizmos.DrawLine(ahead, ahead + Vector3.down * (hitbox.bounds.extents.y + groundTolerance));
        }
    }
}

