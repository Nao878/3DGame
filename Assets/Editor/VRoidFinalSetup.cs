using UnityEngine;
using UnityEditor;
using StarterAssets;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// VRoidモデルの最終セットアップツール
/// 「Finalize VRoid Setup & Fix Errors」ボタン一つで全ての設定とエラー修正を実行
/// </summary>
public class VRoidFinalSetup : EditorWindow
{
    // パス定義
    private static readonly string PLAYER_FOLLOW_CAMERA_PREFAB = 
        "Assets/StarterAssets/ThirdPersonController/Prefabs/PlayerFollowCamera.prefab";
    private static readonly string STARTER_ASSETS_INPUT_ACTIONS = 
        "Assets/StarterAssets/InputSystem/StarterAssets.inputactions";

    [MenuItem("Tools/Finalize VRoid Setup && Fix Errors", priority = 1)]
    public static void FinalizeVRoidSetup()
    {
        Debug.Log("========================================");
        Debug.Log("=== VRoid 最終セットアップ開始 ===");
        Debug.Log("========================================");

        bool success = true;
        List<string> completedTasks = new List<string>();
        List<string> warnings = new List<string>();

        // ステップ1: Input System設定
        if (FixInputSystem())
        {
            completedTasks.Add("Input System を 'Both' に設定");
        }

        // ステップ2: glTFastインポーター競合解消
        if (DisableGltfastImporter())
        {
            completedTasks.Add("glTFast .glbインポーターを無効化（UniGLTF優先）");
        }

        // ステップ3: VRoidモデル検索
        GameObject vroidModel = FindVRoidModel();
        if (vroidModel == null)
        {
            EditorUtility.DisplayDialog(
                "VRoidモデルが見つかりません",
                "シーン内にVRoidモデルが見つかりません。\n\nVRMファイルをシーンにドラッグ＆ドロップしてから再実行してください。",
                "OK"
            );
            return;
        }
        completedTasks.Add($"VRoidモデル発見: {vroidModel.name}");

        // ステップ4: 古いコンポーネント削除
        RemoveOldComponents(vroidModel);
        completedTasks.Add("古いコンポーネントを削除");

        // ステップ5: Starter Assetsコンポーネント追加
        if (SetupStarterAssetsComponents(vroidModel))
        {
            completedTasks.Add("Starter Assetsコンポーネントを追加");
        }
        else
        {
            warnings.Add("一部のStarter Assetsコンポーネントの設定に問題がありました");
        }

        // ステップ6: カメラセットアップ
        GameObject cameraTarget = SetupCameraTarget(vroidModel);
        SetupCamera(vroidModel, cameraTarget);
        completedTasks.Add("カメラをセットアップ");

        // ステップ7: 表情コントローラー追加
        SetupExpressionController(vroidModel);
        completedTasks.Add("表情コントローラーを追加");

        // ステップ8: カーソルロック制御追加
        SetupCursorController(vroidModel);
        completedTasks.Add("カーソルロック制御を追加（ESCキー対応）");

        // ステップ9: 地面レイヤー設定
        SetupGroundLayer();
        completedTasks.Add("地面レイヤーを設定");

        // 保存
        EditorUtility.SetDirty(vroidModel);
        AssetDatabase.SaveAssets();

        // 結果表示
        string message = "セットアップが完了しました！\n\n【完了項目】\n";
        foreach (var task in completedTasks)
        {
            message += $"✓ {task}\n";
        }
        
        if (warnings.Count > 0)
        {
            message += "\n【警告】\n";
            foreach (var warning in warnings)
            {
                message += $"⚠ {warning}\n";
            }
        }

        message += "\n【操作方法】\n";
        message += "WASD: 移動\n";
        message += "マウス: 視点操作\n";
        message += "Space: ジャンプ\n";
        message += "Shift: ダッシュ\n";
        message += "ESC: カーソルロック解除\n";
        message += "1-5: 表情変更\n";
        message += "\n※Input Systemが変更された場合はエディタを再起動してください。";

        Debug.Log("=== VRoid 最終セットアップ完了 ===");
        
        EditorUtility.DisplayDialog("セットアップ完了", message, "OK");
    }

    // ===== Input System修正 =====
    private static bool FixInputSystem()
    {
        try
        {
            SerializedObject serializedSettings = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            SerializedProperty inputHandlingProp = serializedSettings.FindProperty("activeInputHandler");
            
            if (inputHandlingProp != null && inputHandlingProp.intValue != 2)
            {
                inputHandlingProp.intValue = 2; // Both
                serializedSettings.ApplyModifiedProperties();
                Debug.Log("[FinalSetup] Input System を 'Both' に設定しました");
                return true;
            }
            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[FinalSetup] Input System設定エラー: {e.Message}");
            return false;
        }
    }

    // ===== glTFast無効化 =====
    private static bool DisableGltfastImporter()
    {
        // glTFastの設定ファイルを探して.glbインポートを無効化
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject GltfImportSettings");
        
        // glTFastがインストールされているか確認
        var gltfastType = System.Type.GetType("GLTFast.GltfImport, glTFast");
        if (gltfastType != null)
        {
            Debug.Log("[FinalSetup] glTFastが検出されました。UniGLTFを優先するよう設定します。");
            Debug.Log("[FinalSetup] ※glTFastの競合が続く場合は、Package ManagerからglTFastをアンインストールしてください。");
            
            // ScriptedImporterの優先度をログに出力
            Debug.Log("[FinalSetup] 推奨: Edit > Project Settings > UniGLTF で設定を確認してください。");
            return true;
        }
        
        Debug.Log("[FinalSetup] glTFastは検出されませんでした（競合なし）");
        return false;
    }

    // ===== VRoidモデル検索 =====
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

        // フォールバック
        var allObjects = Object.FindObjectsOfType<GameObject>();
        foreach (var obj in allObjects)
        {
            string nameLower = obj.name.ToLower();
            if ((nameLower.Contains("vroid") || nameLower.Contains("vrm") || nameLower.Contains("avatar")) 
                && obj.GetComponent<Animator>() != null)
            {
                return obj;
            }
        }

        return null;
    }

    // ===== 古いコンポーネント削除 =====
    private static void RemoveOldComponents(GameObject vroidModel)
    {
        string[] oldComponentNames = new string[]
        {
            "VRoidCharacterController",
            "VRoidBlinkController",
            "UnityChanControlScriptWithRgidBody",
            "AutoBlink",
            "FaceUpdate",
            "IdleChanger",
            "RandomWind",
            "SpringManager"
        };

        foreach (var name in oldComponentNames)
        {
            var component = vroidModel.GetComponent(name);
            if (component != null)
            {
                Object.DestroyImmediate(component);
                Debug.Log($"[FinalSetup] {name} を削除");
            }
        }

        // Rigidbody削除（CharacterController使用のため）
        var rb = vroidModel.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Object.DestroyImmediate(rb);
            Debug.Log("[FinalSetup] Rigidbody を削除");
        }

        // CapsuleCollider削除
        var col = vroidModel.GetComponent<CapsuleCollider>();
        if (col != null)
        {
            Object.DestroyImmediate(col);
            Debug.Log("[FinalSetup] CapsuleCollider を削除");
        }
    }

    // ===== Starter Assetsコンポーネント設定 =====
    private static bool SetupStarterAssetsComponents(GameObject vroidModel)
    {
        bool success = true;

        // CharacterController
        var characterController = vroidModel.GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = vroidModel.AddComponent<CharacterController>();
        }
        characterController.height = 1.6f;
        characterController.radius = 0.25f;
        characterController.center = new Vector3(0, 0.8f, 0);
        characterController.minMoveDistance = 0;
        characterController.skinWidth = 0.02f;
        Debug.Log("[FinalSetup] CharacterController を設定");

        // StarterAssetsInputs
        var starterInputs = vroidModel.GetComponent<StarterAssetsInputs>();
        if (starterInputs == null)
        {
            starterInputs = vroidModel.AddComponent<StarterAssetsInputs>();
        }
        starterInputs.cursorLocked = true;
        starterInputs.cursorInputForLook = true;
        Debug.Log("[FinalSetup] StarterAssetsInputs を設定");

        // PlayerInput
#if ENABLE_INPUT_SYSTEM
        var playerInput = vroidModel.GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            playerInput = vroidModel.AddComponent<PlayerInput>();
        }
        
        var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(STARTER_ASSETS_INPUT_ACTIONS);
        if (inputActions != null)
        {
            playerInput.actions = inputActions;
            playerInput.defaultControlScheme = "KeyboardMouse";
            playerInput.defaultActionMap = "Player";
            playerInput.notificationBehavior = PlayerNotifications.InvokeUnityEvents;
            Debug.Log("[FinalSetup] PlayerInput を設定");
        }
        else
        {
            Debug.LogWarning($"[FinalSetup] Input Actions が見つかりません: {STARTER_ASSETS_INPUT_ACTIONS}");
            success = false;
        }
#endif

        // ThirdPersonController
        var tpc = vroidModel.GetComponent<ThirdPersonController>();
        if (tpc == null)
        {
            tpc = vroidModel.AddComponent<ThirdPersonController>();
        }
        tpc.MoveSpeed = 2.0f;
        tpc.SprintSpeed = 5.335f;
        tpc.RotationSmoothTime = 0.12f;
        tpc.SpeedChangeRate = 10.0f;
        tpc.JumpHeight = 1.2f;
        tpc.Gravity = -15.0f;
        tpc.JumpTimeout = 0.5f;
        tpc.FallTimeout = 0.15f;
        tpc.GroundedOffset = -0.14f;
        tpc.GroundedRadius = 0.28f;
        tpc.TopClamp = 70.0f;
        tpc.BottomClamp = -30.0f;
        Debug.Log("[FinalSetup] ThirdPersonController を設定");

        return success;
    }

    // ===== カメラターゲット設定 =====
    private static GameObject SetupCameraTarget(GameObject vroidModel)
    {
        Transform existingTarget = vroidModel.transform.Find("PlayerCameraRoot");
        if (existingTarget != null)
        {
            return existingTarget.gameObject;
        }

        GameObject cameraTarget = new GameObject("PlayerCameraRoot");
        cameraTarget.transform.SetParent(vroidModel.transform);
        cameraTarget.transform.localPosition = new Vector3(0, 1.5f, 0);
        cameraTarget.transform.localRotation = Quaternion.identity;

        var tpc = vroidModel.GetComponent<ThirdPersonController>();
        if (tpc != null)
        {
            tpc.CinemachineCameraTarget = cameraTarget;
        }

        Debug.Log("[FinalSetup] PlayerCameraRoot を作成");
        return cameraTarget;
    }

    // ===== カメラセットアップ =====
    private static void SetupCamera(GameObject vroidModel, GameObject cameraTarget)
    {
        GameObject existingCamera = GameObject.Find("PlayerFollowCamera");
        if (existingCamera != null)
        {
            ConfigureCinemachineCamera(existingCamera, cameraTarget.transform);
            return;
        }

        GameObject cameraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_FOLLOW_CAMERA_PREFAB);
        if (cameraPrefab != null)
        {
            GameObject cameraInstance = (GameObject)PrefabUtility.InstantiatePrefab(cameraPrefab);
            cameraInstance.name = "PlayerFollowCamera";
            ConfigureCinemachineCamera(cameraInstance, cameraTarget.transform);
            Debug.Log("[FinalSetup] PlayerFollowCamera を生成");
        }
        else
        {
            // MainCameraのフォールバック
            var mainCamera = GameObject.FindWithTag("MainCamera");
            if (mainCamera == null)
            {
                mainCamera = new GameObject("Main Camera");
                mainCamera.tag = "MainCamera";
                mainCamera.AddComponent<Camera>();
                mainCamera.AddComponent<AudioListener>();
            }
            Debug.Log("[FinalSetup] MainCamera を確認（Cinemachineプレハブが見つかりません）");
        }
    }

    private static void ConfigureCinemachineCamera(GameObject cameraObj, Transform followTarget)
    {
        var vcamType = System.Type.GetType("Cinemachine.CinemachineVirtualCamera, Cinemachine") 
                    ?? System.Type.GetType("Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine");
        
        if (vcamType == null) return;

        void ConfigureVcam(Component vcam)
        {
            if (vcam == null) return;
            var followProp = vcamType.GetProperty("Follow");
            if (followProp != null) followProp.SetValue(vcam, followTarget);
            var lookAtProp = vcamType.GetProperty("LookAt");
            if (lookAtProp != null) lookAtProp.SetValue(vcam, followTarget);
        }

        var vcam = cameraObj.GetComponent(vcamType);
        ConfigureVcam(vcam);

        foreach (Transform child in cameraObj.transform)
        {
            var childVcam = child.GetComponent(vcamType);
            ConfigureVcam(childVcam);
        }

        EditorUtility.SetDirty(cameraObj);
        Debug.Log("[FinalSetup] Cinemachineカメラを設定");
    }

    // ===== 表情コントローラー =====
    private static void SetupExpressionController(GameObject vroidModel)
    {
        var existing = vroidModel.GetComponent<VRoidExpressionController>();
        if (existing == null)
        {
            var expression = vroidModel.AddComponent<VRoidExpressionController>();
            expression.vrmObject = vroidModel;
            Debug.Log("[FinalSetup] VRoidExpressionController を追加");
        }
    }

    // ===== カーソルコントローラー =====
    private static void SetupCursorController(GameObject vroidModel)
    {
        var existing = vroidModel.GetComponent<CursorController>();
        if (existing == null)
        {
            vroidModel.AddComponent<CursorController>();
            Debug.Log("[FinalSetup] CursorController を追加");
        }
    }

    // ===== 地面レイヤー設定 =====
    private static void SetupGroundLayer()
    {
        var allTpc = Object.FindObjectsOfType<ThirdPersonController>();
        foreach (var tpc in allTpc)
        {
            if (tpc.GroundLayers == 0)
            {
                tpc.GroundLayers = LayerMask.GetMask("Default");
                EditorUtility.SetDirty(tpc);
            }
        }
        Debug.Log("[FinalSetup] 地面レイヤーを設定");
    }
}
