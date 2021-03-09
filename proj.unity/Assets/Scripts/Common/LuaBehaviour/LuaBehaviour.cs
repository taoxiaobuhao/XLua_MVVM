/*
 * Tencent is pleased to support the open source community by making xLua available.
 * Copyright (C) 2016 THL A29 Limited, a Tencent company. All rights reserved.
 * Licensed under the MIT License (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
 * http://opensource.org/licenses/MIT
 * Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using XLua;
using Object = UnityEngine.Object;

[System.Serializable]
public class Injection
{
    [Tooltip("备注，方便理解该配置的意思，可以使用中文")]
    public string remark = "";
    public string name;
    public ValueType valueType;
    public UnityEngine.Object objValue;
    public List<UnityEngine.Object> listObjValue;
    public string strValue;
    public int intValue;
    public float floatValue;
    public Vector2 vec2Value;
    public Vector3 vec3Value;
    public List<string> listStrValue;
    public Object atlasValue;
    public AnimationCurve aniCurve;
}

[LuaCallCSharp]
public class LuaBehaviour : MonoBehaviour
{
    public TextAsset luaScript;
    public SceneInjections sceneInjections;
    public Injection[] injections;

    protected LuaEnv luaEnv; //all lua behaviour shared one luaenv only!
    internal static float lastGCTime = 0;
    internal const float GCInterval = 1; //1 second 

    protected Action<LuaTable> luaAwake;
    protected Action<LuaTable> luaStart;
    protected Action<LuaTable> luaUpdate;
    protected Action<LuaTable> luaLateUpdate;
    protected Action<LuaTable> luaFixedUpdate;
    protected Action<LuaTable> luaOnDestroy;
    protected Action<LuaTable> luaOnEnable;
    protected Action<LuaTable> luaOnDisable;

    protected Action<LuaTable> luaEnterForeground;
    protected Action<LuaTable> luaEnterBackground;
    protected Action<LuaTable, bool> luaApplicationFoucs;

    public LuaTable scriptEnv;
    public LuaTable returnScriptEnv;

    /// <summary>
    /// 是否手动动态设置脚本
    /// 防止执行两次Awake,Start
    /// </summary>
    protected bool isManualSetScript = false;

    protected virtual LuaEnv GetLuaEnv()
    {
        return XLuaManager.Instance.GetLuaEnv();
    }

    protected virtual void CheckAssetBundleManager()
    {

    }

    protected virtual void SetMessager()
    {

    }

    void StartScript()
    {
        luaEnv = GetLuaEnv();
        scriptEnv = luaEnv.NewTable();

        LuaTable meta = luaEnv.NewTable();
        meta.Set("__index", luaEnv.Global);
        scriptEnv.SetMetaTable(meta);
        meta.Dispose();

        scriptEnv.Set("this", this);
        if (injections != null)
        {
            foreach (var injection in injections)
            {
                scriptEnv.Set(injection.name, injection);
            }
        }

        if (sceneInjections != null && sceneInjections.variables != null)
        {
            foreach (var injection in sceneInjections.variables)
            {
                scriptEnv.Set(injection.name, injection);
            }
        }

        CheckAssetBundleManager();

        var retTable = luaEnv.DoString(luaScript.bytes, luaScript.name, scriptEnv);
        if (retTable.Length > 0)
        {
            returnScriptEnv = retTable[0] as LuaTable;

            var newfunc = returnScriptEnv.Get<LuaFunction>("New");
            retTable = newfunc.Call();
            returnScriptEnv = retTable[0] as LuaTable;
            returnScriptEnv.Set("this", this);
            if (injections != null)
            {
                foreach (var injection in injections)
                {
                    returnScriptEnv.Set(injection.name, injection);
                }
            }

            if (sceneInjections != null && sceneInjections.variables != null)
            {
                foreach (var injection in sceneInjections.variables)
                {
                    returnScriptEnv.Set(injection.name, injection);
                }
            }

            returnScriptEnv.Get("Awake", out luaAwake);
            returnScriptEnv.Get("Start", out luaStart);
            returnScriptEnv.Get("OnDestroy", out luaOnDestroy);
            returnScriptEnv.Get("ApplicationDidEnterBackground", out luaEnterBackground);
            returnScriptEnv.Get("ApplicationWillEnterForeground", out luaEnterForeground);
            returnScriptEnv.Get("OnApplicationFocus", out luaApplicationFoucs);
            returnScriptEnv.Get("OnEnable", out luaOnEnable);
            returnScriptEnv.Get("OnDisable", out luaOnDisable);


            SetMessager();
        }
        else
        {
            Debug.LogError("Not table returned! :" + this.name + "  ->  " + this.luaScript.name);
        }

        if (luaAwake != null)
        {
            luaAwake(returnScriptEnv);
        }
    }

    void Awake()
    {
        if (luaScript != null && !isManualSetScript)
        {
            StartScript();
        }
    }

    // Use this for initialization
    void Start()
    {
        if (luaStart != null && !isManualSetScript)
        {
            luaStart(returnScriptEnv);
        }
    }


    /// <summary>
    ///  Update 在 LuaUpdate里面统一管理
    /// </summary>
    // Update is called once per frame
    //void Update ()
    //   {
    //       if (luaUpdate != null)
    //       {
    //           luaUpdate(returnScriptEnv);
    //       }
    //}
    void OnDestroy()
    {
        if (returnScriptEnv != null)
        {
            var deleteFunc = returnScriptEnv.Get<LuaFunction>("Delete");
            deleteFunc.Call(returnScriptEnv);
        }

        if (luaOnDestroy != null)
        {
            luaOnDestroy(returnScriptEnv);
        }

        luaOnDestroy = null;
        luaUpdate = null;
        luaStart = null;
        if (scriptEnv != null)
        {
            scriptEnv.Dispose();
        }

        injections = null;
    }

    private void OnApplicationFocus(bool focus)
    {
        if (luaApplicationFoucs != null)
        {
            luaApplicationFoucs(returnScriptEnv, focus);
        }
    }

    public void ApplicationDidEnterBackground()
    {
        if (luaEnterBackground != null)
        {
            luaEnterBackground(returnScriptEnv);
        }
    }

    public void ApplicationWillEnterForeground()
    {
        if (luaEnterForeground != null)
        {
            luaEnterForeground(returnScriptEnv);
        }
    }

    private void OnEnable()
    {
        if (luaOnEnable != null)
        {
            luaOnEnable(returnScriptEnv);
        }
    }

    private void OnDisable()
    {
        if (luaOnDisable != null)
        {
            luaOnDisable(returnScriptEnv);
        }
    }

    /// <summary>
    /// 获取当前的lua脚本实例
    /// </summary>
    /// <returns></returns>
    public LuaTable GetTable()
    {
        return returnScriptEnv;
    }

    /// <summary>
    /// Animation Event Callback
    /// </summary>
    /// <param name="luaMethod">lua端的方法名</param>
    public void AnimationCallback(string luaMethod)
    {
        Action<LuaTable> callbackFunc;
        GetTable().Get(luaMethod, out callbackFunc);
        if (callbackFunc != null)
        {
            callbackFunc(GetTable());
        }
    }
}