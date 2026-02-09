using Cryo;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace Cryo
{
    public class CryoRenderPass : ScriptableRenderPass
    {
        private readonly Material _uiMaterial;
        private const string PassName = "CryoUI Render";

        public CryoRenderPass(Material uiMaterial)
        {
            _uiMaterial = uiMaterial;
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }

        // Unity 6 RenderGraph API
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var ctx = CryoContext.Current;
            if (ctx == null) return;

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PassName, out var passData))
            {
                passData.uiMaterial = _uiMaterial;
                passData.ctx = ctx;

                var resourceData = frameData.Get<UniversalResourceData>();
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);

                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                {
                    var cmd = rgContext.cmd;
                    float w = Screen.width;
                    float h = Screen.height;
                    Matrix4x4 proj = Matrix4x4.Ortho(0, w, 0, h, -1, 1);
                    cmd.SetViewProjectionMatrices(Matrix4x4.identity, proj);

                    DrawMeshIfValid(cmd, data.ctx.DrawListBackground.Mesh, data.uiMaterial);
                    DrawMeshIfValid(cmd, data.ctx.DrawListForeground.Mesh, data.uiMaterial);
                    DrawMeshIfValid(cmd, data.ctx.TextRenderer.TextMesh, data.ctx.TextRenderer.FontMaterial);
                    DrawMeshIfValid(cmd, data.ctx.DrawListOverlay.Mesh, data.uiMaterial);
                    DrawMeshIfValid(cmd, data.ctx.TextRendererOverlay.TextMesh, data.ctx.TextRendererOverlay.FontMaterial);
                });
            }
        }

        private class PassData
        {
            public Material uiMaterial;
            public CryoContext ctx;
        }

        private static void DrawMeshIfValid(RasterCommandBuffer cmd, Mesh mesh, Material material)
        {
            if (mesh != null && mesh.vertexCount > 0 && material != null)
                cmd.DrawMesh(mesh, Matrix4x4.identity, material, 0, 0);
        }

        // 保留旧 API 以兼容旧版本
        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var ctx = CryoContext.Current;
            if (ctx == null) return;

            CommandBuffer cmd = CommandBufferPool.Get(PassName);

            float w = Screen.width;
            float h = Screen.height;
            Matrix4x4 proj = Matrix4x4.Ortho(0, w, 0, h, -1, 1);
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, proj);

            DrawMeshIfValid(cmd, ctx.DrawListBackground.Mesh, _uiMaterial);
            DrawMeshIfValid(cmd, ctx.DrawListForeground.Mesh, _uiMaterial);
            DrawMeshIfValid(cmd, ctx.TextRenderer.TextMesh, ctx.TextRenderer.FontMaterial);
            DrawMeshIfValid(cmd, ctx.DrawListOverlay.Mesh, _uiMaterial);
            DrawMeshIfValid(cmd, ctx.TextRendererOverlay.TextMesh, ctx.TextRendererOverlay.FontMaterial);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        private static void DrawMeshIfValid(CommandBuffer cmd, Mesh mesh, Material material)
        {
            if (mesh != null && mesh.vertexCount > 0 && material != null)
                cmd.DrawMesh(mesh, Matrix4x4.identity, material, 0, 0);
        }
    }
}