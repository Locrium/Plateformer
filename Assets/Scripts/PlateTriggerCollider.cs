using UnityEngine;

public class PlateTriggerCollider : MonoBehaviour
{

    [SerializeField] Plate plate;

    [SerializeField] LayerMask layerMask;

    void Start()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        // TODO: Match layers with tag (ground isn't built yet so I'm not doing that yet)
        // right now it will interact with any other hitbox (including other plates)

        if (((layerMask & (1 << other.gameObject.layer)) != 0))
        {
            plate.landed = true;
            Debug.Log("stopping plate!");
        }
    }

    void Update()
    {
        
    }
}

