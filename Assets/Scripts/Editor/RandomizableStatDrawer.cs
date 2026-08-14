using UnityEditor;
using UnityEngine;

// UWAGA: ten plik MUSI lezec w folderze o nazwie "Editor"!
//
// Sprawia, ze w Inspektorze widzisz tylko to, co ma znaczenie:
// - checkbox odznaczony -> jedno pole "Wartosc"
// - checkbox zaznaczony -> dwa pola "Od" i "Do" + zaokraglanie
[CustomPropertyDrawer(typeof(RandomizableStat))]
public class RandomizableStatDrawer : PropertyDrawer
{
    private const float LINE = 18f;
    private const float GAP = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty useRandom = property.FindPropertyRelative("useRandomRange");

        // naglowek + checkbox + (1 pole albo 3 pola)
        int lines = useRandom.boolValue ? 5 : 3;
        return lines * (LINE + GAP);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty useRandom = property.FindPropertyRelative("useRandomRange");
        SerializedProperty fixedValue = property.FindPropertyRelative("fixedValue");
        SerializedProperty minValue = property.FindPropertyRelative("minValue");
        SerializedProperty maxValue = property.FindPropertyRelative("maxValue");
        SerializedProperty roundToWhole = property.FindPropertyRelative("roundToWhole");

        Rect r = new Rect(position.x, position.y, position.width, LINE);

        EditorGUI.LabelField(r, label, EditorStyles.boldLabel);
        r.y += LINE + GAP;

        EditorGUI.indentLevel++;

        useRandom.boolValue = EditorGUI.Toggle(r, "Losowy zakres", useRandom.boolValue);
        r.y += LINE + GAP;

        if (useRandom.boolValue)
        {
            EditorGUI.PropertyField(r, minValue, new GUIContent("Od"));
            r.y += LINE + GAP;

            EditorGUI.PropertyField(r, maxValue, new GUIContent("Do"));
            r.y += LINE + GAP;

            EditorGUI.PropertyField(r, roundToWhole, new GUIContent("Pelne liczby"));
        }
        else
        {
            EditorGUI.PropertyField(r, fixedValue, new GUIContent("Wartosc"));
        }

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }
}