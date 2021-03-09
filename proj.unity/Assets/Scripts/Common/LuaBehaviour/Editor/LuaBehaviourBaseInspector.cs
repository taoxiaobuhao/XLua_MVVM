using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;


    [CustomEditor(typeof(LuaBehaviour))]
    public class LuaBehaviourInspector : UnityEditor.Editor
    {
        LuaBehaviour luaBehaviour = null;
        TextAsset textAsset = null;
        string autoParameterString = string.Empty;
        double lastUpdateTime = -1.0f;

        void OnEnable()
        {
            luaBehaviour = target as LuaBehaviour;
            if (luaBehaviour.luaScript != null)
            {
                textAsset = luaBehaviour.luaScript;
                UpdateAutoParameterString();
            }
        }

        public override void OnInspectorGUI()
        {
            var behaviour = (global::LuaBehaviour) target;
            if (GUILayout.Button("Print Content"))
            {
                if (behaviour && behaviour.luaScript)
                    Debug.Log(behaviour.luaScript.text);
            }

            if (!Application.isPlaying)
            {
                if (luaBehaviour.luaScript != null)
                {
                    if (textAsset != luaBehaviour.luaScript)
                    {
                        textAsset = luaBehaviour.luaScript;
                        UpdateAutoParameterString();
                    }

                    AutoParameters();
                }
            }

            base.OnInspectorGUI();
        }

        void UpdateAutoParameterString()
        {
            lastUpdateTime = -1.0f;

            StringBuilder findingString = new StringBuilder();
            StringReader stringReader = new StringReader(textAsset.text);
            bool started = false;
            string line = stringReader.ReadLine();
            while (line != null)
            {
                if (started)
                {
                    findingString.AppendLine(line);
                }
                else if (line.StartsWith("function") && line.Contains(".AutoParameters("))
                {
                    findingString.AppendLine("function AutoParameters(self, go, injection)");
                    started = true;
                }

                if (started && line.StartsWith("end"))
                {
                    break;
                }

                line = stringReader.ReadLine();
            }

            stringReader.Close();
            autoParameterString = findingString.ToString();
            if (!string.IsNullOrEmpty(autoParameterString))
            {
                Debug.Log(autoParameterString);

                serializedObject.Update();
                if (luaBehaviour.injections == null || luaBehaviour.injections.Length == 0)
                {
                    serializedObject.FindProperty("injections").arraySize = 1;
                }

                serializedObject.ApplyModifiedProperties();
            }
        }

        void AutoParameters()
        {
            if (!string.IsNullOrEmpty(autoParameterString) && luaBehaviour.injections != null)
            {
                serializedObject.Update();
                LuaBehaviorAutoParameters.Start(luaBehaviour, serializedObject);
                if (!LuaBehaviorAutoParameters.HasRecord(luaBehaviour) || lastUpdateTime < 0.0f ||
                    EditorApplication.timeSinceStartup - lastUpdateTime > 1.0f)
                {
                    LuaBehaviorAutoParameters.StartRecord(luaBehaviour);
                    MiniLuaInEditor lua = MiniLuaInEditor.Instance();
                    lua.DoFunction(autoParameterString, "AutoParameters");
                    lastUpdateTime = EditorApplication.timeSinceStartup;
                }
                else
                {
                    LuaBehaviorAutoParameters.RunRecord();
                }

                LuaBehaviorAutoParameters.End();
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
