using Eevee.Collection;
using Eevee.PathFind;
using Eevee.Pool;
using EeveeEditor.PathFind;
using System;
using System.Collections.Generic;
using System.Globalization;
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
    private enum GroundType : Ground
    {
        // todo eevee
    }

    private enum MoveType : MoveFunc
    {
        Fly,
        Marin,
        Water,
        Foot,
    }

    private enum CollType : CollSize
    {
        S0,
        S1,
        S2,
        S3,
        S4,
    }

    private sealed class SamplePathFindDrawProxy : IPathFindDrawProxy
    {
        public Type GroupTypeEnum => typeof(GroundType);
        public Type MoveTypeEnum => typeof(MoveType);
        public Type CollTypeEnum => typeof(CollType);
        public PathFindComponent Component => _sample._component;
        public Vector2 MinBoundary => _sample._minBoundary;
        public float GridSize => _sample._gridSize;
        public bool ValidColl(CollSize value) => value is >= (CollSize)CollType.S1 and <= (CollSize)CollType.S4;
        // todo eevee
        public Vector2Int? GetCurrentPoint(int index) => throw new NotImplementedException();
        public Vector2? GetMoveDirection(int index) => throw new NotImplementedException();
    }

    private sealed class SamplePathFindCollisionGetter : IPathFindCollisionGetter
    {
        public CollSize GetNull() => (CollSize)CollType.S0;
        public CollSize GetMax(IList<CollSize> collisions) => collisions.GetMax();
        public PathFindPeek Get(CollSize coll) => coll switch
        {
            (CollSize)CollType.S1 => default,
            (CollSize)CollType.S2 => new PathFindPeek(-1, -1, 0, 0),
            (CollSize)CollType.S3 => new PathFindPeek(-1, -1, 1, 1),
            (CollSize)CollType.S4 => new PathFindPeek(-2, -2, 1, 1),
            _ => throw new IndexOutOfRangeException($"不合法的碰撞尺寸：{coll}"),
        };
        public PathFindPeek Get(int x, int y, CollSize coll) => coll switch
        {
            (CollSize)CollType.S1 => new PathFindPeek(x, y, x, y),
            (CollSize)CollType.S2 => new PathFindPeek(x - 1, y - 1, x, y),
            (CollSize)CollType.S3 => new PathFindPeek(x - 1, y - 1, x + 1, y + 1),
            (CollSize)CollType.S4 => new PathFindPeek(x - 2, y - 2, x + 1, y + 1),
            _ => throw new IndexOutOfRangeException($"不合法的碰撞尺寸：{coll}"),
        };
    }

    private sealed class SamplePathPathFindObjectPoolGetter : IPathFindObjectPoolGetter
    {
        public List<T> ListAlloc<T>() => ListPool.Alloc<T>();
        public List<T> ListAlloc<T>(int capacity) => ListPool.Alloc<T>();
        public void Alloc<T>(ref List<T> collection) => ListPool.Alloc(ref collection);
        public void Release<T>(List<T> collection) => collection.Release2Pool();
        public void Release<T>(ref List<T> collection) => ListPool.Release(ref collection);

        public Stack<T> StackAlloc<T>() => StackPool.Alloc<T>();
        public void Release<T>(Stack<T> collection) => collection.Release2Pool();

        public void Alloc<T>(ref HashSet<T> collection) => HashSetPool.Alloc(ref collection);
        public void Release<T>(ref HashSet<T> collection) => HashSetPool.Release(ref collection);

        public Dictionary<TKey, TValue> MapAlloc<TKey, TValue>() => DictionaryPool.Alloc<TKey, TValue>();
        public void Alloc<TKey, TValue>(ref Dictionary<TKey, TValue> collection) => DictionaryPool.Alloc(ref collection);
        public void Release<TKey, TValue>(Dictionary<TKey, TValue> collection) => collection.Release2Pool();
        public void Release<TKey, TValue>(ref Dictionary<TKey, TValue> collection) => DictionaryPool.Release(ref collection);
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
        string[] lines = _map.text.Split('\n');
        int mapWidth = lines[0].Length / offset;
        int mapHeight = lines.Length;

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
            (CollSize)CollType.S1,
            (CollSize)CollType.S2,
            (CollSize)CollType.S3,
            (CollSize)CollType.S4,
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