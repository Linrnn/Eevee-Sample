using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SRandom = System.Random;

[Serializable]
internal struct KeyWeight<TKey>
{
    [SerializeField] internal TKey Key;
    [SerializeField] internal int Weight;

    internal static KeyWeight<TKey> Get(IList<KeyWeight<TKey>> collection, SRandom random)
    {
        int sum = 0;
        for (int i = 0; i < collection.Count; i++)
            sum += collection[i].Weight;

        for (int next = random.Next(0, sum), value = next, i = 0; i < collection.Count; ++i)
            if (collection[i] is var item && value <= item.Weight)
                return item;
            else
                value -= item.Weight;
        return default;
    }
}

[CustomPropertyDrawer(typeof(KeyWeight<>))]
internal sealed class EnumWeightDrawer : PropertyDrawer
{
    private const int HeightScale = 2;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var size = new Vector2(position.size.x, position.size.y / HeightScale);
        var enumPosition = new Rect(position.position, size);
        var weightPosition = new Rect(position.x, position.y + size.y, size.x, size.y);

        var enumProperty = property.FindPropertyRelative(nameof(KeyWeight<object>.Key));
        var weightProperty = property.FindPropertyRelative(nameof(KeyWeight<object>.Weight));

        EditorGUI.PropertyField(enumPosition, enumProperty);
        EditorGUI.PropertyField(weightPosition, weightProperty);

        enumProperty.Dispose();
        weightProperty.Dispose();
    }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => base.GetPropertyHeight(property, label) * HeightScale;
}