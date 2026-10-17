using UnityEngine;

public class OutlineHighlight : MonoBehaviour
{
    [SerializeField] private Material outlineMaterial;

    private Renderer[] renderers;
    private Material[][] originalMaterials;
    private Material[][] highlightedMaterials;
    private bool highlighted;

    private void Awake()
    {
        CacheMaterials();
    }

    public Material OutlineMaterial => outlineMaterial;

    public void SetOutlineMaterial(Material material)
    {
        outlineMaterial = material;
        CacheMaterials();

        if (highlighted)
            SetHighlighted(true);
    }

    private void CacheMaterials()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length][];
        highlightedMaterials = new Material[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].sharedMaterials;
            int addedMaterialCount = outlineMaterial == null ? 0 : 1;
            highlightedMaterials[i] = new Material[originalMaterials[i].Length + addedMaterialCount];

            for (int j = 0; j < originalMaterials[i].Length; j++)
                highlightedMaterials[i][j] = originalMaterials[i][j];

            if (outlineMaterial != null)
                highlightedMaterials[i][^1] = outlineMaterial;
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        this.highlighted = highlighted;

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sharedMaterials =
                highlighted && outlineMaterial != null
                    ? highlightedMaterials[i]
                    : originalMaterials[i];
        }
    }

    private void OnDestroy()
    {
        SetHighlighted(false);
    }
}
