using UnityEngine;

public class PlateCollisionCollider : MonoBehaviour
{

    [SerializeField] Plate plate;

    [SerializeField] LayerMask ignoredLayers;

    void Start()
    {

        //Physics.IgnoreLayerCollision(gameObject.layer, 8);

        for (int i = 0; i < 32; ++i)
            
        {
            // going through each bit of ANY layermask using 10 digit numerals (count up to 31)

            if ((ignoredLayers & (1 << i)) != 0) // bit shift I over cuz weird fencepost thing I can't understand yet
            // if i is within layers
            {
                Physics.IgnoreLayerCollision(gameObject.layer, i);
                // Physics.IgnoreLayerCollision does NOT require bit shifting because of course, it makes so much sense!! :D
            }
        }
    }

    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        plate.EmbedPlate();
        Debug.Log("plate shouild stop?");
    }
}
