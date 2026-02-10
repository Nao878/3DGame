using UnityEngine;

/// <summary>
/// VRoidモデルの攻撃アクションを制御するスクリプト
/// Starter AssetsのInput Systemと連携して動作
/// Ghost Parent構造のVRoid側にアタッチ
/// </summary>
public class VRoidAttackController : MonoBehaviour
{
    [Header("攻撃設定")]
    [Tooltip("攻撃アニメーションの長さ（秒）")]
    public float attackDuration = 0.8f;
    
    [Tooltip("攻撃中の移動速度倍率（0で停止、1で通常）")]
    [Range(0f, 1f)]
    public float moveSpeedMultiplier = 0.1f;

    [Header("参照")]
    [Tooltip("親のStarter Assets Inputs（空の場合は親から自動検索）")]
    public MonoBehaviour starterAssetsInputs;

    [Header("デバッグ")]
    public bool showDebugLog = false;

    // Animatorパラメータ
    private int attackHash;
    private int attackSpeedMultiplierHash;
    
    // 攻撃状態
    private bool isAttacking = false;
    private float attackTimer = 0f;
    
    // コンポーネント参照
    private Animator vroidAnimator;
    private Animator parentAnimator;
    
    // Starter Assets Inputs のattackフィールド
    private System.Reflection.FieldInfo attackField;
    private System.Reflection.FieldInfo moveField;

    void Start()
    {
        vroidAnimator = GetComponent<Animator>();
        
        // 親のAnimatorとInputを取得
        if (starterAssetsInputs == null)
        {
            Transform parent = transform.parent;
            while (parent != null)
            {
                var inputType = System.Type.GetType("StarterAssets.StarterAssetsInputs, Assembly-CSharp");
                if (inputType != null)
                {
                    var component = parent.GetComponent(inputType);
                    if (component != null)
                    {
                        starterAssetsInputs = component as MonoBehaviour;
                        break;
                    }
                }
                parent = parent.parent;
            }
        }

        if (transform.parent != null)
        {
            parentAnimator = transform.parent.GetComponent<Animator>();
        }

        // StarterAssetsInputs のattackフィールドを取得
        if (starterAssetsInputs != null)
        {
            attackField = starterAssetsInputs.GetType().GetField("attack");
            moveField = starterAssetsInputs.GetType().GetField("move");
        }

        // Animatorパラメータ
        attackHash = Animator.StringToHash("Attack");
        attackSpeedMultiplierHash = Animator.StringToHash("AttackSpeedMultiplier");

        Debug.Log("[VRoidAttackController] 初期化完了");
    }

    void Update()
    {
        HandleAttackInput();
        UpdateAttackState();
    }

    void HandleAttackInput()
    {
        bool attackInput = false;

        // Starter Assets Input SystemからAttackを取得
        if (attackField != null && starterAssetsInputs != null)
        {
            attackInput = (bool)attackField.GetValue(starterAssetsInputs);
        }

        // レガシー入力のフォールバック（Eキー/左クリック）
        if (!attackInput)
        {
            attackInput = Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0);
        }

        // 攻撃開始
        if (attackInput && !isAttacking)
        {
            StartAttack();
        }

        // Starter Assets側のattackフラグをリセット
        if (attackField != null && starterAssetsInputs != null)
        {
            attackField.SetValue(starterAssetsInputs, false);
        }
    }

    void StartAttack()
    {
        isAttacking = true;
        attackTimer = attackDuration;

        // Animatorにトリガーを設定（両方のAnimatorに）
        if (vroidAnimator != null)
        {
            vroidAnimator.SetTrigger(attackHash);
        }
        if (parentAnimator != null)
        {
            parentAnimator.SetTrigger(attackHash);
        }

        if (showDebugLog)
        {
            Debug.Log("[VRoidAttackController] 攻撃開始！");
        }
    }

    void UpdateAttackState()
    {
        if (!isAttacking) return;

        attackTimer -= Time.deltaTime;
        
        if (attackTimer <= 0f)
        {
            isAttacking = false;
            
            if (showDebugLog)
            {
                Debug.Log("[VRoidAttackController] 攻撃終了");
            }
        }
    }

    /// <summary>
    /// 外部から攻撃中かどうかを確認
    /// </summary>
    public bool IsAttacking()
    {
        return isAttacking;
    }

    /// <summary>
    /// 攻撃中の移動速度倍率を取得
    /// </summary>
    public float GetMoveSpeedMultiplier()
    {
        return isAttacking ? moveSpeedMultiplier : 1f;
    }
}
