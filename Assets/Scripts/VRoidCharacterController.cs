using UnityEngine;

/// <summary>
/// VRoidモデル用のキャラクターコントローラー
/// Unityちゃんの移動機能をVRoid向けに簡略化したもの
/// </summary>
[RequireComponent(typeof(Animator))]
public class VRoidCharacterController : MonoBehaviour
{
    [Header("移動設定")]
    public float forwardSpeed = 5.0f;      // 前進速度
    public float backwardSpeed = 2.0f;     // 後退速度
    public float rotateSpeed = 120.0f;     // 旋回速度（度/秒）
    public float jumpPower = 5.0f;         // ジャンプ力

    [Header("物理設定")]
    public float colliderHeight = 1.6f;    // コライダーの高さ
    public float colliderRadius = 0.3f;    // コライダーの半径
    public float colliderCenterY = 0.8f;   // コライダー中心のY座標

    [Header("アニメーション設定")]
    public bool useAnimator = true;        // Animatorを使用するか
    public float animSpeed = 1.0f;         // アニメーション速度

    [Header("デバッグ")]
    public bool showDebugGUI = true;       // デバッグGUIを表示

    private Animator anim;
    private Rigidbody rb;
    private CapsuleCollider col;
    private bool isGrounded = true;
    private Vector3 velocity;

    void Start()
    {
        SetupComponents();
    }

    void SetupComponents()
    {
        // Animator
        anim = GetComponent<Animator>();

        // Rigidbody（なければ追加）
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // CapsuleCollider（なければ追加）
        col = GetComponent<CapsuleCollider>();
        if (col == null)
        {
            col = gameObject.AddComponent<CapsuleCollider>();
        }
        col.height = colliderHeight;
        col.radius = colliderRadius;
        col.center = new Vector3(0, colliderCenterY, 0);
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void Update()
    {
        HandleJump();
        CheckGrounded();
    }

    void HandleMovement()
    {
        // 入力取得（レガシーInput API）
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // アニメーター更新
        if (useAnimator && anim != null)
        {
            anim.SetFloat("Speed", v);
            anim.SetFloat("Direction", h);
            anim.speed = animSpeed;
        }

        // 移動処理
        velocity = new Vector3(0, 0, v);
        velocity = transform.TransformDirection(velocity);

        if (v > 0.1f)
        {
            velocity *= forwardSpeed;
        }
        else if (v < -0.1f)
        {
            velocity *= backwardSpeed;
        }

        // 位置と回転の更新
        transform.localPosition += velocity * Time.fixedDeltaTime;
        transform.Rotate(0, h * rotateSpeed * Time.fixedDeltaTime, 0);
    }

    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpPower, ForceMode.VelocityChange);
            isGrounded = false;

            if (useAnimator && anim != null)
            {
                anim.SetBool("Jump", true);
            }
        }
    }

    void CheckGrounded()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.1f, -Vector3.up);
        if (Physics.Raycast(ray, 0.2f))
        {
            isGrounded = true;
            if (useAnimator && anim != null)
            {
                anim.SetBool("Jump", false);
            }
        }
    }

    void OnGUI()
    {
        if (!showDebugGUI) return;

        GUI.Box(new Rect(10, 10, 220, 100), "VRoid Character Control");
        GUI.Label(new Rect(20, 35, 200, 20), "↑↓ / W S : 前進/後退");
        GUI.Label(new Rect(20, 55, 200, 20), "←→ / A D : 左右旋回");
        GUI.Label(new Rect(20, 75, 200, 20), "Space : ジャンプ");
    }

    void OnCollisionEnter(Collision collision)
    {
        // 地面に接触したらジャンプ可能に
        if (collision.contacts.Length > 0)
        {
            foreach (var contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f)
                {
                    isGrounded = true;
                    break;
                }
            }
        }
    }
}
