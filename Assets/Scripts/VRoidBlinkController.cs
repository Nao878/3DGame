using UnityEngine;
using System.Collections;

/// <summary>
/// VRoidモデル用の自動まばたきコントローラー
/// VRM10のExpression APIを使用
/// </summary>
public class VRoidBlinkController : MonoBehaviour
{
    [Header("まばたき設定")]
    public bool isActive = true;                // まばたき有効
    public float blinkInterval = 3.0f;          // まばたきの間隔（秒）
    public float blinkDuration = 0.1f;          // まばたきの時間（秒）
    public float randomVariation = 1.0f;        // 間隔のランダム変動幅

    [Header("VRM設定")]
    [Tooltip("VRM10Objectコンポーネントを持つオブジェクト（通常は自分自身）")]
    public GameObject vrmObject;

    private object vrm10Instance;
    private System.Reflection.MethodInfo setWeightMethod;
    private object blinkKey;
    private bool isBlinking = false;

    void Start()
    {
        if (vrmObject == null)
        {
            vrmObject = gameObject;
        }

        SetupVRM10();
        
        if (vrm10Instance != null)
        {
            StartCoroutine(BlinkRoutine());
        }
    }

    void SetupVRM10()
    {
        // VRM10のRuntimeを動的に取得（UniVRM v0.131.0対応）
        var vrm10ObjectType = System.Type.GetType("UniVRM10.Vrm10Instance, VRM10");
        
        if (vrm10ObjectType != null)
        {
            vrm10Instance = vrmObject.GetComponent(vrm10ObjectType);
            
            if (vrm10Instance != null)
            {
                // Runtime.Expression.SetWeight を取得
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
                                
                                // ExpressionKey.Blink を取得
                                var expressionKeyType = System.Type.GetType("UniVRM10.ExpressionKey, VRM10");
                                if (expressionKeyType != null)
                                {
                                    var blinkField = expressionKeyType.GetField("Blink", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                                    if (blinkField != null)
                                    {
                                        blinkKey = blinkField.GetValue(null);
                                    }
                                }
                            }
                        }
                    }
                }
                
                Debug.Log("[VRoidBlinkController] VRM10 まばたき初期化完了");
            }
        }
        
        if (vrm10Instance == null)
        {
            Debug.LogWarning("[VRoidBlinkController] VRM10Instanceが見つかりません。VRoidモデルにアタッチしてください。");
        }
    }

    IEnumerator BlinkRoutine()
    {
        while (true)
        {
            // ランダムな間隔で待機
            float waitTime = blinkInterval + Random.Range(-randomVariation, randomVariation);
            waitTime = Mathf.Max(0.5f, waitTime);
            yield return new WaitForSeconds(waitTime);

            if (isActive && !isBlinking)
            {
                yield return StartCoroutine(DoBlink());
            }
        }
    }

    IEnumerator DoBlink()
    {
        isBlinking = true;

        // 目を閉じる
        SetBlinkWeight(1.0f);
        yield return new WaitForSeconds(blinkDuration);

        // 目を開く
        SetBlinkWeight(0.0f);

        isBlinking = false;
    }

    void SetBlinkWeight(float weight)
    {
        if (setWeightMethod != null && blinkKey != null)
        {
            try
            {
                // VRM10のRuntimeを再取得（毎フレーム変わる可能性があるため）
                var vrm10ObjectType = System.Type.GetType("UniVRM10.Vrm10Instance, VRM10");
                var runtimeProp = vrm10ObjectType.GetProperty("Runtime");
                var runtime = runtimeProp.GetValue(vrm10Instance);
                var expressionProp = runtime.GetType().GetProperty("Expression");
                var expression = expressionProp.GetValue(runtime);
                
                setWeightMethod.Invoke(expression, new object[] { blinkKey, weight });
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[VRoidBlinkController] まばたき設定エラー: {e.Message}");
            }
        }
    }

    /// <summary>
    /// 手動でまばたきを実行
    /// </summary>
    public void TriggerBlink()
    {
        if (!isBlinking)
        {
            StartCoroutine(DoBlink());
        }
    }
}
