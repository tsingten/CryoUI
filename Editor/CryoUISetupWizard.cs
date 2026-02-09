using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CryoUI.Editor
{
    public static class CryoUISetupWizard
    {
        [MenuItem("Tools/CryoUI/Setup Renderer Feature")]
        public static void SetupRendererFeature()
        {
            // 获取当前 URP 资产
            var urpAsset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset == null)
            {
                EditorUtility.DisplayDialog("CryoUI Setup", "未检测到 URP 渲染管线，请先配置 URP。", "确定");
                return;
            }

            // 获取 Renderer Data
            var rendererData = GetRendererData(urpAsset);
            if (rendererData == null)
            {
                EditorUtility.DisplayDialog("CryoUI Setup", "无法获取 Renderer Data。", "确定");
                return;
            }

            // 检查是否已添加
            foreach (var feature in rendererData.rendererFeatures)
            {
                if (feature is CryoRendererFeature)
                {
                    EditorUtility.DisplayDialog("CryoUI Setup", "CryoRendererFeature 已存在！", "确定");
                    return;
                }
            }

            // 添加 Feature
            var cryoFeature = ScriptableObject.CreateInstance<CryoRendererFeature>();
            cryoFeature.name = "CryoRendererFeature";
            
            AssetDatabase.AddObjectToAsset(cryoFeature, rendererData);
            rendererData.rendererFeatures.Add(cryoFeature);
            
            EditorUtility.SetDirty(rendererData);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("CryoUI Setup", "✅ CryoRendererFeature 添加成功！", "确定");
        }

        private static ScriptableRendererData GetRendererData(UniversalRenderPipelineAsset urpAsset)
        {
            // 通过反射获取 Renderer Data（Unity 没有公开 API）
            var propertyInfo = typeof(UniversalRenderPipelineAsset)
                .GetField("m_RendererDataList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (propertyInfo?.GetValue(urpAsset) is ScriptableRendererData[] rendererDataList && rendererDataList.Length > 0)
                return rendererDataList[0];
            
            return null;
        }
    }
}