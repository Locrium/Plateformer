using UnityEngine;
using UnityEngine.SceneManagement;

public class WinEnd : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Donut"))
        {
            EndGame();
        }
    }

    private void EndGame()
    {
        Debug.Log("Yummy donuts");
    }
}
