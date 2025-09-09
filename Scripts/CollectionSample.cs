using Eevee.Collection;
using Eevee.Pool;
using Eevee.Random;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Collection示例代码
/// </summary>
internal sealed class CollectionSample : MonoBehaviour
{
    private IRandom _random;
    [SerializeField] private int _times;

    private readonly List<int> _helpList = new();
    private readonly List<int> _sysList = new();
    [SerializeField] private WeakOrderList<int> _eveList = new();

    private readonly HashSet<int> _sysSet = new();
    [SerializeField] private FixedOrderSet<int> _eveSet = new();

    private readonly Dictionary<int, int> _helpDic = new();
    private readonly Dictionary<int, int> _sysDic = new();
    [SerializeField] private FixedOrderDic<int, int> _eveDic = new();

    private void OnEnable()
    {
        _random = new UnityRandom();
    }
    private void Update()
    {
        Clean();

        #region Profiler List
        Profiler.BeginSample("CollectionSample.Update.WeakOrderList`1");
        Test_List_Add();
        Test_List_Insert();
        Test_List_InsertRange();
        Test_List_Remove();
        Test_List_RemoveAt();
        Test_List_RemoveRange();
        Profiler.EndSample();
        #endregion

        #region Profiler Set
        Profiler.BeginSample("CollectionSample.Update.FixedOrderSet`1");
        Test_Set_Add();
        Test_Set_Remove();
        Test_Set_UnionWith();
        Test_Set_IntersectWith();
        Test_Set_ExceptWith();
        Test_Set_SymmetricExceptWith();
        Test_Set_IsSubsetOf();
        Test_Set_IsSupersetOf();
        Test_Set_IsProperSubsetOf();
        Test_Set_IsProperSupersetOf();
        Test_Set_Overlaps();
        Profiler.EndSample();
        #endregion

        #region Profiler Dic
        Profiler.BeginSample("CollectionSample.Update.FixedOrderDic`2");
        Test_Dic_Set();
        Test_Dic_Add();
        Test_Dic_TryAdd();
        Test_Dic_Remove();
        Profiler.EndSample();
        #endregion
    }
    private void OnDisable()
    {
        Clean();
    }
    private void OnDestroy()
    {
        CollectionPool.CleanImpl();
    }

    #region Test List
    private void Test_List_Add()
    {
        for (int i = 0; i < _times; ++i)
        {
            int item = RandomItem();
            _sysList.Add(item);
            _eveList.Add(item);
        }

        Test_List();
    }
    private void Test_List_Insert()
    {
        for (int i = 0; i < _times; ++i)
        {
            int index = _random.GetInt32(0, GetListCount());
            int item = RandomItem();
            _sysList.Insert(index, item);
            _eveList.Insert(index, item);
        }

        Test_List();
    }
    private void Test_List_InsertRange()
    {
        HelperListAdd();
        int index = _random.GetInt32(0, GetListCount());
        _sysList.InsertRange(index, _helpList);
        _eveList.InsertRangeLowGC(index, _helpList);

        Test_List();
    }
    private void Test_List_Remove()
    {
        for (int i = 0; i < _times; ++i)
        {
            int item = RandomItem();
            _sysList.Remove(item);
            _eveList.Remove(item);
        }

        Test_List();
    }
    private void Test_List_RemoveAt()
    {
        for (int i = 0; i < _times; ++i)
        {
            int sIndex = _random.GetInt32(0, GetListCount());
            int item = _sysList[sIndex];
            int eIndex = _eveList.IndexOf(item);

            _sysList.RemoveAt(sIndex);
            _eveList.RemoveAt(eIndex);
        }

        Test_List();
    }
    private void Test_List_RemoveRange()
    {
        for (int i = 0; i < _times; ++i)
        {
            int index = _random.GetInt32(0, GetListCount());
            int count = _random.GetInt32(0, GetListCount() - index);

            _eveList.UpdateLowGC(_sysList);
            _sysList.RemoveRange(index, count);
            _eveList.RemoveRange(index, count);
        }

        Test_List();
    }
    private void Test_List()
    {
        if (_sysList.Count != _eveList.Count)
            throw new Exception($"_sysList.Count:{_sysList.Count} != _eveList.Count:{_eveList.Count}");

        _helpList.UpdateLowGC(_sysList);

        foreach (int item in _eveList)
            if (!_helpList.Remove(item))
                throw new Exception($"_sysList:{_sysList.JsonString()} != _eveList:{_eveList.JsonString()}");

        if (!_helpList.IsEmpty())
            throw new Exception($"_helpList.Count:{_helpList.Count} != 0");
    }
    #endregion

    #region Test Set
    private void Test_Set_Add()
    {
        for (int i = 0; i < _times; ++i)
        {
            int item = RandomItem();
            _sysSet.Add(item);
            _eveSet.Add(item);
        }

        Test_Set();
    }
    private void Test_Set_Remove()
    {
        for (int i = 0; i < _times; ++i)
        {
            int item = RandomItem();
            _sysSet.Remove(item);
            _eveSet.Remove(item);
        }

        Test_Set();
    }
    private void Test_Set_UnionWith()
    {
        HelperListAdd();
        _sysSet.UnionWith(_helpList);
        _eveSet.UnionWith(_helpList);
        Test_Set();
    }
    private void Test_Set_IntersectWith()
    {
        HelperListAdd();
        _sysSet.IntersectWith(_helpList);
        _eveSet.IntersectWith(_helpList);
        Test_Set();
    }
    private void Test_Set_ExceptWith()
    {
        HelperListAdd();
        _sysSet.ExceptWith(_helpList);
        _eveSet.ExceptWith(_helpList);
        Test_Set();
    }
    private void Test_Set_SymmetricExceptWith()
    {
        HelperListAdd();
        _sysSet.SymmetricExceptWith(_helpList);
        _eveSet.SymmetricExceptWith(_helpList);
        Test_Set();
    }
    private void Test_Set_IsSubsetOf()
    {
        Test_Set_HelperList_AddOrRemove(true);
        if (_sysSet.IsSubsetOf(_helpList) != _eveSet.IsSubsetOf(_helpList))
            ExceptionSet(nameof(ISet<int>.IsSubsetOf));

        Test_Set_HelperList_AddOrRemove(false);
        if (_sysSet.IsSubsetOf(_helpList) != _eveSet.IsSubsetOf(_helpList))
            ExceptionSet(nameof(ISet<int>.IsSubsetOf));
    }
    private void Test_Set_IsSupersetOf()
    {
        Test_Set_HelperList_AddOrRemove(true);
        if (_sysSet.IsSupersetOf(_helpList) != _eveSet.IsSupersetOf(_helpList))
            ExceptionSet(nameof(ISet<int>.IsSupersetOf));

        Test_Set_HelperList_AddOrRemove(false);
        if (_sysSet.IsSupersetOf(_helpList) != _eveSet.IsSupersetOf(_helpList))
            ExceptionSet(nameof(ISet<int>.IsSupersetOf));
    }
    private void Test_Set_IsProperSubsetOf()
    {
        Test_Set_HelperList_AddOrRemove(true);
        if (_sysSet.IsProperSubsetOf(_helpList) != _eveSet.IsProperSubsetOf(_helpList))
            ExceptionSet(nameof(ISet<int>.IsProperSubsetOf));

        Test_Set_HelperList_AddOrRemove(false);
        if (_sysSet.IsProperSubsetOf(_helpList) != _eveSet.IsProperSubsetOf(_helpList))
            ExceptionSet(nameof(ISet<int>.IsProperSubsetOf));
    }
    private void Test_Set_IsProperSupersetOf()
    {
        Test_Set_HelperList_AddOrRemove(true);
        if (_sysSet.IsProperSupersetOf(_helpList) != _eveSet.IsProperSupersetOf(_helpList))
            ExceptionSet(nameof(ISet<int>.IsProperSupersetOf));

        Test_Set_HelperList_AddOrRemove(false);
        if (_sysSet.IsProperSupersetOf(_helpList) != _eveSet.IsProperSupersetOf(_helpList))
            ExceptionSet(nameof(ISet<int>.IsProperSupersetOf));
    }
    private void Test_Set_Overlaps()
    {
        HelperListAdd();
        if (_sysSet.Overlaps(_helpList) != _eveSet.Overlaps(_helpList))
            ExceptionSet(nameof(ISet<int>.Overlaps));
    }
    private void Test_Set()
    {
        if (_sysSet.Count != _eveSet.Count)
            throw new Exception($"_sysSet.Count:{_sysSet.Count} != _eveSet.Count:{_eveSet.Count}");

        _ = _sysSet; // 平衡“_eveSet”的引用次数
        _ = _sysSet; // 平衡“_eveSet”的引用次数
        if (!_eveSet.CheckEquals())
            throw new Exception($"CheckEquals fail, _eveSet:{_eveSet.JsonString()}");

        if (!_eveSet.SetEquals(_sysSet))
            ExceptionSet(nameof(ISet<object>.SetEquals));

        if (!_sysSet.SetEquals(_eveSet))
            ExceptionSet(nameof(ISet<object>.SetEquals));
    }
    private void Test_Set_HelperList_AddOrRemove(bool addOrRemove)
    {
        _helpList.Clear();
        _helpList.AddRangeLowGC(_sysSet);
        _helpList.AddRangeLowGC(_eveSet);

        if (addOrRemove)
            for (int times = RandomTimes(), i = 0; i < times; ++i)
                _helpList.Add(RandomItem());
        else
            for (int times = RandomTimes(), i = 0; i < times; ++i)
                _helpList.Remove(RandomItem());
    }
    private void ExceptionSet(string methodName)
    {
        throw new Exception($"{methodName} fail, _sysSet:{_sysSet.JsonString()}, _eveSet:{_eveSet.JsonString()}");
    }
    #endregion

    #region Test Dic
    private void Test_Dic_Set()
    {
        for (int i = 0; i < _times; ++i)
        {
            int key = RandomItem();
            int value = RandomItem();
            _sysDic[key] = value;
            _eveDic[key] = value;
        }

        Test_Dic();
    }
    private void Test_Dic_Add()
    {
        for (int i = 0; i < _times; ++i)
        {
            int key = RandomItem();
            int value = RandomItem();
            if (_sysDic.ContainsKey(key) != _eveDic.ContainsKey(key))
                ExceptionDic(nameof(IDictionary<object, object>.Add));

            if (!_sysDic.ContainsKey(key))
                _sysDic.Add(key, value);

            if (!_eveDic.ContainsKey(key))
                _eveDic.Add(key, value);
        }

        Test_Dic();
    }
    private void Test_Dic_TryAdd()
    {
        for (int i = 0; i < _times; ++i)
        {
            int key = RandomItem();
            int value = RandomItem();
            if (_sysDic.TryAdd(key, value) != _eveDic.TryAdd(key, value))
                ExceptionDic(nameof(Dictionary<object, object>.TryAdd));
        }

        Test_Dic();
    }
    private void Test_Dic_Remove()
    {
        for (int i = 0; i < _times; ++i)
        {
            int key = RandomItem();
            _sysDic.Remove(key);
            _eveDic.Remove(key);
        }

        Test_Dic();
    }
    private void Test_Dic()
    {
        if (_sysDic.Count != _eveDic.Count)
            ExceptionDic(nameof(Test_Dic));

        _ = _sysDic; // 平衡“_eveDic”的引用次数
        _ = _sysDic; // 平衡“_eveDic”的引用次数
        if (!_eveDic.CheckEquals())
            ExceptionDic(nameof(Test_Dic));

        _helpDic.UpdateLowGC(_eveDic);
        foreach (var pair in _sysDic)
        {
            if (_helpDic.Remove(pair.Key, out int value) && pair.Value == value)
                continue;
            ExceptionDic(nameof(Test_Dic));
        }
        if (!_helpDic.IsEmpty())
            ExceptionDic(nameof(Test_Dic));

        _helpDic.UpdateLowGC(_sysDic);
        foreach (var pair in _eveDic)
        {
            if (_helpDic.Remove(pair.Key, out int value) && pair.Value == value)
                continue;
            ExceptionDic(nameof(Test_Dic));
        }
        if (!_helpDic.IsEmpty())
            ExceptionDic(nameof(Test_Dic));
    }
    private void ExceptionDic(string methodName)
    {
        throw new Exception($"{methodName} fail, _sysDic:{_sysDic.JsonString()}, _eveDic:{_eveDic.AsPair().JsonString()}");
    }
    #endregion

    private int GetListCount() => _sysList.Count + _eveList.Count >> 1;
    private void HelperListAdd()
    {
        _helpList.Clear();
        for (int i = 0; i < _times; ++i)
            _helpList.Add(RandomItem());
    }
    private int RandomItem() => _random.GetInt32(-100, 100);
    private int RandomTimes() => _random.GetInt32(-9, 11);
    private void Clean()
    {
        _helpList.Clear();
        _sysList.Clear();
        _sysSet.Clear();
        _eveList.Clear();
        _eveSet.Clear();
        _helpDic.Clear();
        _sysDic.Clear();
        _eveDic.Clear();
    }
}