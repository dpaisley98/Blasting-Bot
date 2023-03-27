using UnityEngine;
using UnityEngine.AI;

public class EnemyBehaviour : MonoBehaviour
{
    private NavMeshAgent navMeshAgent;
    GameObject player;
    public float speed = 5.0f;
    public float maxDistance = 80f;
    public Transform body;


    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        navMeshAgent = GetComponentInChildren<NavMeshAgent>();
    }

    void Update()
    {
        if (player.transform != null) {
            float playerHeight = player.gameObject.GetComponent<PlayerMovement>().playerHeight;
            float distance = Vector3.Distance(player.transform.position, body.transform.position);
            float scale = Mathf.Clamp01(distance / maxDistance);
            float offsetChange = scale * speed * Time.deltaTime;
            if (player.transform.position.y + playerHeight * 0.5f + 0.2f > body.transform.position.y) {
                navMeshAgent.baseOffset += offsetChange;
            } else  if (player.transform.position.y + playerHeight * 0.5f + 0.2f < body.transform.position.y){
                navMeshAgent.baseOffset -= offsetChange;
            }

            navMeshAgent.SetDestination(player.transform.position);
            Vector3 direction = (player.transform.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }
}
