using UnityEngine;

public class test_player : MonoBehaviour
{

    [SerializeField] GameObject platePrefab;
    //string platePrefabPath = "Prefabs/Plate Version 1";
    GameObject plateInstance;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
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
    }

}
