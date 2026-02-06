using UnityEngine;
using UnityEditor;
using StarterAssets;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// VRoidモデルをStarter Assets (Third Person Controller)に対応させる一括セットアップツール
/// Tools > Setup VRoid with Starter Assets で全ての設定が完了
/// </summary>
public class StarterAssetsVRoidSetup : EditorWindow
{
    // パス定義
    private static readonly string PLAYER_FOLLOW_CAMERA_PREFAB = 
        "Assets/StarterAssets/ThirdPersonController/Prefabs/PlayerFollowCamera.prefab";
    private static readonly string STARTER_ASSETS_INPUT_ACTIONS = 
        "Assets/StarterAssets/InputSystem/StarterAssets.inputactions";

    [MenuItem("Tools/Setup VRoid with Starter Assets")]
    public static void SetupVRoidWithStarterAssets()
    {
        Debug.Log("=== VRoid Starter Assets セットアップ開始 ===");

        // 1. VRoidモデルを検索
        GameObject vroidModel = FindVRoidModel();
        if (vroidModel == null)
        {
            EditorUtility.DisplayDialog(
                "エラー",
                "シーン内にVRoidモデルが見つかりません。\n\nVRMファイルをシーンに配置してから再実行してください。",
                "OK"
            );
            return;
        }

        Debug.Log($"[Setup] VRoidモデル発見: {vroidModel.name}");

        // 2. 既存コンポーネントを削除
        RemoveOldComponents(vroidModel);

        // 3. Starter Assetsコンポーネントを追加
        SetupStarterAssetsComponents(vroidModel);

        // 4. カメラターゲットを作成
        GameObject cameraTarget = SetupCameraTarget(vroidModel);

        // 5. Cinemachineカメラをセットアップ
        SetupCamera(vroidModel, cameraTarget);

        // 6. 表情コントローラーを追加（維持）
        SetupExpressionController(vroidModel);

        // 7. 地面レイヤー設定
        SetupGroundLayer();

        EditorUtility.SetDirty(vroidModel);
        
        EditorUtility.DisplayDialog(
            "セットアップ完了",
            $"VRoidモデル「{vroidModel.name}」のStarter Assets対応が完了しました。\n\n" +
            "追加されたコンポーネント:\n" +
            "• ThirdPersonController\n" +
            "• CharacterController\n" +
            "• PlayerInput\n" +
            "• StarterAssetsInputs\n" +
            "• VRoidExpressionController (表情)\n\n" +
            "操作方法:\n" +
            "• WASD: 移動\n" +
            "• マウス: 視点操作\n" +
            "• Space: ジャンプ\n" +
            "• Shift: ダッシュ\n" +
            "• 1-5: 表情変更",
            "OK"
        );

        Debug.Log("=== VRoid Starter Assets セットアップ完了 ===");
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

        // フォールバック: VRoid/VRM/Avatar名のオブジェクトを検索
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

    private static void RemoveOldComponents(GameObject vroidModel)
    {
        // 古いVRoid用スクリプトを削除
        var charController = vroidModel.GetComponent("VRoidCharacterController");
        if (charController != null)
        {
            Object.DestroyImmediate(charController);
            Debug.Log("[Setup] VRoidCharacterController を削除");
        }

        var blinkController = vroidModel.GetComponent("VRoidBlinkController");
        if (blinkController != null)
        {
            Object.DestroyImmediate(blinkController);
            Debug.Log("[Setup] VRoidBlinkController を削除");
        }

        // 古いRigidbodyを削除（CharacterControllerを使用するため）
        var rb = vroidModel.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Object.DestroyImmediate(rb);
            Debug.Log("[Setup] Rigidbody を削除");
        }

        // 古いCapsuleColliderを削除
        var col = vroidModel.GetComponent<CapsuleCollider>();
        if (col != null)
        {
            Object.DestroyImmediate(col);
            Debug.Log("[Setup] CapsuleCollider を削除");
        }
    }

    private static void SetupStarterAssetsComponents(GameObject vroidModel)
    {
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
        Debug.Log("[Setup] CharacterController を設定");

        // StarterAssetsInputs
        var starterInputs = vroidModel.GetComponent<StarterAssetsInputs>();
        if (starterInputs == null)
        {
            starterInputs = vroidModel.AddComponent<StarterAssetsInputs>();
        }
        starterInputs.cursorLocked = true;
        starterInputs.cursorInputForLook = true;
        Debug.Log("[Setup] StarterAssetsInputs を追加");

        // PlayerInput
#if ENABLE_INPUT_SYSTEM
        var playerInput = vroidModel.GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            playerInput = vroidModel.AddComponent<PlayerInput>();
        }
        
        // Input Actionsを設定
        var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(STARTER_ASSETS_INPUT_ACTIONS);
        if (inputActions != null)
        {
            playerInput.actions = inputActions;
            playerInput.defaultControlScheme = "KeyboardMouse";
            playerInput.defaultActionMap = "Player";
            playerInput.notificationBehavior = PlayerNotifications.InvokeUnityEvents;
            Debug.Log("[Setup] PlayerInput を設定");
        }
        else
        {
            Debug.LogWarning($"[Setup] Input Actions が見つかりません: {STARTER_ASSETS_INPUT_ACTIONS}");
        }
#endif

        // ThirdPersonController
        var thirdPersonController = vroidModel.GetComponent<ThirdPersonController>();
        if (thirdPersonController == null)
        {
            thirdPersonController = vroidModel.AddComponent<ThirdPersonController>();
        }
        thirdPersonController.MoveSpeed = 2.0f;
        thirdPersonController.SprintSpeed = 5.335f;
        thirdPersonController.RotationSmoothTime = 0.12f;
        thirdPersonController.SpeedChangeRate = 10.0f;
        thirdPersonController.JumpHeight = 1.2f;
        thirdPersonController.Gravity = -15.0f;
        thirdPersonController.JumpTimeout = 0.5f;
        thirdPersonController.FallTimeout = 0.15f;
        thirdPersonController.GroundedOffset = -0.14f;
        thirdPersonController.GroundedRadius = 0.28f;
        thirdPersonController.TopClamp = 70.0f;
        thirdPersonController.BottomClamp = -30.0f;
        Debug.Log("[Setup] ThirdPersonController を追加");
    }

    private static GameObject SetupCameraTarget(GameObject vroidModel)
    {
        // 既存のカメラターゲットを検索
        Transform existingTarget = vroidModel.transform.Find("PlayerCameraRoot");
        if (existingTarget != null)
        {
            return existingTarget.gameObject;
        }

        // 新規カメラターゲットを作成
        GameObject cameraTarget = new GameObject("PlayerCameraRoot");
        cameraTarget.transform.SetParent(vroidModel.transform);
        cameraTarget.transform.localPosition = new Vector3(0, 1.5f, 0); // 頭の高さ
        cameraTarget.transform.localRotation = Quaternion.identity;

        // ThirdPersonControllerにカメラターゲットを設定
        var tpc = vroidModel.GetComponent<ThirdPersonController>();
        if (tpc != null)
        {
            tpc.CinemachineCameraTarget = cameraTarget;
        }

        Debug.Log("[Setup] PlayerCameraRoot を作成");
        return cameraTarget;
    }

    private static void SetupCamera(GameObject vroidModel, GameObject cameraTarget)
    {
        // 既存のPlayerFollowCameraを検索
        GameObject existingCamera = GameObject.Find("PlayerFollowCamera");
        if (existingCamera != null)
        {
            Debug.Log("[Setup] 既存のPlayerFollowCameraを使用");
            ConfigureCinemachineCamera(existingCamera, cameraTarget.transform);
            return;
        }

        // プレハブからカメラを生成
        GameObject cameraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_FOLLOW_CAMERA_PREFAB);
        if (cameraPrefab != null)
        {
            GameObject cameraInstance = (GameObject)PrefabUtility.InstantiatePrefab(cameraPrefab);
            cameraInstance.name = "PlayerFollowCamera";
            ConfigureCinemachineCamera(cameraInstance, cameraTarget.transform);
            Debug.Log("[Setup] PlayerFollowCamera を生成");
        }
        else
        {
            Debug.LogWarning($"[Setup] カメラプレハブが見つかりません: {PLAYER_FOLLOW_CAMERA_PREFAB}");
            
            // フォールバック: MainCameraを検索して簡易設定
            var mainCamera = GameObject.FindWithTag("MainCamera");
            if (mainCamera == null)
            {
                mainCamera = new GameObject("Main Camera");
                mainCamera.tag = "MainCamera";
                mainCamera.AddComponent<Camera>();
                mainCamera.AddComponent<AudioListener>();
            }
            Debug.Log("[Setup] MainCamera を確認");
        }
    }

    private static void ConfigureCinemachineCamera(GameObject cameraObj, Transform followTarget)
    {
        // CinemachineVirtualCameraを検索して設定
        var vcamType = System.Type.GetType("Cinemachine.CinemachineVirtualCamera, Cinemachine") 
                    ?? System.Type.GetType("Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine");
        
        if (vcamType != null)
        {
            var vcam = cameraObj.GetComponent(vcamType);
            if (vcam != null)
            {
                // Followを設定
                var followProp = vcamType.GetProperty("Follow");
                if (followProp != null)
                {
                    followProp.SetValue(vcam, followTarget);
                }
                
                // LookAtを設定
                var lookAtProp = vcamType.GetProperty("LookAt");
                if (lookAtProp != null)
                {
                    lookAtProp.SetValue(vcam, followTarget);
                }
                
                EditorUtility.SetDirty(cameraObj);
                Debug.Log("[Setup] Cinemachine カメラを設定");
            }
        }

        // 子オブジェクトにVirtualCameraがある場合も検索
        foreach (Transform child in cameraObj.transform)
        {
            if (vcamType != null)
            {
                var childVcam = child.GetComponent(vcamType);
                if (childVcam != null)
                {
                    var followProp = vcamType.GetProperty("Follow");
                    if (followProp != null) followProp.SetValue(childVcam, followTarget);
                    
                    var lookAtProp = vcamType.GetProperty("LookAt");
                    if (lookAtProp != null) lookAtProp.SetValue(childVcam, followTarget);
                    
                    EditorUtility.SetDirty(child.gameObject);
                }
            }
        }
    }

    private static void SetupExpressionController(GameObject vroidModel)
    {
        var expression = vroidModel.GetComponent("VRoidExpressionController") as MonoBehaviour;
        if (expression == null)
        {
            // VRoidExpressionControllerがあれば追加
            var expressionType = System.Type.GetType("VRoidExpressionController");
            if (expressionType != null)
            {
                var newExpression = vroidModel.AddComponent(expressionType);
                var vrmObjField = expressionType.GetField("vrmObject");
                if (vrmObjField != null)
                {
                    vrmObjField.SetValue(newExpression, vroidModel);
                }
                Debug.Log("[Setup] VRoidExpressionController を追加");
            }
        }
    }

    private static void SetupGroundLayer()
    {
        // Ground レイヤーが存在するか確認し、なければ Default を使用
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer == -1)
        {
            groundLayer = 0; // Default layer
            Debug.Log("[Setup] 'Ground'レイヤーが見つからないため、Defaultを使用");
        }

        // 地面オブジェクトにレイヤーを設定
        var planes = Object.FindObjectsOfType<MeshRenderer>();
        foreach (var plane in planes)
        {
            if (plane.gameObject.name.ToLower().Contains("plane") || 
                plane.gameObject.name.ToLower().Contains("ground") ||
                plane.gameObject.name.ToLower().Contains("floor"))
            {
                // Ground レイヤーマスクをThirdPersonControllerに設定
                var tpc = Object.FindObjectOfType<ThirdPersonController>();
                if (tpc != null)
                {
                    tpc.GroundLayers = LayerMask.GetMask("Default", "Ground");
                }
                break;
            }
        }

        // 全てのThirdPersonControllerにGroundLayersを設定
        var allTpc = Object.FindObjectsOfType<ThirdPersonController>();
        foreach (var tpc in allTpc)
        {
            if (tpc.GroundLayers == 0)
            {
                tpc.GroundLayers = LayerMask.GetMask("Default");
            }
        }
    }

    [MenuItem("Tools/Remove Starter Assets from VRoid")]
    public static void RemoveStarterAssetsFromVRoid()
    {
        GameObject vroidModel = FindVRoidModel();
        if (vroidModel == null)
        {
            EditorUtility.DisplayDialog("エラー", "VRoidモデルが見つかりません。", "OK");
            return;
        }

        // Starter Assetsコンポーネント削除
        var tpc = vroidModel.GetComponent<ThirdPersonController>();
        var starterInputs = vroidModel.GetComponent<StarterAssetsInputs>();
        var cc = vroidModel.GetComponent<CharacterController>();
#if ENABLE_INPUT_SYSTEM
        var playerInput = vroidModel.GetComponent<PlayerInput>();
        if (playerInput != null) Object.DestroyImmediate(playerInput);
#endif

        if (tpc != null) Object.DestroyImmediate(tpc);
        if (starterInputs != null) Object.DestroyImmediate(starterInputs);
        if (cc != null) Object.DestroyImmediate(cc);

        // カメラターゲット削除
        var cameraRoot = vroidModel.transform.Find("PlayerCameraRoot");
        if (cameraRoot != null) Object.DestroyImmediate(cameraRoot.gameObject);

        EditorUtility.SetDirty(vroidModel);
        EditorUtility.DisplayDialog("削除完了", "Starter Assetsコンポーネントを削除しました。", "OK");
    }
}
