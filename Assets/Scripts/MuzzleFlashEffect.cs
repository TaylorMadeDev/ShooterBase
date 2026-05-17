using System.Collections;
using UnityEngine;

public class MuzzleFlashEffect : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("How long the flash lasts in seconds")]
    public float duration = 0.1f;
    
    [Tooltip("If true, destroys the GameObject after the flash. Great for spawned prefabs!")]
    public bool destroyOnComplete = false;
    
    private MeshRenderer[] meshRenderers;
    private MaterialPropertyBlock[] propBlocks;
    private int erosionId;

    private Vector3 originalScale;

    private void Awake()
    {
        meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        if (meshRenderers == null || meshRenderers.Length == 0)
        {
            Debug.LogWarning("MuzzleFlashEffect requires at least one MeshRenderer in the GameObject or its children.", this);
            return;
        }

        propBlocks = new MaterialPropertyBlock[meshRenderers.Length];
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            propBlocks[i] = new MaterialPropertyBlock();
        }

        erosionId = Shader.PropertyToID("_Erosion");
        originalScale = transform.localScale;

        // Hide it initially
        SetRenderersEnabled(false);
    }

    /// <summary>
    /// Call this when the weapon shoots.
    /// multiplier allows different barrels to create bigger/smaller flashes!
    /// </summary>
    public void PlayFlash(float barrelScaleMultiplier = 1f)
    {
        StopAllCoroutines();

        // Calculate the final target scale for this flash
        Vector3 targetScale = originalScale * barrelScaleMultiplier;
        transform.localScale = Vector3.zero;

        // Reset shader erosion to 0 and show all renderers
        SetRenderersEnabled(true);
        SetErosion(0f);

        // Animate growth and disappearance
        StartCoroutine(AnimateFlash(targetScale));
    }

    private IEnumerator AnimateFlash(Vector3 targetScale)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            // t goes from 0 to 1 over the duration
            float t = Mathf.Clamp01(elapsed / duration);
            
            SetErosion(t);
            transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
            yield return null;
        }

        // Ensure it's fully erased and hide the mesh
        SetErosion(1f);
        transform.localScale = targetScale;
        SetRenderersEnabled(false);
        
        if (destroyOnComplete)
        {
            Destroy(gameObject);
        }
    }

    private void SetErosion(float value)
    {
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            meshRenderers[i].GetPropertyBlock(propBlocks[i]);
            propBlocks[i].SetFloat(erosionId, value);
            meshRenderers[i].SetPropertyBlock(propBlocks[i]);
        }
    }

    private void SetRenderersEnabled(bool enabled)
    {
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            meshRenderers[i].enabled = enabled;
        }
    }
}

