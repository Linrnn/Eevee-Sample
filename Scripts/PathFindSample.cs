using Eevee.Collection;
using Eevee.PathFind;
using Eevee.Pool;
using EeveeEditor.PathFind;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using CollSize = System.SByte;
using Ground = System.Byte;
using MoveFunc = System.Byte;

/// <summary>
/// 寻路示例代码
/// </summary>
internal sealed class PathFindSample : MonoBehaviour
{
    #region 类型
    private sealed class SamplePathPathFindObjectPoolGetter : IPathFindObjectPoolGetter
    {
        private readonly object _listPoolLock = new();
        private readonly object _mapPoolLock = new();

        public List<T> ListAlloc<T>() => ListPool.Alloc<T>();
        public List<T> ListAlloc<T>(bool fromThread)
        {
            if (fromThread)
                lock (_listPoolLock)
                    return ListPool.Alloc<T>();
            return ListPool.Alloc<T>();
        }
        public List<T> ListAlloc<T>(int capacity)
        {
            var collection = ListPool.Alloc<T>();
            collection.Capacity = capacity;
            return collection;
        }
        public void Alloc<T>(ref List<T> collection) => ListPool.Alloc(ref collection);
        public void Release<T>(List<T> collection) => collection.Release2Pool();
        public void Release<T>(ref List<T> collection) => ListPool.Release(ref collection);

        public Stack<T> StackAlloc<T>() => StackPool.Alloc<T>();
        public void Release<T>(Stack<T> collection) => collection.Release2Pool();

        public void Alloc<T>(ref HashSet<T> collection) => HashSetPool.Alloc(ref collection);
        public void Release<T>(ref HashSet<T> collection) => HashSetPool.Release(ref collection);

        public Dictionary<TKey, TValue> MapAlloc<TKey, TValue>(bool fromThread)
        {
            if (fromThread)
                lock (_mapPoolLock)
                    return DictionaryPool.Alloc<TKey, TValue>();
            return DictionaryPool.Alloc<TKey, TValue>();
        }
        public void Alloc<TKey, TValue>(ref Dictionary<TKey, TValue> collection) => DictionaryPool.Alloc(ref collection);
        public void Release<TKey, TValue>(Dictionary<TKey, TValue> collection, bool fromThread)
        {
            if (fromThread)
                lock (_mapPoolLock)
                    collection.Release2Pool();
            else
                collection.Release2Pool();
        }
        public void Release<TKey, TValue>(ref Dictionary<TKey, TValue> collection) => DictionaryPool.Release(ref collection);
    }

    private sealed class SamplePathFindDrawProxy : IPathFindDrawProxy
    {
        public Type GroupTypeEnum => typeof(GroundType);
        public Type MoveTypeEnum => typeof(MoveType);
        public Type CollTypeEnum => typeof(CollType);
        public PathFindComponent Component => _sample._component;
        public Vector2 MinBoundary => _sample._minBoundary;
        public float GridSize => _sample._gridSize;
        public bool ValidColl(CollSize value) => value is >= (CollSize)CollType._1 and <= (CollSize)CollType._4;
        // todo eevee
        public Vector2Int? GetCurrentPoint(int index) => throw new NotImplementedException();
        public Vector2? GetMoveDirection(int index) => throw new NotImplementedException();
    }

    private sealed class SamplePathFindCollisionGetter : IPathFindCollisionGetter
    {
        public CollSize GetNull() => (CollSize)CollType._0;
        public CollSize GetMax(IList<CollSize> collisions) => collisions.GetMax();
        public PathFindPeek Get(CollSize coll) => coll switch
        {
            (CollSize)CollType._1 => default,
            (CollSize)CollType._2 => new PathFindPeek(-1, -1, 0, 0),
            (CollSize)CollType._3 => new PathFindPeek(-1, -1, 1, 1),
            (CollSize)CollType._4 => new PathFindPeek(-2, -2, 1, 1),
            _ => throw new IndexOutOfRangeException($"不合法的碰撞尺寸：{coll}"),
        };
        public PathFindPeek Get(int x, int y, CollSize coll) => coll switch
        {
            (CollSize)CollType._1 => new PathFindPeek(x, y, x, y),
            (CollSize)CollType._2 => new PathFindPeek(x - 1, y - 1, x, y),
            (CollSize)CollType._3 => new PathFindPeek(x - 1, y - 1, x + 1, y + 1),
            (CollSize)CollType._4 => new PathFindPeek(x - 2, y - 2, x + 1, y + 1),
            _ => throw new IndexOutOfRangeException($"不合法的碰撞尺寸：{coll}"),
        };
    }

    private enum GroundType : Ground
    {
        None = 0,
        Any = 1,
        Walk = 2,
        Fly = 4,
        Cons = 8,
        Peon = 16,
        Blight = 32,
        Water = 64,
        Amp = 128,
    }

    private enum MoveType : MoveFunc
    {
        Fly = GroundType.Fly,
        Marin = GroundType.Amp,
        Water = GroundType.Water | GroundType.Amp,
        Foot = GroundType.Walk | GroundType.Amp,
    }

    private enum CollType : CollSize
    {
        _0,
        _1,
        _2,
        _3,
        _4,
    }
    #endregion

    #region 序列化字段
    [SerializeField] private TextAsset _map;
    [SerializeField] private Vector2 _minBoundary;
    [SerializeField] private float _gridSize;
    #endregion

    #region 运行时缓存
    private static PathFindSample _sample;
    private PathFindComponent _component;
    #endregion

    private void OnEnable()
    {
        const int offset = 2;
        string[] lines = _map.text.Split('\n', '\r').Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
        int mapWidth = lines.Length;
        int mapHeight = lines[0].Length / offset;

        Ground[,] nodes = new Ground[mapWidth, mapHeight];
        MoveFunc[][] moveTypeGroups =
        {
            new[]
            {
                (MoveFunc)MoveType.Fly,
            },
            new[]
            {
                (MoveFunc)MoveType.Marin,
                (MoveFunc)MoveType.Water,
                (MoveFunc)MoveType.Foot,
            },
        };
        CollSize[] collTypes =
        {
            (CollSize)CollType._1,
            (CollSize)CollType._2,
            (CollSize)CollType._3,
            (CollSize)CollType._4,
        };
        var getters = new PathFindGetters(new SamplePathFindCollisionGetter(), new SamplePathPathFindObjectPoolGetter());

        for (int i = 0; i < mapWidth; ++i)
        {
            string line = lines[i];
            for (int j = 0; j < mapHeight; ++j)
            {
                var span = line.AsSpan(j * offset, offset);
                byte.TryParse(span, NumberStyles.AllowHexSpecifier, null, out Ground ground);
                nodes[i, j] = ground;
            }
        }

        var component = new PathFindComponent(nodes, moveTypeGroups, collTypes, in getters);
        component.Initialize(true, true, true, true);

        _sample = this;
        _component = component;
    }
    private void OnDisable()
    {
        _sample = null;
        _component = null;
    }
}