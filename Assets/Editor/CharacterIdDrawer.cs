using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Выпадающий список id врагов из Resources/CharacterLibrary для полей с [CharacterId].
/// Работает и для одиночной строки, и для массива строк (элементы рисуются как слоты).
/// </summary>
[CustomPropertyDrawer(typeof(CharacterIdAttribute))]
public class CharacterIdDrawer : PropertyDrawer
{
    private const string LibraryResourceName = "CharacterLibrary";
    private const string EmptyLabel = "— не задан —";

    private static CharacterLibrary cachedLibrary;
    private static string[] cachedIds;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Массив рисуем сами, но высота у него такая же, как у стандартного поля.
        if (IsArray(property))
            return EditorGUI.GetPropertyHeight(property, label, true);

        return base.GetPropertyHeight(property, label);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (IsArray(property))
        {
            DrawArray(position, property, label);
            return;
        }

        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        EditorGUI.BeginProperty(position, label, property);
        DrawPopup(position, property, label);
        EditorGUI.EndProperty();
    }

    private static bool IsArray(SerializedProperty property)
    {
        return property.isArray && property.propertyType != SerializedPropertyType.String;
    }

    private void DrawArray(Rect position, SerializedProperty property, GUIContent label)
    {
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

            DrawPopup(new Rect(position.x, y, position.width, line), element, new GUIContent("Slot " + i));

            y += line + spacing;
        }

        EditorGUI.EndProperty();
    }

    private static void DrawPopup(Rect rect, SerializedProperty property, GUIContent label)
    {
        string[] ids = GetIds();

        string[] options = new string[ids.Length + 1];
        options[0] = EmptyLabel;

        for (int i = 0; i < ids.Length; i++)
            options[i + 1] = ids[i];

        int current = System.Array.IndexOf(ids, property.stringValue);

        // Значение вне библиотеки — показываем его, чтобы не потерять при переименовании.
        if (current < 0 && !string.IsNullOrEmpty(property.stringValue))
            options[0] = string.Format("{0} (нет в библиотеке)", property.stringValue);

        int picked = EditorGUI.Popup(rect, label.text, current + 1, options);

        if (picked != current + 1)
            property.stringValue = picked <= 0 ? string.Empty : options[picked];
    }

    private static string[] GetIds()
    {
        CharacterLibrary library = Resources.Load<CharacterLibrary>(LibraryResourceName);

        if (library == cachedLibrary && cachedIds != null)
            return cachedIds;

        cachedLibrary = library;

        List<string> ids = new List<string>();

        if (library != null && library.characters != null)
        {
            for (int i = 0; i < library.characters.Length; i++)
            {
                CharacterData data = library.characters[i];

                if (data != null && !string.IsNullOrEmpty(data.id))
                    ids.Add(data.id);
            }
        }

        cachedIds = ids.ToArray();
        return cachedIds;
    }
}
