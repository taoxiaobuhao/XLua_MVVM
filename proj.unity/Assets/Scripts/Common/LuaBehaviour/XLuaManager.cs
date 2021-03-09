using System;
using System.Collections;
using UnityEngine;
using XLua;
using Object = UnityEngine.Object;

/// <summary>
/// 说明：xLua管理类
/// 注意：
/// 1、整个Lua虚拟机执行的脚本分成3个模块：热修复、公共模块、逻辑模块
/// 2、公共模块：提供Lua语言级别的工具类支持，和游戏逻辑无关，最先被启动
/// 3、热修复模块：脚本全部放Lua/XLua目录下，随着游戏的启动而启动
/// 4、逻辑模块：资源热更完毕后启动
/// 5、资源热更以后，理论上所有被加载的Lua脚本都要重新执行加载，如果热更某个模块被删除，则可能导致Lua加载异常，这里的方案是释放掉旧的虚拟器另起一个
/// @by wsh 2017-12-28
/// </summary>
[Hotfix]
[LuaCallCSharp]
public class XLuaManager : XLuaBaseManager<XLuaManager>
{
    LuaEnv luaEnv = null;

    protected override void Init()
    {
        base.Init();
    }


    public IEnumerator RestartGame()
    {
        yield return null;
    }

    protected override LuaEnv.CustomLoader GetCustomLoader()
    {
        return CustomLoader;
    }


    public static byte[] CustomLoader(ref string filepath)
    {
        filepath = filepath.Replace(".", "/");

        //filepath = filepath + ".lua.txt";
        filepath = filepath + ".lua";

        var asset = Resources.Load<Object>(filepath) as TextAsset;

        if (asset != null)
        {
            // Debug.Log("Load lua script : " + filepath);
            return asset.bytes;
        }
        else
        {
            Debug.LogError("Load lua script error with all paths:" + filepath);
        }

        return null;
    }
}