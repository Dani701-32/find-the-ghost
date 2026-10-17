using UnityEngine;

public class DebuggingObject : MonoBehaviour, IInteractable
{
    [SerializeField] private bool isHaunted;
    [SerializeField] private GameObject ghostPrefab;

    public void Interact()
    {
        if (!isHaunted)
            return;

        if (ghostPrefab == null)
        {
            Debug.LogError("O objeto assombrado precisa de um prefab de fantasma.", this);
            return;
        }

        OutlineHighlight sourceHighlight = GetComponentInParent<OutlineHighlight>();
        if (sourceHighlight == null || sourceHighlight.OutlineMaterial == null)
        {
            Debug.LogError("O objeto assombrado precisa de um OutlineHighlight com material configurado.", this);
            return;
        }

        GameObject ghost = Instantiate(ghostPrefab, transform.position, transform.rotation);
        SetLayerRecursively(ghost.transform, gameObject.layer);

        Renderer[] renderers = ghost.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogError("O prefab de fantasma precisa conter pelo menos um Renderer.", ghost);
            Destroy(ghost);
            return;
        }

        OutlineHighlight ghostHighlight = ghost.GetComponent<OutlineHighlight>();
        if (ghostHighlight == null)
            ghostHighlight = ghost.AddComponent<OutlineHighlight>();
        ghostHighlight.SetOutlineMaterial(sourceHighlight.OutlineMaterial);

        if (ghost.GetComponentInChildren<Collider>() == null)
        {
            if (!AddInteractionCollider(ghost, renderers))
            {
                Destroy(ghost);
                return;
            }
        }

        GhostBehavior ghostBehavior = ghost.GetComponentInChildren<GhostBehavior>();
        if (ghostBehavior == null)
            ghostBehavior = ghost.AddComponent<GhostBehavior>();

        if (!ghostBehavior.Activate())
        {
            Destroy(ghost);
            return;
        }

        Destroy(gameObject);
    }

    private static bool AddInteractionCollider(GameObject ghost, Renderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        Vector3 scale = ghost.transform.lossyScale;
        float scaleX = Mathf.Abs(scale.x);
        float scaleY = Mathf.Abs(scale.y);
        float scaleZ = Mathf.Abs(scale.z);

        if (scaleX < 0.001f || scaleY < 0.001f || scaleZ < 0.001f)
        {
            Debug.LogError("Não foi possível criar o collider do fantasma: escala inválida.", ghost);
            return false;
        }

        CapsuleCollider collider = ghost.AddComponent<CapsuleCollider>();
        collider.center = ghost.transform.InverseTransformPoint(bounds.center);
        collider.radius = Mathf.Max(bounds.extents.x / scaleX, bounds.extents.z / scaleZ);
        collider.height = Mathf.Max(bounds.extents.y * 2f / scaleY, collider.radius * 2f);
        return true;
    }

    private static void SetLayerRecursively(Transform target, int layer)
    {
        target.gameObject.layer = layer;
        foreach (Transform child in target)
            SetLayerRecursively(child, layer);
    }
}
