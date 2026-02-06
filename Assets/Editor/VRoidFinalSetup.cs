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

        // ステップ5: ロボットのメッシュを非表示
        HideRobotMesh(playerArmature);

        // ステップ6: Animator Controllerをコピー
        SetupVRoidAnimator(playerArmature, vroidModel);

        // ステップ7: 表情コントローラーを追加
        SetupExpressionController(vroidModel);

        // ステップ8: カーソルコントローラーを追加
        SetupCursorController(playerArmature);

        // 保存
        EditorUtility.SetDirty(playerArmature);
        EditorUtility.SetDirty(vroidModel);

        string message = "Ghost Parentセットアップが完了しました！\n\n";
        message += "【構造】\n";
        message += $"PlayerArmature (親)\n";
        message += $"  └ {vroidModel.name} (VRoid)\n\n";
        message += "【動作確認】\n";
        message += "• ゲームを再生してWASD移動を確認\n";
        message += "• ロボットの見た目は非表示\n";
        message += "• VRoidモデルがアニメーション付きで移動\n\n";
        message += "【操作方法】\n";
        message += "WASD: 移動\n";
        message += "マウス: 視点操作\n";
        message += "Space: ジャンプ\n";
        message += "Shift: ダッシュ\n";
        message += "ESC: カーソルロック解除\n";
        message += "1-5: 表情変更";

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

    // ===== ロボットメッシュ非表示 =====
    private static void HideRobotMesh(GameObject playerArmature)
    {
        // Geometryという名前の子オブジェクトを検索
        Transform geometry = playerArmature.transform.Find("Geometry");
        if (geometry != null)
        {
            geometry.gameObject.SetActive(false);
            Debug.Log("[GhostParent] Geometry を非表示にしました");
            return;
        }

        // SkinnedMeshRendererを持つオブジェクトを検索して非表示
        var renderers = playerArmature.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        int hiddenCount = 0;
        foreach (var renderer in renderers)
        {
            // VRoidのメッシュは除外
            if (renderer.gameObject.name.ToLower().Contains("vroid") ||
                renderer.gameObject.name.ToLower().Contains("vrm"))
            {
                continue;
            }

            // PlayerArmatureの直接の子孫のみ対象
            Transform parent = renderer.transform.parent;
            bool isPlayerChild = false;
            while (parent != null)
            {
                if (parent == playerArmature.transform)
                {
                    isPlayerChild = true;
                    break;
                }
                // VRoidの子なら除外
                if (parent.name.ToLower().Contains("vroid") || parent.name.ToLower().Contains("vrm"))
                {
                    break;
                }
                parent = parent.parent;
            }

            if (isPlayerChild)
            {
                renderer.enabled = false;
                hiddenCount++;
            }
        }

        // MeshRendererも同様に処理
        var meshRenderers = playerArmature.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var renderer in meshRenderers)
        {
            if (renderer.gameObject.name.ToLower().Contains("vroid") ||
                renderer.gameObject.name.ToLower().Contains("vrm"))
            {
                continue;
            }

            Transform parent = renderer.transform.parent;
            bool isPlayerChild = false;
            while (parent != null)
            {
                if (parent == playerArmature.transform)
                {
                    isPlayerChild = true;
                    break;
                }
                if (parent.name.ToLower().Contains("vroid") || parent.name.ToLower().Contains("vrm"))
                {
                    break;
                }
                parent = parent.parent;
            }

            if (isPlayerChild)
            {
                renderer.enabled = false;
                hiddenCount++;
            }
        }

        if (hiddenCount > 0)
        {
            Debug.Log($"[GhostParent] {hiddenCount}個のRendererを非表示にしました");
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

        // ロボット側のAnimatorは無効化（二重アニメーション防止）
        // ただし、ThirdPersonControllerがAnimatorを参照するため、そのままにする
        // playerAnimator.enabled = false; // 必要に応じてコメント解除
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
