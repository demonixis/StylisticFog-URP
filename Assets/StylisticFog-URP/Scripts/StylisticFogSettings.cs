using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Demonixis.Toolbox.Rendering
{
    [Serializable]
    public class StylisticFogSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        public HeightFogSettings HeightFog = HeightFogSettings.defaultSettings;
        public DistanceFogSettings DistanceFog = DistanceFogSettings.defaultSettings;
        public Material Material = null;
    }
}
