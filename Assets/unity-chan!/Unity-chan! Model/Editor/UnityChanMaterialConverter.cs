using UnityEngine;
using UnityEditor;
using System.IO;

namespace UnityChan
{
    /// <summary>
    /// UnityちゃんのマテリアルをURPに対応させるエディタツール
    /// VRoidモデル（UniVRM関連）は変更しない
    /// </summary>
    public class UnityChanMaterialConverter : EditorWindow
    {
        private static readonly string[] UNITYCHAN_MATERIAL_FOLDERS = new string[]
        {
            "Assets/unity-chan!/Unity-chan! Model/Art/Materials",
            "Assets/unity-chan!/Unity-chan! Model/Art/UnityChanShader/Materials",
            "Assets/unity-chan!/Unity-chan! Model/Art/Stage/Materials"
        };

        // 変換対象のシェーダー名プレフィックス
        private static readonly string[] SHADER_PREFIXES_TO_CONVERT = new string[]
        {
            "UnityChan/",
            "Unlit/",
            "Legacy Shaders/",
            "Standard"
        };

        // 変換しないフォルダ（VRoid/UniVRM関連）
        private static readonly string[] EXCLUDED_FOLDERS = new string[]
        {
            "VRoid",
            "UniVRM",
            "VRM",
            "UniGLTF",
            "MToon"
        };

        [MenuItem("Tools/Convert UnityChan Materials to URP")]
        public static void ConvertMaterials()
        {
            int convertedCount = 0;
            int skippedCount = 0;
            int errorCount = 0;

            Debug.Log("=== Unityちゃんマテリアル変換開始 ===");

            // URP Lit シェーダーを取得
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            Shader urpUnlitShader = Shader.Find("Universal Render Pipeline/Unlit");

            if (urpLitShader == null)
            {
                Debug.LogError("URP Lit シェーダーが見つかりません。URPパッケージがインストールされているか確認してください。");
                return;
            }

            // 指定フォルダ内のマテリアルを処理
            foreach (string folderPath in UNITYCHAN_MATERIAL_FOLDERS)
            {
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    Debug.LogWarning($"フォルダが見つかりません: {folderPath}");
                    continue;
                }

                string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { folderPath });

                foreach (string guid in materialGuids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                    // 除外フォルダのチェック
                    if (IsExcludedPath(assetPath))
                    {
                        skippedCount++;
                        continue;
                    }

                    Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                    if (material == null)
                    {
                        errorCount++;
                        continue;
                    }

                    // 既にURPシェーダーを使用している場合はスキップ
                    if (material.shader != null && material.shader.name.StartsWith("Universal Render Pipeline"))
                    {
                        skippedCount++;
                        continue;
                    }

                    // 変換対象のシェーダーかチェック
                    if (ShouldConvertShader(material.shader))
                    {
                        ConvertMaterial(material, urpLitShader, urpUnlitShader);
                        convertedCount++;
                        Debug.Log($"変換完了: {assetPath} ({material.shader.name} → Universal Render Pipeline/Lit)");
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"=== 変換完了 ===");
            Debug.Log($"変換: {convertedCount}個, スキップ: {skippedCount}個, エラー: {errorCount}個");

            EditorUtility.DisplayDialog(
                "マテリアル変換完了",
                $"変換: {convertedCount}個\nスキップ: {skippedCount}個\nエラー: {errorCount}個",
                "OK"
            );
        }

        private static bool IsExcludedPath(string path)
        {
            foreach (string excluded in EXCLUDED_FOLDERS)
            {
                if (path.Contains(excluded))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool ShouldConvertShader(Shader shader)
        {
            if (shader == null) return true; // Missing shader

            string shaderName = shader.name;

            // ピンク表示（シェーダーエラー）の場合
            if (shaderName == "Hidden/InternalErrorShader")
            {
                return true;
            }

            foreach (string prefix in SHADER_PREFIXES_TO_CONVERT)
            {
                if (shaderName.StartsWith(prefix) || shaderName.Contains("UnityChan"))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ConvertMaterial(Material material, Shader urpLitShader, Shader urpUnlitShader)
        {
            // 変換前のプロパティを保存
            Texture mainTex = null;
            Color mainColor = Color.white;
            float smoothness = 0.5f;
            float metallic = 0f;

            if (material.HasProperty("_MainTex"))
            {
                mainTex = material.GetTexture("_MainTex");
            }

            if (material.HasProperty("_Color"))
            {
                mainColor = material.GetColor("_Color");
            }

            // シェーダーを変更
            bool isTransparent = material.shader != null && 
                (material.shader.name.Contains("Transparent") || 
                 material.shader.name.Contains("Blend") ||
                 material.shader.name.Contains("Cutout"));

            material.shader = urpLitShader;

            // プロパティを復元
            if (mainTex != null && material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", mainTex);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", mainColor);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            // 透明設定
            if (isTransparent)
            {
                material.SetFloat("_Surface", 1); // Transparent
                material.SetFloat("_Blend", 0); // Alpha
                material.renderQueue = 3000;
            }

            EditorUtility.SetDirty(material);
        }
    }
}
