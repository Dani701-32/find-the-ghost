using UnityEngine;

public class OutlineHighlight : MonoBehaviour
{
    [SerializeField] private Material outlineMaterial;

    private MeshRenderer[] renderers; 
    private Material[][] originalMaterials;
    private Material[][] highlightedMaterials;

    private void Awake()
    {
        renderers = GetComponentsInChildren<MeshRenderer>();
        originalMaterials = new Material[renderers.Length][];
        highlightedMaterials = new Material[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].sharedMaterials;
            highlightedMaterials[i] = new Material[originalMaterials[i].Length + 1];

            for (int j = 0; j < originalMaterials[i].Length; j++)
                highlightedMaterials[i][j] = originalMaterials[i][j];

            highlightedMaterials[i][^1] = outlineMaterial;
        }
    }
    public void SetHighlighted(bool highlighted)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sharedMaterials =
                highlighted ? highlightedMaterials[i] : originalMaterials[i];
        }
    }

    private void OnDestroy()
    {
        SetHighlighted(false);
    }
}
