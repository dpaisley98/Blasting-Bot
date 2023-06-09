using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public Transform player;
    public GameObject map;
    public LayerMask teleporter;
    public float interactRange;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Ray ray = new Ray(player.position, player.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactRange, teleporter))
            {
                GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

                // Loop through the results and destroy the objects
                foreach (GameObject enemy in enemies) {
                    GameObject.Destroy(enemy);
                }

                map.GetComponent<MapGenerator>().GenerateMap();
            }
        }
    }
}
