using System;
using System.Collections;
using UnityEngine;
using XLua;

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
public class XLuaBaseManager<T> : MonoSingleton<T> where T : MonoSingleton<T>
{
    const string commonMainScriptName = "CommonMain";
    const string gameMainScriptName = "GameMain";

    public static string frameworkPathFolder = "LuaScripts/";
    
    LuaEnv luaEnv = null;

    public static string CurrentGameFolder { get; protected set; }

    protected override void Init()
    {
        base.Init();
        InitLuaEnv();
        OnInit();
        StartGame();
        StartHotfix();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }


    protected override void Awake()
    {
        //var prefab = Resources.Load<GameObject>("LuaEnvStarter");
        //if (prefab)
        //    Instantiate(prefab);
        //else
        //{
        //    Debug.LogError("You haven't generated xlua wrap classes!!!");
        //}
        //// Resources.UnloadAsset(prefab);

        base.Awake();
    }


    public static bool HasGameStart { get; protected set; }

    public LuaEnv GetLuaEnv()
    {
        return luaEnv;
    }

    protected void InitLuaEnv()
    {
        luaEnv = new LuaEnv();
        HasGameStart = false;
        if (luaEnv != null)
        {
            luaEnv.AddLoader(GetCustomLoader());
            //luaEnv.AddBuildin("pb", XLua.LuaDLL.Lua.LoadPb);
        }
        else
        {
            //Logger.LogError("InitLuaEnv null!!!");
        }
    }

    // 这里必须要等待资源管理模块加载Lua AB包以后才能初始化
    public void OnInit()
    {
        if (luaEnv != null)
        {
            LoadScript(commonMainScriptName);
        }
    }

    protected virtual LuaEnv.CustomLoader GetCustomLoader()
    {
        return null;
    }

    public string AssetbundleName { get; protected set; }

    // 重启虚拟机：热更资源以后被加载的lua脚本可能已经过时，需要重新加载
    // 最简单和安全的方式是另外创建一个虚拟器，所有东西一概重启
    public void Restart()
    {
        StopHotfix();
        Dispose();
        InitLuaEnv();
        OnInit();
        StartGame();
        StartHotfix();
    }


    public void SafeDoString(string scriptContent, bool isLoggingError = false)
    {
        if (luaEnv != null)
        {
            try
            {
#if UNITY_WEBGL
                Debug.Log(typeof(T).Name + " -> SafeDoString :" + scriptContent);
#endif
                luaEnv.DoString(scriptContent);
            }
            catch (System.Exception ex)
            {
                string msg = string.Format("xLua exception : {0}\n {1}", ex.Message, ex.StackTrace);
                if (isLoggingError)
                    Debug.LogError(msg, null);
            }
        }
    }

    public void StartHotfix(bool restart = false)
    {
        if (luaEnv == null)
        {
            return;
        }

        if (restart)
        {
            StopHotfix();
            //ReloadScript(hotfixMainScriptName);
        }
        else
        {
            //LoadScript(hotfixMainScriptName);
        }

        //SafeDoString("HotfixMain.Start()");
    }

    public void StopHotfix()
    {
        //SafeDoString("HotfixMain.Stop()");
    }

    public void StartGame()
    {
        if (luaEnv != null)
        {
            //LoadScript(gameMainScriptName);
            //SafeDoString("GameMain.Start()");
            HasGameStart = true;
        }
    }


    public void ReloadScript(string scriptName)
    {
        SafeDoString(string.Format("package.loaded['{0}'] = nil", scriptName),true);
        LoadScript(scriptName);
    }

    void LoadScript(string scriptName)
    {
        SafeDoString(string.Format("require('{0}')", scriptName),true);
    }


    private void Update()
    {
        if (luaEnv != null)
        {
            luaEnv.Tick();

            if (Time.frameCount % 100 == 0)
            {
                luaEnv.FullGc();
            }
        }
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene,
        UnityEngine.SceneManagement.LoadSceneMode loadMode)
    {
        if (luaEnv != null && HasGameStart)
        {
            SafeDoString("GameMain.OnLevelWasLoaded(\"" + scene.name + "\")");
        }
    }


    private void OnApplicationQuit()
    {
        if (luaEnv != null && HasGameStart)
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            SafeDoString("GameMain.OnApplicationQuit()");
        }
    }

    public override void Dispose()
    {
        if (luaEnv != null)
        {
            try
            {
                luaEnv.Dispose();
                luaEnv = null;
            }
            catch (System.Exception ex)
            {
                string msg = string.Format("xLua exception : {0}\n {1}", ex.Message, ex.StackTrace);
                //Logger.LogError(msg, null);
            }
        }
    }
}