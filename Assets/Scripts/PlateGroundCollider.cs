using UnityEngine;

public class PlateGroundCollider : MonoBehaviour
{

    [SerializeField] Plate plate;

    [SerializeField] LayerMask interactableLayers;

    void Start()
    {

    }
    private void OnCollisionEnter(Collision other)
    {
        if (((interactableLayers & (1 << other.gameObject.layer)) != 0))
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



