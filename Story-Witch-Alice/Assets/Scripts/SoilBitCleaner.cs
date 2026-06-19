using UnityEngine;
public class ParticlesCleaner : MonoBehaviour
{
    public float lifetime = 3f;
    void Start() => Destroy(gameObject, lifetime);
}