using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

using MilDataStructs;

public class AutoReleaseParticle : MonoBehaviour
{
    public ObjectPool<GameObject> pool;
    public HashSet<GameObject> activeHitParticles;
    public MilNote master;
    public bool isHold;

    public void Start() {
        
    }

    public void Update() {

    }

    void OnParticleSystemStopped() {
        if (!isHold) master.hitParticle = null;
        else master.holdHitParticle = null;
        
        pool?.Release(gameObject);
        activeHitParticles?.Remove(gameObject);
    }
}
