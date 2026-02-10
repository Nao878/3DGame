using UnityEngine;
using UnityEditor;

/// <summary>
/// VRoidモデルをPlayerArmatureの子にして、ロボットの外見を透明化する
/// 「Ghost Parent」構造のセットアップツール
/// </summary>
public class VRoidGhostParentSetup : EditorWindow
{
    private static readonly string STARTER_ASSETS_ANIMATOR_PATH = 
        "Assets/StarterAssets/ThirdPersonController/Character/Animations/StarterAssetsThirdPerson.controller";

    [MenuItem("Tools/Finalize VRoid Setup && Fix Errors", priority = 1)]
    public static void SetupGhostParent()
    {
        Debug.Log("========================================");
        Debug.Log("=== VRoid Ghost Parent セットアップ開始 ===");
        Debug.Log("========================================");

        // ステップ1: PlayerArmature（ロボット）を検索
        GameObject playerArmature = FindPlayerArmature();
        if (playerArmature == null)
        {
            EditorUtility.DisplayDialog(
                "PlayerArmatureが見つかりません",
                "シーン内にPlayerArmature（Starter Assetsのプレイヤー）が見つかりません。\n\nPlaygroundシーンを開いているか確認してください。",
                "OK"
            );
            return;
        }
        Debug.Log($"[GhostParent] PlayerArmature発見: {playerArmature.name}");

        // ステップ2: VRoidモデルを検索
        GameObject vroidModel = FindVRoidModel(playerArmature);
        if (vroidModel == null)
        {
            EditorUtility.DisplayDialog(
                "VRoidモデルが見つかりません",
                "シーン内にVRoidモデルが見つかりません。\n\nVRMファイルをシーンにドラッグ＆ドロップしてから再実行してください。",
                "OK"
            );
            return;
        }
        Debug.Log($"[GhostParent] VRoidモデル発見: {vroidModel.name}");

        // ステップ3: VRoidを既にセットアップ済みか確認
        bool alreadyChild = vroidModel.transform.parent == playerArmature.transform;

        // ステップ4: VRoidをPlayerArmatureの子にする
        if (!alreadyChild)
        {
            Undo.SetTransformParent(vroidModel.transform, playerArmature.transform, "VRoid Ghost Parent Setup");
            vroidModel.transform.localPosition = Vector3.zero;
            vroidModel.transform.localRotation = Quaternion.identity;
            vroidModel.transform.localScale = Vector3.one;
            Debug.Log("[GhostParent] VRoidをPlayerArmatureの子オブジェクトに設定");
        }

        // ステップ5: ロボットのメッシュを完全非表示
        HideRobotMeshCompletely(playerArmature, vroidModel);

        // ステップ6: VRoidの表示を確実に有効化
        EnsureVRoidVisible(vroidModel);

        // ステップ7: Animator Controllerをコピー
        SetupVRoidAnimator(playerArmature, vroidModel);

        // ステップ8: Animator Controllerに攻撃ステートを追加
        AttackAnimatorSetup.SetupAttackState();

        // ステップ9: AnimatorSyncを追加（パラメータ同期）
        SetupAnimatorSync(playerArmature, vroidModel);

        // ステップ10: AnimationEventReceiverを追加（警告解消）
        SetupAnimationEventReceiver(vroidModel);

        // ステップ11: 攻撃コントローラーを追加
        SetupAttackController(vroidModel);

        // ステップ12: 表情コントローラーを追加
        SetupExpressionController(vroidModel);

        // ステップ13: 自動まばたきを追加
        SetupAutoBlink(vroidModel);

        // ステップ14: カーソルコントローラーを追加
        SetupCursorController(playerArmature);

        // 保存
        EditorUtility.SetDirty(playerArmature);
        EditorUtility.SetDirty(vroidModel);

        string message = "Ghost Parentセットアップが完了しました！\n\n";
        message += "【構造】\n";
        message += $"PlayerArmature (親)\n";
        message += $"  └ {vroidModel.name} (VRoid)\n\n";
        message += "【操作方法】\n";
        message += "WASD: 移動\n";
        message += "マウス: 視点操作\n";
        message += "Space: ジャンプ\n";
        message += "Shift: ダッシュ\n";
        message += "左クリック/E: 攻撃\n";
        message += "1-5: 表情変更\n";
        message += "0: 表情リセット\n";
        message += "ESC: カーソルロック解除";

        Debug.Log("=== VRoid Ghost Parent セットアップ完了 ===");
        EditorUtility.DisplayDialog("セットアップ完了", message, "OK");
    }

    // ===== PlayerArmature検索 =====
    private static GameObject FindPlayerArmature()
    {
        // まず名前で検索
        GameObject player = GameObject.Find("PlayerArmature");
        if (player != null) return player;

        // ThirdPersonControllerを持つオブジェクトを検索
        var tpcType = System.Type.GetType("StarterAssets.ThirdPersonController, Assembly-CSharp");
        if (tpcType != null)
        {
            var tpc = Object.FindObjectOfType(tpcType) as Component;
            if (tpc != null)
            {
                return tpc.gameObject;
            }
        }

        // CharacterControllerを持つオブジェクトを検索
        var characterControllers = Object.FindObjectsOfType<CharacterController>();
        foreach (var cc in characterControllers)
        {
            // VRoidは除外
            if (!cc.gameObject.name.ToLower().Contains("vroid") && 
                !cc.gameObject.name.ToLower().Contains("vrm"))
            {
                return cc.gameObject;
            }
        }

        return null;
    }

    // ===== VRoidモデル検索 =====
    private static GameObject FindVRoidModel(GameObject excludeParent)
    {
        // まず既にPlayerArmatureの子にいるかチェック
        foreach (Transform child in excludeParent.transform)
        {
            if (IsVRoidModel(child.gameObject))
            {
                return child.gameObject;
            }
        }

        // Vrm10Instanceを持つオブジェクトを検索
        var vrm10Type = System.Type.GetType("UniVRM10.Vrm10Instance, VRM10");
        if (vrm10Type != null)
        {
            var instances = Object.FindObjectsOfType(vrm10Type);
            foreach (var instance in instances)
            {
                var component = instance as Component;
                if (component != null && component.gameObject != excludeParent)
                {
                    return component.gameObject;
                }
            }
        }

        // フォールバック: 名前で検索
        var allObjects = Object.FindObjectsOfType<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj != excludeParent && IsVRoidModel(obj))
            {
                return obj;
            }
        }

        return null;
    }

    private static bool IsVRoidModel(GameObject obj)
    {
        string nameLower = obj.name.ToLower();
        if ((nameLower.Contains("vroid") || nameLower.Contains("vrm") || nameLower.Contains("avatar")) 
            && obj.GetComponent<Animator>() != null)
        {
            // PlayerArmatureは除外
            if (obj.GetComponent<CharacterController>() != null) return false;
            return true;
        }
        return false;
    }

    // ===== ロボットメッシュを完全非表示（影も消す） =====
    private static void HideRobotMeshCompletely(GameObject playerArmature, GameObject vroidModel)
    {
        int hiddenCount = 0;

        // Geometryという名前の子オブジェクトを検索して無効化
        Transform geometry = playerArmature.transform.Find("Geometry");
        if (geometry != null)
        {
            geometry.gameObject.SetActive(false);
            Debug.Log("[GhostParent] Geometry を非表示にしました");
            hiddenCount++;
        }

        // 全てのRendererを検索して完全に無効化（影も消す）
        // ただしVRoidの子は除外
        var skinnedRenderers = playerArmature.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var renderer in skinnedRenderers)
        {
            // VRoidの子孫は除外
            if (IsChildOf(renderer.transform, vroidModel.transform)) continue;
            
            renderer.enabled = false;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            hiddenCount++;
        }

        var meshRenderers = playerArmature.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var renderer in meshRenderers)
        {
            if (IsChildOf(renderer.transform, vroidModel.transform)) continue;
            
            renderer.enabled = false;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            hiddenCount++;
        }

        Debug.Log($"[GhostParent] {hiddenCount}個のRendererを完全非表示にしました");
    }

    private static bool IsChildOf(Transform child, Transform parent)
    {
        Transform t = child;
        while (t != null)
        {
            if (t == parent) return true;
            t = t.parent;
        }
        return false;
    }

    // ===== VRoidの表示を確実に有効化 =====
    private static void EnsureVRoidVisible(GameObject vroidModel)
    {
        // GameObjectを有効化
        vroidModel.SetActive(true);
        
        // Scaleを確認・修正
        if (vroidModel.transform.localScale == Vector3.zero)
        {
            vroidModel.transform.localScale = Vector3.one;
            Debug.Log("[GhostParent] VRoidのScaleを(1,1,1)に修正");
        }

        // 全てのRendererを有効化
        var skinnedRenderers = vroidModel.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        int enabledCount = 0;
        foreach (var renderer in skinnedRenderers)
        {
            renderer.enabled = true;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            enabledCount++;
        }

        var meshRenderers = vroidModel.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var renderer in meshRenderers)
        {
            renderer.enabled = true;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            enabledCount++;
        }

        Debug.Log($"[GhostParent] VRoidの{enabledCount}個のRendererを有効化しました");
    }

    // ===== AnimationEventReceiver設定 =====
    private static void SetupAnimationEventReceiver(GameObject vroidModel)
    {
        var receiverType = System.Type.GetType("AnimationEventReceiver, Assembly-CSharp");
        if (receiverType != null)
        {
            var existing = vroidModel.GetComponent(receiverType);
            if (existing == null)
            {
                vroidModel.AddComponent(receiverType);
                Debug.Log("[GhostParent] AnimationEventReceiver を追加");
            }
        }
        else
        {
            Debug.LogWarning("[GhostParent] AnimationEventReceiver スクリプトが見つかりません");
        }
    }

    // ===== VRoid Animator設定 =====
    private static void SetupVRoidAnimator(GameObject playerArmature, GameObject vroidModel)
    {
        var playerAnimator = playerArmature.GetComponent<Animator>();
        var vroidAnimator = vroidModel.GetComponent<Animator>();

        if (playerAnimator == null || vroidAnimator == null)
        {
            Debug.LogWarning("[GhostParent] Animatorが見つかりません");
            return;
        }

        // ロボット側のAnimator Controllerを取得
        RuntimeAnimatorController controller = playerAnimator.runtimeAnimatorController;
        
        if (controller == null)
        {
            // パスから読み込み
            controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(STARTER_ASSETS_ANIMATOR_PATH);
        }

        if (controller != null)
        {
            vroidAnimator.runtimeAnimatorController = controller;
            vroidAnimator.applyRootMotion = false; // Root Motionは親のCharacterControllerが担当
            
            Debug.Log($"[GhostParent] VRoidに Animator Controller を設定: {controller.name}");
        }
        else
        {
            Debug.LogWarning("[GhostParent] Starter Assets Animator Controller が見つかりません");
        }

    }

    // ===== AnimatorSync設定 =====
    private static void SetupAnimatorSync(GameObject playerArmature, GameObject vroidModel)
    {
        var syncType = System.Type.GetType("AnimatorSync, Assembly-CSharp");
        if (syncType != null)
        {
            var existing = vroidModel.GetComponent(syncType);
            if (existing == null)
            {
                var sync = vroidModel.AddComponent(syncType);
                // sourceAnimatorを設定
                var sourceField = syncType.GetField("sourceAnimator");
                if (sourceField != null)
                {
                    sourceField.SetValue(sync, playerArmature.GetComponent<Animator>());
                }
                Debug.Log("[GhostParent] AnimatorSync を追加");
            }
        }
        else
        {
            Debug.LogWarning("[GhostParent] AnimatorSync スクリプトが見つかりません");
        }
    }

    // ===== 表情コントローラー =====
    private static void SetupExpressionController(GameObject vroidModel)
    {
        var expressionType = System.Type.GetType("VRoidExpressionController, Assembly-CSharp");
        if (expressionType != null)
        {
            var existing = vroidModel.GetComponent(expressionType);
            if (existing == null)
            {
                var expression = vroidModel.AddComponent(expressionType);
                var vrmObjField = expressionType.GetField("vrmObject");
                if (vrmObjField != null)
                {
                    vrmObjField.SetValue(expression, vroidModel);
                }
                Debug.Log("[GhostParent] VRoidExpressionController を追加");
            }
        }
    }

    // ===== 攻撃コントローラー =====
    private static void SetupAttackController(GameObject vroidModel)
    {
        var attackType = System.Type.GetType("VRoidAttackController, Assembly-CSharp");
        if (attackType != null)
        {
            var existing = vroidModel.GetComponent(attackType);
            if (existing == null)
            {
                vroidModel.AddComponent(attackType);
                Debug.Log("[GhostParent] VRoidAttackController を追加");
            }
        }
        else
        {
            Debug.LogWarning("[GhostParent] VRoidAttackController スクリプトが見つかりません");
        }
    }

    // ===== 自動まばたき =====
    private static void SetupAutoBlink(GameObject vroidModel)
    {
        var blinkType = System.Type.GetType("VRoidAutoBlink, Assembly-CSharp");
        if (blinkType != null)
        {
            var existing = vroidModel.GetComponent(blinkType);
            if (existing == null)
            {
                var blink = vroidModel.AddComponent(blinkType);
                var vrmObjField = blinkType.GetField("vrmObject");
                if (vrmObjField != null)
                {
                    vrmObjField.SetValue(blink, vroidModel);
                }
                Debug.Log("[GhostParent] VRoidAutoBlink を追加");
            }
        }
        else
        {
            Debug.LogWarning("[GhostParent] VRoidAutoBlink スクリプトが見つかりません");
        }
    }

    // ===== カーソルコントローラー =====
    private static void SetupCursorController(GameObject playerArmature)
    {
        var cursorType = System.Type.GetType("CursorController, Assembly-CSharp");
        if (cursorType != null)
        {
            var existing = playerArmature.GetComponent(cursorType);
            if (existing == null)
            {
                playerArmature.AddComponent(cursorType);
                Debug.Log("[GhostParent] CursorController を追加");
            }
        }
    }

    // ===== リセット機能 =====
    [MenuItem("Tools/Reset VRoid Ghost Parent")]
    public static void ResetGhostParent()
    {
        GameObject playerArmature = FindPlayerArmature();
        if (playerArmature == null)
        {
            EditorUtility.DisplayDialog("エラー", "PlayerArmatureが見つかりません。", "OK");
            return;
        }

        // VRoidモデルを親から外す
        foreach (Transform child in playerArmature.transform)
        {
            if (IsVRoidModel(child.gameObject))
            {
                Undo.SetTransformParent(child, null, "Reset VRoid Ghost Parent");
                child.position = playerArmature.transform.position + Vector3.right * 2;
                Debug.Log($"[GhostParent] {child.name} を親から解除");
            }
        }

        // ロボットメッシュを再表示
        Transform geometry = playerArmature.transform.Find("Geometry");
        if (geometry != null)
        {
            geometry.gameObject.SetActive(true);
        }

        var renderers = playerArmature.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var renderer in renderers)
        {
            renderer.enabled = true;
        }

        var meshRenderers = playerArmature.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var renderer in meshRenderers)
        {
            renderer.enabled = true;
        }

        EditorUtility.DisplayDialog("リセット完了", "Ghost Parent構造をリセットしました。", "OK");
    }
}
