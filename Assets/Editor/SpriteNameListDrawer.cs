using UnityEditor;
using UnityEngine;

/// <summary>
/// Рисует массив спрайтов с понятными подписями: «0 — Desert», «1 — Desert-2» и т.д.
/// Сами слоты остаются обычными полями — можно перетаскивать спрайты и менять порядок.
/// </summary>
[CustomPropertyDrawer(typeof(SpriteNameListAttribute))]
public class SpriteNameListDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!IsArray(property))
            return base.GetPropertyHeight(property, label);

        float line = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        return (property.arraySize + 1) * (line + spacing) - spacing;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (!IsArray(property))
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        SerializedProperty size = property.FindPropertyRelative("Array.size");

        float line = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        float y = position.y;

        EditorGUI.BeginProperty(position, label, property);

        if (size != null)
        {
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, line), size, label);
            y += line + spacing;
        }

        for (int i = 0; i < property.arraySize; i++)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(i);
            Sprite sprite = element.objectReferenceValue as Sprite;

            string caption = string.Format("{0} — {1}", i, GetDisplayName(sprite));

            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, line),
                element,
                new GUIContent(caption));

            y += line + spacing;
        }

        EditorGUI.EndProperty();
    }

    private static bool IsArray(SerializedProperty property)
    {
        return property.isArray && property.propertyType != SerializedPropertyType.String;
    }

    /// <summary>Имя папки спрайта (сцена), а если пути нет — имя самого спрайта.</summary>
    private static string GetDisplayName(Sprite sprite)
    {
        if (sprite == null)
            return "пусто";

        string path = AssetDatabase.GetAssetPath(sprite);

        if (string.IsNullOrEmpty(path))
            return sprite.name;

        string folder = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(path));

        return string.IsNullOrEmpty(folder) ? sprite.name : folder;
    }
}
