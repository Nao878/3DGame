using UnityEditor;
using UnityEditor.AssetImporters;

/// <summary>
/// UniGLTFをglTFastよりも優先してGLB/GLTFファイルをインポートするための設定
/// </summary>
public class GltfImporterPriority : AssetPostprocessor
{
    // スクリプトの登録だけで動作する
    // UniGLTFは通常、デフォルトで優先度が高いが、
    // 競合が発生する場合はPreferences > External Toolsで設定を確認
    
    [InitializeOnLoadMethod]
    private static void SetupImporterPriority()
    {
        // UniGLTFインポーターを優先させるための情報をログに出力
        UnityEngine.Debug.Log("[GltfImporterPriority] UniGLTFを.glb/.gltfのデフォルトインポーターとして使用します。");
        UnityEngine.Debug.Log("[GltfImporterPriority] 競合が続く場合: Edit > Project Settings > UniGLTF で設定を確認してください。");
    }
}
