using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class GhostBehavior : MonoBehaviour, IInteractable
{
    [SerializeField] private float minFleeDistance = 4f;
    [SerializeField] private float maxFleeDistance = 8f;
    [SerializeField] private float sampleRadius = 2f;
    [SerializeField] private Vector2 destinationInterval = new Vector2(1.5f, 3f);

    private NavMeshAgent agent;
    [SerializeField] private Transform player;
    [SerializeField] private bool activated;
    private float nextDestinationTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.isStopped = true;
    }

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            Debug.LogError("GhostBehavior precisa encontrar um objeto com a tag Player.", this);
            return;
        }

        player = playerObject.transform;
    }

    private void Update()
    {
        if (!activated || player == null || !agent.isOnNavMesh ||
            Time.time < nextDestinationTime)
            return;

        ChooseFleeDestination();
    }

    public void Interact()
    {
        if (activated)
        {
            Destroy(gameObject);
            return;
        }

        if (player == null)
        {
            Debug.LogError("O fantasma não pode ser ativado sem encontrar o Player.", this);
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError("O NavMeshAgent do fantasma não está sobre um NavMesh.", this);
            return;
        }

        activated = true;
        agent.isStopped = false;
        ChooseFleeDestination();
    }

    private void ChooseFleeDestination()
    {
        Vector3 awayDirection = transform.position - player.position;
        awayDirection.y = 0f;

        if (awayDirection.sqrMagnitude < 0.001f)
            awayDirection = transform.forward;

        for (int attempt = 0; attempt < 8; attempt++)
        {
            float randomAngle = Random.Range(-75f, 75f);
            Vector3 direction = Quaternion.Euler(0f, randomAngle, 0f) *
                                awayDirection.normalized;
            float distance = Random.Range(minFleeDistance, maxFleeDistance);
            Vector3 candidate = transform.position + direction * distance;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit,
                    sampleRadius, agent.areaMask) &&
                agent.SetDestination(hit.position))
            {
                nextDestinationTime = Time.time +
                    Random.Range(destinationInterval.x, destinationInterval.y);
                return;
            }
        }

        Debug.LogWarning("Não foi possível encontrar um destino para o fantasma fugir.", this);
        nextDestinationTime = Time.time +
            Random.Range(destinationInterval.x, destinationInterval.y);
    }
}