using System.Collections;
using System.IO;
using FMOD.Studio;
using FMODUnity;
using NUnit.Framework.Internal.Commands;
using UnityEngine;

public class Plate : MonoBehaviour
{
    
    // READ ME:
    // ok so this script will work for both plate versions (as of oct 28), but not all methods will be used in either.
    // Plate version 1 has hitbox problems with its child hitboxes. Because the parent has a rigidbody, they seem to
    // have weird interactions with objects (delayed, or sometimes absent reactions). This is why I opted to change my strategy. 
    // Version 2 pushes the plate into the wall slightly, in the direction it was going once the collision hitbox hits the 
    // wall. This is more reliable and doesn't use children. I don't see it breaking, but then landing angle isn't quite right, because 
    // when it's pushed into the wall, it can only push in the direction it's facing/has been going, but can't continue the arc down. 
    // it's still lowkey good enough though. Fixable later (I have other ideas lol)


    [Header("References")]
    Camera camera;
    // main camera (player orientation)

    Rigidbody rb;

    MeshRenderer meshRenderer;


    [Header("Values")]
    float initialSpeed = 12f;
    // Amount of impulse force added to propel plate 
    // (how hard it is thrown)
    // Note: At very high speeds, the plate sinks too far into the wall. It will need some troubleshooting to make sure it doesnt sink too far.
    // like maybe a hitbox in front of the plate, or 
    
    Vector3 offset = new Vector3 (0.5f, -0.5f, 2);
    // offset the launch location so it isn't coming right out of the player's face

    float gravityScale = 0.2f;
    // Don't want it to drop super quickly, so I'm modifying the gravity scale

    float dragScale = 0.255f;
    // how much air drag there is (I think there should be 0)

    [Header("Collision and Stopping")]
    
    public bool landed = false;

    bool stopped = false;

    float pushAmt = 0.2f;
    // how far the plate gets pushed into the wall

    [SerializeField] LayerMask ignoredLayers;

    [SerializeField] LayerMask ignoredLayersOnlyInFlight;
    // this is for ignoring the player while the plate is being thrown. Once the plate is landed, 
    // layers assigned to this will then no longer be ignored.

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
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        meshRenderer.enabled = true;
    }

     void LaunchPlate()
    {

        rb.AddForce(transform.forward * initialSpeed, ForceMode.Impulse);
        //Debug.Log("Applying impulse? Impulse direciton given is" + Vector3.forward * initialSpeed);

        RuntimeManager.AttachInstanceToGameObject(launchAudio, gameObject);// best practice to do this right before playing the audio
        launchAudio.start();

    }

    private void FixedUpdate()
    {
        applyGravity();

        if (!landed) 
        {
        RotateWithVelocity();
        }
        
    }

     void RotateWithVelocity()
        // Plate will always face the direction its going!
  
    {
        transform.rotation = Quaternion.LookRotation(rb.linearVelocity);



    }

    public void StopPlate()
    // I guess this could also be done by changing the amount of friction, but that is scary. Easily changable though
    // Called by PlateTriggerCollider
    {

        //StartCoroutine(StopAfter1Tick());
        rb.isKinematic = true;
        RuntimeManager.AttachInstanceToGameObject(stickAudio, gameObject); // best practice to do this right before playing the audio
        stickAudio.start();

    }

    public void EmbedPlate()
    {
        RotateWithVelocity();
        rb.isKinematic = true;
        landed = true;
        transform.position += transform.forward * pushAmt;
         
    }

    private void OnCollisionEnter(Collision collision)
    {
        EmbedPlate();

        UnIgnoreInFlightLayers();
        // player will now be able to jump on the plate
    }


    [ContextMenu("Break Plate")]
    void BreakPlate()
    {
        GameObject.Destroy(gameObject);
        RuntimeManager.AttachInstanceToGameObject(breakAudio, gameObject);
        breakAudio.start();
    }

    IEnumerator StopAfter1Tick()
        // stops after 1 fixedupdate.
    {
        yield return new WaitForFixedUpdate();

        rb.isKinematic = true;

    }

    void applyGravity()
    // constantly applies gravity to the plate object. Gravity is disabled under the rigidbody settings
    // in inspector, so this is a modifiable substitute (no native way to change rigidbody gravity per object in 3d - only 2d)
    {

        rb.AddForce(Vector3.up * Physics.gravity.y * gravityScale, ForceMode.Acceleration);
        // physics.gravity is negative, so we use Vector3.up (0, 1, 0)

    }

}
