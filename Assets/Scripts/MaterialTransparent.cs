using UnityEngine;

public class MaterialTransparent : MonoBehaviour
{
    [SerializeField] Renderer targetRenderer;

    [SerializeField] Material opaqueMaterial;
    [SerializeField] Material transparentMaterial;

    public void ToggleTransparency(bool isTransparent)
    {
        targetRenderer.material = isTransparent ? transparentMaterial : opaqueMaterial;
    }
}