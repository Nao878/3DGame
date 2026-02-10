using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// Starter AssetsのAnimator Controllerに攻撃ステートを追加するEditorツール
/// </summary>
public class AttackAnimatorSetup
{
    private static readonly string ANIMATOR_CONTROLLER_PATH =
        "Assets/StarterAssets/ThirdPersonController/Character/Animations/StarterAssetsThirdPerson.controller";

    // Unityちゃんのスライドアニメーションを攻撃として使用
    private static readonly string ATTACK_ANIM_PATH =
        "Assets/unity-chan!/Unity-chan! Model/Art/Animations/unitychan_SLIDE00.fbx";

    [MenuItem("Tools/Add Attack State to Animator")]
    public static void AddAttackState()
    {
        SetupAttackState();
    }

    /// <summary>
    /// Animator Controllerに攻撃ステートを追加
    /// </summary>
    public static bool SetupAttackState()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ANIMATOR_CONTROLLER_PATH);
        if (controller == null)
        {
            Debug.LogWarning("[AttackAnimatorSetup] Animator Controllerが見つかりません: " + ANIMATOR_CONTROLLER_PATH);
            return false;
        }

        // 既にAttackパラメータが存在するか確認
        bool hasAttackParam = false;
        foreach (var param in controller.parameters)
        {
            if (param.name == "Attack")
            {
                hasAttackParam = true;
                break;
            }
        }

        // Attackトリガーパラメータを追加
        if (!hasAttackParam)
        {
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            Debug.Log("[AttackAnimatorSetup] 'Attack' トリガーパラメータを追加");
        }

        // BaseLayerを取得
        var baseLayer = controller.layers[0];
        var stateMachine = baseLayer.stateMachine;

        // 既にAttackステートが存在するか確認
        foreach (var state in stateMachine.states)
        {
            if (state.state.name == "Attack")
            {
                Debug.Log("[AttackAnimatorSetup] Attackステートは既に存在します");
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
                return true;
            }
        }

        // 攻撃アニメーションクリップを検索
        AnimationClip attackClip = LoadAttackClip();

        // Attackステートを作成
        var attackState = stateMachine.AddState("Attack", new Vector3(400, 200, 0));
        if (attackClip != null)
        {
            attackState.motion = attackClip;
            Debug.Log($"[AttackAnimatorSetup] 攻撃アニメーション設定: {attackClip.name}");
        }
        else
        {
            Debug.LogWarning("[AttackAnimatorSetup] 攻撃アニメーションが見つかりません。空のステートを作成しました。");
        }

        // AnyState -> Attack へのトランジション（Attackトリガー）
        var toAttack = stateMachine.AddAnyStateTransition(attackState);
        toAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        toAttack.duration = 0.1f;
        toAttack.hasExitTime = false;
        toAttack.canTransitionToSelf = false;

        // Attack -> デフォルトステート へのトランジション（ExitTime）
        var defaultState = stateMachine.defaultState;
        if (defaultState != null)
        {
            var toDefault = attackState.AddTransition(defaultState);
            toDefault.hasExitTime = true;
            toDefault.exitTime = 0.9f;
            toDefault.duration = 0.15f;
        }
        else
        {
            // デフォルトステートがない場合はExitに遷移
            var toExit = attackState.AddExitTransition();
            toExit.hasExitTime = true;
            toExit.exitTime = 0.9f;
            toExit.duration = 0.15f;
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log("[AttackAnimatorSetup] Attackステートの追加完了");
        return true;
    }

    private static AnimationClip LoadAttackClip()
    {
        // まずFBXからアニメーションクリップを取得
        var objects = AssetDatabase.LoadAllAssetsAtPath(ATTACK_ANIM_PATH);
        if (objects != null)
        {
            foreach (var obj in objects)
            {
                if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }
        }

        // フォールバック: 他の攻撃っぽいアニメーションを検索
        string[] attackPaths = new string[]
        {
            "Assets/unity-chan!/Unity-chan! Model/Art/Animations/unitychan_HANDUP00_R.fbx",
            "Assets/unity-chan!/Unity-chan! Model/Art/Animations/unitychan_DAMAGED00.fbx",
        };

        foreach (var path in attackPaths)
        {
            objects = AssetDatabase.LoadAllAssetsAtPath(path);
            if (objects != null)
            {
                foreach (var obj in objects)
                {
                    if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    {
                        return clip;
                    }
                }
            }
        }

        return null;
    }
}
