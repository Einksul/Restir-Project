using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class LightDecalRendererFeature: ScriptableRendererFeature
{
    private LightDecalPass _LightDecalPass;
    public Material material;
    public Mesh mesh;


    public class LightDecalPass : ScriptableRenderPass
    {
        private Mesh _mesh;
        private Material _material;

        public LightDecalPass(Material material, Mesh mesh)
        {
            _material = material;
            _mesh = mesh;
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents; 
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(name: "LightDecalPass");
            Camera camera = renderingData.cameraData.camera;
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);

            Vector3 object_scale= new Vector3(1, camera.aspect, 1);

            foreach (var visibleLight in renderingData.lightData.visibleLights)
            {
                Light light = visibleLight.light;
                Vector3 position = camera.WorldToViewportPoint(light.transform.position);
                position.z = 1;
                cmd.DrawMesh(_mesh, Matrix4x4.TRS(position, Quaternion.identity, object_scale), _material);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    public override void Create()
    {
        _LightDecalPass = new LightDecalPass(material, mesh);
        _LightDecalPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material != null && mesh != null)
        {
            renderer.EnqueuePass(_LightDecalPass);
        }
    }

}