using Eevee.Debug;
using Eevee.Fixed;
using Eevee.Random;
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
        MersenneTwister,
    }

    [Serializable]
    private sealed class MinMax<T> where T : struct, IComparable<T>
    {
        [SerializeField] internal bool Random = true;
        [SerializeField] internal T Min;
        [SerializeField] internal T Max;
        [SerializeField] private List<T> _value = new();

        internal void Check(T item)
        {
            if (item.CompareTo(Min) < 0)
                LogRelay.Error($"[Sample] {typeof(T).Name} item:{item} < {Min}");

            if (item.CompareTo(Max) >= 0)
                LogRelay.Error($"[Sample] {typeof(T).Name} item:{item} >= {Max}");

            _value.Add(item);
        }
        internal void Clean() => _value.Clear();
    }
    #endregion

    [SerializeField] private RandomType _randomType;
    [SerializeField] private int _seed;
    [SerializeField] private int _times;

    [Space] [SerializeField] private MinMax<sbyte> _sbyte;
    [SerializeField] private MinMax<byte> _byte;
    [SerializeField] private MinMax<short> _short;
    [SerializeField] private MinMax<ushort> _ushort;
    [SerializeField] private MinMax<int> _int;
    [SerializeField] private MinMax<uint> _uint;
    [SerializeField] private MinMax<long> _long;
    [SerializeField] private MinMax<ulong> _ulong;
    [SerializeField] private MinMax<Fixed64> _fixed64;
    [SerializeField] private MinMax<Fixed64> _fixed64_01;

    private void OnEnable()
    {
        switch (_randomType)
        {
            case RandomType.System: RandomProxy.Inject(new SystemRandom(_seed)); break;
            case RandomType.Unity: RandomProxy.Inject(new UnityRandom(_seed)); break;
            case RandomType.MersenneTwister: RandomProxy.Inject(new MtRandom(_seed)); break;
        }
    }
    private void Update()
    {
        Profiler.BeginSample("RandomSample.Update");

        #region Number
        _sbyte.Clean();
        if (_sbyte.Random)
            for (int i = 0; i < _times; ++i)
                _sbyte.Check(RandomRelay.SByte(_sbyte.Min, _sbyte.Max));

        _byte.Clean();
        if (_byte.Random)
            for (int i = 0; i < _times; ++i)
                _byte.Check(RandomRelay.Byte(_byte.Min, _byte.Max));

        _short.Clean();
        if (_short.Random)
            for (int i = 0; i < _times; ++i)
                _short.Check(RandomRelay.Short(_short.Min, _short.Max));

        _ushort.Clean();
        if (_ushort.Random)
            for (int i = 0; i < _times; ++i)
                _ushort.Check(RandomRelay.UShort(_ushort.Min, _ushort.Max));

        _int.Clean();
        if (_int.Random)
            for (int i = 0; i < _times; ++i)
                _int.Check(RandomRelay.Int(_int.Min, _int.Max));

        _uint.Clean();
        if (_uint.Random)
            for (int i = 0; i < _times; ++i)
                _uint.Check(RandomRelay.UInt(_uint.Min, _uint.Max));

        _long.Clean();
        if (_long.Random)
            for (int i = 0; i < _times; ++i)
                _long.Check(RandomRelay.Long(_long.Min, _long.Max));

        _ulong.Clean();
        if (_ulong.Random)
            for (int i = 0; i < _times; ++i)
                _ulong.Check(RandomRelay.ULong(_ulong.Min, _ulong.Max));

        _fixed64.Clean();
        if (_fixed64.Random)
            for (int i = 0; i < _times; ++i)
                _fixed64.Check(RandomRelay.Number(_fixed64.Min, _fixed64.Max));

        _fixed64_01.Clean();
        if (_fixed64_01.Random)
            for (int i = 0; i < _times; ++i)
                _fixed64_01.Check(RandomRelay.Number());
        #endregion

        #region Circle/Sphere
        for (int i = 0; i < _times; ++i)
        {
            var circle = RandomRelay.OnUnitCircle();
            if (circle.Magnitude() != Fixed64.One)
                LogRelay.Error($"[Sample] OnUnitCircle {circle}.Magnitude != 1 ");
        }

        for (int i = 0; i < _times; ++i)
        {
            var radius = RandomRelay.Number(1, 10);
            var circle = RandomRelay.InCircle(radius);
            if (circle.SqrMagnitude() > radius.Sqr())
                LogRelay.Error($"[Sample] InCircle {circle}.Magnitude > {radius.Sqrt()} ");
        }

        for (int i = 0; i < _times; ++i)
        {
            var sphere = RandomRelay.OnUnitSphere();
            if (sphere.Magnitude() != Fixed64.One)
                LogRelay.Error($"[Sample] OnUnitSphere {sphere}.Magnitude != 1 ");
        }

        for (int i = 0; i < _times; ++i)
        {
            var radius = RandomRelay.Number(1, 10);
            var sphere = RandomRelay.InSphere(radius);
            if (sphere.SqrMagnitude() > radius.Sqr())
                LogRelay.Error($"[Sample] InSphere {sphere}.Magnitude > {radius.Sqrt()} ");
        }
        #endregion

        Profiler.EndSample();
    }
    private void OnDisable()
    {
        RandomProxy.UnInject();
    }
}