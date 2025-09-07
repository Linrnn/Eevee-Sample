using Eevee.Collection;
using Eevee.Fixed;
using Eevee.QuadTree;
using Eevee.Utils;
using EeveeEditor;
using EeveeEditor.QuadTree;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SRandom = System.Random;

/// <summary>
/// 四叉树示例代码
/// </summary>
internal sealed class QuadTreeSample : MonoBehaviour
{
    #region 类型
    private sealed class SampleQuadTreeDrawProxy : IQuadTreeDrawProxy
    {
        public Type TreeEnum => typeof(Func);
        public QuadTreeManager Manager => _sample._manager;
    }

    private readonly struct Runtime
    {
        private readonly Func _func;
        private readonly Vector2DInt _lastPosition;
        internal readonly AABB2DInt Shape;
        internal Runtime(int treeId, in AABB2DInt shape)
        {
            _func = (Func)treeId;
            _lastPosition = shape.Center();
            Shape = shape;
        }
        internal Runtime(in Runtime other, Vector2DInt lastPosition, in AABB2DInt shape)
        {
            _func = other._func;
            _lastPosition = lastPosition;
            Shape = shape;
        }
        internal int TreeId() => (int)_func;
        internal Vector2DInt Direction() => Shape.Center() - _lastPosition;
    }

    private enum Func
    {
        None,
        Unit,
        GuardBox,
        Shop,
        Item,
        GuardArea,
        Region,
    }

    private enum Operate
    {
        None,
        Insert,
        Remove,
    }
    #endregion

    #region 序列化字段
    [Header("随机权重")] [SerializeField] private Weight<Func>[] _funcWeights;
    [SerializeField] private Weight<Operate>[] _operateWeights;

    [Header("四叉树配置")] [SerializeField] private int _scale;
    [SerializeField] private int _depthCount;
    [SerializeField] private Vector2DInt _center;
    [SerializeField] private Vector2DInt _extents;

    [Header("运行时数据")] [SerializeField] private int _seed;
    [SerializeField] private int _countLimit;
    [SerializeField] private Vector2 _speedRange;
    [SerializeField] private bool _removeEmptyNode;
    [ReadOnly] [SerializeField] private int _indexAllocator;
    [ReadOnly] [SerializeField] private int _indexCount;
    #endregion

    #region 运行时缓存
    private static QuadTreeSample _sample;
    private QuadTreeManager _manager;

    private readonly Dictionary<Func, QuadTreeConfig> _configs = new()
    {
        [Func.Unit] = QuadTreeConfig.Build<DynamicQuadTree>((int)Func.Unit, QuadTreeShape.Circle, new Vector2DInt(64, 64)),
        [Func.GuardBox] = QuadTreeConfig.Build<DynamicQuadTree>((int)Func.GuardBox, QuadTreeShape.AABB, new Vector2DInt(64, 64)),
        [Func.Shop] = QuadTreeConfig.Build<MeshQuadTree>((int)Func.Shop, QuadTreeShape.Circle, new Vector2DInt(512, 512)),
        [Func.Item] = QuadTreeConfig.Build<MeshQuadTree>((int)Func.Item, QuadTreeShape.AABB, new Vector2DInt(16, 16)),
        [Func.GuardArea] = QuadTreeConfig.Build<LooseQuadTree>((int)Func.GuardArea, QuadTreeShape.Circle, new Vector2DInt(1024, 1024)),
        [Func.Region] = QuadTreeConfig.Build<LooseQuadTree>((int)Func.Region, QuadTreeShape.AABB, new Vector2DInt(256, 256)),
    };
    private SRandom _random;
    private Dictionary<int, Runtime> _runtime;
    private List<int> _indexes;
    #endregion

    private void OnEnable()
    {
        _indexAllocator = 0;
        _sample = this;
        _manager = new QuadTreeManager(_scale, _depthCount, new AABB2DInt(_center, _extents), _configs.Values.ToArray());
        _random = new SRandom(_seed);
        _runtime = new Dictionary<int, Runtime>();
        _indexes = new List<int>();
    }
    private void Update()
    {
        var weight = Weight<Operate>.Get(_operateWeights, _random);
        switch (weight.Key)
        {
            case Operate.Insert: InsertElement(); break;
            case Operate.Remove: RemoveElement(); break;
        }

        MoveElement();
        if (_removeEmptyNode)
            _manager.RemoveEmptyNode();
        _indexCount = _indexes.Count;
    }
    private void OnDisable()
    {
        _sample = null;
        _manager.Clean();
        _manager = null;
    }

    private void InsertElement()
    {
        if (_indexes.Count >= _countLimit)
            return;

        var boundary = _manager.MaxBoundary;
        var weight = Weight<Func>.Get(_funcWeights, _random);
        var config = _configs[weight.Key];

        int width = _random.Next(config.Extents.X >> 1, config.Extents.X << 2);
        int height = _random.Next(config.Extents.Y >> 1, config.Extents.Y << 2);
        int cx = _random.Next(boundary.Left() + width, boundary.Right() - width);
        int cy = _random.Next(boundary.Bottom() + height, boundary.Top() - height);
        var shape = config.Shape is QuadTreeShape.Circle ? new AABB2DInt(cx, cy, Math.Min(width, height)) : new AABB2DInt(cx, cy, width, height);

        int index = ++_indexAllocator;
        _manager.Insert(config.TreeId, index, in shape);
        _runtime.Add(index, new Runtime(config.TreeId, in shape));
        _indexes.Add(index);
        _indexes.Sort(Comparable<int>.Default);
    }
    private void MoveElement()
    {
        foreach (int index in _indexes)
        {
            var runtime = _runtime[index];
            float speed = _speedRange.x + (float)_random.NextDouble() * (_speedRange.y - _speedRange.x);

            var direction = runtime.Direction();
            if (direction != default && ChangePosition(index, in runtime, (Vector2DInt)((Vector2D)direction).ScaleMagnitude(speed)))
                continue;

            while (2 * MathF.PI * (float)_random.NextDouble() is var rad)
                if (ChangePosition(index, in runtime, (Vector2DInt)(new Vector2(MathF.Cos(rad), MathF.Sin(rad)) * speed)))
                    break;
        }
    }
    private void RemoveElement()
    {
        if (_indexes.IsEmpty())
            return;

        int idx = _random.Next(0, _indexes.Count);
        int index = _indexes[idx];
        var runtime = _runtime[index];

        _manager.Remove(runtime.TreeId(), index, in runtime.Shape);
        _runtime.Remove(index);
        _indexes.RemoveAt(idx);
    }
    private bool ChangePosition(int index, in Runtime runtime, Vector2DInt offset)
    {
        var oldPosition = runtime.Shape.Center();
        var newPosition = oldPosition + offset;
        var extents = runtime.Shape.HalfSize();
        var shape = new AABB2DInt(newPosition, extents);
        if (!Geometry.Contain(in _manager.MaxBoundary, in shape))
            return false;

        _manager.Update(runtime.TreeId(), index, new Change<Vector2DInt>(oldPosition, newPosition), extents);
        _runtime[index] = new Runtime(in runtime, oldPosition, new AABB2DInt(newPosition, extents));
        return true;
    }
}