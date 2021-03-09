using System.Collections.Generic;
using UnityEngine;



public enum ValueType
{
    FloatValue,
    IntValue,
    StringValue,
    Vector2Value,
    Vector3Value,
    AtlasValue,
    AniCurveValue,
    ObjectValue,
    ListObjectValue,
}



public class SceneInjections : MonoBehaviour
{
    [Tooltip("配置列表，需要添加时，请在下放的Size中添加数值，就会产生新的可配置项目")]
    public List<Injection> variables = new List<Injection>();
}