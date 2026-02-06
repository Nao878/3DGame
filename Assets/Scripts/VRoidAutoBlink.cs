using UnityEngine;

/// <summary>
/// VRoidモデル用の自動まばたきスクリプト
/// UniVRM10のExpressionAPIを使用
/// </summary>
public class VRoidAutoBlink : MonoBehaviour
{
    [Header("まばたき設定")]
    [Tooltip("まばたきの最小間隔（秒）")]
    public float minInterval = 2.0f;
    
    [Tooltip("まばたきの最大間隔（秒）")]
    public float maxInterval = 6.0f;
    
    [Tooltip("まばたきの速度（秒）")]
    public float blinkSpeed = 0.1f;

    [Header("表情設定")]
    [Tooltip("基本表情（微笑み）の強さ 0-1")]
    [Range(0, 1)]
    public float baseSmileWeight = 0.2f;

    [Header("VRM設定")]
    [Tooltip("VRMオブジェクト（空の場合は自動検索）")]
    public GameObject vrmObject;

    // VRM10 API用
    private object vrm10Instance;
    private object expressionRuntime;
    private System.Reflection.MethodInfo setWeightMethod;
    private object blinkKey;
    private object happyKey;

    // まばたき状態
    private float nextBlinkTime;
    private float currentBlinkWeight = 0f;
    private bool isBlinking = false;
    private bool isClosing = true;

    void Start()
    {
        if (vrmObject == null)
        {
            vrmObject = gameObject;
        }

        SetupVRM10();
        ScheduleNextBlink();

        // 基本表情を設定
        if (baseSmileWeight > 0)
        {
            SetExpression(happyKey, baseSmileWeight);
        }
    }

    void SetupVRM10()
    {
        var vrm10Type = System.Type.GetType("UniVRM10.Vrm10Instance, VRM10");
        if (vrm10Type == null)
        {
            Debug.LogWarning("[VRoidAutoBlink] UniVRM10が見つかりません");
            enabled = false;
            return;
        }

        vrm10Instance = vrmObject.GetComponent(vrm10Type);
        if (vrm10Instance == null)
        {
            Debug.LogWarning("[VRoidAutoBlink] Vrm10Instanceが見つかりません");
            enabled = false;
            return;
        }

        // Runtime.Expression を取得
        var runtimeProp = vrm10Type.GetProperty("Runtime");
        if (runtimeProp != null)
        {
            var runtime = runtimeProp.GetValue(vrm10Instance);
            if (runtime != null)
            {
                var expressionProp = runtime.GetType().GetProperty("Expression");
                if (expressionProp != null)
                {
                    expressionRuntime = expressionProp.GetValue(runtime);
                    if (expressionRuntime != null)
                    {
                        setWeightMethod = expressionRuntime.GetType().GetMethod("SetWeight");
                    }
                }
            }
        }

        // ExpressionKey を取得
        var expressionKeyType = System.Type.GetType("UniVRM10.ExpressionKey, VRM10");
        if (expressionKeyType != null)
        {
            var blinkField = expressionKeyType.GetField("Blink", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (blinkField != null)
            {
                blinkKey = blinkField.GetValue(null);
            }

            var happyField = expressionKeyType.GetField("Happy", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (happyField != null)
            {
                happyKey = happyField.GetValue(null);
            }
        }

        if (blinkKey != null)
        {
            Debug.Log("[VRoidAutoBlink] 初期化完了");
        }
        else
        {
            Debug.LogWarning("[VRoidAutoBlink] ExpressionKeyの取得に失敗");
            enabled = false;
        }
    }

    void Update()
    {
        UpdateBlink();
    }

    void UpdateBlink()
    {
        if (isBlinking)
        {
            // まばたき中
            if (isClosing)
            {
                // 閉じる
                currentBlinkWeight += Time.deltaTime / blinkSpeed;
                if (currentBlinkWeight >= 1.0f)
                {
                    currentBlinkWeight = 1.0f;
                    isClosing = false;
                }
            }
            else
            {
                // 開く
                currentBlinkWeight -= Time.deltaTime / blinkSpeed;
                if (currentBlinkWeight <= 0.0f)
                {
                    currentBlinkWeight = 0.0f;
                    isBlinking = false;
                    ScheduleNextBlink();
                }
            }

            SetExpression(blinkKey, currentBlinkWeight);
        }
        else
        {
            // まばたき待機中
            if (Time.time >= nextBlinkTime)
            {
                StartBlink();
            }
        }
    }

    void StartBlink()
    {
        isBlinking = true;
        isClosing = true;
        currentBlinkWeight = 0f;
    }

    void ScheduleNextBlink()
    {
        nextBlinkTime = Time.time + Random.Range(minInterval, maxInterval);
    }

    void SetExpression(object key, float weight)
    {
        if (setWeightMethod == null || expressionRuntime == null || key == null) return;

        try
        {
            // Runtime.Expressionを再取得（フレームごとに変わる可能性があるため）
            var vrm10Type = System.Type.GetType("UniVRM10.Vrm10Instance, VRM10");
            var runtimeProp = vrm10Type.GetProperty("Runtime");
            var runtime = runtimeProp.GetValue(vrm10Instance);
            var expressionProp = runtime.GetType().GetProperty("Expression");
            var expression = expressionProp.GetValue(runtime);

            setWeightMethod.Invoke(expression, new object[] { key, weight });
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[VRoidAutoBlink] 表情設定エラー: {e.Message}");
        }
    }
}
