using Eevee.Diagnosis;
using Eevee.Pool;
using Eevee.Random;
using UnityEngine;

internal sealed class ProxySample : MonoBehaviour
{
    [SerializeField] private int _randomSeed;

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