using Eevee.Fixed;
using Eevee.Log;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Random示例代码
/// </summary>
internal sealed class RandomSample : MonoBehaviour
{
    #region Type
    private enum RandomType
    {
        System,
        Unity,
        Array,
    }

    [Serializable]
    private sealed class MinMax<T> where T : struct, IComparable<T>
    {
        [SerializeField] internal T Min;
        [SerializeField] internal T Max;
        [SerializeField] private List<T> _value = new();

        internal void Check(T item)
        {
            if (item.CompareTo(Min) < 0)
                LogRelay.Error($"[Random] item:{item} < {Min}");

            if (item.CompareTo(Max) >= 0)
                LogRelay.Error($"[Random] item:{item} >= {Max}");

            _value.Add(item);
        }
        internal void Clean() => _value.Clear();
    }
    #endregion

    [SerializeField] private RandomType _randomType;
    [SerializeField] private int _seed;
    [SerializeField] private byte _times;

    [Space] [SerializeField] private MinMax<sbyte> _sbyte;
    [SerializeField] private MinMax<byte> _byte;
    [SerializeField] private MinMax<short> _short;
    [SerializeField] private MinMax<ushort> _ushort;
    [SerializeField] private MinMax<int> _int;
    [SerializeField] private MinMax<uint> _uint;
    [SerializeField] private MinMax<long> _long;
    [SerializeField] private MinMax<ulong> _ulong;

    private void OnEnable()
    {
        switch (_randomType)
        {
            case RandomType.System: RandomProxy.Inject(new SystemRandom(_seed)); break;
            case RandomType.Unity: RandomProxy.Inject(new UnityRandom(_seed)); break;
            case RandomType.Array: RandomProxy.Inject(new ArrayRandom()); break;
        }
    }
    private void Update()
    {
        Profiler.BeginSample($"RandomSample.Update RandomType:{_randomType}");

        #region loop random
        _sbyte.Clean();
        for (int i = 0; i < _times; ++i)
            _sbyte.Check(RandomRelay.Get(_sbyte.Min, _sbyte.Max));

        _byte.Clean();
        for (int i = 0; i < _times; ++i)
            _byte.Check(RandomRelay.Get(_byte.Min, _byte.Max));

        _short.Clean();
        for (int i = 0; i < _times; ++i)
            _short.Check(RandomRelay.Get(_short.Min, _short.Max));

        _ushort.Clean();
        for (int i = 0; i < _times; ++i)
            _ushort.Check(RandomRelay.Get(_ushort.Min, _ushort.Max));

        _int.Clean();
        for (int i = 0; i < _times; ++i)
            _int.Check(RandomRelay.Get(_int.Min, _int.Max));

        _uint.Clean();
        for (int i = 0; i < _times; ++i)
            _uint.Check(RandomRelay.Get(_uint.Min, _uint.Max));

        _long.Clean();
        for (int i = 0; i < _times; ++i)
            _long.Check(RandomRelay.Get(_long.Min, _long.Max));

        _ulong.Clean();
        for (int i = 0; i < _times; ++i)
            _ulong.Check(RandomRelay.Get(_ulong.Min, _ulong.Max));
        #endregion

        Profiler.EndSample();
    }
    private void OnDisable()
    {
        RandomProxy.UnInject();
    }
}