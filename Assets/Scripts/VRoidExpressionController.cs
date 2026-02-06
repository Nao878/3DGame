using UnityEngine;

/// <summary>
/// VRoidモデル用の表情コントローラー
/// キー入力で表情を切り替え
/// </summary>
public class VRoidExpressionController : MonoBehaviour
{
    [Header("VRM設定")]
    [Tooltip("VRM10Objectコンポーネントを持つオブジェクト（通常は自分自身）")]
    public GameObject vrmObject;

    [Header("設定")]
    public bool showGUI = true;           // GUIを表示
    public float expressionSpeed = 5.0f;  // 表情遷移速度

    private object vrm10Instance;
    private System.Reflection.MethodInfo setWeightMethod;
    private System.Collections.Generic.Dictionary<string, object> expressionKeys = new System.Collections.Generic.Dictionary<string, object>();
    
    private string currentExpression = "Neutral";
    private float currentWeight = 0f;
    private float targetWeight = 0f;

    // 表情名リスト（VRM10の標準表情）
    private readonly string[] expressionNames = new string[]
    {
        "Neutral",
        "Happy",
        "Angry", 
        "Sad",
        "Relaxed",
        "Surprised"
    };

    void Start()
    {
        if (vrmObject == null)
        {
            vrmObject = gameObject;
        }

        SetupVRM10();
    }

    void SetupVRM10()
    {
        var vrm10ObjectType = System.Type.GetType("UniVRM10.Vrm10Instance, VRM10");
        
        if (vrm10ObjectType != null)
        {
            vrm10Instance = vrmObject.GetComponent(vrm10ObjectType);
            
            if (vrm10Instance != null)
            {
                var runtimeProp = vrm10ObjectType.GetProperty("Runtime");
                if (runtimeProp != null)
                {
                    var runtime = runtimeProp.GetValue(vrm10Instance);
                    if (runtime != null)
                    {
                        var expressionProp = runtime.GetType().GetProperty("Expression");
                        if (expressionProp != null)
                        {
                            var expression = expressionProp.GetValue(runtime);
                            if (expression != null)
                            {
                                setWeightMethod = expression.GetType().GetMethod("SetWeight");
                                
                                // ExpressionKey を取得
                                var expressionKeyType = System.Type.GetType("UniVRM10.ExpressionKey, VRM10");
                                if (expressionKeyType != null)
                                {
                                    foreach (var name in expressionNames)
                                    {
                                        var field = expressionKeyType.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                                        if (field != null)
                                        {
                                            expressionKeys[name] = field.GetValue(null);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                
                Debug.Log("[VRoidExpressionController] VRM10 表情初期化完了");
            }
        }
        
        if (vrm10Instance == null)
        {
            Debug.LogWarning("[VRoidExpressionController] VRM10Instanceが見つかりません。");
        }
    }

    void Update()
    {
        HandleInput();
        UpdateExpression();
    }

    void HandleInput()
    {
        // 数字キーで表情切り替え
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetExpression("Happy");
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetExpression("Angry");
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetExpression("Sad");
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetExpression("Surprised");
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetExpression("Relaxed");
        if (Input.GetKeyDown(KeyCode.Alpha0)) SetExpression("Neutral");
    }

    void UpdateExpression()
    {
        // 表情ウェイトのスムーズな遷移
        currentWeight = Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * expressionSpeed);
        
        if (setWeightMethod != null && vrm10Instance != null)
        {
            ApplyExpression();
        }
    }

    void ApplyExpression()
    {
        try
        {
            var vrm10ObjectType = System.Type.GetType("UniVRM10.Vrm10Instance, VRM10");
            var runtimeProp = vrm10ObjectType.GetProperty("Runtime");
            var runtime = runtimeProp.GetValue(vrm10Instance);
            var expressionProp = runtime.GetType().GetProperty("Expression");
            var expression = expressionProp.GetValue(runtime);
            
            // 全ての表情をリセット
            foreach (var kvp in expressionKeys)
            {
                float weight = (kvp.Key == currentExpression) ? currentWeight : 0f;
                setWeightMethod.Invoke(expression, new object[] { kvp.Value, weight });
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[VRoidExpressionController] 表情設定エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 表情を設定
    /// </summary>
    public void SetExpression(string expressionName)
    {
        if (expressionKeys.ContainsKey(expressionName))
        {
            // 現在の表情をリセット
            if (currentExpression != expressionName)
            {
                currentWeight = 0f;
            }
            
            currentExpression = expressionName;
            targetWeight = (expressionName == "Neutral") ? 0f : 1f;
            
            Debug.Log($"[VRoidExpressionController] 表情変更: {expressionName}");
        }
    }

    void OnGUI()
    {
        if (!showGUI) return;

        GUI.Box(new Rect(10, 120, 220, 150), "VRoid Expression Control");
        GUI.Label(new Rect(20, 145, 200, 20), "1 : Happy (幸せ)");
        GUI.Label(new Rect(20, 165, 200, 20), "2 : Angry (怒り)");
        GUI.Label(new Rect(20, 185, 200, 20), "3 : Sad (悲しみ)");
        GUI.Label(new Rect(20, 205, 200, 20), "4 : Surprised (驚き)");
        GUI.Label(new Rect(20, 225, 200, 20), "5 : Relaxed (リラックス)");
        GUI.Label(new Rect(20, 245, 200, 20), "0 : Neutral (通常)");
    }
}
