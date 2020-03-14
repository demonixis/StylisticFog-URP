using System;
using UnityEngine;

namespace Demonixis.Toolbox.Rendering
{
    [Serializable]
    public enum ColorSelectionType
    {
        Gradient = 1,
        TextureRamp = 2,
        CopyOther = 3
    }

    public enum FogTypePass
    {
        DistanceOnly = 0,
        HeightOnly = 1,
        BothSharedColorSettings = 2,
        BothSeperateColorSettinsg = 3,
        None
    }

    [Serializable]
    public struct FogColorSource
    {
        [AttributeUsage(AttributeTargets.Field)]
        public class DisplayOnSelectionType : Attribute
        {
            public readonly ColorSelectionType selectionType;
            public DisplayOnSelectionType(ColorSelectionType _selectionType)
            {
                selectionType = _selectionType;
            }
        }

        [Tooltip("Color gradient.")]
        [DisplayOnSelectionType(ColorSelectionType.Gradient)]
        public Gradient gradient;

        [Tooltip("Custom fog color ramp.")]
        [DisplayOnSelectionType(ColorSelectionType.TextureRamp)]
        public Texture2D colorRamp;

        public static FogColorSource defaultSettings
        {
            get
            {
                GradientAlphaKey firstAlpha = new GradientAlphaKey(0f, 0f);
                GradientAlphaKey lastAlpha = new GradientAlphaKey(1f, 1f);
                GradientAlphaKey[] initialAlphaKeys = { firstAlpha, lastAlpha };
                FogColorSource source = new FogColorSource()
                {
                    gradient = new Gradient(),
                    colorRamp = null,
                };
                source.gradient.alphaKeys = initialAlphaKeys;
                return source;
            }
        }
    }

    [Serializable]
    public struct DistanceFogSettings
    {
        [Tooltip("Wheter or not to apply distance based fog.")]
        public bool enabled;

        [Tooltip("Wheter or not to apply distance based fog to the skybox.")]
        public bool fogSkybox;

        [Tooltip("Fog is fully saturated beyond this distance.")]
        public float endDistance;

        [Tooltip("Color selection for distance fog")]
        public ColorSelectionType colorSelectionType;

        public static DistanceFogSettings defaultSettings
        {
            get
            {
                return new DistanceFogSettings()
                {
                    enabled = false,
                    fogSkybox = false,
                    endDistance = 100f,
                    colorSelectionType = ColorSelectionType.Gradient,
                };
            }
        }
    }

    [Serializable]
    public struct HeightFogSettings
    {
        [Tooltip("Wheter or not to apply height based fog.")]
        public bool enabled;

        [Tooltip("Wheter or not to apply height based fog to the skybox.")]
        public bool fogSkybox;

        [Tooltip("Height where the fog starts.")]
        public float baseHeight;

        [Tooltip("Fog density at fog altitude given by height.")]
        public float baseDensity;

        [Tooltip("The rate at which the thickness of the fog decays with altitude.")]
        [Range(0.001f, 1f)]
        public float densityFalloff;

        [Tooltip("Color selection for height fog.")]
        public ColorSelectionType colorSelectionType;

        public static HeightFogSettings defaultSettings
        {
            get
            {
                return new HeightFogSettings()
                {
                    enabled = true,
                    fogSkybox = true,
                    baseHeight = 0f,
                    baseDensity = 0.1f,
                    densityFalloff = 0.5f,
                    colorSelectionType = ColorSelectionType.CopyOther,
                };
            }
        }
    }
}
