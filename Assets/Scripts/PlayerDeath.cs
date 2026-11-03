using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    private Vector3 respawnPoint;

    public float deathThreshold = -20f; // y position when the player is marked dead

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        respawnPoint = transform.position;  // initial spawn point -- for respawn later
    }

    // Update is called once per frame
    // checks to see if player has fallen in the void by checking y position
    void Update()
    {
        if (transform.position.y < deathThreshold)
        {
            Debug.Log("Died");
            Respawn();
        }
        
    }

    // put player back in spawn point upon death
    void Respawn()
    {
        // turn off physics
        if (TryGetComponent<CharacterController>(out CharacterController cc))
        {
            cc.enabled = false;
        }

        // respawn
        transform.position = respawnPoint;

        // turn on physics
        if (cc != null)
        {
            cc.enabled = true;
        }
    }
}
