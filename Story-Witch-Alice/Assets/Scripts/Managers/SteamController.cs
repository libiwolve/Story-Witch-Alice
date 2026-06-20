using UnityEngine;

public class SteamController : MonoBehaviour
{
    public ParticleSystem steamParticles;

    // 由 Animation Event 调用
    public void StartSteam()
    {
        
        if (steamParticles != null)
            steamParticles.Play();
    }

    public void StopSteam()
    {
        
        if (steamParticles != null)
            steamParticles.Stop();
    }
}