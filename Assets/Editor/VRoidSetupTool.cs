using UnityEditor;
using UnityEngine;

/// <summary>
/// VRoidモデルに必要なコンポーネントを一括セットアップするエディタツール
/// </summary>
public class VRoidSetupTool : EditorWindow
{
    private static readonly string UNITYCHAN_LOCOMOTION_CONTROLLER = 
        "Assets/unity-chan!/Unity-chan! Model/Art/Animations/Animators/UnityChanLocomotions.controller";

    [MenuItem("Tools/Setup VRoid Character")]
    public static void SetupVRoidCharacter()
    {
        // シーン内のVRoidモデルを検索
        GameObject vroidModel = FindVRoidModel();
        
        if (vroidModel == null)
        {
            EditorUtility.DisplayDialog(
                "VRoidモデルが見つかりません",
                "シーン内にVRoidモデル（Vrm10Instanceコンポーネント）が見つかりません。\n\nVRMファイルをシーンにドラッグ＆ドロップしてから再実行してください。",
                "OK"
            );
            return;
        }

        Debug.Log($"[VRoidSetupTool] VRoidモデル発見: {vroidModel.name}");

        // セットアップ実行
        SetupCharacterController(vroidModel);
        SetupBlinkController(vroidModel);
        SetupExpressionController(vroidModel);
        SetupAnimator(vroidModel);

        EditorUtility.SetDirty(vroidModel);

        EditorUtility.DisplayDialog(
            "セットアップ完了",
            $"VRoidモデル「{vroidModel.name}」のセットアップが完了しました。\n\n" +
            "追加されたコンポーネント:\n" +
            "• VRoidCharacterController (移動)\n" +
            "• VRoidBlinkController (まばたき)\n" +
            "• VRoidExpressionController (表情)\n" +
            "• Rigidbody\n" +
            "• CapsuleCollider\n\n" +
            "操作方法:\n" +
            "• WASD/矢印キー: 移動\n" +
            "• Space: ジャンプ\n" +
            "• 1-5: 表情切替\n" +
            "• 0: 通常表情",
            "OK"
        );

        Debug.Log("[VRoidSetupTool] セットアップ完了");
    }

    private static GameObject FindVRoidModel()
    {
        // Vrm10Instanceを持つオブジェクトを検索
        var vrm10Type = System.Type.GetType("UniVRM10.Vrm10Instance, VRM10");
        if (vrm10Type != null)
        {
            var instances = Object.FindObjectsOfType(vrm10Type);
            if (instances.Length > 0)
            {
                var component = instances[0] as Component;
                if (component != null)
                {
                    return component.gameObject;
                }
            }
        }

        // フォールバック: "VRoid"または"vrm"を含む名前のオブジェクトを検索
        var allObjects = Object.FindObjectsOfType<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj.name.ToLower().Contains("vroid") || 
                obj.name.ToLower().Contains("vrm") ||
                obj.name.ToLower().Contains("avatar"))
            {
                if (obj.GetComponent<Animator>() != null)
                {
                    return obj;
                }
            }
        }

        return null;
    }

    private static void SetupCharacterController(GameObject vroidModel)
    {
        var controller = vroidModel.GetComponent<VRoidCharacterController>();
        if (controller == null)
        {
            controller = vroidModel.AddComponent<VRoidCharacterController>();
            Debug.Log("[VRoidSetupTool] VRoidCharacterController を追加");
        }

        // Rigidbody設定
        var rb = vroidModel.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = vroidModel.AddComponent<Rigidbody>();
        }
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.mass = 50f;
        rb.linearDamping = 0.5f;

        // CapsuleCollider設定
        var col = vroidModel.GetComponent<CapsuleCollider>();
        if (col == null)
        {
            col = vroidModel.AddComponent<CapsuleCollider>();
        }
        col.height = 1.6f;
        col.radius = 0.25f;
        col.center = new Vector3(0, 0.8f, 0);

        Debug.Log("[VRoidSetupTool] Rigidbody と CapsuleCollider を設定");
    }

    private static void SetupBlinkController(GameObject vroidModel)
    {
        var blink = vroidModel.GetComponent<VRoidBlinkController>();
        if (blink == null)
        {
            blink = vroidModel.AddComponent<VRoidBlinkController>();
            blink.vrmObject = vroidModel;
            Debug.Log("[VRoidSetupTool] VRoidBlinkController を追加");
        }
    }

    private static void SetupExpressionController(GameObject vroidModel)
    {
        var expression = vroidModel.GetComponent<VRoidExpressionController>();
        if (expression == null)
        {
            expression = vroidModel.AddComponent<VRoidExpressionController>();
            expression.vrmObject = vroidModel;
            Debug.Log("[VRoidSetupTool] VRoidExpressionController を追加");
        }
    }

    private static void SetupAnimator(GameObject vroidModel)
    {
        var animator = vroidModel.GetComponent<Animator>();
        if (animator == null)
        {
            animator = vroidModel.AddComponent<Animator>();
        }

        // Unityちゃんの Animator Controller を読み込み
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(UNITYCHAN_LOCOMOTION_CONTROLLER);
        
        if (controller != null)
        {
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            Debug.Log("[VRoidSetupTool] Animator Controller を設定: UnityChanLocomotions");
        }
        else
        {
            Debug.LogWarning($"[VRoidSetupTool] Animator Controller が見つかりません: {UNITYCHAN_LOCOMOTION_CONTROLLER}");
            Debug.LogWarning("[VRoidSetupTool] VRoidCharacterController.useAnimator を false に設定しました");
            
            var charController = vroidModel.GetComponent<VRoidCharacterController>();
            if (charController != null)
            {
                charController.useAnimator = false;
            }
        }
    }

    [MenuItem("Tools/Remove VRoid Components")]
    public static void RemoveVRoidComponents()
    {
        GameObject vroidModel = FindVRoidModel();
        
        if (vroidModel == null)
        {
            EditorUtility.DisplayDialog("エラー", "VRoidモデルが見つかりません。", "OK");
            return;
        }

        // コンポーネント削除
        var charController = vroidModel.GetComponent<VRoidCharacterController>();
        var blinkController = vroidModel.GetComponent<VRoidBlinkController>();
        var expressionController = vroidModel.GetComponent<VRoidExpressionController>();
        var rb = vroidModel.GetComponent<Rigidbody>();
        var col = vroidModel.GetComponent<CapsuleCollider>();

        if (charController != null) DestroyImmediate(charController);
        if (blinkController != null) DestroyImmediate(blinkController);
        if (expressionController != null) DestroyImmediate(expressionController);
        if (col != null) DestroyImmediate(col);
        if (rb != null) DestroyImmediate(rb);

        EditorUtility.SetDirty(vroidModel);

        EditorUtility.DisplayDialog(
            "削除完了",
            "VRoidモデルからコンポーネントを削除しました。",
            "OK"
        );
    }
}
