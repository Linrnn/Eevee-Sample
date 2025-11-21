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
        private readonly CollectionPool _collectionPool = new();

        public List<T> AllocList<T>(bool needLock)
        {
            if (needLock)
                lock (_collectionPool)
                    return CollectionPool<List<T>>.Alloc(_collectionPool);
            return CollectionPool<List<T>>.Alloc(_collectionPool);
        }
        public void ReleaseList<T>(List<T> collection) => CollectionPool<List<T>>.Release(collection, _collectionPool);

        public Stack<T> AllocStack<T>() => CollectionPool<Stack<T>>.Alloc(_collectionPool);
        public void ReleaseStack<T>(Stack<T> collection) => CollectionPool<Stack<T>>.Release(collection, _collectionPool);

        public HashSet<T> AllocSet<T>() => CollectionPool<HashSet<T>>.Alloc(_collectionPool);
        public void ReleaseSet<T>(HashSet<T> collection) => CollectionPool<HashSet<T>>.Release(collection, _collectionPool);

        public Dictionary<TKey, TValue> AllocMap<TKey, TValue>(bool needLock)
        {
            if (needLock)
                lock (_collectionPool)
                    return CollectionPool<Dictionary<TKey, TValue>>.Alloc(_collectionPool);
            return CollectionPool<Dictionary<TKey, TValue>>.Alloc(_collectionPool);
        }
        public void ReleaseMap<TKey, TValue>(Dictionary<TKey, TValue> collection, bool needLock)
        {
            if (needLock)
                lock (_collectionPool)
                    CollectionPool<Dictionary<TKey, TValue>>.Release(collection, _collectionPool);
            else
                CollectionPool<Dictionary<TKey, TValue>>.Release(collection, _collectionPool);
        }
    }

    private sealed class SamplePathFindDrawProxy : IPathFindDrawProxy
    {
        public Type GroupTypeEnum => typeof(GroundType);
        public Type MoveTypeEnum => typeof(MoveType);
        public Type CollTypeEnum => typeof(CollType);
        public PathFindComponent Component => _instance._component;
        public Vector2 MinBoundary => _instance._minBoundary;
        public float GridSize => _instance._gridSize;
        public float GridOffset => _instance._gridOffset;
        public bool ValidColl(CollSize value) => value is >= (CollSize)CollType._1x1 and <= (CollSize)CollType._4x4;
        public Vector2Int? GetCurrentPoint(int index) => _instance._moveables.TryGetValue(index, out var runtime) ? _instance.Position2Point(in runtime.Position) : null;
        public Vector2? GetMoveDirection(int index) => _instance._moveables.TryGetValue(index, out var runtime) ? (Vector2)runtime.Direction() : null;
    }

    private sealed class SamplePathFindTerrainGetter : IPathFindTerrainGetter
    {
        private readonly Ground[,] _nodes;
        internal SamplePathFindTerrainGetter(Ground[,] nodes)
        {
            Width = nodes.GetLength(0);
            Height = nodes.GetLength(1);
            _nodes = nodes;
        }

        public int Width { get; }
        public int Height { get; }
        public Ground Get(int x, int y) => _nodes[x, y];
        public void Set(int x, int y, Ground groupType) => _nodes[x, y] = groupType;
    }

    private sealed class SamplePathFindCollisionGetter : IPathFindCollisionGetter
    {
        public CollSize GetNull() => (CollSize)CollType._0x0;
        public CollSize GetMax(IReadOnlyList<CollSize> collisions) => collisions.GetMax();
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
        internal Runtime(in Runtime other, in Vector2D position)
        {
            MoveType = other.MoveType;
            CollType = other.CollType;
            _lastPosition = other.Position;
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

    [Flags]
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
        MoveableInsert,
        MoveableRemove,
        MoveableFind,
        ObstacleInsert,
        ObstacleRemove,
        PortalInsert,
        PortalRemove,
    }
    #endregion

    #region 序列化字段
    [Header("随机权重")] [SerializeField] private Weight<MoveType>[] _moveTypeWeights;
    [SerializeField] private Weight<CollType>[] _collTypeWeights;
    [SerializeField] private Weight<Operate>[] _operateWeights;
    [SerializeField] private Weight<GroundType>[] _obstacleWeights;

    [Header("地图配置")] [SerializeField] private TextAsset _map;
    [SerializeField] private Vector2 _minBoundary;
    [SerializeField] private float _gridSize;
    [SerializeField] private float _gridOffset;

    [Header("运行时数据")] [SerializeField] private bool _run = true;
    [SerializeField] private int _seed;
    [SerializeField] private uint _moveableLimit;
    [SerializeField] private uint _obstacleLimit;
    [SerializeField] private uint _portalLimit;
    [SerializeField] private Vector2 _speedRange;
    [SerializeField] private Vector2Int _obstacleRange;
    [SerializeField] private Fixed64 _arrive;
    [ReadOnly] [SerializeField] private int _indexAllocator;
    [ReadOnly] [SerializeField] private int _indexCount;
    #endregion

    #region 运行时缓存
    private static PathFindSample _instance;
    private PathFindComponent _component;
    private readonly SamplePathFindCollisionGetter _collisionGetter = new();
    private readonly List<Vector2DInt16> _pathPoints = new();
    private readonly List<int> _pathPortalIndexes = new();

    private SRandom _random;
    private Dictionary<int, Runtime> _moveables;
    private Dictionary<int, Dictionary<Vector2DInt16, Ground>> _obstacles;
    private List<int> _moveableIndexes;
    private List<int> _obstacleIndexes;
    private List<int> _portalIndexes;
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
        var getters = new PathFindGetters(new SamplePathFindTerrainGetter(nodes), _collisionGetter, new SamplePathPathFindObjectPoolGetter());

        for (int i = 0; i < mapWidth; ++i)
        {
            string line = lines[i];
            for (int j = 0; j < mapHeight; ++j)
            {
                var span = line.AsSpan(j * offset, offset);
                byte.TryParse(span, NumberStyles.AllowHexSpecifier, null, out Ground ground);
                nodes[i, j] = (Ground)(~ground);
            }
        }
        #endregion

        var component = new PathFindComponent(moveTypeGroups, collTypes, in getters);
        component.Initialize(true, true, true, true);

        _indexAllocator = 0;
        _instance = this;
        _component = component;
        _random = new SRandom(_seed);
        _moveables = new Dictionary<int, Runtime>();
        _obstacles = new Dictionary<int, Dictionary<Vector2DInt16, Ground>>();
        _moveableIndexes = new List<int>();
        _obstacleIndexes = new List<int>();
        _portalIndexes = new List<int>();
    }
    private void Update()
    {
        if (!_run)
            return;

        var weight = Weight<Operate>.Get(_operateWeights, _random);
        switch (weight.Key)
        {
            case Operate.MoveableInsert: MoveableInsert(); break;
            case Operate.MoveableRemove: MoveableRemove(); break;
            case Operate.MoveableFind: MoveableFind(); break;
            case Operate.ObstacleInsert: ObstacleInsert(); break;
            case Operate.ObstacleRemove: ObstacleRemove(); break;
            case Operate.PortalInsert: PortalInsert(); break;
            case Operate.PortalRemove: PortalRemove(); break;
        }

        MoveableMove();
        _indexCount = _moveableIndexes.Count + _obstacleIndexes.Count + _portalIndexes.Count;
    }
    private void OnDisable()
    {
        _instance = null;
        _component = null;
    }

    private void MoveableInsert()
    {
        if (_moveableIndexes.Count >= _moveableLimit)
            return;

        var moveTypeWeight = Weight<MoveType>.Get(_moveTypeWeights, _random);
        var collTypeWeight = Weight<CollType>.Get(_collTypeWeights, _random);
        var moveType = moveTypeWeight.Key;
        var collType = collTypeWeight.Key;

        var point = RandomPoint(PathFindExt.EmptyIndex, moveType, collType);
        var collRange = _collisionGetter.Get(point.X, point.Y, (CollSize)collType);

        int index = ++_indexAllocator;
        _component.SetMoveable(index, (MoveFunc)moveType, collRange);
        _moveables.Add(index, new Runtime(moveType, collType, Point2Position(point)));
        _moveableIndexes.Add(index);
        _moveableIndexes.Sort(Comparable<int>.Default);
    }
    private void MoveableRemove()
    {
        if (_moveableIndexes.IsEmpty())
            return;

        int idx = _random.Next(0, _moveableIndexes.Count);
        int index = _moveableIndexes[idx];
        var runtime = _moveables[index];
        var point = Position2Point(in runtime.Position);
        var collRange = _collisionGetter.Get(point.X, point.Y, (CollSize)runtime.CollType);

        _component.ResetMoveable(index, (MoveFunc)runtime.MoveType, collRange);
        _moveables.Remove(index);
        _moveableIndexes.RemoveAt(idx);
    }
    private void MoveableFind()
    {
        if (_moveableIndexes.IsEmpty())
            return;

        int idx = _random.Next(0, _moveableIndexes.Count);
        int index = _moveableIndexes[idx];
        var runtime = _moveables[index];

        var moveType = runtime.MoveType;
        var collType = runtime.CollType;
        var startPoint = Position2Point(in runtime.Position);
        var endPoint = RandomEndPoint(index, startPoint, moveType, collType);
        var sePoint = new PathFindPoint(startPoint, endPoint);
        var range = new PathFindPeek(_component.GetSize());

        var input = new PathFindInput(index, PathFindExt.EmptyIndex, true, (MoveFunc)moveType, (CollSize)collType, range, sePoint);
        var extra = new PathFindLongInput(false);
        var output = new PathFindOutput(_pathPoints, _pathPortalIndexes);
        _pathPoints.Clear();
        _pathPortalIndexes.Clear();
        _component.GetLongPath(in input, in extra, ref output);

        var longPath = runtime.Long.Start();
        foreach (var point in _pathPoints)
            longPath.Path.Add(Point2Position(point));
        _moveables[index] = new Runtime(in runtime, longPath);
    }
    private void MoveableMove()
    {
        foreach (int index in _moveableIndexes)
        {
            const PathFindFunc func = PathFindFunc.JPSPlus;
            PathFindDiagnosis.RemoveNextPoint(func, index);

            var runtime = _moveables[index];
            var longPath = runtime.Long;
            var nextPosition = longPath.NextPosition();
            if (nextPosition == null)
                continue;

            float speed = _speedRange.x + (float)_random.NextDouble() * (_speedRange.y - _speedRange.x);
            var delta = nextPosition.Value - runtime.Position;
            var offset = delta.ScaleMagnitude(speed);
            if (!ChangePosition(index, in runtime, offset))
                continue;

            var newRuntime = _moveables[index]; // “ChangePosition”会修改“_runtime”
            var newDelta = nextPosition.Value - newRuntime.Position;
            if (newDelta.SqrMagnitude() <= _arrive)
                _moveables[index] = new Runtime(in newRuntime, longPath.Next());

            if (_moveables[index].Long.NextPosition() is { } nextPoint) // “PathHandle.Next()”会修改“_runtime”
                PathFindDiagnosis.SetNextPoint(func, index, Position2Point(in nextPoint));
            else
                PathFindDiagnosis.RemoveNextPoint(func, index);
        }
    }

    private void ObstacleInsert()
    {
        if (_obstacleIndexes.Count >= _obstacleLimit)
            return;

        var obstacleWeight = Weight<GroundType>.Get(_obstacleWeights, _random);
        int width = _random.Next(_obstacleRange.x, _obstacleRange.y + 1);
        int height = _random.Next(_obstacleRange.x, _obstacleRange.y + 1);
        var obstacle = new Dictionary<Vector2DInt16, Ground>(width * height);
        var range = RandomPoint(width, height);
        for (int i = range.Min.X; i <= range.Max.X; ++i)
        for (int j = range.Min.Y; j < range.Max.Y; ++j)
            obstacle.Add(new Vector2DInt16(i, j), (Ground)obstacleWeight.Key);

        int index = ++_indexAllocator;
        _component.SetObstacle(index, obstacle);
        _obstacles.Add(index, obstacle);
        _obstacleIndexes.Add(index);
        _obstacleIndexes.Sort(Comparable<int>.Default);
    }
    private void ObstacleRemove()
    {
        if (_obstacleIndexes.IsEmpty())
            return;

        int idx = _random.Next(0, _obstacleIndexes.Count);
        int index = _obstacleIndexes[idx];
        var runtime = _obstacles[index];

        _component.ResetObstacle(index, runtime);
        _obstacles.Remove(index);
        _obstacleIndexes.RemoveAt(idx);
    }

    private void PortalInsert()
    {
        if (_portalIndexes.Count >= _portalLimit)
            return;

        var size = _component.GetSize();
        int sx = _random.Next(size.X);
        int sy = _random.Next(size.Y);
        int ex = _random.Next(size.X);
        int ey = _random.Next(size.Y);

        int index = ++_indexAllocator;
        _component.AddPortal(index, new PathFindPoint(sx, sy, ex, ey));
        _portalIndexes.Add(index);
        _portalIndexes.Sort(Comparable<int>.Default);
    }
    private void PortalRemove()
    {
        if (_portalIndexes.IsEmpty())
            return;

        int idx = _random.Next(0, _portalIndexes.Count);
        int index = _portalIndexes[idx];

        _component.RemovePortal(index);
        _portalIndexes.RemoveAt(idx);
    }

    private Vector2DInt16 RandomEndPoint(int index, Vector2DInt16 start, MoveType moveType, CollType collType)
    {
        while (true)
        {
            var end = RandomPoint(index, moveType, collType);
            if (_component.CheckArea(start, end, (MoveFunc)moveType, (CollSize)collType))
                return end;
        }
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
    private PathFindPeek RandomPoint(int width, int height)
    {
        for (var size = _component.GetSize();;)
        {
            int px = _random.Next(size.X - width);
            int py = _random.Next(size.Y - height);
            var range = new PathFindPeek(px, py, px + width, py + height);
            if (_component.CanStand(range))
                return range;
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
        _moveables[index] = new Runtime(in runtime, in newPosition);
        return true;
    }

    public Vector2DInt16 Position2Point(in Vector2D position) => new()
    {
        X = (short)((position.X - _minBoundary.x) / _gridSize).Floor(),
        Y = (short)((position.Y - _minBoundary.y) / _gridSize).Floor(),
    };
    public Vector2D Point2Position(Vector2DInt16 point) => new()
    {
        X = point.X * _gridSize + _minBoundary.x + _gridSize * 0.5F,
        Y = point.Y * _gridSize + _minBoundary.y + _gridSize * 0.5F,
    };
}