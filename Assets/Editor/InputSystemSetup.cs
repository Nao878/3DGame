using UnityEditor;
using UnityEngine;

/// <summary>
/// Input System設定を変更するエディタツール
/// </summary>
public class InputSystemSetup : EditorWindow
{
    [MenuItem("Tools/Input System/Set to Both (Legacy + New)")]
    public static void SetInputHandlingToBoth()
    {
        SetInputHandling(2, "Both");
    }

    [MenuItem("Tools/Input System/Set to New (Starter Assets用)")]
    public static void SetInputHandlingToNew()
    {
        SetInputHandling(1, "Input System Package (New)");
    }

    [MenuItem("Tools/Input System/Set to Old (Legacy)")]
    public static void SetInputHandlingToOld()
    {
        SetInputHandling(0, "Input Manager (Old)");
    }

    private static void SetInputHandling(int value, string name)
    {
        SerializedObject serializedSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        SerializedProperty inputHandlingProp = serializedSettings.FindProperty("activeInputHandler");
        
        if (inputHandlingProp != null)
        {
            inputHandlingProp.intValue = value;
            serializedSettings.ApplyModifiedProperties();
            
            Debug.Log($"[InputSystemSetup] Active Input Handling を '{name}' に設定しました。");
            
            EditorUtility.DisplayDialog(
                "Input System設定完了",
                $"Active Input Handling を '{name}' に設定しました。\n\n設定を反映するにはUnityエディタを再起動してください。",
                "OK"
            );
        }
        else
        {
            Debug.LogError("activeInputHandler プロパティが見つかりません。");
        }
    }

    [InitializeOnLoadMethod]
    private static void CheckInputHandling()
    {
        EditorApplication.delayCall += () =>
        {
            SerializedObject serializedSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            SerializedProperty inputHandlingProp = serializedSettings.FindProperty("activeInputHandler");
            
            if (inputHandlingProp != null)
            {
                int currentValue = inputHandlingProp.intValue;
                // Starter Assetsを使う場合は1(New)または2(Both)が必要
                if (currentValue == 0)
                {
                    Debug.LogWarning("[InputSystemSetup] Input Systemが 'Old' に設定されています。Starter Assetsを使用するには 'New' または 'Both' に変更してください。");
                    Debug.LogWarning("[InputSystemSetup] Tools > Input System > Set to New (Starter Assets用) を実行してください。");
                }
            }
        };
    }
}

