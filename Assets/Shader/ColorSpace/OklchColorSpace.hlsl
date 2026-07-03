#ifndef OKLCH_COLOR_SPACE_INCLUDED
#define OKLCH_COLOR_SPACE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "OklchColorSpaceConfig.hlsl"

TEXTURE2D(_OklchRgbToOklabLut);
TEXTURE2D(_OklchOklabToRgbLut);
SAMPLER(sampler_OklchRgbToOklabLut);
SAMPLER(sampler_OklchOklabToRgbLut);

float3 Oklch_LoadLutTexel(TEXTURE2D_PARAM(lutTex, lutSampler), float slice, float2 cell)
{
    const float lutArea = OKLCH_LUT_SIZE_F * OKLCH_LUT_SIZE_F;
    float px = slice * OKLCH_LUT_SIZE_F + cell.x + 0.5;
    float py = cell.y + 0.5;
    float2 uv = float2(px / lutArea, py / OKLCH_LUT_SIZE_F);
    return SAMPLE_TEXTURE2D_LOD(lutTex, lutSampler, uv, 0.0).rgb;
}

float3 Oklch_SampleBilinearInSlice(TEXTURE2D_PARAM(lutTex, lutSampler), float slice, float2 coord)
{
    const float lutSizeMinusOne = OKLCH_LUT_SIZE_F - 1.0;
    coord = min(coord, float2(lutSizeMinusOne, lutSizeMinusOne));

    float2 i0 = floor(coord);
    float2 f = coord - i0;
    i0 = min(i0, float2(lutSizeMinusOne - 1.0, lutSizeMinusOne - 1.0));

    float3 c00 = Oklch_LoadLutTexel(TEXTURE2D_ARGS(lutTex, lutSampler), slice, i0);
    float3 c10 = Oklch_LoadLutTexel(TEXTURE2D_ARGS(lutTex, lutSampler), slice, i0 + float2(1.0, 0.0));
    float3 c01 = Oklch_LoadLutTexel(TEXTURE2D_ARGS(lutTex, lutSampler), slice, i0 + float2(0.0, 1.0));
    float3 c11 = Oklch_LoadLutTexel(TEXTURE2D_ARGS(lutTex, lutSampler), slice, i0 + float2(1.0, 1.0));
    return lerp(lerp(c00, c10, f.x), lerp(c01, c11, f.x), f.y);
}

float3 Oklch_SampleLut3D(TEXTURE2D_PARAM(lutTex, lutSampler), float3 uvw)
{
    uvw = saturate(uvw);
    float3 coord = uvw * (OKLCH_LUT_SIZE_F - 1.0);

    float z0 = floor(coord.z);
    float z1 = min(z0 + 1.0, OKLCH_LUT_SIZE_F - 1.0);
    float zFrac = coord.z - z0;

    float3 col0 = Oklch_SampleBilinearInSlice(TEXTURE2D_ARGS(lutTex, lutSampler), z0, coord.xy);
    float3 col1 = Oklch_SampleBilinearInSlice(TEXTURE2D_ARGS(lutTex, lutSampler), z1, coord.xy);
    return lerp(col0, col1, zFrac);
}

float3 Oklch_OklabToOklch(float3 lab)
{
    float c = length(lab.yz);
    float h = atan2(lab.z, lab.y);
    h = h < 0.0 ? h + TWO_PI : h;
    return float3(lab.x, c, h / TWO_PI);
}

float3 Oklch_OklchToOklab(float3 oklch)
{
    float hRad = oklch.z * TWO_PI;
    float a = oklch.y * cos(hRad);
    float b = oklch.y * sin(hRad);
    return float3(oklch.x, a, b);
}

float3 Oklch_LinearRgbToOklab(float3 linearRgb)
{
    return Oklch_SampleLut3D(TEXTURE2D_ARGS(_OklchRgbToOklabLut, sampler_OklchRgbToOklabLut), saturate(linearRgb));
}

float3 Oklch_OklabToLinearRgb(float3 lab)
{
    float3 uvw;
    uvw.x = saturate(lab.x);
    uvw.y = saturate((lab.y + OKLAB_AB_RANGE) / (2.0 * OKLAB_AB_RANGE));
    uvw.z = saturate((lab.z + OKLAB_AB_RANGE) / (2.0 * OKLAB_AB_RANGE));
    return saturate(Oklch_SampleLut3D(TEXTURE2D_ARGS(_OklchOklabToRgbLut, sampler_OklchOklabToRgbLut), uvw));
}

float3 Oklch_LinearRgbToOklch(float3 linearRgb)
{
    float3 lab = Oklch_LinearRgbToOklab(saturate(linearRgb));
    return Oklch_OklabToOklch(lab);
}

float3 Oklch_OklchToLinearRgb(float3 oklch)
{
    oklch.x = saturate(oklch.x);
    oklch.y = saturate(oklch.y);

    float3 lab = Oklch_OklchToOklab(oklch);
    lab.y = clamp(lab.y, -OKLAB_AB_RANGE, OKLAB_AB_RANGE);
    lab.z = clamp(lab.z, -OKLAB_AB_RANGE, OKLAB_AB_RANGE);
    return Oklch_OklabToLinearRgb(lab);
}

float Oklch_HueDelta(float h0, float h1)
{
    float d = h1 - h0;
    d = fmod(d + 1.5, 1.0) - 0.5;
    return d;
}

float3 Oklch_LerpOklch(float3 a, float3 b, float t)
{
    float3 result;
    result.x = lerp(a.x, b.x, t);
    result.y = lerp(a.y, b.y, t);
    result.z = a.z + Oklch_HueDelta(a.z, b.z) * t;
    result.z = frac(result.z);
    return result;
}

float3 Oklch_LerpRgb(float3 rgb1, float3 rgb2, float t)
{
    float3 oklch1 = Oklch_LinearRgbToOklch(rgb1);
    float3 oklch2 = Oklch_LinearRgbToOklch(rgb2);
    float3 oklchResult = Oklch_LerpOklch(oklch1, oklch2, t);
    return Oklch_OklchToLinearRgb(oklchResult);
}

#endif
