using Eevee.Diagnosis;
using Eevee.Pool;
using Eevee.Random;
using UnityEngine;

public sealed class ProxySample : MonoBehaviour
{
    [SerializeField] private int _randomSeed = 0;

    private void OnEnable()
    {
        LogProxy.Inject(new UnityLog());
        RandomProxy.Inject(new MtRandom(_randomSeed));
    }
    private void OnDisable()
    {
        LogProxy.UnInject();
        RandomProxy.UnInject();
    }
    private void OnDestroy()
    {
        CollectionPool.CleanImpl();
    }
}