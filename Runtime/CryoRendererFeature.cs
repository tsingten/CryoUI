using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Cryo
{
    public class CryoRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader uiShader;

        private Material _uiMaterial;
        private CryoRenderPass _renderPass;

        public override void Create()
        {
            if (uiShader == null)
                uiShader = Shader.Find("Sprites/Default");

            if (uiShader != null && _uiMaterial == null)
                _uiMaterial = new Material(uiShader) { hideFlags = HideFlags.HideAndDontSave };

            _renderPass = new CryoRenderPass(_uiMaterial);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_renderPass == null) return;
            if (renderingData.cameraData.cameraType == CameraType.Game)
                renderer.EnqueuePass(_renderPass);
        }

        protected override void Dispose(bool disposing)
        {
            if (_uiMaterial != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(_uiMaterial);
#else
                Destroy(_uiMaterial);
#endif
            }
        }
    }
}