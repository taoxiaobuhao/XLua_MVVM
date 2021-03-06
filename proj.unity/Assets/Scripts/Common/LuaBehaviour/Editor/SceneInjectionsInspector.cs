using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;


[CustomEditor(typeof(SceneInjections))]
public class SceneInjectionsInspector : Editor
{
    private SerializedProperty mData;
    private SceneInjections injections;

    private void OnEnable()
    {
        mData = serializedObject.FindProperty("variables");
        injections = (SceneInjections) target;
        mCount = injections.variables.Count;
    }

    private int mCount;

    public override void OnInspectorGUI()
    {
        injections = (SceneInjections) target;

        serializedObject.Update();
        var script = MonoScript.FromMonoBehaviour((SceneInjections) target);
        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script:", script, typeof(MonoScript), false);
        GUI.enabled = true;

        EditorGUILayout.BeginVertical();
        {
            mCount = EditorGUILayout.IntField("Size", mCount);
            Event e = Event.current;
            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                if (mCount > injections.variables.Count)
                {
                    var add = mCount - injections.variables.Count;
                    for (int i = 0; i < add; i++)
                    {
                        injections.variables.Add(new Injection());
                    }
                }
                else
                {
                    injections.variables.RemoveRange(mCount, injections.variables.Count - mCount);
                }

                serializedObject.ApplyModifiedProperties();
                return;
            }

            GUILayout.Space(20);
            for (int i = 0; i < injections.variables.Count; i++)
            {
                var injection = injections.variables[i];
                var field = mData.GetArrayElementAtIndex(i);
                // EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PropertyField(field.FindPropertyRelative("name"), new GUIContent("Key"),
                        GUILayout.Width(250));
                    EditorGUILayout.PropertyField(field.FindPropertyRelative("remark"), new GUIContent("????????????"));
                    // EditorGUILayout.EndHorizontal();
                }

                // EditorGUILayout.BeginHorizontal();
                {
                    injection.valueType =
                        (ValueType) EditorGUILayout.Popup("????????????", (int) injection.valueType,
                            new string[] {"?????????", "?????????", "?????????","Vector2", "Vector3", "??????", "????????????", "????????????", "????????????"});
                    // EditorGUILayout.PropertyField(field.FindPropertyRelative("valueType"), new GUIContent("????????????"),
                    // GUILayout.Width(250));
                    switch (injection.valueType)
                    {
                        case ValueType.FloatValue:
                            EditorGUILayout.PropertyField(field.FindPropertyRelative("floatValue"),
                                new GUIContent("?????????"), true);
                            break;
                        case ValueType.IntValue:
                            EditorGUILayout.PropertyField(field.FindPropertyRelative("intValue"),
                                new GUIContent("?????????"),
                                true);
                            break;
                        case ValueType.StringValue:
                            EditorGUILayout.PropertyField(field.FindPropertyRelative("strValue"),
                                new GUIContent("?????????"),
                                true);
                            break;
                        case ValueType.Vector2Value:
                            EditorGUILayout.PropertyField(field.FindPropertyRelative("vec2Value"),
                                new GUIContent("Vector2"), true);
                            break;
                        case ValueType.Vector3Value:
                            EditorGUILayout.PropertyField(field.FindPropertyRelative("vec3Value"),
                                new GUIContent("Vector3"), true);
                            break;
                        case ValueType.AtlasValue:
                            EditorGUILayout.PropertyField(field.FindPropertyRelative("atlasValue"),
                                new GUIContent("??????"), true);
                            break;
                        case ValueType.AniCurveValue:
                            EditorGUILayout.PropertyField(field.FindPropertyRelative("aniCurve"),
                                new GUIContent("????????????"),
                                true);
                            break;
                        case ValueType.ObjectValue:
                            EditorGUILayout.PropertyField(field.FindPropertyRelative("objValue"),
                                new GUIContent("????????????"),
                                true);
                            break;
                        case ValueType.ListObjectValue:
                            EditorGUILayout.PropertyField(field.FindPropertyRelative("listObjValue"),
                                new GUIContent("????????????"), true);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    // EditorGUILayout.EndHorizontal();
                }

                GUILayout.Space(20);
            }

            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.EndVertical();
            // mCount = injections.variables.Count;
        }
    }
}