using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


[Serializable, VolumeComponentMenuForRenderPipeline("Test/CustomEffect", typeof(UniversalRenderPipeline))]
public class CustomPostProcessingEffect : VolumeComponent, IPostProcessComponent
{
    public FloatParameter tintIntensity = new FloatParameter(1);
    public ColorParameter tintColour = new ColorParameter(Color.white);
    public bool IsActive() => true;
    public bool IsTileCompatible() => true;
}
