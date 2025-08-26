using Eevee.Diagnosis;
using Eevee.Fixed;
using Eevee.QuadTree;
using Eevee.Utils;
using EeveeEditor;
using EeveeEditor.QuadTree;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SRandom = System.Random;

/// <summary>
/// 四叉树示例代码
/// </summary>
internal sealed class QuadTreeSample : MonoBehaviour
{
    #region 类型
    [Serializable]
    private struct RandomWeight
    {
        private interface IWeight
        {
            int GetWeight();
        }

        [Serializable]
        private struct ShapeWeight : IWeight
        {
            [SerializeField] internal QuadTreeShape Shape;
            [SerializeField] internal int Weight;
            public readonly int GetWeight() => Weight;

            [CustomPropertyDrawer(typeof(ShapeWeight))]
            private sealed class ShapeWeightDrawer : PropertyDrawer
            {
                private const int HeightScale = 2;
                public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
                {
                    var size = new Vector2(position.size.x, position.size.y / HeightScale);
                    var shapePosition = new Rect(position.position, size);
                    var weightPosition = new Rect(position.x, position.y + size.y, size.x, size.y);

                    var shapeProperty = property.FindPropertyRelative(nameof(Shape));
                    var weightProperty = property.FindPropertyRelative(nameof(Weight));

                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.PropertyField(shapePosition, shapeProperty);
                    EditorGUI.PropertyField(weightPosition, weightProperty);
                    EditorGUILayout.EndHorizontal();

                    shapeProperty.Dispose();
                    weightProperty.Dispose();
                }
                public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => base.GetPropertyHeight(property, label) * HeightScale;
            }
        }

        [Serializable]
        private struct OperateWeight : IWeight
        {
            [SerializeField] internal ElementOperate Operate;
            [SerializeField] internal int Weight;
            public readonly int GetWeight() => Weight;

            [CustomPropertyDrawer(typeof(OperateWeight))]
            private sealed class OperateWeightWeightDrawer : PropertyDrawer
            {
                private const int HeightScale = 2;
                public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
                {
                    var size = new Vector2(position.size.x, position.size.y / HeightScale);
                    var operatePosition = new Rect(position.position, size);
                    var weightPosition = new Rect(position.x, position.y + size.y, size.x, size.y);

                    var operateProperty = property.FindPropertyRelative(nameof(Operate));
                    var weightProperty = property.FindPropertyRelative(nameof(Weight));

                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.PropertyField(operatePosition, operateProperty);
                    EditorGUI.PropertyField(weightPosition, weightProperty);
                    EditorGUILayout.EndHorizontal();

                    operateProperty.Dispose();
                    weightProperty.Dispose();
                }
                public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => base.GetPropertyHeight(property, label) * HeightScale;
            }
        }

        [SerializeField] private ShapeWeight[] _shapeWeights;
        [SerializeField] private OperateWeight[] _operateWeights;
        internal SRandom Random;

        internal readonly QuadTreeShape GetShape() => Get(_shapeWeights).Shape;
        internal readonly ElementOperate GetOperate() => Get(_operateWeights).Operate;
        private readonly T Get<T>(T[] weights) where T : IWeight
        {
            int sum = 0;
            foreach (var weight in weights)
                sum += weight.GetWeight();

            int next = Random.Next(0, sum);
            int value = next;
            foreach (var weight in weights)
                if (value <= weight.GetWeight())
                    return weight;
                else
                    value -= weight.GetWeight();

            return default;
        }
    }

    private sealed class SampleQuadTreeDrawProxy : IQuadTreeDrawProxy
    {
        public Type TreeEnum => typeof(QuadFunc);
        public QuadTreeManager Manager => _manager;
        public int GetIndex(GameObject go)
        {
            var entity = go.GetComponent<QuadEntity>();
            return entity.Index;
        }
        public void GetIndexes(GameObject go, ICollection<int> indexes)
        {
            var entities = go.GetComponentsInChildren<QuadEntity>();
            foreach (var entity in entities)
                indexes.Add(entity.Index);
        }
    }

    private readonly struct QuadRuntime
    {
        private readonly QuadFunc _func;
        private readonly Vector2DInt _lastPosition;
        internal readonly AABB2DInt Shape;
        internal QuadRuntime(int treeId, in AABB2DInt shape)
        {
            _func = (QuadFunc)treeId;
            _lastPosition = shape.Center();
            Shape = shape;
        }
        internal QuadRuntime(int treeId, Vector2DInt lastPosition, in AABB2DInt shape)
        {
            _func = (QuadFunc)treeId;
            _lastPosition = lastPosition;
            Shape = shape;
        }
        internal int TreeId() => (int)_func;
        internal Vector2DInt Direction() => Shape.Center() - _lastPosition;
    }

    private enum QuadFunc
    {
        None,
        Unit,
        GuardBox,
        Shop,
        Item,
        GuardArea,
        Region,
    }

    private enum ElementOperate
    {
        None,
        Insert,
        Remove,
    }
    #endregion

    #region 序列化字段
    [Header("随机权重")] [SerializeField] private RandomWeight _randomWeight;

    [Header("四叉树配置")] [SerializeField] private int _scale;
    [SerializeField] private int _depthCount;
    [SerializeField] private Vector2DInt _center;
    [SerializeField] private Vector2DInt _extents;

    [Header("运行时数据")] [SerializeField] private int _seed;
    [SerializeField] private int _countLimit;
    [SerializeField] private Vector2 _speedRange;
    [SerializeField] private bool _removeEmptyNode;
    [ReadOnly] [SerializeField] private int _indexCount;
    #endregion

    #region 运行时缓存
    private SRandom _random;
    private List<QuadTreeConfig> _configs;
    private Dictionary<int, QuadRuntime> _runtime;
    private List<int> _indexes;
    private static QuadTreeManager _manager;
    #endregion

    private void OnEnable()
    {
        var random = new SRandom(_seed);
        var configs = new List<QuadTreeConfig>();

        configs.Add(QuadTreeConfig.Build<DynamicQuadTree>((int)QuadFunc.Unit, QuadTreeShape.Circle, new Vector2DInt(64, 64)));
        configs.Add(QuadTreeConfig.Build<DynamicQuadTree>((int)QuadFunc.GuardBox, QuadTreeShape.AABB, new Vector2DInt(64, 64)));
        configs.Add(QuadTreeConfig.Build<MeshQuadTree>((int)QuadFunc.Shop, QuadTreeShape.Circle, new Vector2DInt(512, 512)));
        configs.Add(QuadTreeConfig.Build<MeshQuadTree>((int)QuadFunc.Item, QuadTreeShape.AABB, new Vector2DInt(16, 16)));
        configs.Add(QuadTreeConfig.Build<LooseQuadTree>((int)QuadFunc.GuardArea, QuadTreeShape.Circle, new Vector2DInt(1024, 1024)));
        configs.Add(QuadTreeConfig.Build<LooseQuadTree>((int)QuadFunc.Region, QuadTreeShape.AABB, new Vector2DInt(256, 256)));

        _randomWeight.Random = random;
        _random = random;
        _configs = configs;
        _runtime = new Dictionary<int, QuadRuntime>();
        _indexes = new List<int>();
        _manager = new QuadTreeManager(_scale, _depthCount, new AABB2DInt(_center, _extents), _configs);
    }
    private void Update()
    {
        var operate = _randomWeight.GetOperate();
        switch (operate)
        {
            case ElementOperate.Insert:
                if (_indexes.Count < _countLimit)
                    QuadInsert();
                break;
            case ElementOperate.Remove: QuadRemove(); break;
        }

        QuadUpdate();

        if (_removeEmptyNode)
            _manager.RemoveEmptyNode();
        _indexCount = _indexes.Count;
    }
    private void OnDisable()
    {
        LogProxy.Inject(new UnityLog());
        _manager.Clean();
        _manager = null;
    }

    private void QuadInsert()
    {
        var boundary = _manager.MaxBoundary;
        int index = _indexes.Count > 0 ? _indexes[^1] + 1 : 1;
        var config = _configs[_random.Next(0, _configs.Count)];
        int width = _random.Next(config.Extents.X >> 1, config.Extents.X << 2);
        int height = _random.Next(config.Extents.Y >> 1, config.Extents.Y << 2);
        int cx = _random.Next(boundary.Left() + width, boundary.Right() - width);
        int cy = _random.Next(boundary.Bottom() + height, boundary.Top() - height);
        var shape = config.Shape is QuadTreeShape.Circle ? new AABB2DInt(cx, cy, Math.Min(width, height)) : new AABB2DInt(cx, cy, width, height);

        _manager.Insert(config.TreeId, index, in shape);
        _runtime.Add(index, new QuadRuntime(config.TreeId, in shape));
        _indexes.Add(index);
        _indexes.Sort(Comparer.Int);
    }
    private void QuadUpdate()
    {
        foreach (int index in _indexes)
        {
            var runtime = _runtime[index];
            float speed = _speedRange.x + (float)_random.NextDouble() * (_speedRange.y - _speedRange.x);

            if (runtime.Direction() is { } direction && direction != default)
            {
                var dir = (Vector2D)direction;
                var offset = dir.ScaleMagnitude(speed);
                if (ChangePosition(index, in runtime, (Vector2DInt)offset))
                    continue;
            }

            while (true)
            {
                float rad = (float)_random.NextDouble() * MathF.PI * 2;
                var offset = new Vector2(MathF.Sin(rad), MathF.Cos(rad)) * speed;
                if (ChangePosition(index, in runtime, (Vector2DInt)offset))
                    break;
            }
        }
    }
    private void QuadRemove()
    {
        if (_indexes.Count == 0)
            return;

        int idx = _random.Next(0, _indexes.Count);
        int index = _indexes[idx];
        var runtime = _runtime[index];

        _manager.Remove(runtime.TreeId(), index, in runtime.Shape);
        _runtime.Remove(index);
        _indexes.RemoveAt(idx);
    }

    private bool ChangePosition(int index, in QuadRuntime runtime, Vector2DInt offset)
    {
        var oldPosition = runtime.Shape.Center();
        var newPosition = oldPosition + offset;
        var extents = runtime.Shape.HalfSize();
        var shape = new AABB2DInt(newPosition, extents);
        if (!Geometry.Contain(in _manager.MaxBoundary, in shape))
            return false;

        _manager.Update(runtime.TreeId(), index, new Change<Vector2DInt>(oldPosition, newPosition), extents);
        _runtime[index] = new QuadRuntime(runtime.TreeId(), oldPosition, new AABB2DInt(newPosition, extents));
        return true;
    }
}