using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using XLua;

#if USE_UNI_LUA
using LuaAPI = UniLua.Lua;
using RealStatePtr = UniLua.ILuaState;
using LuaCSFunction = UniLua.CSharpFunctionDelegate;
#else
using LuaAPI = XLua.LuaDLL.Lua;
using RealStatePtr = System.IntPtr;
using LuaCSFunction = XLua.LuaDLL.lua_CSFunction;
#endif


public class LuaBehaviorAutoParameters
{
	class Contents
    {
		static Dictionary<string, Type> cachedTypeDic = new Dictionary<string, Type> ();

		public enum PropertyType
        {
			GameObject = 0,
			LuaComponet = 1,
			Texture = 2,
			Material = 3,
			Sprite = 4,
			AudioClip = 5,
			TextAsset = 6,
            Shader = 7,
			Value = 8
		}
		public enum ValueType
        {
			String = 0,
			Number = 1,
			Vector3 = 2,
			Vector2 = 3,
			Color = 4,
			Boolean = 5,
            Vector4 = 6
        }
		public class PropertyInfo
        {
			public PropertyType propertyType = PropertyType.Value;
			public int index = 0;
			public int propertyIndex = 0;
		}

        SerializedProperty injectionProperty;
        Dictionary<string, PropertyInfo> propertyDic = new Dictionary<string, PropertyInfo> ();

		public void Start(LuaBehaviour luaBehaviour, SerializedObject serializedObject)
        {
            injectionProperty = serializedObject.FindProperty("injections").GetArrayElementAtIndex(0);

            propertyDic.Clear ();
			for (int index = 0; index < luaBehaviour.injections[0].listStrValue.Count; ++index)
            {
				string value = luaBehaviour.injections[0].listStrValue[index];
				if (value.StartsWith ("@property"))
                {
					string[] infos = value.Split (',');
					PropertyInfo info = new PropertyInfo ();
					info.propertyType = (PropertyType)int.Parse (infos [2]);
					info.index = int.Parse (infos [3]);
					info.propertyIndex = index;
					propertyDic.Add (infos [1], info);
				}
			}
		}

		public void End()
        {
			propertyDic.Clear ();
		}

		#region Property
		public bool ComponetProperty(string propertyName, string componmentName)
        {
			Type targetType = GetTypeByName(componmentName);
			SerializedProperty property = contents.GetProperty(propertyName, Contents.PropertyType.LuaComponet);
			bool hasComponent = ComponentField (property, propertyName, targetType);
			return hasComponent;
		}

		public bool ReferenceProperty(string propertyName, Contents.PropertyType propertyType)
        {
			SerializedProperty property = contents.GetProperty(propertyName, propertyType);
            Type referenceType = null;
            switch (propertyType)
            {
                case PropertyType.AudioClip:
                    referenceType = typeof(AudioClip);
                    break;
                case PropertyType.GameObject:
                    referenceType = typeof(GameObject);
                    break;
                case PropertyType.Material:
                    referenceType = typeof(Material);
                    break;
                case PropertyType.Sprite:
                    referenceType = typeof(Sprite);
                    break;
                case PropertyType.TextAsset:
                    referenceType = typeof(TextAsset);
                    break;
                case PropertyType.Texture:
                    referenceType = typeof(Texture);
                    break;
                case PropertyType.Shader:
                    referenceType = typeof(Shader);
                    break;
            }
            property.objectReferenceValue = EditorGUILayout.ObjectField (new GUIContent(propertyName), property.objectReferenceValue, referenceType, true);
			bool hasReference = property.objectReferenceValue != null || property.exposedReferenceValue != null;
			return hasReference;
		}

		public void StringProperty(string propertyName, string defaultValue = "")
        {
			SerializedProperty property = contents.GetProperty(propertyName, PropertyType.Value, defaultValue);
			property.stringValue = EditorGUILayout.TextField (propertyName, property.stringValue);
		}

		public double NumberProperty(string propertyName, string defaultValue = "0")
        {
			SerializedProperty property = contents.GetProperty(propertyName, PropertyType.Value, defaultValue);
			double currentNumber = 0;
			if (!string.IsNullOrEmpty (property.stringValue)) {
				double.TryParse (property.stringValue, out currentNumber);
			}
			currentNumber = EditorGUILayout.DoubleField (propertyName, currentNumber);
			property.stringValue = currentNumber.ToString ();
			return currentNumber;
		}

		public bool BooleanProperty(string propertyName, string defaultValue = "")
        {
			SerializedProperty property = contents.GetProperty(propertyName, PropertyType.Value, defaultValue);
			bool currentBool = false;
			if (!string.IsNullOrEmpty (property.stringValue)) {
				currentBool = true;
			}
			currentBool = EditorGUILayout.ToggleLeft (propertyName, currentBool);
			if (currentBool == false) {
				property.stringValue = "";
			} else {
				property.stringValue = "true";
			}
			return currentBool;
		}

		public void Vector3Property(string propertyName, string defaultValue = "0,0,0")
        {
			SerializedProperty property = contents.GetProperty(propertyName, PropertyType.Value, defaultValue);
			Vector3 currentVector = Vector3.zero;
			string[] subStr = property.stringValue.Split (',');
			if (subStr.Length == 3)
            {
				float.TryParse (subStr [0], out currentVector.x);
				float.TryParse (subStr [1], out currentVector.y);
				float.TryParse (subStr [2], out currentVector.z);
			}
			currentVector = EditorGUILayout.Vector3Field (propertyName, currentVector);
			property.stringValue = string.Format ("{0},{1},{2}", currentVector.x, currentVector.y, currentVector.z);
		}

		public void Vector2Property(string propertyName, string defaultValue = "0,0")
        {
			SerializedProperty property = contents.GetProperty(propertyName, PropertyType.Value, defaultValue);
			Vector2 currentVector = Vector2.zero;
			string[] subStr = property.stringValue.Split (',');
			if (subStr.Length == 2)
            {
				float.TryParse (subStr [0], out currentVector.x);
				float.TryParse (subStr [1], out currentVector.y);
			}
			currentVector = EditorGUILayout.Vector2Field (propertyName, currentVector);
			property.stringValue = string.Format ("{0},{1}", currentVector.x, currentVector.y);
		}

        public void Vector4Property(string propertyName, string defaultValue = "0,0,0,1")
        {
            SerializedProperty property = contents.GetProperty(propertyName, PropertyType.Value, defaultValue);
            Vector4 currentVector = Vector4.zero;
            string[] subStr = property.stringValue.Split(',');
            if (subStr.Length == 4)
            {
                float.TryParse(subStr[0], out currentVector.x);
                float.TryParse(subStr[1], out currentVector.y);
                float.TryParse(subStr[2], out currentVector.z);
                float.TryParse(subStr[3], out currentVector.w);
            }
            currentVector = EditorGUILayout.Vector4Field(propertyName, currentVector);
            property.stringValue = string.Format("{0},{1},{2},{3}", currentVector.x, currentVector.y, currentVector.z, currentVector.w);
        }

        public void ColorProperty(string propertyName, string defaultValue = "0,0,0,0")
        {
			SerializedProperty property = contents.GetProperty(propertyName, PropertyType.Value, defaultValue);
			Color currentColor = Color.black;
			string[] subStr = property.stringValue.Split (',');
			if (subStr.Length == 4)
            {
				float.TryParse (subStr [0], out currentColor.r);
				float.TryParse (subStr [1], out currentColor.g);
				float.TryParse (subStr [2], out currentColor.b);
				float.TryParse (subStr [3], out currentColor.a);
			}
			currentColor = EditorGUILayout.ColorField (propertyName, currentColor);
			property.stringValue = string.Format ("{0},{1},{2},{3}", currentColor.r, currentColor.g, currentColor.b, currentColor.a);
		}

		public void ComponetParameterList(string parameterName, string componmentName)
        {
			double listSize = contents.NumberProperty (parameterName + "[-1]");
			bool showList = contents.BooleanProperty (parameterName + "[-2]", "true");
			if (showList && listSize > 0 )
            {
				EditorGUI.indentLevel = 1;
				for (int index = 0; index < listSize; ++index) {
					string propertyName = string.Format ("{0}[{1}]", parameterName, index);
					contents.ComponetProperty (propertyName, componmentName);
				}
				EditorGUI.indentLevel = 0;
			}
		}

		public void ReferenceParameterList(string parameterName, PropertyType propertyType)
        {
			double listSize = contents.NumberProperty (parameterName + "[-1]");
			bool showList = contents.BooleanProperty (parameterName + "[-2]", "true");
			if (showList && listSize > 0 )
            {
				EditorGUI.indentLevel = 1;
				for (int index = 0; index < listSize; ++index) {
					string propertyName = string.Format ("{0}[{1}]", parameterName, index);
					contents.ReferenceProperty (propertyName, propertyType);
				}
				EditorGUI.indentLevel = 0;
			}
		}

		public void ValueParameterList(string parameterName, ValueType valueType)
        {
			double listSize = contents.NumberProperty (parameterName + "[-1]");
			bool showList = contents.BooleanProperty (parameterName + "[-2]", "true");
			if (showList && listSize > 0 )
            {
				EditorGUI.indentLevel = 1;
				for (int index = 0; index < listSize; ++index)
                {
					string propertyName = string.Format ("{0}[{1}]", parameterName, index);
					switch (valueType)
                    {
					case Contents.ValueType.String:
						contents.StringProperty (propertyName);
						break;
					case Contents.ValueType.Number:
						contents.NumberProperty (propertyName);
						break;
					case Contents.ValueType.Boolean:
						contents.BooleanProperty (propertyName);
						break;
					case Contents.ValueType.Color:
						contents.ColorProperty (propertyName);
						break;
					case Contents.ValueType.Vector3:
						contents.Vector3Property (propertyName);
						break;
					case Contents.ValueType.Vector2:
						contents.Vector2Property (propertyName);
						break;
                    case Contents.ValueType.Vector4:
                        contents.Vector4Property(propertyName);
                        break;
                    }
				}
				EditorGUI.indentLevel = 0;
			}
		}

		public string EnumProperty(string propertyName, string[] splitEnumString, string defaultValue = "")
        {
			SerializedProperty property = contents.GetProperty(propertyName, PropertyType.Value, defaultValue);
			int selected = 0;
			for (; selected < splitEnumString.Length; ++selected)
            {
				if (property.stringValue == splitEnumString [selected])
                {
					break;
				}
			}
			selected = EditorGUILayout.Popup(propertyName, selected, splitEnumString);
			property.stringValue = splitEnumString[selected];
			return property.stringValue; 
		}
		#endregion

		SerializedProperty GetProperty(string propertyName, PropertyType propertyType, string defaultValue = "")
        {
			PropertyInfo info = null;
			SerializedProperty infoProperty = null;
			if (propertyDic.ContainsKey (propertyName))
            {
				info = propertyDic [propertyName];
				if (info.propertyType == propertyType)
                {
					return GetArrayProperty (info.propertyType).GetArrayElementAtIndex (info.index);	
				}
				info.propertyType = propertyType;
				info.index = -1;
				infoProperty = GetArrayProperty (PropertyType.Value).GetArrayElementAtIndex (info.propertyIndex);
			}
            else
            {
				info = new PropertyInfo ();
				info.propertyType = propertyType;
				info.index = -1;
				infoProperty = GetFreePropertyIn (PropertyType.Value, out info.propertyIndex);
				propertyDic.Add (propertyName, info);
			}
			SerializedProperty valueProperty = GetFreePropertyIn (propertyType, out info.index, defaultValue);
			infoProperty.stringValue = string.Format ("@property,{0},{1},{2},{3}", propertyName, (int)info.propertyType, info.index, info.propertyIndex);
			return valueProperty;
		}

		SerializedProperty GetArrayProperty(PropertyType propertyType)
        {
			switch (propertyType)
            {
			case PropertyType.GameObject:
			case PropertyType.LuaComponet:
            case PropertyType.AudioClip:
            case PropertyType.Material:
            case PropertyType.Texture:
            case PropertyType.Sprite:
            case PropertyType.TextAsset:
            case PropertyType.Shader:
                return injectionProperty.FindPropertyRelative ("listObjValue");
			case PropertyType.Value:
				return injectionProperty.FindPropertyRelative ("listStrValue");
			}
			return null;
		}

		SerializedProperty GetFreePropertyIn(PropertyType propertyType, out int index, string defaultValue = "")
        {
			SerializedProperty arrayProperty = GetArrayProperty (propertyType);
			int size = arrayProperty.arraySize;
			HashSet<int> usedSlots = new HashSet<int> ();
			foreach (KeyValuePair<string, PropertyInfo> propertyInfo in propertyDic)
            {
                if (propertyType == PropertyType.Value)
                {
                    if (propertyInfo.Value.index != -1)
                    {
                        usedSlots.Add(propertyInfo.Value.index);
                    }
                    usedSlots.Add (propertyInfo.Value.propertyIndex);
				}
                else if(propertyInfo.Value.propertyType != PropertyType.Value)
                {
                    if (propertyInfo.Value.index != -1)
                    {
                        usedSlots.Add(propertyInfo.Value.index);
                    }
                }
			}
			for (int findIndex = 0; findIndex < size; ++findIndex)
            {
				if (!usedSlots.Contains (findIndex))
                {
					index = findIndex;
					return arrayProperty.GetArrayElementAtIndex (findIndex);
				}
			}
			arrayProperty.arraySize = size + 1;
			index = size;
			SerializedProperty newProperty = arrayProperty.GetArrayElementAtIndex (size);
			if (propertyType == PropertyType.Value)
            {
				newProperty.stringValue = defaultValue;
			}
			return newProperty;
		}
			
		Type GetTypeByName(string name)
        {
			if (cachedTypeDic.ContainsKey (name))
            {
				return cachedTypeDic [name];
			}
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
				if (assembly.FullName.StartsWith ("Unity") || assembly.FullName.StartsWith("DOTween") || assembly.FullName.StartsWith("Assembly") || assembly.FullName.StartsWith("TextMeshPro"))
                {
					foreach (Type type in assembly.GetTypes())
                    {
						if (type.FullName == name && type.IsSubclassOf(typeof(Component)))
                        {
							cachedTypeDic.Add (name, type);
							return type;
						}
					}
				}
			}
			return null;
		}

		bool ComponentField(SerializedProperty property, string fieldName, Type componetType)
        {
			UnityEngine.Object obj = property.objectReferenceValue;
            obj = EditorGUILayout.ObjectField(fieldName, obj, componetType, true);
			if (obj != null)
            {
				property.objectReferenceValue = (obj as Component);
				return true;
			}
			property.objectReferenceValue = null;
			return false;
		}
	}
	static Contents contents = null;
	static LuaBehaviour recordedBehaviour = null;
	static List<Action> callRecords = new List<Action>();
	
	public static void Start(LuaBehaviour luaBehaviour, SerializedObject serializedObject)
    {
		if (contents == null)
        {
			contents = new Contents ();
		}
		contents.Start (luaBehaviour, serializedObject);
	}

	public static void End()
    {
		contents.End ();
	}

	public static bool HasRecord(LuaBehaviour luaBehaviour)
    {
		return luaBehaviour == recordedBehaviour;
	}

	public static void StartRecord(LuaBehaviour luaBehaviour)
    {
		recordedBehaviour = luaBehaviour;
		callRecords.Clear ();
	}
		
	public static void RunRecord()
    {
		foreach (Action action in callRecords)
        {
			action ();
		}
	}

	#region Lua
	public static void Register(LuaEnv luaEnv)
    {
        Type type = luaEnv.GetType();
        FieldInfo fieldInfo = type.GetField("rawL", BindingFlags.NonPublic | BindingFlags.Instance);
        RealStatePtr L = (RealStatePtr)fieldInfo.GetValue(luaEnv);
        LuaAPI.lua_newtable(L);
        LuaAPI.xlua_pushasciistring(L, "Property_GameObject");
        LuaAPI.lua_pushnumber(L, (double)Contents.PropertyType.GameObject);
        LuaAPI.lua_rawset(L, -3);
        LuaAPI.xlua_pushasciistring(L, "Property_LuaComponet");
        LuaAPI.lua_pushnumber(L, (double)Contents.PropertyType.LuaComponet);
        LuaAPI.lua_rawset(L, -3);
        LuaAPI.xlua_pushasciistring(L, "Property_Texture");
        LuaAPI.lua_pushnumber(L, (double)Contents.PropertyType.Texture);
        LuaAPI.lua_rawset(L, -3);
        LuaAPI.xlua_pushasciistring(L, "Property_Material");
        LuaAPI.lua_pushnumber(L, (double)Contents.PropertyType.Material);
        LuaAPI.lua_rawset(L, -3);
        LuaAPI.xlua_pushasciistring(L, "Property_Sprite");
        LuaAPI.lua_pushnumber(L, (double)Contents.PropertyType.Sprite);
        LuaAPI.lua_rawset(L, -3);
        LuaAPI.xlua_pushasciistring(L, "Property_AudioClip");
        LuaAPI.lua_pushnumber(L, (double)Contents.PropertyType.AudioClip);
        LuaAPI.lua_rawset(L, -3);
        LuaAPI.xlua_pushasciistring(L, "Property_TextAsset");
        LuaAPI.lua_pushnumber(L, (double)Contents.PropertyType.TextAsset);
        LuaAPI.lua_rawset(L, -3);
        LuaAPI.xlua_pushasciistring(L, "Property_Shader");
        LuaAPI.lua_pushnumber(L, (double)Contents.PropertyType.Shader);
        LuaAPI.lua_rawset(L, -3);
        LuaAPI.xlua_pushasciistring(L, "Property_Value");
        LuaAPI.lua_pushnumber(L, (double)Contents.PropertyType.Value);
        LuaAPI.lua_rawset(L, -3);
        LuaAPI.xlua_pushasciistring(L, "Value_String");
        LuaAPI.lua_pushnumber(L, (double)Contents.ValueType.String);
        LuaAPI.lua_rawset(L, -3);
        LuaAPI.xlua_pushasciistring(L, "Value_Number");
        LuaAPI.lua_pushnumber(L, (double)Contents.ValueType.Number);
        LuaAPI.lua_rawset(L, -3);
        LuaAPI.xlua_pushasciistring(L, "Value_Vector3");
        LuaAPI.lua_pushnumber(L, (double)Contents.ValueType.Vector3);
        LuaAPI.lua_rawset(L, -3);
        LuaAPI.xlua_pushasciistring(L, "Value_Vector2");
        LuaAPI.lua_pushnumber(L, (double)Contents.ValueType.Vector2);
        LuaAPI.lua_rawset(L, -3);
        LuaAPI.xlua_pushasciistring(L, "Value_Color");
        LuaAPI.lua_pushnumber(L, (double)Contents.ValueType.Color);
        LuaAPI.lua_rawset(L, -3);
        LuaAPI.xlua_pushasciistring(L, "Value_Boolean");
        LuaAPI.lua_pushnumber(L, (double)Contents.ValueType.Boolean);
        LuaAPI.lua_rawset(L, -3);
        LuaAPI.xlua_pushasciistring(L, "Value_Vector4");
        LuaAPI.lua_pushnumber(L, (double)Contents.ValueType.Vector4);
        LuaAPI.lua_rawset(L, -3);
        LuaAPI.xlua_pushasciistring(L, "SetContent");
        LuaAPI.lua_pushstdcallcfunction(L, new LuaCSFunction(SetContent));
        LuaAPI.lua_rawset(L, -3);
        LuaAPI.xlua_pushasciistring(L, "Label");
        LuaAPI.lua_pushstdcallcfunction(L, new LuaCSFunction(Label));
        LuaAPI.lua_rawset(L, -3);
        LuaAPI.xlua_pushasciistring(L, "ComponentParam");
        LuaAPI.lua_pushstdcallcfunction(L, new LuaCSFunction(ComponentParam));
        LuaAPI.lua_rawset(L, -3);
        LuaAPI.xlua_pushasciistring(L, "ReferenceParam");
        LuaAPI.lua_pushstdcallcfunction(L, new LuaCSFunction(ReferenceParam));
        LuaAPI.lua_rawset(L, -3);
        LuaAPI.xlua_pushasciistring(L, "ValueParam");
        LuaAPI.lua_pushstdcallcfunction(L, new LuaCSFunction(ValueParam));
        LuaAPI.lua_rawset(L, -3);
        LuaAPI.xlua_pushasciistring(L, "EnumParam");
        LuaAPI.lua_pushstdcallcfunction(L, new LuaCSFunction(EnumParam));
        LuaAPI.lua_rawset(L, -3);
        LuaAPI.xlua_pushasciistring(L, "ComponentParamList");
        LuaAPI.lua_pushstdcallcfunction(L, new LuaCSFunction(ComponentParamList));
        LuaAPI.lua_rawset(L, -3);
        LuaAPI.xlua_pushasciistring(L, "ReferenceParamList");
        LuaAPI.lua_pushstdcallcfunction(L, new LuaCSFunction(ReferenceParamList));
        LuaAPI.lua_rawset(L, -3);
        LuaAPI.xlua_pushasciistring(L, "ValueParamList");
        LuaAPI.lua_pushstdcallcfunction(L, new LuaCSFunction(ValueParamList));
        LuaAPI.lua_rawset(L, -3);
        LuaAPI.xlua_setglobal(L, "AutoParameter");
    }

    [MonoPInvokeCallback(typeof(LuaCSFunction))]
    static int SetContent(RealStatePtr L)
    {
		return 0;
	}

    [MonoPInvokeCallback(typeof(LuaCSFunction))]
    static int Label(RealStatePtr L)
    {
		try{
            int paramCount = LuaAPI.lua_gettop(L);
			string labelName = LuaAPI.lua_tostring(L, 2);
			EditorGUILayout.LabelField(labelName);
			if (recordedBehaviour)
            {
				callRecords.Add (()=>
                {
					EditorGUILayout.LabelField(labelName);
				});
			}
			return 0;
		}
        catch (Exception e)
        {
            return LuaAPI.luaL_error(L, "c# exception:" + e);
        }
	}

    [MonoPInvokeCallback(typeof(LuaCSFunction))]
    static int ComponentParam(RealStatePtr L)
    {
		string parameterName;
		string componmentName;
		try
        {
            int paramCount = LuaAPI.lua_gettop(L);
			parameterName = LuaAPI.lua_tostring(L, 2);
			componmentName = LuaAPI.lua_tostring(L, 3);
		}
        catch (Exception e)
        {
            return LuaAPI.luaL_error(L, "c# exception:" + e);
        }

		bool hasComponent = contents.ComponetProperty (parameterName, componmentName);
		if (recordedBehaviour)
        {
			callRecords.Add (()=>
            {
				contents.ComponetProperty (parameterName, componmentName);
			});
		}
		try
        {
			if (hasComponent)
            {
                LuaAPI.lua_pushboolean(L, true);
			}
            else
            {
                LuaAPI.lua_pushboolean(L, false);
			}
			return 1;
		}
        catch (Exception e)
        {
            return LuaAPI.luaL_error(L, "c# exception:" + e);
        }
	}

    [MonoPInvokeCallback(typeof(LuaCSFunction))]
    static int ReferenceParam(RealStatePtr L)
    {
		string parameterName;
		Contents.PropertyType propertyType = Contents.PropertyType.GameObject;
		try
        {
            int paramCount = LuaAPI.lua_gettop(L);
			parameterName = LuaAPI.lua_tostring(L, 2);
			propertyType = (Contents.PropertyType)LuaAPI.lua_tonumber(L, 3);
		}
        catch (Exception e)
        {
            return LuaAPI.luaL_error(L, "c# exception:" + e);
        }
		bool hasReference = contents.ReferenceProperty (parameterName, propertyType);
		if (recordedBehaviour)
        {
			callRecords.Add (()=>
            {
				contents.ReferenceProperty (parameterName, propertyType);
			});
		}
		try
        {
            LuaAPI.lua_pushboolean(L, hasReference);
			return 1;
		}
        catch (Exception e)
        {
            return LuaAPI.luaL_error(L, "c# exception:" + e);
        }
	}

    [MonoPInvokeCallback(typeof(LuaCSFunction))]
    static int ValueParam(RealStatePtr L)
    {
		string parameterName = "";
		Contents.ValueType valueType = Contents.ValueType.String;
		string defaultValue = "";
		try {
            int paramCount = LuaAPI.lua_gettop(L);
            if (paramCount == 3)
            {
				parameterName = LuaAPI.lua_tostring(L, 2);
				valueType = (Contents.ValueType)LuaAPI.lua_tonumber(L, 3);				
			}
            else if (paramCount == 4)
            {
				parameterName = LuaAPI.lua_tostring(L, 2);
				valueType = (Contents.ValueType)LuaAPI.lua_tonumber(L, 3);	
				defaultValue = LuaAPI.lua_tostring(L, 4);
			}
		}
        catch (Exception e)
        {
            return LuaAPI.luaL_error(L, "c# exception:" + e);
        }
		if (string.IsNullOrEmpty (defaultValue))
        {
			switch (valueType)
            {
			case Contents.ValueType.String:
				contents.StringProperty (parameterName);
				if (recordedBehaviour)
                {
					callRecords.Add (()=>
                    {
						contents.StringProperty (parameterName);
					});
				}
				break;
			case Contents.ValueType.Number:
				double number = contents.NumberProperty (parameterName);
				if (recordedBehaviour)
                {
					callRecords.Add (()=>
                    {
						contents.NumberProperty (parameterName);
					});
				}
				try 
                {
                        LuaAPI.lua_pushnumber (L, number);
					return 1;
				}
                catch (Exception e)
                {
					return LuaAPI.luaL_error(L, "c# exception:" + e);
				}
			case Contents.ValueType.Boolean:
				bool boolean = contents.BooleanProperty (parameterName);
				if (recordedBehaviour)
                {
					callRecords.Add (()=>
                    {
						contents.BooleanProperty (parameterName);
					});
				}
				try
                {
                        LuaAPI.lua_pushboolean (L, boolean);
					return 1;
				}
                catch (Exception e)
                {
					return LuaAPI.luaL_error(L, "c# exception:" + e);
				}
			case Contents.ValueType.Color:
				contents.ColorProperty (parameterName);
				if (recordedBehaviour)
                {
					callRecords.Add (()=>
                    {
						contents.ColorProperty (parameterName);
					});
				}
				break;
			case Contents.ValueType.Vector3:
				contents.Vector3Property (parameterName);
				if (recordedBehaviour)
                {
					callRecords.Add (()=>
                    {
						contents.Vector3Property (parameterName);
					});
				}
				break;
			case Contents.ValueType.Vector2:
				contents.Vector2Property (parameterName);
				if (recordedBehaviour)
                {
					callRecords.Add (()=>
                    {
						contents.Vector2Property (parameterName);
					});
				}
				break;
            case Contents.ValueType.Vector4:
                contents.Vector4Property(parameterName);
                if (recordedBehaviour)
                {
                    callRecords.Add(() =>
                    {
                        contents.Vector4Property(parameterName);
                    });
                }
                break;
            }
		}
        else
        {
			switch (valueType)
            {
			case Contents.ValueType.String:
				contents.StringProperty (parameterName, defaultValue);
				if (recordedBehaviour)
                {
					callRecords.Add (()=>
                    {
						contents.StringProperty (parameterName, defaultValue);
					});
				}
				break;
			case Contents.ValueType.Number:
				double number = contents.NumberProperty (parameterName, defaultValue);
				if (recordedBehaviour)
                {
					callRecords.Add (()=>
                    {
						contents.NumberProperty (parameterName, defaultValue);
					});
				}
				try
                {
                    LuaAPI.lua_pushnumber (L, number);
					return 1;
				}
                catch (Exception e)
                {
					return LuaAPI.luaL_error(L, "c# exception:" + e);
				}
			case Contents.ValueType.Boolean:
				bool boolean = contents.BooleanProperty (parameterName, defaultValue);
				if (recordedBehaviour)
                {
					callRecords.Add (()=>
                    {
						contents.BooleanProperty (parameterName, defaultValue);
					});
				}
				try
                {
                    LuaAPI.lua_pushboolean (L, boolean);
					return 1;
				}
                catch (Exception e)
                {
					return LuaAPI.luaL_error(L, "c# exception:" + e);
				}
			case Contents.ValueType.Color:
				contents.ColorProperty (parameterName, defaultValue);
				if (recordedBehaviour)
                {
					callRecords.Add (()=>
                    {
						contents.ColorProperty (parameterName, defaultValue);
					});
				}
				break;
			case Contents.ValueType.Vector3:
				contents.Vector3Property (parameterName, defaultValue);
				if (recordedBehaviour)
                {
					callRecords.Add (()=>
                    {
						contents.Vector3Property (parameterName, defaultValue);
					});
				}
				break;
			case Contents.ValueType.Vector2:
				contents.Vector2Property (parameterName, defaultValue);
				if (recordedBehaviour)
                {
					callRecords.Add (()=>
                    {
						contents.Vector2Property (parameterName, defaultValue);
					});
				}
				break;
            case Contents.ValueType.Vector4:
                contents.Vector4Property(parameterName, defaultValue);
                if (recordedBehaviour)
                {
                    callRecords.Add(() =>
                    {
                        contents.Vector4Property(parameterName, defaultValue);
                    });
                }
                break;
            }			
		}
		return 0;
	}

    [MonoPInvokeCallback(typeof(LuaCSFunction))]
    static int EnumParam(RealStatePtr L)
    {
		string parameterName = "";
		string enumString = "";
		string defaultValue = "";
		try
        {
            int paramCount = LuaAPI.lua_gettop(L);
			parameterName = LuaAPI.lua_tostring(L, 2);
			enumString = LuaAPI.lua_tostring(L, 3);
			defaultValue = LuaAPI.lua_tostring(L, 4);
		}
        catch (Exception e)
        {
			return LuaAPI.luaL_error(L, "c# exception:" + e);
		}
		string[] splitEnumString = enumString.Split ('|');
		string result = contents.EnumProperty(parameterName, splitEnumString, defaultValue);
		if (recordedBehaviour)
        {
			callRecords.Add (()=>
            {
				contents.EnumProperty(parameterName, splitEnumString, defaultValue);
			});
		}
		try
        {
            LuaAPI.lua_pushstring(L, result);
			return 1;
		}
        catch (Exception e)
        {
			return LuaAPI.luaL_error(L, "c# exception:" + e);
		}
	}

    [MonoPInvokeCallback(typeof(LuaCSFunction))]
    static int ComponentParamList(RealStatePtr L)
    {
		string parameterName;
		string componmentName;
		try
        {
            int paramCount = LuaAPI.lua_gettop(L);
			parameterName = LuaAPI.lua_tostring(L, 2);
			componmentName = LuaAPI.lua_tostring(L, 3);
		}
        catch (Exception e)
        {
			return LuaAPI.luaL_error(L, "c# exception:" + e);
		}
		contents.ComponetParameterList (parameterName, componmentName);
		if (recordedBehaviour)
        {
			callRecords.Add (()=>
            {
				contents.ComponetParameterList (parameterName, componmentName);
			});
		}
		return 0;
	}

    [MonoPInvokeCallback(typeof(LuaCSFunction))]
    static int ReferenceParamList(RealStatePtr L)
    {
		string parameterName;
		Contents.PropertyType propertyType = Contents.PropertyType.GameObject;
		try
        {
            int paramCount = LuaAPI.lua_gettop(L);
			parameterName = LuaAPI.lua_tostring(L, 2);
			propertyType = (Contents.PropertyType)LuaAPI.lua_tonumber(L, 3);
		}
        catch (Exception e)
        {
			return LuaAPI.luaL_error(L, "c# exception:" + e);
		}
		contents.ReferenceParameterList (parameterName, propertyType);
		if (recordedBehaviour)
        {
			callRecords.Add (()=>
            {
				contents.ReferenceParameterList (parameterName, propertyType);
			});
		}
		return 0;
	}

    [MonoPInvokeCallback(typeof(LuaCSFunction))]
    static int ValueParamList(RealStatePtr L)
    {
		string parameterName;
		Contents.ValueType valueType = Contents.ValueType.String;
		try
        {
            int paramCount = LuaAPI.lua_gettop(L);
			parameterName = LuaAPI.lua_tostring(L, 2);
			valueType = (Contents.ValueType)LuaAPI.lua_tonumber(L, 3);
		}
        catch (Exception e)
        {
			return LuaAPI.luaL_error(L, "c# exception:" + e);
		}
		contents.ValueParameterList (parameterName, valueType);
		if (recordedBehaviour)
        {
			callRecords.Add (()=>
            {
				contents.ValueParameterList (parameterName, valueType);
			});
		}
		return 0;
	}
	#endregion
}
