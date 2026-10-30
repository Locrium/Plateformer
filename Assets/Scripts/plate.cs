using System.Collections;
using System.IO;
using FMOD.Studio;
using FMODUnity;
using NUnit.Framework.Internal.Commands;
using UnityEngine;

public class Plate : MonoBehaviour
{
    
    // READ ME:
    // plate will only work for plate version 4. Fuck it all. FUck!

    // BUGS:
    // There's a bug where the plate's angle changes a bit when landing. Don't know where it's coming from
    // if any big brains wanna try and figure it out....?


    [Header("References")]

    Camera camera;
    // main camera (player orientation)

    Rigidbody rb;

    MeshRenderer meshRenderer;

    [SerializeField] GameObject plateCollider;
    // the hitbox. We will need to resize it, but it will work just like a component. Can even
    // trigger OnCollisionEnter way up here in the parent!

    [Header("Values")]
    float initialSpeed = 12f;
    // Amount of impulse force added to propel plate 
    // (how hared it is thrown)
    
    Vector3 offset = new Vector3 (0.5f, -0.5f, 2);
    // offset the launch location so it isn't coming right out of the player's face

    float gravityScale = 0.2f;
    // Don't want it to drop super quickly, so I'm modifying the gravity scale

    float dragScale = 0.255f;
    // how much air drag there is (probably best for it to be a low number)

    [Header("Collision and Stopping")]
    
    public bool landed = false;

    float pushAmt = 0.2f;
    // how far the plate gets pushed into the wall

    float sinkAmt = 0.2f;
    // this will determine how much to shrink the hitbox, and therefore how far
    // the plate sinks into the wall. 
    // NOTE: Must be between 0 and 1. 0 is sinking halway through, and 1 is not sinking at all.

    [SerializeField] LayerMask ignoredLayers;
    // completely ignored layers (BLACKLIST)

    [SerializeField] LayerMask ignoredLayersOnlyInFlight;
    // this is for ignoring the player while the plate is being thrown. Once the plate is landed, 
    // layers assigned to this will then no longer be ignored.
    // (BLACKLIST)

    [SerializeField] LayerMask layersPlateCanLandOn;
    // used only for landing. Player interaction should not use this (when implemented)
    // (WHITELIST)

    [Header("Audio")]

    string plateLaunchPath = "event:/plate_launch_1";
    EventInstance launchAudio;

    string plateStickPath = "event:/plate_stick_1";
    EventInstance stickAudio;

    string plateBreakPath = "event:/plate_break_1";
    EventInstance breakAudio;


    [Header("Debug")]

    float gizmoRayLength = 3f;


    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.enabled = false;

        StartCoroutine(LightsOn());

        camera = FindAnyObjectByType<Camera>();
        // must be only one camera

        rb = GetComponent<Rigidbody>();

        rb.linearDamping = dragScale;

        transform.rotation = camera.transform.rotation;

        transform.position = camera.transform.position + 
            (camera.transform.right * offset.x) + (camera.transform.up * offset.y) + (camera.transform.forward * offset.z);
        // I know this is a mess, but just adding a vector3 makes it so the vector 3 is in the world space, and not rotated to the player. Multiplying each 
        // player's cardinal direction by each offset value means that the rotation is taken into account. I'm sure there's a smoother way to do this... -J        

        BuildHitboxIgnoreList();

        LoadAudio();

        LaunchPlate();

    }

    void BuildHitboxIgnoreList ()
    {
        for (int i = 0; i < 32; ++i)

        {
            // going through each bit of ANY layermask using 10 digit numerals (count up to 31)

            if ((ignoredLayers & (1 << i)) != 0) // bit shift I over cuz weird fencepost thing I can't understand yet
            // if i is within layers
            {
                Physics.IgnoreLayerCollision(gameObject.layer, i);
                // Physics.IgnoreLayerCollision does NOT require bit shifting because of course, it makes so much sense!! :D
            }


            if ((ignoredLayersOnlyInFlight & (1 << i)) != 0)
            // ok now for these
            {
                Physics.IgnoreLayerCollision(gameObject.layer, i);
            }

        }
    }

    void UnIgnoreInFlightLayers()
    {
        for (int i = 0; i < 32; ++i)

        {
            if ((ignoredLayersOnlyInFlight & (1 << i)) != 0)
            {
                Physics.IgnoreLayerCollision(gameObject.layer, i, false);
            }
        }

    }
    void LoadAudio()
    {
        launchAudio = RuntimeManager.CreateInstance(plateLaunchPath);

        stickAudio = RuntimeManager.CreateInstance(plateStickPath);

        breakAudio = RuntimeManager.CreateInstance(plateBreakPath);

    }

    IEnumerator LightsOn()
        // enables renderer 2 fixed updates after instantiation (physics was taking a little while ok?)
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        meshRenderer.enabled = true;
    }

    void ScaleForFlight()
        // resizes the plate HITBOX to the size it should be in flight.
        // hitbox will be smaller than model, so the plate can sink into the wall a bit.
    {
        if (sinkAmt < 0 || sinkAmt > 1)
        {
            Debug.LogWarning("yooo sinkAmt is the wrong size please please ");
        }

        Vector3 flightScale = new Vector3(1-sinkAmt, 1, 1-sinkAmt);

        plateCollider.transform.localScale = flightScale;

    }

    void ScaleForGround()
    // resets the HITBOX scale to 1, 1, 1
    {
        plateCollider.transform.localScale = new Vector3(1, 1, 1);
    }
   
    void LaunchPlate()
    {
        ScaleForFlight();
        // resizes so it can sink into wall

        rb.AddForce(transform.forward * initialSpeed, ForceMode.Impulse);
        //Debug.Log("Applying impulse? Impulse direciton given is" + Vector3.forward * initialSpeed);

        RuntimeManager.AttachInstanceToGameObject(launchAudio, gameObject);// best practice to do this right before playing the audio
        launchAudio.start();

    }
    public void StopPlate()
    // own method because idk what if something else needs to call it someday? :pleading:
    {
        ScaleForGround();
        // resizes so it can be interacted with normally;
        UnIgnoreInFlightLayers();
        landed = true;
        rb.isKinematic = true;
        RuntimeManager.AttachInstanceToGameObject(stickAudio, gameObject); // best practice to do this right before playing the audio
        stickAudio.start();

    }

    [ContextMenu("Break Plate")]
    void BreakPlate()
    {
        GameObject.Destroy(gameObject);
        RuntimeManager.AttachInstanceToGameObject(breakAudio, gameObject);
        breakAudio.start();
    }

    void applyGravity()
    // constantly applies gravity to the plate object. Gravity is disabled under the rigidbody settings
    // in inspector, so this is a modifiable substitute (no native way to change rigidbody gravity per object in 3d - only 2d)
    {

        rb.AddForce(Vector3.up * Physics.gravity.y * gravityScale, ForceMode.Acceleration);
        // physics.gravity is negative, so we use Vector3.up (0, 1, 0)

    }
    void RotateWithVelocity()
    // Plate will always face the direction its going!

    {
        transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
    }
    private void OnCollisionEnter(Collision other)
    {
        //Debug.Log("Collision Enter!");

        if (((layersPlateCanLandOn & (1 << other.gameObject.layer)) != 0))
        {
            if (!landed)

            {
                StopPlate();
                Debug.Log("plate shouild stop?");
            }
        }
    }

    private void FixedUpdate()
    {
        applyGravity();

        if (!landed)
        {
            RotateWithVelocity();
        }

    }


}
