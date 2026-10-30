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
        if (((layerMask & (1 << other.gameObject.layer)) != 0))
        {
            if (!plate.landed)
            {
                plate.landed = true;
                plate.StopPlate();
                Debug.Log("plate shouild stop?");
            }
        }
    }

    void Update()
    {
        
    }
}

