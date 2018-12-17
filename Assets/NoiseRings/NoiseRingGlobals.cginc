#ifndef NOISE_RING_GLOBALS_CGINC
#define NOISE_RING_GLOBALS_CGINC

#include "UnityCG.cginc"

float4x4 NoiseRingGlobals_ClipToWorld[2];

// This function is necessary to retrieve the correct ClipToWorld matrix for
// the current eye if Unity's Single Pass Stereo rendering mode is used.
float4x4 NoiseRingGlobals_ClipToWorldMatrix()
{
#if UNITY_SINGLE_PASS_STEREO
    int index = unity_StereoEyeIndex;
#else
    int index = 0;
#endif
    return NoiseRingGlobals_ClipToWorld[index];
}

#endif //NOISE_RING_GLOBALS_CGINC