using UnityEngine;
using UnityEngine.InputSystem;

public class test_player : MonoBehaviour
{
    [Header("Plate Spawning Prefab Stuff")]

    // Please just copy this and the SpawnPlate, and maybe Fire methods into your player script. 
    // also please include any applicable class variables

    [SerializeField] GameObject platePrefab;
    GameObject plateInstance;

    Vector3 offset = new Vector3(0.5f, -0.5f, 2);
    // offset the plate's launch location so it isn't coming right out of the player's face





    void Start()
    {
        //platePrefab = Resources.Load<GameObject>(platePrefabPath);
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
