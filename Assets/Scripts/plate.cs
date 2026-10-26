using System.IO;
using NUnit.Framework.Internal.Commands;
using UnityEngine;

public class plate : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created


    [Header("References")]
    Camera camera;
    // main camera (player orientation)

    Rigidbody rb;

    [Header("Values")]
    float initialSpeed = 2f;
    // Amount of impulse force added to propel plate 
    // (how hard it is thrown)
    
    float offset = 1f;
    // not using this yet, but might need to offset the launch location so it isn't
    // coming right out of the player's face

    float gravityScale = 0.1f;
    // Don't want it to drop super quickly, so I'm modifying the gravity scale



    float dragScale = 0f;
    // how much air drag there is (I think there should be 0)


    [Header("Debug")]
    float gizmoRayLength = 3f;


    void Start()
    {
        camera = FindAnyObjectByType<Camera>();
        // must be only one camera

        rb = GetComponent<Rigidbody>();


        // setting physics scales
        rb.linearDamping = dragScale;

        transform.rotation = camera.transform.rotation;
        transform.position = camera.transform.position + camera.transform.forward * offset;
        // inherit camera's rotation and position
        // so the object should be pointing forwards... right?

        Debug.Log("Camera rotation is: " + camera.transform.rotation + " and Plate rotation is: " + transform.rotation);
        Debug.Log("Camera position is: " + camera.transform.position + " and Plate position is: " + transform.position);

        //pushes plate forward a bit so that there's no collision issues

        LaunchPlate();
    }

    [ContextMenu("Launch?")]
    public void LaunchPlate()
    {

        rb.AddForce(transform.forward * initialSpeed, ForceMode.Impulse);
        //Debug.Log("Applying impulse? Impulse direciton given is" + Vector3.forward * initialSpeed);

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        applyGravity();

    }

    void applyGravity()
    // constantly applies gravity to the plate object. Gravity is disabled under the rigidbody settings
    // in inspector, so this is a modifyable substitute (no native way to change rigidbody gravity per instance in 3d - only 2d)
    {

        rb.AddForce(Vector3.up * Physics.gravity.y * gravityScale, ForceMode.Acceleration);
        // physics.gravity is negative, so we use Vector3.up (0, 1, 0)

    }


    private void OnDrawGizmosSelected()
    {
        //Gizmos.color = Color.yellow;
        //Gizmos.DrawRay(gizmoRayLength);
    }
}
