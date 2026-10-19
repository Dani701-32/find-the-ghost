using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class GhostBehavior : MonoBehaviour, IInteractable
{
    [SerializeField] private float minFleeDistance = 4f;
    [SerializeField] private float maxFleeDistance = 8f;
    [SerializeField] private float sampleRadius = 2f;
    [SerializeField] private Vector2 destinationInterval = new Vector2(1.5f, 3f);
    [SerializeField] private float stareDuration = 2f;

    private NavMeshAgent agent;
    [SerializeField] private Transform player;
    [SerializeField] private bool activated;
    private bool fleeing;
    private float fleeStartTime;
    private float nextDestinationTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.isStopped = true;
    }

    private void Start()
    {
        FindPlayer();
    }

    private void Update()
    {
        if (!activated || player == null || !agent.isOnNavMesh)
            return;

        if (!fleeing)
        {
            FacePlayer();
            if (Time.time >= fleeStartTime)
                StartFleeing();
            return;
        }

        if (Time.time < nextDestinationTime)
            return;

        ChooseFleeDestination();
    }

    public void Interact()
    {
        if (!activated || !fleeing)
            return;

        GameController controller = GameController.Instance;
        if (controller == null || !controller.CaptureGhost(this))
            return;

        Destroy(transform.root.gameObject);
    }

    public bool Activate()
    {
        if (activated)
            return true;

        if (player == null)
        {
            FindPlayer();
        }

        if (player == null)
        {
            Debug.LogError("O fantasma não pode ser ativado sem encontrar o Player.", this);
            return false;
        }

        if (!agent.isOnNavMesh &&
            (!NavMesh.SamplePosition(transform.position, out NavMeshHit spawnPosition,
                 sampleRadius, agent.areaMask) ||
             !agent.Warp(spawnPosition.position)))
        {
            Debug.LogError("O NavMeshAgent do fantasma não está sobre um NavMesh.", this);
            return false;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError("Não foi possível posicionar o fantasma sobre um NavMesh.", this);
            return false;
        }

        activated = true;
        agent.isStopped = true;
        fleeStartTime = Time.time + Mathf.Max(0f, stareDuration);
        FacePlayer();
        return true;
    }

    private void StartFleeing()
    {
        fleeing = true;
        agent.isStopped = false;
        ChooseFleeDestination();
    }

    private void FacePlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
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