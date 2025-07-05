using Eevee.Collection;
using Eevee.Fixed;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Geometry示例代码
/// </summary>
internal sealed class GeometrySample : MonoBehaviour
{
    [CustomEditor(typeof(GeometrySample))]
    private sealed class GeometrySampleInspector : Editor
    {
        private const string Polygon0 = nameof(_polygon0);
        private const string Polygon1 = nameof(_polygon1);
        private static readonly Handles.CapFunction _handleCap = Handles.CircleHandleCap;
        private SerializedProperty _polygon0Property;
        private SerializedProperty _polygon1Property;

        private void OnEnable()
        {
            _polygon0Property = serializedObject.FindProperty(Polygon0);
            _polygon1Property = serializedObject.FindProperty(Polygon1);
        }
        private void OnSceneGUI()
        {
            serializedObject.Update();
            Draw(_polygon0Property);
            Draw(_polygon1Property);
            serializedObject.ApplyModifiedProperties();
        }
        private void OnDisable()
        {
            _polygon0Property.Dispose();
            _polygon1Property.Dispose();
        }

        private void Draw(SerializedProperty polygonProperty)
        {
            for (int length = polygonProperty.arraySize, i = 0; i < length; ++i)
            {
                var property = polygonProperty.GetArrayElementAtIndex(i);
                var point = property.vector2IntValue;
                var center2 = new Vector3(point.x, Height, point.y);
                float size = HandleUtility.GetHandleSize(center2) * 0.1F;

                var center3 = Handles.FreeMoveHandle(center2, size, default, _handleCap);
                property.vector2IntValue = new Vector2Int((int)center3.x, (int)center3.z);
            }
        }
    }

    private enum State
    {
        None,
        Normal,
        Intersect,
        Contain,
        Contained,
    }

    private const float Height = 0;
    [Header("多边形")] [SerializeField] private Vector2Int[] _polygon0;
    [SerializeField] private Vector2Int[] _polygon1;
    [Header("渲染数据")] [SerializeField] private Color _normalColor;
    [SerializeField] private Color _intersectColor;
    [SerializeField] private Color _containedColor;

    private State _state;

    private void Update()
    {
        var polygon0 = AsPolygon(_polygon0);
        var polygon1 = AsPolygon(_polygon1);
        if (Geometry.Intersect(in polygon0, in polygon1))
        {
            if (Geometry.Contain(in polygon0, in polygon1))
                _state = State.Contain;
            else if (Geometry.Contain(in polygon1, in polygon0))
                _state = State.Contained;
            else
                _state = State.Intersect;
        }
        else
        {
            _state = State.Normal;
        }
    }
    private void OnDrawGizmos()
    {
        switch (_state)
        {
            case State.Normal:
                Draw(_polygon0, in _normalColor);
                Draw(_polygon1, in _normalColor);
                break;

            case State.Intersect:
                Draw(_polygon0, in _intersectColor);
                Draw(_polygon1, in _intersectColor);
                break;

            case State.Contain:
                Draw(_polygon0, in _intersectColor);
                Draw(_polygon1, in _containedColor);
                break;

            case State.Contained:
                Draw(_polygon0, in _containedColor);
                Draw(_polygon1, in _intersectColor);
                break;

            default:
                Draw(_polygon0, in _normalColor, true);
                Draw(_polygon1, in _normalColor, true);
                break;
        }
    }

    private void Draw(IReadOnlyList<Vector2Int> polygon, in Color color, bool dotted = false)
    {
        var oldColor = Handles.color;

        Handles.color = color;
        for (int count = polygon.Count, i = 0, j = count - 1; i < count; j = i++)
        {
            var pi = polygon[i];
            var pj = polygon[j];

            if (dotted)
                Handles.DrawDottedLine(new Vector3(pi.x, Height, pi.y), new Vector3(pj.x, Height, pj.y), 6);
            else
                Handles.DrawLine(new Vector3(pi.x, Height, pi.y), new Vector3(pj.x, Height, pj.y));
        }
        Handles.color = oldColor;
    }
    private PolygonInt AsPolygon(IEnumerable<Vector2Int> polygon)
    {
        var enumerable = polygon.Select(point => (Vector2DInt)point);
        var readOnlyArray = new ReadOnlyArray<Vector2DInt>(enumerable.ToArray());
        return new PolygonInt(in readOnlyArray);
    }
}