using UnityEngine;
using UnityEditor;
using XLua;

public class MiniLuaInEditor 
{
	static MiniLuaInEditor instance = null;

    LuaEnv luaEnv = null;

	public static MiniLuaInEditor Instance()
    {
		if (instance == null)
        {
			instance = new MiniLuaInEditor ();
		}
		return instance;
	}

	MiniLuaInEditor()
    {
		Debug.Log ("MiniLuaInEditor Ctor");
        luaEnv = new LuaEnv();
        if (luaEnv != null)
        {
            luaEnv.AddLoader(CustomLoader);
            LuaBehaviorAutoParameters.Register(luaEnv);
        }
    }

	public void DoFunction(string str, string func)
    {
        luaEnv.DoString(str);
        luaEnv.DoString(func + "()");

    }

    byte[] CustomLoader(ref string filepath)
    {
       // filepath = filepath.Replace(".", "/") + ".lua.txt";
        filepath = filepath.Replace(".", "/") + ".lua";

        string scriptPath = filepath;
        var asset = Resources.Load<TextAsset>(filepath) as TextAsset;

        if (asset != null)
        {
            return asset.bytes;
        }
        else
        {
            Debug.LogError("Load lua script error:" + filepath);
        }

        return null;
    }
}
