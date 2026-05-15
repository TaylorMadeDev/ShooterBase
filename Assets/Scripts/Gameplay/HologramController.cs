using UnityEngine;

/// <summary>
/// Simple controller to drive hologram shader properties: scanline offset and fresnel pulsing.
/// Attach to the pickup prefab and assign the hologram material instance.
/// </summary>
public class HologramController : MonoBehaviour
{
    [Tooltip("Material instance using the Scrapout/Hologram shader")]
    public Material HologramMaterial;

    [Tooltip("Vertical scrolling speed for scanlines")]
    public float ScrollSpeed = 0.45f;

    [Tooltip("Pulse speed for fresnel intensity")]
    public float PulseSpeed = 1.2f;

    [Tooltip("Pulse amplitude applied to _FresnelIntensity")]
    public float PulseAmplitude = 0.6f;

    Vector2 _scanOffset = Vector2.zero;

    void Update()
    {
        if (HologramMaterial == null) return;

        // Scroll the scanlines vertically
        _scanOffset.y += Time.deltaTime * ScrollSpeed;
        HologramMaterial.SetVector("_ScanOffset", new Vector4(_scanOffset.x, _scanOffset.y, 0, 0));

        // Pulse fresnel intensity
        float baseIntensity = 0.8f;
        float pulse = (Mathf.Sin(Time.time * PulseSpeed) * 0.5f + 0.5f) * PulseAmplitude;
        HologramMaterial.SetFloat("_FresnelIntensity", baseIntensity + pulse);
    }
}
