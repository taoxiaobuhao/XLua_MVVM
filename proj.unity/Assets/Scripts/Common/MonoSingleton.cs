using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static T mInstance = null;
    private static bool applicationIsQuitting = false;
    private static GameObject parent;

    public static T Instance
    {
        get
        {
            if (mInstance == null && !applicationIsQuitting)
            {
                mInstance = GameObject.FindObjectOfType(typeof(T)) as T;
                if (mInstance == null)
                {
                    GameObject go = new GameObject(typeof(T).Name);
                    mInstance = go.AddComponent<T>();
                    if (!parent)
                    {
                        parent = GameObject.Find("Boot");
                        if (!parent)
                        {
                            parent = new GameObject("Boot");
                        }
                    }

#if UNITY_EDITOR
                    if (Application.isPlaying)
#endif
                        GameObject.DontDestroyOnLoad(parent);

                    if (parent)
                    {
                        go.transform.parent = parent.transform;
                    }
                }
            }

            return mInstance;
        }
    }


    /// <summary>
    /// MonoSingleton起始点
    /// </summary>
    public void Startup()
    {
    }

    protected virtual void Awake()
    {
        if (mInstance == null)
        {
            mInstance = this as T;
        }

        if (this != mInstance)
        {
            Debug.LogWarning("Destroy unused instances of " + typeof(T).Name + "   Boot has/have " +
                             this.transform.parent.childCount + " child(ren)");
            Destroy(this.gameObject);
        }

#if UNITY_EDITOR
        if (Application.isPlaying)
#endif
            if (gameObject.transform.parent)
                DontDestroyOnLoad(gameObject.transform.parent.gameObject);

        Init();
    }

    protected virtual void Init()
    {
    }

    public void DestroySelf()
    {
        Dispose();
        MonoSingleton<T>.mInstance = null;
        UnityEngine.Object.Destroy(gameObject);
    }

    public virtual void Dispose()
    {
    }

    public virtual void OnDestroy()
    {
        applicationIsQuitting = true;
    }
}