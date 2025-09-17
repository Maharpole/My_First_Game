using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponFireProfile))]
public class WeaponFireProfileEditor : Editor
{
    SerializedProperty fireRateProp;
    SerializedProperty pelletsProp;
    SerializedProperty spreadProp;
    SerializedProperty bulletProp;
    SerializedProperty effectsProp;
    SerializedProperty fireClipsProp;
    SerializedProperty fireVolumeProp;
    SerializedProperty firePitchRangeProp;

    void OnEnable()
    {
        fireRateProp = serializedObject.FindProperty("fireRate");
        pelletsProp = serializedObject.FindProperty("pellets");
        spreadProp = serializedObject.FindProperty("extraPelletSpread");
        bulletProp = serializedObject.FindProperty("bullet");
        effectsProp = serializedObject.FindProperty("effects");
        fireClipsProp = serializedObject.FindProperty("fireClips");
        fireVolumeProp = serializedObject.FindProperty("fireVolume");
        firePitchRangeProp = serializedObject.FindProperty("firePitchRange");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(fireRateProp);
        EditorGUILayout.PropertyField(pelletsProp);
        EditorGUILayout.PropertyField(spreadProp);
        EditorGUILayout.PropertyField(bulletProp);

        DrawEffectsList();

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(fireClipsProp, true);
        EditorGUILayout.PropertyField(fireVolumeProp);
        EditorGUILayout.PropertyField(firePitchRangeProp);

        serializedObject.ApplyModifiedProperties();
    }

    void DrawEffectsList()
    {
        EditorGUILayout.LabelField("Effects (ordered)", EditorStyles.boldLabel);

        int size = effectsProp.arraySize;
        int newSize = EditorGUILayout.IntField("Count", size);
        if (newSize != size)
        {
            effectsProp.arraySize = Mathf.Max(0, newSize);
            size = effectsProp.arraySize;
        }

        var profile = (WeaponFireProfile)target;

        for (int i = 0; i < size; i++)
        {
            var element = effectsProp.GetArrayElementAtIndex(i);
            var effectProp = element.FindPropertyRelative("effect");
            var paramsProp = element.FindPropertyRelative("parameters");

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Element {i}", EditorStyles.boldLabel);
            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                effectsProp.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(effectProp);
            bool effectChanged = EditorGUI.EndChangeCheck();

            var effectObj = effectProp.objectReferenceValue as BulletEffect;
            if (effectChanged && effectObj != null)
            {
                // Auto-create a default params object typed for the effect
                var def = effectObj.CreateDefaultParams();
                paramsProp.managedReferenceValue = def;
            }

            if (effectObj != null)
            {
                if (paramsProp.managedReferenceValue == null)
                {
                    var def = effectObj.CreateDefaultParams();
                    paramsProp.managedReferenceValue = def;
                }

                if (paramsProp.managedReferenceValue != null)
                {
                    EditorGUILayout.PropertyField(paramsProp, true);
                }
                else
                {
                    EditorGUILayout.HelpBox("No parameters available for this effect.", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Assign an Effect to edit its parameters.", MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Add Effect"))
        {
            effectsProp.arraySize += 1;
        }
    }
}


