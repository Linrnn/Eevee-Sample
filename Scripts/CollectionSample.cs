using Eevee.Collection;
using Eevee.Debug;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Collection示例代码
/// </summary>
public sealed class CollectionSample : MonoBehaviour
{
    private readonly List<int> _list = new();
    private readonly Stack<int> _stack = new();
    private readonly HashSet<int> _hashSet = new();
    [SerializeField] private WeakOrderList<int> _weakOrderList = new();

    private void OnEnable()
    {
        for (int i = 0; i < 10; ++i)
            _weakOrderList.Add(i);

        _weakOrderList.RemoveAt(0);
        _weakOrderList.RemoveAt(5);
        LogRelay.Log($"[Sample] RemoveAt:0,5; {Log(_weakOrderList)}");

        _weakOrderList.Remove(1);
        _weakOrderList.Remove(3);
        LogRelay.Log($"[Sample] Remove:1,3; {Log(_weakOrderList)}");

        _weakOrderList.Insert(3, 10);
        _weakOrderList.Insert(0, 11);
        LogRelay.Log($"[Sample] Insert:3,10;0,11; {Log(_weakOrderList)}");

        _weakOrderList.RemoveRange(3, 4);
        LogRelay.Log($"[Sample] RemoveRange:3,4; {Log(_weakOrderList)}");

        _weakOrderList.RemoveRange(1, 2);
        LogRelay.Log($"[Sample] RemoveRange:1,2; {Log(_weakOrderList)}");

        _weakOrderList.InsertRange(2, new[] { 20, 21, 22, 23 });
        LogRelay.Log($"[Sample] InsertRange:2,20-21-22-23; {Log(_weakOrderList)}");

        _weakOrderList.InsertRange(_weakOrderList.Count - 2, new[] { 30, 31, 32, 33 });
        LogRelay.Log($"[Sample] InsertRange:count-2,30-31-32-33; {Log(_weakOrderList)}");

        _weakOrderList.Update0GC(new[] { 40, 41, 42, 43 });
        LogRelay.Log($"[Sample] Update0GC:40-41-42-43; {Log(_weakOrderList)}");
    }
    private void Update()
    {
        Profiler.BeginSample("CollectionSample.Update");
        Clean();

        for (int i = 0; i < 10; ++i)
            _stack.Push(i);

        _list.AddRange0GC(_stack);
        _list.RemoveRange0GC(_stack);
        _stack.GetFirst0GC();
        _hashSet.Union0GC(_stack);
        _weakOrderList.InsertRange0GC(0, _stack);

        Profiler.EndSample();
    }
    private void OnDisable()
    {
        Clean();
    }

    private void Clean()
    {
        _list.Clear();
        _stack.Clear();
        _hashSet.Clear();
        _weakOrderList.Clear();
    }
    private string Log<T>(IEnumerable<T> list) => $"[{string.Join(',', list)}]";
}