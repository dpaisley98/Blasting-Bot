using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSpawner : MonoBehaviour
{
    public List<GameObject> items;
    public GameObject enemy, player, teleporter;
    public int amountOfItems, maxAmountOfItems;
    public int amountOfEnemies, maxAmountOfEnemies, enemySpawnDelay;
    public float maxHeight;
    public LayerMask ground;
    private Coroutine spawnCoroutine;
    MapGenerator mapGen;

    void Start()
    {
        mapGen = GetComponent<MapGenerator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartSpawningEnemies()
    {
        spawnCoroutine = StartCoroutine(SpawnEnemies());
    }

    public void StopSpawningEnemies()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnEnemies()
    {
        while (amountOfEnemies < maxAmountOfEnemies)
        {
            Vector3 spawnPosition = transform.position + new Vector3(
                Random.Range(-300f, 300f), 
                maxHeight,
                Random.Range(-180, 180f) 
            );
        
            RaycastHit hit;
            if (Physics.Raycast(spawnPosition, Vector3.down, out hit, ground)) {
                Vector3 objectSpawnPosition = hit.point + Vector3.up * hit.point.y;
                Instantiate(enemy, objectSpawnPosition, Quaternion.identity);
                amountOfEnemies++;
            }

            yield return new WaitForSeconds(enemySpawnDelay);
        }
    }

    public void SpawnPlayer()
    {
       player.transform.position = transform.position + new Vector3(0, 100, 0);
    }

    public void SpawnTeleporter()
    {
        GameObject currentTeleporter = GameObject.FindGameObjectWithTag("Teleporter");
        if(currentTeleporter != null)
            GameObject.Destroy(currentTeleporter);

        Vector3 spawnPosition = transform.position + new Vector3(
            Random.Range(-300f, 300f), 
            maxHeight,
            Random.Range(-180, 180f) 
        );
        
        RaycastHit hit;
        if (Physics.Raycast(spawnPosition, Vector3.down, out hit, ground)) {
            Vector3 objectSpawnPosition = hit.point + Vector3.up * hit.point.y;
            Instantiate(teleporter, objectSpawnPosition, Quaternion.identity);
        }
    }
}
