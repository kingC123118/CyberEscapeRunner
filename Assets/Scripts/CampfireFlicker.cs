using UnityEngine;

/// <summary>
/// Simulates campfire light flickering by randomly modulating
/// the attached light's intensity and range over time (Perlin-noise based).
/// </summary>
[RequireComponent(typeof(Light))]
public class CampfireFlicker : MonoBehaviour
{
    [Header("Intensity")]
    [Tooltip("Base light intensity around which flickering occurs")]
    public float baseIntensity = 2.4f;
    [Tooltip("How far the intensity can swing above/below the base value")]
    public float intensityVariation = 0.8f;

    [Header("Range")]
    [Tooltip("Base light range around which flickering occurs")]
    public float baseRange = 13f;
    [Tooltip("How far the range can swing above/below the base value")]
    public float rangeVariation = 2f;

    [Header("Speed")]
    [Tooltip("How fast the flicker animates")]
    public float flickerSpeed = 8f;

    Light fireLight;
    float seed;

    void Awake()
    {
        fireLight = GetComponent<Light>();
        seed = Random.value * 100f;
    }

    void Update()
    {
        float t = Time.time * flickerSpeed + seed;
        // Two layered Perlin noises: a slow wave plus a fast jitter for a natural flame feel
        float n = Mathf.PerlinNoise(t, seed) * 0.65f
                + Mathf.PerlinNoise(t * 2.7f, seed * 1.3f) * 0.35f;
        float offset = (n - 0.5f) * 2f; // -1 .. 1
        fireLight.intensity = baseIntensity + offset * intensityVariation;
        fireLight.range = baseRange + offset * rangeVariation;
    }
}
