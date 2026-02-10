using UnityEngine;

/// <summary>
/// 親オブジェクトのAnimatorパラメータを子オブジェクトに同期する
/// Ghost Parent構造でVRoidを動かすためのスクリプト
/// </summary>
public class AnimatorSync : MonoBehaviour
{
    [Header("同期設定")]
    [Tooltip("パラメータの同期元（親のAnimator）。空の場合は親を自動検索")]
    public Animator sourceAnimator;
    
    [Tooltip("パラメータの同期先（自分のAnimator）。空の場合は自動取得")]
    public Animator targetAnimator;

    [Header("デバッグ")]
    public bool showDebugLog = false;

    // Starter Assetsで使用されるパラメータID
    private int speedHash;
    private int motionSpeedHash;
    private int groundedHash;
    private int jumpHash;
    private int freeFallHash;
    private int attackHash;

    // Attack同期用
    private bool hasAttackParam = false;

    void Start()
    {
        // Animatorの自動取得
        if (targetAnimator == null)
        {
            targetAnimator = GetComponent<Animator>();
        }

        if (sourceAnimator == null)
        {
            // 親からAnimatorを検索
            Transform parent = transform.parent;
            while (parent != null)
            {
                sourceAnimator = parent.GetComponent<Animator>();
                if (sourceAnimator != null)
                {
                    break;
                }
                parent = parent.parent;
            }
        }

        if (sourceAnimator == null)
        {
            Debug.LogError("[AnimatorSync] 同期元のAnimatorが見つかりません！");
            enabled = false;
            return;
        }

        if (targetAnimator == null)
        {
            Debug.LogError("[AnimatorSync] 同期先のAnimatorが見つかりません！");
            enabled = false;
            return;
        }

        // パラメータIDをキャッシュ
        speedHash = Animator.StringToHash("Speed");
        motionSpeedHash = Animator.StringToHash("MotionSpeed");
        groundedHash = Animator.StringToHash("Grounded");
        jumpHash = Animator.StringToHash("Jump");
        freeFallHash = Animator.StringToHash("FreeFall");
        attackHash = Animator.StringToHash("Attack");

        // Attackパラメータが存在するか確認
        foreach (var param in sourceAnimator.parameters)
        {
            if (param.name == "Attack")
            {
                hasAttackParam = true;
                break;
            }
        }

        Debug.Log($"[AnimatorSync] 同期開始: {sourceAnimator.gameObject.name} → {targetAnimator.gameObject.name} (Attack={hasAttackParam})");
    }

    void Update()
    {
        if (sourceAnimator == null || targetAnimator == null) return;

        SyncParameters();
    }

    void SyncParameters()
    {
        // Float パラメータ
        float speed = sourceAnimator.GetFloat(speedHash);
        float motionSpeed = sourceAnimator.GetFloat(motionSpeedHash);
        
        targetAnimator.SetFloat(speedHash, speed);
        targetAnimator.SetFloat(motionSpeedHash, motionSpeed);

        // Bool パラメータ
        bool grounded = sourceAnimator.GetBool(groundedHash);
        bool jump = sourceAnimator.GetBool(jumpHash);
        bool freeFall = sourceAnimator.GetBool(freeFallHash);

        targetAnimator.SetBool(groundedHash, grounded);
        targetAnimator.SetBool(jumpHash, jump);
        targetAnimator.SetBool(freeFallHash, freeFall);

        // Attackトリガーの同期
        if (hasAttackParam)
        {
            // AnimatorStateInfoで攻撃中かどうかを判定
            var sourceInfo = sourceAnimator.GetCurrentAnimatorStateInfo(0);
            var targetInfo = targetAnimator.GetCurrentAnimatorStateInfo(0);
            
            // 親が攻撃ステートに入ったら子にもトリガー
            if (sourceInfo.IsName("Attack") && !targetInfo.IsName("Attack"))
            {
                targetAnimator.SetTrigger(attackHash);
            }
        }

        if (showDebugLog)
        {
            Debug.Log($"[AnimatorSync] Speed={speed:F2}, MotionSpeed={motionSpeed:F2}, Grounded={grounded}, Jump={jump}, FreeFall={freeFall}");
        }
    }
}
