using Eevee.Collection;
using Eevee.Fixed;
using Eevee.PathFind;
using Eevee.Pool;
using Eevee.Utils;
using EeveeEditor;
using EeveeEditor.PathFind;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using CollSize = System.SByte;
using Ground = System.Byte;
using MoveFunc = System.Byte;
using SRandom = System.Random;

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
        public bool ValidColl(CollSize value) => value is >= (CollSize)CollType._1x1 and <= (CollSize)CollType._4x4;
        public Vector2Int? GetCurrentPoint(int index) => _sample._runtime.TryGetValue(index, out var runtime) ? _sample.Position2Point(in runtime.Position) : null;
        public Vector2? GetMoveDirection(int index) => _sample._runtime.TryGetValue(index, out var runtime) ? (Vector2)runtime.Direction() : null;
    }

    private sealed class SamplePathFindCollisionGetter : IPathFindCollisionGetter
    {
        public CollSize GetNull() => (CollSize)CollType._0x0;
        public CollSize GetMax(IList<CollSize> collisions) => collisions.GetMax();
        public PathFindPeek Get(CollSize coll) => coll switch
        {
            (CollSize)CollType._1x1 => default,
            (CollSize)CollType._2x2 => new PathFindPeek(-1, -1, 0, 0),
            (CollSize)CollType._3x3 => new PathFindPeek(-1, -1, 1, 1),
            (CollSize)CollType._4x4 => new PathFindPeek(-2, -2, 1, 1),
            _ => throw new IndexOutOfRangeException($"不合法的碰撞尺寸：{coll}"),
        };
        public PathFindPeek Get(int x, int y, CollSize coll) => coll switch
        {
            (CollSize)CollType._1x1 => new PathFindPeek(x, y, x, y),
            (CollSize)CollType._2x2 => new PathFindPeek(x - 1, y - 1, x, y),
            (CollSize)CollType._3x3 => new PathFindPeek(x - 1, y - 1, x + 1, y + 1),
            (CollSize)CollType._4x4 => new PathFindPeek(x - 2, y - 2, x + 1, y + 1),
            _ => throw new IndexOutOfRangeException($"不合法的碰撞尺寸：{coll}"),
        };
    }

    private readonly struct Runtime
    {
        internal readonly MoveType MoveType;
        internal readonly CollType CollType;
        private readonly Vector2D _lastPosition;
        internal readonly Vector2D Position;
        internal readonly PathHandle Long;
        internal Runtime(MoveType moveType, CollType collType, in Vector2D position)
        {
            MoveType = moveType;
            CollType = collType;
            _lastPosition = position;
            Position = position;
            Long = new PathHandle(null);
        }
        internal Runtime(in Runtime other, in PathHandle path)
        {
            MoveType = other.MoveType;
            CollType = other.CollType;
            _lastPosition = other._lastPosition;
            Position = other.Position;
            Long = path;
        }
        internal Runtime(in Runtime other, in Vector2D lastPosition, in Vector2D position)
        {
            MoveType = other.MoveType;
            CollType = other.CollType;
            _lastPosition = lastPosition;
            Position = position;
            Long = other.Long;
        }
        internal Vector2D Direction() => Position - _lastPosition;
    }

    private readonly struct PathHandle
    {
        internal readonly List<Vector2D> Path;
        private readonly int _index;
        internal PathHandle(object _)
        {
            Path = new List<Vector2D>();
            _index = -1;
        }
        private PathHandle(int index, List<Vector2D> path)
        {
            Path = path;
            _index = index;
        }
        internal PathHandle Start()
        {
            var path = Path;
            path.Clear();
            return new PathHandle(0, path);
        }
        internal PathHandle Next() => new(_index + 1, Path);
        internal Vector2D? NextPosition() => _index >= 0 && _index < Path.Count ? Path[_index] : null;
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
        _0x0,
        _1x1,
        _2x2,
        _3x3,
        _4x4,
    }

    private enum Operate
    {
        None,
        Insert,
        Remove,
        Find,
    }
    #endregion

    #region 序列化字段
    [Header("随机权重")] [SerializeField] private Weight<MoveType>[] _moveTypeWeights;
    [SerializeField] private Weight<CollType>[] _collTypeWeights;
    [SerializeField] private Weight<Operate>[] _operateWeights;

    [Header("地图配置")] [SerializeField] private TextAsset _map;
    [SerializeField] private Vector2 _minBoundary;
    [SerializeField] private float _gridSize;

    [Header("运行时数据")] [SerializeField] private int _seed;
    [SerializeField] private int _countLimit;
    [SerializeField] private Vector2 _speedRange;
    [SerializeField] private Fixed64 _arrive;
    [ReadOnly] [SerializeField] private int _indexAllocator;
    [ReadOnly] [SerializeField] private int _indexCount;
    #endregion

    #region 运行时缓存
    private static PathFindSample _sample;
    private PathFindComponent _component;
    private readonly SamplePathFindCollisionGetter _collisionGetter = new();
    private readonly List<Vector2DInt16> _points = new();

    private Dictionary<int, Runtime> _runtime;
    private SRandom _random;
    private List<int> _indexes;
    #endregion

    private void OnEnable()
    {
        #region 解析地图/初始化数据
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
            (CollSize)CollType._1x1,
            (CollSize)CollType._2x2,
            (CollSize)CollType._3x3,
            (CollSize)CollType._4x4,
        };
        var getters = new PathFindGetters(_collisionGetter, new SamplePathPathFindObjectPoolGetter());

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
        #endregion

        var component = new PathFindComponent(nodes, moveTypeGroups, collTypes, in getters);
        component.Initialize(true, true, true, true);

        _indexAllocator = 0;
        _sample = this;
        _component = component;
        _runtime = new Dictionary<int, Runtime>();
        _random = new SRandom(_seed);
        _indexes = new List<int>();
    }
    private void Update()
    {
        var weight = Weight<Operate>.Get(_operateWeights, _random);
        switch (weight.Key)
        {
            case Operate.Insert: InsertElement(); break;
            case Operate.Remove: RemoveElement(); break;
            case Operate.Find: FindElement(); break;
        }

        MoveElement();
        _indexCount = _indexes.Count;
    }
    private void OnDisable()
    {
        _sample = null;
        _component = null;
    }

    private void InsertElement()
    {
        if (_indexes.Count >= _countLimit)
            return;

        var moveTypeWeight = Weight<MoveType>.Get(_moveTypeWeights, _random);
        var collTypeWeight = Weight<CollType>.Get(_collTypeWeights, _random);
        var moveType = moveTypeWeight.Key;
        var collType = collTypeWeight.Key;

        var point = RandomPoint(PathFindExt.EmptyIndex, moveType, collType);
        var collRange = _collisionGetter.Get(point.X, point.Y, (CollSize)collType);

        int index = ++_indexAllocator;
        _component.SetMoveable(index, (MoveFunc)moveType, collRange);
        _runtime.Add(index, new Runtime(moveType, collType, Point2Position(point)));
        _indexes.Add(index);
        _indexes.Sort(Comparable<int>.Default);
    }
    private void MoveElement()
    {
        foreach (int index in _indexes)
        {
            var runtime = _runtime[index];
            var longPath = runtime.Long;
            var nextPosition = longPath.NextPosition();
            if (nextPosition == null)
                continue;

            float speed = _speedRange.x + (float)_random.NextDouble() * (_speedRange.y - _speedRange.x);
            var delta = nextPosition.Value - runtime.Position;
            var offset = delta.ScaleMagnitude(speed);
            if (!ChangePosition(index, in runtime, offset))
                continue;

            var newDelta = nextPosition.Value - runtime.Position;
            if (newDelta.SqrMagnitude() <= _arrive)
                _runtime[index] = new Runtime(in runtime, longPath.Next());
        }
    }
    private void RemoveElement()
    {
        if (_indexes.IsEmpty())
            return;

        int idx = _random.Next(0, _indexes.Count);
        int index = _indexes[idx];
        var runtime = _runtime[index];
        var point = Position2Point(in runtime.Position);
        var collRange = _collisionGetter.Get(point.X, point.Y, (CollSize)runtime.CollType);

        _component.ResetMoveable(index, (MoveFunc)runtime.MoveType, collRange);
        _runtime.Remove(index);
        _indexes.RemoveAt(idx);
    }
    private void FindElement()
    {
        int idx = _random.Next(0, _indexes.Count);
        int index = _indexes[idx];
        var runtime = _runtime[index];

        var moveType = runtime.MoveType;
        var collType = runtime.CollType;
        var endPoint = RandomPoint(index, moveType, collType);
        var sePoint = new PathFindPoint(Position2Point(in runtime.Position), endPoint);
        var range = new PathFindPeek(_component.GetSize());

        var input = new PathFindInput(index, PathFindExt.EmptyIndex, true, (MoveFunc)moveType, (CollSize)collType, range, sePoint);
        var output = new PathFindOutput(_points);
        _points.Clear();
        _component.GetLongPath(input, ref output);

        var longPath = runtime.Long.Start();
        foreach (var point in _points)
            longPath.Path.Add(Position2Point(point));
        _runtime[index] = new Runtime(in runtime, longPath);
    }

    private Vector2DInt16 RandomPoint(int index, MoveType moveType, CollType collType)
    {
        var size = _component.GetSize();
        var collRange = _collisionGetter.Get((CollSize)collType);
        while (true)
        {
            int px = _random.Next(Math.Abs(collRange.Min.X), size.X - Math.Abs(collRange.Max.X) - 1);
            int py = _random.Next(Math.Abs(collRange.Min.Y), size.Y - Math.Abs(collRange.Max.Y) - 1);
            var point = new Vector2DInt16(px, py);
            if (_component.CanStand(point, (MoveFunc)moveType, (CollSize)collType, index))
                return point;
        }
    }
    private bool ChangePosition(int index, in Runtime runtime, in Vector2D offset)
    {
        MoveFunc moveFunc = (MoveFunc)runtime.MoveType;
        CollSize collSize = (CollSize)runtime.CollType;

        var oldPosition = runtime.Position;
        var newPosition = oldPosition + offset;
        var newPoint = Position2Point(in newPosition);
        if (!_component.CanStand(newPoint, moveFunc, collSize, index))
            return false;

        var oldPoint = Position2Point(in oldPosition);
        var oldCollRange = _collisionGetter.Get(oldPoint.X, oldPoint.Y, collSize);
        var newCollRange = _collisionGetter.Get(newPoint.X, newPoint.Y, collSize);

        _component.ResetMoveable(index, moveFunc, oldCollRange);
        _component.SetMoveable(index, moveFunc, newCollRange);
        _runtime[index] = new Runtime(in runtime, in oldPosition, in newPosition);
        return true;
    }

    public Vector2DInt16 Position2Point(in Vector2D position) => Position2Point(position.X, position.Y);
    public Vector2DInt16 Position2Point(Fixed64 x, Fixed64 y) => new()
    {
        X = (short)((x - _minBoundary.x) / _gridSize).Floor(),
        Y = (short)((y - _minBoundary.y) / _gridSize).Floor(),
    };
    public Vector2D Point2Position(Vector2DInt16 point) => Point2Position(point.X, point.Y);
    public Vector2D Point2Position(int x, int y) => new()
    {
        X = x * _gridSize + _minBoundary.x + _gridSize * 0.5F,
        Y = y * _gridSize + _minBoundary.y + _gridSize * 0.5F,
    };
}