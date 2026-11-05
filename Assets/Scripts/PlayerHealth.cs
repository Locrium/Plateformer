using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class PlayerHealth : MonoBehaviour
{
    [Min(1)] public int maxHealth = 5;
    [Min(0f)] public float invulnerabilityTime = 1f;
    public bool respawnOnDeath = true;
    public Transform respawnPoint;
    public UnityEvent onDamaged = new UnityEvent();
    public UnityEvent onDied = new UnityEvent();
    public UnityEvent onRespawned = new UnityEvent();
    [SerializeField] int currentHealth;
    public int Health => currentHealth;
    public bool IsDead => currentHealth <= 0;
    float nextDamageTime;
    Vector3 initialPosition;
    Quaternion initialRotation;
    bool movementWasEnabled;

    void Awake()
    {
        currentHealth = maxHealth;
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        var movement = GetComponent<SimplePlayerController>();
        movementWasEnabled = movement != null && movement.enabled;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead || Time.time < nextDamageTime) return;
        nextDamageTime = Time.time + invulnerabilityTime;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        onDamaged.Invoke();
        if (!IsDead) return;
        var movement = GetComponent<SimplePlayerController>();
        if (movement != null) movement.enabled = false;
        onDied.Invoke();
        if (respawnOnDeath) Respawn();
    }

    public void Heal(int amount)
    {
        if (amount > 0 && !IsDead) currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    public void Respawn()
    {
        var controller = GetComponent<CharacterController>();
        bool wasEnabled = controller != null && controller.enabled;
        if (wasEnabled) controller.enabled = false;
        transform.SetPositionAndRotation(respawnPoint != null ? respawnPoint.position : initialPosition,
            respawnPoint != null ? respawnPoint.rotation : initialRotation);
        var body = GetComponent<Rigidbody>();
        if (body != null && !body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        var movement = GetComponent<SimplePlayerController>();
        if (movement != null)
        {
            movement.ResetVelocity();
            movement.enabled = movementWasEnabled;
        }
        if (wasEnabled) controller.enabled = true;
        currentHealth = maxHealth;
        nextDamageTime = Time.time + invulnerabilityTime;
        onRespawned.Invoke();
    }
}
