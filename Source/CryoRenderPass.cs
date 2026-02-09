using Cryo;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Cryo
{
    public class CryoRenderPass : ScriptableRenderPass
    {
        private readonly Material _uiMaterial;

        public CryoRenderPass(Material uiMaterial)
        {
            _uiMaterial = uiMaterial;
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var ctx = CryoContext.Current;
            if (ctx == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("CryoUI Render");

            float w = Screen.width;
            float h = Screen.height;
            Matrix4x4 proj = Matrix4x4.Ortho(0, w, 0, h, -1, 1);
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, proj);

            // 1. 背景层
            DrawMeshIfValid(cmd, ctx.DrawListBackground.Mesh, _uiMaterial);

            // 2. 前景层（普通控件）
            DrawMeshIfValid(cmd, ctx.DrawListForeground.Mesh, _uiMaterial);

            // 3. 前景层文字
            DrawMeshIfValid(cmd, ctx.TextRenderer.TextMesh, ctx.TextRenderer.FontMaterial);

            // 4. ★ 覆盖层（菜单/下拉框）- 最上层
            DrawMeshIfValid(cmd, ctx.DrawListOverlay.Mesh, _uiMaterial);

            // 5. 覆盖层文字
            DrawMeshIfValid(cmd, ctx.TextRendererOverlay.TextMesh, ctx.TextRendererOverlay.FontMaterial);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        private void DrawMeshIfValid(CommandBuffer cmd, Mesh mesh, Material material)
        {
            if (mesh != null && mesh.vertexCount > 0 && material != null)
                cmd.DrawMesh(mesh, Matrix4x4.identity, material, 0, 0);
        }
    }
}