using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CandleFlicker : MonoBehaviour
{
    public Light2D candleLight;
    public float baseIntensity = 0.9f;
    public float flickerAmount = 0.2f;
    public float speed = 3f;          // 闪烁速度
    public float smoothness = 0.5f;   // 平滑度（越大越跳）

    private float randomSeed;

    void Start()
    {
        randomSeed = Random.Range(0f, 100f);
    }

    void Update()
    {
        // Perlin 噪声生成平滑的随机波形
        float noise = Mathf.PerlinNoise(Time.time * speed, randomSeed);
        // 映射到 [-flickerAmount, flickerAmount]
        float flicker = (noise - 0.5f) * 2f * flickerAmount;
        // 用 SmoothDamp 让过渡更自然
        float targetIntensity = baseIntensity + flicker;
        candleLight.intensity = Mathf.Lerp(candleLight.intensity, targetIntensity, smoothness);
    }
}