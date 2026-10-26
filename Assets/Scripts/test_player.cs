using System.Collections;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.InputSystem;

public class test_player : MonoBehaviour
{
    [Header("Plate Spawning Prefab Stuff")]

    // Please just copy this and the SpawnPlate, and maybe Fire methods into your player script. 
    // also please include any applicable class variables

    [SerializeField] GameObject platePrefab;
    GameObject plateInstance;

    string plateLaunchPath = "event:/plate_launch_1";

    EventInstance launchAudio;


    void Start()
    {
        //StartCoroutine(LoadAudio());
    }

    IEnumerator LoadAudio()
    {
        yield return new WaitUntil(() => RuntimeManager.HaveAllBanksLoaded);
        launchAudio = RuntimeManager.CreateInstance(plateLaunchPath);


    }

    // Update is called once per frame
    void Update()
    {
        
    }
    [ContextMenu("FIRE!!!")]
    void SpawnPlate()
    {
        plateInstance = Instantiate(platePrefab);
        //plateInstance.transform.rotation = transform.rotation;
        //Plate plateInstanceScript = plateInstance.GetComponent<Plate>();
        //plateInstanceScript.LaunchPlate();
        //plateInstanceScript.RotateWithVelocity();



    }

    public void Fire(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            SpawnPlate();
        }
        
    }

}
