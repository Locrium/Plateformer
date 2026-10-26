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
    float initialSpeed = 6f;
    // Amount of impulse force added to propel plate 
    // (how hard it is thrown)
    
    Vector3 offset = new Vector3 (0.5f, -0.5f, 2);
    // offset the launch location so it isn't coming right out of the player's face

    float gravityScale = 0.1f;
    // Don't want it to drop super quickly, so I'm modifying the gravity scale

    float dragScale = 0f;
    // how much air drag there is (I think there should be 0)

    [Header("Collision and Stopping")]
    
    bool landed = false;

    float landingLerpValue = 0.75f; // between 0 and 1. Defines how quickly the velocity comes to a stop.
    // Once the plate's hitbox touches wall/ground it will slowly come to a stop through lerping. That way it will be buried a certain amount into
    // the wall. 

    float frozenMovementThreshold = 0.6f;
    // used to stop the plate when it is moving slow enough that it's not really moving anymore. (gravity + lerp stuff will keep it very gently oozing down otherwise)

    float pitchingLerpValue= 0.9f;

    Quaternion initialRotation;

    bool stopped = false;


    [Header("Debug")]
    float gizmoRayLength = 3f;


    void Start()
    {
        camera = FindAnyObjectByType<Camera>();
        // must be only one camera

        rb = GetComponent<Rigidbody>();

        rb.linearDamping = dragScale;


        transform.rotation = camera.transform.rotation;

        transform.position = camera.transform.position + 
            (camera.transform.right * offset.x) + (camera.transform.up * offset.y) + (camera.transform.forward * offset.z);
        // I know this is a mess, but just adding a vector3 makes it so the vector 3 is in the world space, and not rotated to the player. Multiplying each 
        // player's cardinal direction by each offset value means that the rotation is taken into account. I'm sure there's a smoother way to do this... -J

        initialRotation = transform.rotation;

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


    private void OnTriggerEnter(Collider other)
    {
        // TODO: Match layers with tag (ground isn't built yet so I'm not doing that yet)
        // right now it will interact with any other hitbox (including other plates)

        landed = true;

    }

    private void FixedUpdate()
    {
        applyGravity();

        if (landed)
        {
            slowToStop();
        }

        if (!landed) 
        {

        RotateWithVelocity();

        }
    }

    void RotateWithVelocity()
        // I want plate to rise and fall like a frisbee. Right now it's angle is always the same. not so cool\
        // I don't want it to look like it's pointing too low, so I will clamp the angle on the way down.
        // a better way would be to lerp it towards 0 maybe?
    {
        transform.rotation = Quaternion.LookRotation(rb.linearVelocity);

        // I'm leaving my horrid attempt at what Jonathan found out could just be one line down here 

        //float velocityVector = Mathf.Atan(rb.linearVelocity.y / rb.linearVelocity.z);

        //Debug.Log("velocity Vector = " + velocityVector);

        //float newRotation = Mathf.SmoothStep(-initialSpeed, initialSpeed, velocityVector);
        //// value between 0 and 1

        //Debug.Log("newRotation pre adjustments: " +  newRotation);

        //newRotation = (newRotation * 2) -1;
        //// make it a value between 1 and -1

        //newRotation *= 90;
        //// now it will be straight up/straight down based off of vertical and horizontal velocity mix

        //Debug.Log("x angle should be: " + newRotation);

        //transform.rotation = Quaternion.Euler(newRotation, 0, 0);




    }

    void slowToStop()
    // I guess this could also be done by changing the amount of friction, but that is scary. Easily changable though
    {
        if (!stopped)
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, landingLerpValue);
        }
        if (rb.linearVelocity.x < frozenMovementThreshold && rb.linearVelocity.y < frozenMovementThreshold && rb.linearVelocity.z < frozenMovementThreshold)
            // must be && because || will be triggered by Y velocity during throw
        {
            rb.isKinematic = true;
            Debug.Log("freezing movement");
            stopped = true;
        }

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
