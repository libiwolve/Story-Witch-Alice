using UnityEngine;

/// <summary>
/// 雾元素特效：使用粒子系统模拟飘散的黑雾
/// 挂载到 PhysicsElement 所在的 GameObject 上
/// </summary>
[RequireComponent(typeof(PhysicsElement))]
public class FogEffect : MonoBehaviour
{
    [Header("粒子外观")]
    [SerializeField] private Color particleColor = new Color(0.1f, 0.08f, 0.15f, 0.3f);
    [SerializeField] private float particleSize = 0.5f;
    [SerializeField] private float particleSizeVariation = 0.3f;
    
    [Header("发射参数")]
    [SerializeField] private float emissionRate = 15f;
    [SerializeField] private float emissionRadius = 0.8f;
    
    [Header("运动参数")]
    [SerializeField] private float floatSpeed = 0.3f;
    [SerializeField] private float rotationSpeed = 20f;
    
    [Header("生命周期")]
    [SerializeField] private float minLifetime = 2f;
    [SerializeField] private float maxLifetime = 4f;
    
    private ParticleSystem particleSystem;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.ShapeModule shapeModule;
    private ParticleSystem.VelocityOverLifetimeModule velocityModule;
    private ParticleSystem.RotationOverLifetimeModule rotationModule;
    private ParticleSystemRenderer particleRenderer;

    void Start()
    {
        PhysicsElement pe = GetComponent<PhysicsElement>();
        if (pe == null || pe.elementData == null) return;
        if (pe.elementData.elementID != "fog") return;

        CreateFogParticles();
    }

    void CreateFogParticles()
    {
        // 创建粒子系统
        GameObject particleObj = new GameObject("FogParticles");
        particleObj.transform.SetParent(transform);
        particleObj.transform.localPosition = Vector3.zero;
        
        particleSystem = particleObj.AddComponent<ParticleSystem>();
        
        // 先停止粒子系统，防止播放时修改参数报错
        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        // 获取各模块
        mainModule = particleSystem.main;
        emissionModule = particleSystem.emission;
        shapeModule = particleSystem.shape;
        velocityModule = particleSystem.velocityOverLifetime;
        rotationModule = particleSystem.rotationOverLifetime;
        particleRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        
        // === 配置主模块 ===
        mainModule.duration = 1f;
        mainModule.loop = true;
        mainModule.playOnAwake = false;
        mainModule.startLifetime = new ParticleSystem.MinMaxCurve(minLifetime, maxLifetime);
        mainModule.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
        mainModule.startSize = new ParticleSystem.MinMaxCurve(
            particleSize - particleSizeVariation, 
            particleSize + particleSizeVariation
        );
        mainModule.startColor = particleColor;
        mainModule.gravityModifier = 0f;
        mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
        
        // === 配置发射模块 ===
        emissionModule.rateOverTime = emissionRate;
        emissionModule.SetBursts(new ParticleSystem.Burst[0]);
        
        // === 配置形状模块 ===
        shapeModule.shapeType = ParticleSystemShapeType.Sphere;
        shapeModule.radius = emissionRadius;
        shapeModule.position = Vector3.zero;
        
        // === 配置速度模块 ===
        velocityModule.enabled = true;
        velocityModule.x = new ParticleSystem.MinMaxCurve(-floatSpeed, floatSpeed);
        velocityModule.y = new ParticleSystem.MinMaxCurve(-floatSpeed, floatSpeed);
        velocityModule.z = new ParticleSystem.MinMaxCurve(-floatSpeed, floatSpeed);
        velocityModule.space = ParticleSystemSimulationSpace.World;
        
        // === 配置旋转模块 ===
        rotationModule.enabled = true;
        rotationModule.z = new ParticleSystem.MinMaxCurve(-rotationSpeed, rotationSpeed);
        
        // === 配置渲染器（关键修复） ===
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.material = CreateParticleMaterial();
        
        // 🔥 修复遮挡问题
        particleRenderer.sortingLayerName = "Foreground";  // 使用前景层
        particleRenderer.sortingOrder = 100;               // 高优先级，确保在最前面
        
        // 🔥 关键：禁用深度写入，但保留深度测试，让粒子永远显示在场景前面
        Material mat = particleRenderer.material;
        mat.SetInt("_ZWrite", 0);                          // 关闭深度写入
        mat.SetFloat("_Mode", 3);                          // Transparent 模式
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 4000;                            // Transparent 队列，在场景之后渲染
        
        // 所有配置完成后，手动播放
        particleSystem.Play();
    }

    Material CreateParticleMaterial()
    {
        // 使用 Unlit 透明着色器，不受光照影响，确保始终可见
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Particles/Additive");
        if (shader == null) shader = Shader.Find("UI/Default");  // 备选
        
        Material mat = new Material(shader);
        Texture2D texture = GenerateSoftCircleTexture(64);
        mat.mainTexture = texture;
        
        return mat;
    }

    Texture2D GenerateSoftCircleTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                
                float alpha;
                if (d <= 0.3f)
                    alpha = 1f;
                else if (d <= 1f)
                    alpha = 1f - Mathf.Pow((d - 0.3f) / 0.7f, 1.5f);
                else
                    alpha = 0f;
                
                tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }
        tex.Apply();
        
        return tex;
    }

    void OnDestroy()
    {
        if (particleSystem != null)
        {
            Destroy(particleSystem.gameObject);
        }
    }
}