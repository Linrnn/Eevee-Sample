using Box2DSharp.Common;
using Eevee.Fixed;
using Eevee.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Fixed64示例代码
/// </summary>
internal sealed class Fixed64Sample : MonoBehaviour
{
    [SerializeField] private int _absTimes;
    [SerializeField] private int _sqrtTimes;
    [SerializeField] private int _mulTimes;
    [SerializeField] private int _divTimes;
    [Space] [SerializeField] private bool _testSin;
    [SerializeField] private bool _testCos;
    [SerializeField] private bool _testTan;
    [SerializeField] private bool _testCot;
    [Space] [SerializeField] private bool _testAsin;
    [SerializeField] private bool _testAcos;
    [SerializeField] private bool _testAtan;
    [SerializeField] private bool _testAtan2;
    [SerializeField] private bool _testAcot;

    private const double Epsilon = 0.001;
    private readonly IRandom _random = new MtRandom(DateTime.Now.Millisecond);

    private readonly List<long> _numbers = new();
    private readonly List<double> _systemRad = new();
    private readonly List<FP> _systemOne = new();
    private readonly List<Fixed64> _fixed64BigNumbers = new();
    private readonly List<FP> _fpBigNumbers = new();
    private readonly List<Fixed64> _fixed64SmallNumbers = new();
    private readonly List<FP> _fpSmallNumbers = new();
    private readonly List<Fixed64> _fixed64Rad = new();
    private readonly List<FP> _fpRad = new();
    private readonly List<Fixed64> _fixed64One = new();
    private readonly List<FP> _fpOne = new();

    private void OnEnable()
    {
        Initialize();
        Random();
    }
    private void Update()
    {
        Abs();
        Sqrt();
        Mul();
        Div();

        Sin();
        Cos();
        Tan();
        Cot();

        Asin();
        Acos();
        Atan();
        Atan2();
        Acot();
    }

    private void Initialize()
    {
        _systemRad.Clear();
        for (double value = -500D; value <= 500D; value += 0.015625)
            _systemRad.Add(value);

        _systemOne.Clear();
        for (double value = -1D; value <= 1D; value += 0.015625)
            _systemOne.Add(value);

        _fixed64Rad.Clear();
        for (Fixed64 value = -500; value <= 500; value += 0.015625)
            _fixed64Rad.Add(value);

        _fpRad.Clear();
        for (FP value = -500; value <= 500; value += 0.015625)
            _fpRad.Add(value);

        _fixed64One.Clear();
        for (var value = -Fixed64.One; value <= Fixed64.One; value += 0.015625)
            _fixed64One.Add(value);

        _fpOne.Clear();
        for (var value = -FP.One; value <= FP.One; value += 0.015625)
            _fpOne.Add(value);
    }
    private void Random()
    {
        int[] counts = { _absTimes, _sqrtTimes, _mulTimes, _divTimes };
        int count = counts.Max();

        _numbers.Clear();
        _fixed64BigNumbers.Clear();
        _fpBigNumbers.Clear();
        _fixed64SmallNumbers.Clear();
        _fpSmallNumbers.Clear();

        for (int i = 0; i < count >> 1; ++i)
            _numbers.Add(_random.GetInt16(0, short.MaxValue));
        for (int i = count >> 1; i < count; ++i)
            _numbers.Add(_random.GetInt32(short.MaxValue, int.MaxValue));
        for (int i = 0; i < _numbers.Count; ++i)
        {
            int j = _random.GetInt32(0, _numbers.Count);
            (_numbers[i], _numbers[j]) = (_numbers[j], _numbers[i]);
        }

        foreach (long number in _numbers)
        {
            bool sign = Convert.ToBoolean(_random.GetByte(0, 2));
            long rawValue = ((sign ? number : -number) << 32) + (sign ? 1L : -1L);

            _fixed64BigNumbers.Add(number);
            _fpBigNumbers.Add(number);
            _fixed64SmallNumbers.Add(new Fixed64(rawValue));
            _fpSmallNumbers.Add(FP.FromRaw(rawValue));
        }
    }

    private void Abs()
    {
        if (_absTimes <= 0)
            return;

        Profiler.BeginSample("Fixed64Sample.Abs Fixed64");
        for (int i = 0; i < _absTimes; i++)
            _ = _fixed64BigNumbers[i].Abs();
        Profiler.EndSample();

        Profiler.BeginSample("Fixed64Sample.Abs FP");
        for (int i = 0; i < _absTimes; i++)
            FP.Abs(_fpBigNumbers[i]);
        Profiler.EndSample();
    }
    private void Sqrt()
    {
        if (_sqrtTimes <= 0)
            return;

        for (int i = 0; i < _sqrtTimes; ++i)
        {
            long number = _numbers[i];
            var fSqrt = Maths.Sqrt(number);
            double sSqrt = Math.Sqrt(number);

            double diff = Math.Abs((double)(fSqrt - sSqrt));
            if (diff >= 1D)
                LogRelay.Error($"[Sample] fSqrt:{fSqrt:0.000}, sSqrt:{sSqrt:0.000}, diff:{diff:0.000} >= 1, number:{number}");
        }

        Profiler.BeginSample("Fixed64Sample.Sqrt System");
        for (int i = 0; i < _sqrtTimes; ++i)
            Math.Sqrt((double)_fixed64BigNumbers[i]);
        Profiler.EndSample();

        Profiler.BeginSample("Fixed64Sample.Sqrt Fixed64");
        for (int i = 0; i < _sqrtTimes; ++i)
            _fixed64BigNumbers[i].Sqrt();
        Profiler.EndSample();

        Profiler.BeginSample("Fixed64Sample.Sqrt FP");
        for (int i = 0; i < _sqrtTimes; ++i)
            FP.Sqrt(_fpBigNumbers[i]);
        Profiler.EndSample();
    }
    private void Mul()
    {
        if (_mulTimes <= 0)
            return;

        Profiler.BeginSample("Fixed64Sample.Mul Fixed64");
        for (int i = 0; i < _mulTimes; ++i)
            _ = _fixed64SmallNumbers[i] * _fixed64SmallNumbers[i];
        Profiler.EndSample();

        Profiler.BeginSample("Fixed64Sample.Mul FP");
        for (int i = 0; i < _mulTimes; ++i)
            _ = _fpSmallNumbers[i] * _fpSmallNumbers[i];
        Profiler.EndSample();
    }
    private void Div()
    {
        if (_divTimes <= 0)
            return;

        foreach (var left in _fixed64SmallNumbers)
        {
            var right = _fixed64SmallNumbers[_random.GetInt32(0, _fixed64SmallNumbers.Count)];
            var fDiv = left / right;
            var sDiv = (Fixed64)((decimal)left / (decimal)right);

            if (fDiv != sDiv)
                LogRelay.Error($"[Sample] fDiv:{fDiv:0.000} != sDiv:{sDiv:0.000}, left:{left:0.000}, right:{right:0.000}");
        }

        Profiler.BeginSample("Fixed64Sample.Div Fixed64");
        for (int count = Math.Min(_fixed64SmallNumbers.Count - 1, _divTimes), i = 0; i < count; ++i)
            _ = _fixed64SmallNumbers[i] / _fixed64SmallNumbers[i + 1];
        Profiler.EndSample();

        Profiler.BeginSample("Fixed64Sample.Div FP");
        for (int count = Math.Min(_fpSmallNumbers.Count - 1, _divTimes), i = 0; i < count; ++i)
            _ = _fpSmallNumbers[i] / _fpSmallNumbers[i + 1];
        Profiler.EndSample();
    }

    private void Sin()
    {
        if (!_testSin)
            return;

        foreach (var rad in _fixed64Rad)
        {
            var fSin = Maths.Sin(rad);
            double sSin = Math.Sin((double)rad);

            double diff = Math.Abs((double)(fSin - sSin));
            if (diff >= Epsilon)
                LogRelay.Error($"[Sample] fSin:{fSin:0.000}, sSin:{sSin:0.000}, diff:{diff:0.000} >= {Epsilon:0.000}, rad:{rad:0.000}");
        }

        foreach (var deg in _fixed64Rad)
        {
            var fSin = Maths.SinDeg(deg);
            double sSin = Math.Sin((double)deg * Math.PI / 180);

            double diff = Math.Abs((double)(fSin - sSin));
            if (diff >= Epsilon)
                LogRelay.Error($"[Sample] fSin:{fSin:0.000}, sSin:{sSin:0.000}, diff:{diff:0.000} >= {Epsilon:0.000}, deg:{deg:0.000}");
        }

        Profiler.BeginSample("Fixed64Sample.Sin Rad System");
        foreach (double rad in _systemRad)
            Math.Sin(rad);
        Profiler.EndSample();

        Profiler.BeginSample("Fixed64Sample.Sin Rad FP");
        foreach (var rad in _fpRad)
            FP.Sin(rad);
        Profiler.EndSample();

        Profiler.BeginSample("Fixed64Sample.Sin Slow Rad FP");
        foreach (var rad in _fpRad)
            FP.SlowSin(rad);
        Profiler.EndSample();

        Profiler.BeginSample("Fixed64Sample.Sin Rad Fixed64");
        foreach (var rad in _fixed64Rad)
            Maths.Sin(rad);
        Profiler.EndSample();

        Profiler.BeginSample("Fixed64Sample.Sin Deg Fixed64");
        foreach (var deg in _fixed64Rad)
            Maths.SinDeg(deg);
        Profiler.EndSample();
    }
    private void Cos()
    {
        if (!_testCos)
            return;

        foreach (var rad in _fixed64Rad)
        {
            var fCos = Maths.Cos(rad);
            double sCos = Math.Cos((double)rad);

            double diff = Math.Abs((double)(fCos - sCos));
            if (diff >= Epsilon)
                LogRelay.Error($"[Sample] cos0:{fCos:0.000}, sCos:{sCos:0.000}, diff:{diff:0.000} >= {Epsilon}, rad:{rad:0.000}");
        }

        foreach (var deg in _fixed64Rad)
        {
            var fCos = Maths.CosDeg(deg);
            double sCos = Math.Cos((double)deg * Math.PI / 180);

            double diff = Math.Abs((double)(fCos - sCos));
            if (diff >= Epsilon)
                LogRelay.Error($"[Sample] fCos:{fCos:0.000}, sCos:{sCos:0.000}, diff:{diff:0.000} >= {Epsilon}, deg:{deg:0.000}");
        }
    }
    private void Tan()
    {
        if (!_testTan)
            return;

        foreach (var rad in _fixed64Rad)
        {
            if (Maths.Cos(rad) == 0)
                continue;
            if (((double)rad * 180D / Math.PI % 180D + 180D) % 180D is > 89D and < 91D)
                continue;

            var fTan = Maths.Tan(rad);
            double sTan = Math.Tan((double)rad);

            double diff = Math.Abs((double)(fTan - sTan));
            if (diff >= Epsilon)
                LogRelay.Error($"[Sample] fTan:{fTan:0.000}, sTan:{sTan:0.000}, diff:{diff:0.000} >= {Epsilon}, rad:{rad:0.000}");
        }

        foreach (var deg in _fixed64Rad)
        {
            if (Maths.CosDeg(deg) == Fixed64.Zero)
                continue;
            var mod = (deg % 180 + 180) % 180;
            if (mod > 89 && mod < 91)
                continue;

            var fTan = Maths.TanDeg(deg);
            double sTan = Math.Tan((double)deg * Math.PI / 180D);

            double diff = Math.Abs((double)(fTan - sTan));
            if (diff >= Epsilon)
                LogRelay.Error($"[Sample] fTan:{fTan:0.000}, sTan:{sTan:0.000}, diff:{diff:0.000} >= {Epsilon}, deg:{deg:0.000}");
        }

        Profiler.BeginSample("Fixed64Sample.Tan Rad System");
        foreach (var rad in _systemRad)
            Math.Tan(rad);
        Profiler.EndSample();

        Profiler.BeginSample("Fixed64Sample.Tan Rad FP");
        foreach (var rad in _fpRad)
            if (FP.Cos(rad) != FP.Zero)
                FP.Tan(rad);
        Profiler.EndSample();

        Profiler.BeginSample("Fixed64Sample.Tan Rad Fixed64");
        foreach (var rad in _fixed64Rad)
            if (Maths.Cos(rad) != Fixed64.Zero)
                Maths.Tan(rad);
        Profiler.EndSample();

        Profiler.BeginSample("Fixed64Sample.Tan Deg Fixed64");
        foreach (var deg in _fixed64Rad)
            if (Maths.CosDeg(deg) != Fixed64.Zero)
                Maths.TanDeg(deg);
        Profiler.EndSample();
    }
    private void Cot()
    {
        if (!_testCot)
            return;

        foreach (var rad in _fixed64Rad)
        {
            if (Maths.Sin(rad) == Fixed64.Zero)
                continue;
            if (((double)rad * 180D / Math.PI % 180D + 180D) % 180D is < 1D or > 179D)
                continue;

            var fCot = Maths.Cot(rad);
            double sCot = 1D / Math.Tan((double)rad);

            double diff = Math.Abs((double)(fCot - sCot));
            if (diff >= Epsilon)
                LogRelay.Error($"[Sample] fCot:{fCot:0.000}, sCot:{sCot:0.000}, diff:{diff:0.000} >= {Epsilon}, rad:{rad:0.000}");
        }

        foreach (var deg in _fixed64Rad)
        {
            if (Maths.SinDeg(deg) == Fixed64.Zero)
                continue;
            var mod = (deg % 180 + 180) % 180;
            if (mod < 1 || mod > 179)
                continue;

            var fCot = Maths.CotDeg(deg);
            double sCot = 1D / Math.Tan((double)(deg * Maths.Deg2Rad));

            double diff = Math.Abs((double)(fCot - sCot));
            if (diff >= Epsilon)
                LogRelay.Error($"[Sample] fCot:{fCot:0.000}, sCot:{sCot:0.000}, diff:{diff:0.000} >= {Epsilon}, deg:{deg:0.000}");
        }
    }

    private void Asin()
    {
        if (!_testAsin)
            return;

        foreach (var value in _fixed64One)
        {
            var fAsin = Maths.Asin(value);
            double sAsin = Math.Asin((double)value);

            double diff = Math.Abs((double)(fAsin - sAsin));
            if (diff >= Epsilon)
                LogRelay.Error($"[Sample] fAsin:{fAsin:0.000}, sAsin:{sAsin:0.000}, diff:{diff:0.000} >= {Epsilon}, value:{value:0.000}");
        }

        foreach (var value in _fixed64One)
        {
            var fAsin = Maths.AsinDeg(value);
            double sAsin = Math.Asin((double)value) * 180D / Math.PI;

            double diff = Math.Abs((double)(fAsin - sAsin));
            if (diff >= Epsilon)
                LogRelay.Error($"[Sample] fAsin:{fAsin:0.000}, sAsin:{sAsin:0.000}, diff:{diff:0.000} >= {Epsilon}, value:{value:0.000}");
        }

        Profiler.BeginSample("Fixed64Sample.ASin System");
        foreach (double value in _systemOne)
            Math.Asin(value);
        Profiler.EndSample();

        Profiler.BeginSample("Fixed64Sample.ASin FP");
        foreach (var value in _fpOne)
            FP.Asin(value);
        Profiler.EndSample();

        Profiler.BeginSample("Fixed64Sample.ASin Fixed64");
        foreach (var value in _fixed64One)
            Maths.Asin(value);
        Profiler.EndSample();

        Profiler.BeginSample("Fixed64Sample.ASinDeg Fixed64");
        foreach (var value in _fixed64One)
            Maths.AsinDeg(value);
        Profiler.EndSample();
    }
    private void Acos()
    {
        if (!_testAcos)
            return;

        foreach (var value in _fixed64One)
        {
            var fAcos = Maths.Acos(value);
            double sAcos = Math.Acos((double)value);

            double diff = Math.Abs((double)(fAcos - sAcos));
            if (diff >= Epsilon)
                LogRelay.Error($"[Sample] fAcos:{fAcos:0.000}, sAcos:{sAcos:0.000}, diff:{diff:0.000} >= {Epsilon}, value:{value:0.000}");
        }

        foreach (var value in _fixed64One)
        {
            var fAcos = Maths.AcosDeg(value);
            double sAcos = Math.Acos((double)value) * 180D / Math.PI;

            double diff = Math.Abs((double)(fAcos - sAcos));
            if (diff >= Epsilon)
                LogRelay.Error($"[Sample] fAcos:{fAcos:0.000}, sAcos:{sAcos:0.000}, diff:{diff:0.000} >= {Epsilon}, value:{value:0.000}");
        }
    }
    private void Atan()
    {
        if (!_testAtan)
            return;

        foreach (var value in _fixed64Rad)
        {
            var fAtan = Maths.Atan(value);
            double sAtan = Math.Atan((double)value);

            double diff = Math.Abs((double)(fAtan - sAtan));
            if (diff >= Epsilon)
                LogRelay.Error($"[Sample] fAtan:{fAtan:0.000}, sAtan:{sAtan:0.000}, diff:{diff:0.000} >= {Epsilon}, value:{value:0.000}");
        }

        foreach (var value in _fixed64Rad)
        {
            var fAtan = Maths.AtanDeg(value);
            double sAtan = Math.Atan((double)value) * 180D / Math.PI;

            double diff = Math.Abs((double)(fAtan - sAtan));
            if (diff >= Epsilon)
                LogRelay.Error($"[Sample] fAtan:{fAtan:0.000}, sAtan:{sAtan:0.000}, diff:{diff:0.000} >= {Epsilon}, value:{value:0.000}");
        }
    }
    private void Atan2()
    {
        if (!_testAtan2)
            return;

        foreach (var value in _fixed64Rad)
        {
            var fAtan2 = Maths.Atan2(value, Fixed64.One);
            double sAtan2 = Math.Atan2((double)value, 1D);

            double diff = Math.Abs((double)(fAtan2 - sAtan2));
            if (diff >= Epsilon)
                LogRelay.Error($"[Sample] fAtan2:{fAtan2:0.000}, sAtan2:{sAtan2:0.000}, diff:{diff:0.000} >= {Epsilon}, value:{value:0.000}");
        }

        foreach (var value in _fixed64Rad)
        {
            var fAtan2 = Maths.Atan2Deg(value, Fixed64.One);
            double sAtan2 = Math.Atan2((double)value, 1D) * 180D / Math.PI;

            double diff = Math.Abs((double)(fAtan2 - sAtan2));
            if (diff >= Epsilon)
                LogRelay.Error($"[Sample] fAtan2:{fAtan2:0.000}, sAtan2:{sAtan2:0.000}, diff:{diff:0.000} >= {Epsilon}, value:{value:0.000}");
        }

        Profiler.BeginSample("Fixed64Sample.Atan2 System");
        foreach (double value in _systemOne)
            Math.Atan2(value, 1D);
        Profiler.EndSample();

        Profiler.BeginSample("Fixed64Sample.Atan2 FP");
        foreach (var value in _fpOne)
            FP.Atan2(value, FP.One);
        Profiler.EndSample();

        Profiler.BeginSample("Fixed64Sample.Atan2 Fixed64");
        foreach (var value in _fixed64One)
            Maths.Atan2(value, Fixed64.One);
        Profiler.EndSample();

        Profiler.BeginSample("Fixed64Sample.Atan2Deg Fixed64");
        foreach (var value in _fixed64One)
            Maths.Atan2Deg(value, Fixed64.One);
        Profiler.EndSample();
    }
    private void Acot()
    {
        if (!_testAcot)
            return;

        foreach (var value in _fixed64Rad)
        {
            var fAcot = Maths.Acot(value);
            double sAcot = Math.PI * 0.5 - Math.Atan((double)value);

            double diff = Math.Abs((double)(fAcot - sAcot));
            if (diff >= Epsilon)
                LogRelay.Error($"[Sample] fAcot:{fAcot:0.000}, sAcot:{sAcot:0.000}, diff:{diff:0.000} >= {Epsilon}, value:{value:0.000}");
        }

        foreach (var value in _fixed64Rad)
        {
            var fAcot = Maths.AcotDeg(value);
            double sAcot = 90D - Math.Atan((double)value) * 180D / Math.PI;

            double diff = Math.Abs((double)(fAcot - sAcot));
            if (diff >= Epsilon)
                LogRelay.Error($"[Sample] fAcot:{fAcot:0.000}, sAcot:{sAcot:0.000}, diff:{diff:0.000} >= {Epsilon}, value:{value:0.000}");
        }
    }
}