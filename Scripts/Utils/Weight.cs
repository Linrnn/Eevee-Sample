using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SRandom = System.Random;

[Serializable]
internal struct Weight<T>
{
    [SerializeField] internal T Key;
    [SerializeField] internal uint Value;

    internal static Weight<T> Get(IList<Weight<T>> collection, SRandom random)
    {
        int count = collection.Count;
        int sum = 0;
        for (int i = 0; i < count; ++i)
            sum += (int)collection[i].Value;

        int flag = random.Next(0, sum);
        for (int left = flag, i = 0; i < count; ++i)
            if (collection[i] is { Value: var value and > 0 } item)
                if (value >= left)
                    return item;
                else
                    left -= (int)value;

        return default;
    }
}

[CustomPropertyDrawer(typeof(Weight<>))]
internal sealed class WeightDrawer : PropertyDrawer
{
    private const int HeightScale = 2;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var size = new Vector2(position.size.x, position.size.y / HeightScale);
        var enumPosition = new Rect(position.position, size);
        var weightPosition = new Rect(position.x, position.y + size.y, size.x, size.y);

        var enumProperty = property.FindPropertyRelative(nameof(Weight<object>.Key));
        var weightProperty = property.FindPropertyRelative(nameof(Weight<object>.Value));

        EditorGUI.PropertyField(enumPosition, enumProperty);
        EditorGUI.PropertyField(weightPosition, weightProperty);

        enumProperty.Dispose();
        weightProperty.Dispose();
    }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => base.GetPropertyHeight(property, label) * HeightScale;
}