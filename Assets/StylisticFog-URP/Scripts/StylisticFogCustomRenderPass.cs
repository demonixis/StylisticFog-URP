using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Demonixis.Toolbox.Rendering
{
    public class StylisticFogCustomRenderPass : ScriptableRenderPass
    {
        private string m_ProfilerTag;
        private RenderTargetIdentifier m_TmpRT1;
        private RenderTargetIdentifier m_Source;

        public Material Material;
        public DistanceFogSettings distanceFog = DistanceFogSettings.defaultSettings;
        public HeightFogSettings heightFog = HeightFogSettings.defaultSettings;
        public FogColorSource distanceColorSource = FogColorSource.defaultSettings;
        public FogColorSource heightColorSource = FogColorSource.defaultSettings;

        private Texture2D m_DistanceColorTexture;
        private Texture2D m_HeightColorTexture;
        private Texture2D m_distanceFogIntensityTexture;

        public Texture2D DistanceColorTexture
        {
            get
            {
                if (m_DistanceColorTexture == null)
                {
                    m_DistanceColorTexture = new Texture2D(1024, 1, TextureFormat.ARGB32, false, false)
                    {
                        name = "Fog property",
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear,
                        anisoLevel = 0,
                    };
                    BakeFogColor(m_DistanceColorTexture, distanceColorSource.gradient);
                }
                return m_DistanceColorTexture;
            }
        }

        public Texture2D HeightColorTexture
        {
            get
            {
                if (m_HeightColorTexture == null)
                {
                    m_HeightColorTexture = new Texture2D(256, 1, TextureFormat.ARGB32, false, false)
                    {
                        name = "Fog property",
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear,
                        anisoLevel = 0,
                    };
                    BakeFogColor(m_HeightColorTexture, heightColorSource.gradient);
                }
                return m_HeightColorTexture;
            }
        }

        public Texture2D DistanceFogIntensityTexture
        {
            get
            {
                if (m_distanceFogIntensityTexture == null)
                {
                    m_distanceFogIntensityTexture = new Texture2D(256, 1, TextureFormat.ARGB32, false, false)
                    {
                        name = "Fog Height density",
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear,
                        anisoLevel = 0,
                    };
                }
                return m_distanceFogIntensityTexture;
            }
        }

        public void Setup(RenderTargetIdentifier source)
        {
            m_Source = source;
        }

        public StylisticFogCustomRenderPass(string profilerTag)
        {
            m_ProfilerTag = profilerTag;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            var width = cameraTextureDescriptor.width;
            var height = cameraTextureDescriptor.height;

            m_TmpRT1 = SetupRenderTargetIdentifier(cmd, 0, width, height);
        }

        private RenderTargetIdentifier SetupRenderTargetIdentifier(CommandBuffer cmd, int id, int width, int height)
        {
            int tmpId = Shader.PropertyToID($"StylisticFog_{id}_RT");
            cmd.GetTemporaryRT(tmpId, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);

            var rt = new RenderTargetIdentifier(tmpId);
            ConfigureTarget(rt);

            return rt;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (Material == null)
            {
                return;
            }

            var cmd = CommandBufferPool.Get(m_ProfilerTag);
            var opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;

            var fogType = SetMaterialUniforms(renderingData.cameraData.camera);
            if (fogType == FogTypePass.None)
            {
                Blit(cmd, m_Source, m_Source);
            }
            else
            {
                Blit(cmd, m_Source, m_TmpRT1, Material, (int)fogType);
                Blit(cmd, m_TmpRT1, m_Source);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
        }

        private FogTypePass SetMaterialUniforms(Camera camera)
        {

            // Determine the fog type pass
            FogTypePass fogType = FogTypePass.DistanceOnly;

            if (!distanceFog.enabled && heightFog.enabled)
                fogType = FogTypePass.HeightOnly;

            // Share color settings if one of the sources are set to copy the other
            bool sharedColorSettings = (distanceFog.colorSelectionType == ColorSelectionType.CopyOther)
                                        || (heightFog.colorSelectionType == ColorSelectionType.CopyOther);

            if (distanceFog.enabled && heightFog.enabled)
            {
                if (sharedColorSettings)
                {
                    fogType = FogTypePass.BothSharedColorSettings;
                }
                else
                {
                    fogType = FogTypePass.BothSeperateColorSettinsg;
                }
            }

            if (!distanceFog.enabled && !heightFog.enabled)
                return FogTypePass.None;

            // Get the inverse view matrix for converting depth to world position.
            Matrix4x4 inverseViewMatrix = camera.cameraToWorldMatrix;
            Material.SetMatrix("_InverseViewMatrix", inverseViewMatrix);

            // Decide wheter the skybox should have fog applied
            Material.SetInt("_ApplyDistToSkybox", distanceFog.fogSkybox ? 1 : 0);
            Material.SetInt("_ApplyHeightToSkybox", heightFog.fogSkybox ? 1 : 0);

            // Is the shared color sampled from a texture? Otherwise it's from a single color( picker)
            if (sharedColorSettings)
            {
                bool selectingFromDistance = true;
                FogColorSource activeSelectionSource = distanceColorSource;
                ColorSelectionType activeSelectionType = distanceFog.colorSelectionType;
                if (activeSelectionType == ColorSelectionType.CopyOther)
                {
                    activeSelectionType = heightFog.colorSelectionType;
                    activeSelectionSource = heightColorSource;
                    selectingFromDistance = false;
                }

                SetDistanceFogUniforms();
                SetHeightFogUniforms();

                if (activeSelectionType == ColorSelectionType.Gradient)
                    Material.SetTexture("_FogColorTexture0", selectingFromDistance ? DistanceColorTexture : HeightColorTexture);
                else
                    Material.SetTexture("_FogColorTexture0", selectingFromDistance ? distanceColorSource.colorRamp : heightColorSource.colorRamp);
            }
            else
            {
                if (distanceFog.enabled)
                    Material.SetTexture("_FogColorTexture0", distanceFog.colorSelectionType == ColorSelectionType.Gradient ? DistanceColorTexture : distanceColorSource.colorRamp);

                if (heightFog.enabled)
                {
                    string colorTextureIdentifier = fogType == FogTypePass.HeightOnly ? "_FogColorTexture0" : "_FogColorTexture1";
                    Material.SetTexture(colorTextureIdentifier, heightFog.colorSelectionType == ColorSelectionType.Gradient ? HeightColorTexture : heightColorSource.colorRamp);
                }
            }

            // Set distance fog properties
            if (distanceFog.enabled)
            {
                SetDistanceFogUniforms();
            }

            // Set height fog properties
            if (heightFog.enabled)
            {
                SetHeightFogUniforms();
            }

            return fogType;
        }

        private void SetDistanceFogUniforms()
        {
            Material.SetFloat("_FogEndDistance", distanceFog.endDistance);
        }

        private void SetHeightFogUniforms()
        {
            Material.SetFloat("_Height", heightFog.baseHeight);
            Material.SetFloat("_BaseDensity", heightFog.baseDensity);
            Material.SetFloat("_DensityFalloff", heightFog.densityFalloff);
        }

        public void UpdateProperties()
        {
            // Check if both color selction types are to copy
            // If so, change one / show warning?
            bool selectionTypeSame = distanceFog.colorSelectionType == heightFog.colorSelectionType;
            bool distanceSelectedCopy = distanceFog.colorSelectionType == ColorSelectionType.CopyOther;
            if (selectionTypeSame && distanceSelectedCopy)
            {
                distanceFog.colorSelectionType = ColorSelectionType.Gradient;
                distanceSelectedCopy = false;
            }

            UpdateDistanceFogTextures(distanceFog.colorSelectionType);
            UpdateHeightFogTextures(heightFog.colorSelectionType);
        }

        private void UpdateDistanceFogTextures(ColorSelectionType selectionType)
        {
            // If the gradient texture is not used, delete it.
            if (selectionType != ColorSelectionType.Gradient)
            {
                //if (m_DistanceColorTexture != null)
                    //DestroyImmediate(m_DistanceColorTexture);
                m_DistanceColorTexture = null;
            }

            if (selectionType == ColorSelectionType.Gradient)
            {
                BakeFogColor(DistanceColorTexture, distanceColorSource.gradient);
            }
        }

        private void UpdateHeightFogTextures(ColorSelectionType selectionType)
        {
            // If the gradient texture is not used, delete it.
            if (selectionType != ColorSelectionType.Gradient)
            {
                //if (m_HeightColorTexture != null)
                    //DestroyImmediate(m_HeightColorTexture);
                m_HeightColorTexture = null;
            }

            if (selectionType == ColorSelectionType.Gradient)
            {
                BakeFogColor(HeightColorTexture, heightColorSource.gradient);
            }
        }

        public void BakeFogColor(Texture2D target, Gradient gradient)
        {
            if (target == null)
            {
                return;
            }

            float fWidth = target.width;
            Color[] pixels = new Color[target.width];

            for (float i = 0f; i <= 1f; i += 1f / fWidth)
            {
                Color color = gradient.Evaluate(i);
                pixels[(int)Mathf.Floor(i * (fWidth - 1f))] = color;
            }

            target.SetPixels(pixels);
            target.Apply();
        }
    }
}
