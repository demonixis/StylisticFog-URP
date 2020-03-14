using UnityEngine.Rendering.Universal;

namespace Demonixis.Toolbox.Rendering
{
    public class StylisticFog : ScriptableRendererFeature
    {
        private StylisticFogCustomRenderPass m_Pass;

        public StylisticFogSettings settings = new StylisticFogSettings();

        public override void Create()
        {
            m_Pass = new StylisticFogCustomRenderPass("StylisticFog");
            m_Pass.Material = settings.Material;
            m_Pass.distanceFog = settings.DistanceFog;
            m_Pass.heightFog = settings.HeightFog;
            m_Pass.renderPassEvent = settings.renderPassEvent;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var src = renderer.cameraColorTarget;
            m_Pass.Setup(src);
            renderer.EnqueuePass(m_Pass);
        }
    }
}