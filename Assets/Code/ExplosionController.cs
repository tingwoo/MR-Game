using UnityEngine;

public class ExplosionController : MonoBehaviour
{
    private ParticleSystem _ps;

    void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
    }

    public void Initialize(Color impactColor)
    {
        // This accesses the "Main" module where Start Color lives
        var main = _ps.main;
        main.startColor = impactColor;
        
        _ps.Play();
        
        // Auto-destroy the object after the particle system finishes
        Destroy(gameObject, main.duration); 
    }
}