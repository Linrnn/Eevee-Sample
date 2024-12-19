using Eevee.Collection;
using Eevee.Log;
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
    private readonly WeakList<int> _weakList = new();

    private void OnEnable()
    {
        for (int i = 0; i < 10; ++i)
            _weakList.Add(i);

        _weakList.RemoveAt(0);
        _weakList.RemoveAt(5);
        LogRelay.Log($"[Sample] RemoveAt:0,5; {Log(_weakList)}");

        _weakList.Remove(1);
        _weakList.Remove(3);
        LogRelay.Log($"[Sample] Remove:1,3; {Log(_weakList)}");

        _weakList.Insert(3, 10);
        _weakList.Insert(0, 11);
        LogRelay.Log($"[Sample] Insert:3,10;0,11; {Log(_weakList)}");

        _weakList.RemoveRange(3, 4);
        LogRelay.Log($"[Sample] RemoveRange:3,4; {Log(_weakList)}");

        _weakList.RemoveRange(1, 2);
        LogRelay.Log($"[Sample] RemoveRange:1,2; {Log(_weakList)}");

        _weakList.InsertRange(2, new[] { 20, 21, 22, 23 });
        LogRelay.Log($"[Sample] InsertRange:2,20-21-22-23; {Log(_weakList)}");

        _weakList.InsertRange(_weakList.Count - 2, new[] { 30, 31, 32, 33 });
        LogRelay.Log($"[Sample] InsertRange:count-2,30-31-32-33; {Log(_weakList)}");

        _weakList.Update0GC(new[] { 40, 41, 42, 43 });
        LogRelay.Log($"[Sample] Update0GC:40-41-42-43; {Log(_weakList)}");
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
        _weakList.InsertRange0GC(0, _stack);

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
        _weakList.Clear();
    }
    private string Log<T>(IEnumerable<T> list) => $"[{string.Join(',', list)}]";
}