using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class SimplePlayerController : MonoBehaviour
{
    public float playerSpeed = 5.0f;
    public float jumpHeight = 3f;
    public float gravityValue = -30f;

    public CharacterController controller;
    private Vector3 playerVelocity;
    private bool groundedPlayer;

    [Header("Plate")]
    [SerializeField] GameObject platePrefab;
    GameObject plateInstance;


    [Header("Camera")]
    Transform cam;
    // This was public, but need to get it to make player prefab - Jl

    [Header("Input Actions")]
    public InputActionReference moveAction;
    public InputActionReference jumpAction;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        cam = FindAnyObjectByType<Camera>().transform;
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Confined;
        moveAction.action.Enable();
        jumpAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        jumpAction.action.Disable();
    }

    public void Fire(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            SpawnPlate();
        }

    }
    void SpawnPlate()
    {
        plateInstance = Instantiate(platePrefab);

    }

    void Update()
    {
        groundedPlayer = controller.isGrounded;

        if (groundedPlayer)
        {
            // Slight downward velocity to keep grounded stable
            if (playerVelocity.y < -2f)
                playerVelocity.y = -2f;
        }

        // Read input
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        Vector3 move = cam.rotation * new Vector3(input.x, 0, input.y);
        move.y = 0;
        move.Normalize();

        if (move != Vector3.zero)
            transform.forward = move;

        // Jump using WasPressedThisFrame()
        if (groundedPlayer && jumpAction.action.WasPressedThisFrame())
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravityValue);
        }

        // Apply gravity
        playerVelocity.y += gravityValue * Time.deltaTime;

        // Move
        Vector3 finalMove = move * playerSpeed + Vector3.up * playerVelocity.y;
        controller.Move(finalMove * Time.deltaTime);
    }
}
