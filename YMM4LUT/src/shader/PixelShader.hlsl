Texture2D InputTexture : register(t0);
SamplerState InputSampler : register(s0);

Texture3D LutTexture : register(t1);
SamplerState LutSampler : register(s1);

cbuffer constants : register(b0)
{
    float value : packoffset(c0.x);   // 強度
    float lutSize : packoffset(c0.y); // LUTサイズ
    float is3D : packoffset(c0.z);    // 1D=0, 3D=1
}; 

float4 main(
    float4 pos : SV_POSITION,
    float4 posScene : SCENE_POSITION,
    float4 uv0 : TEXCOORD0
) : SV_Target
{
    float4 rawColor = InputTexture.Sample(InputSampler, uv0.xy);
    
    // アルファ値が0なら即終了
    if (rawColor.a <= 0.0)
        return float4(0, 0, 0, 0);
    
    // 強度が0なら元の色を返す
    if (value <= 0.0)
        return rawColor;
    
    // Unmultiply: ゼロ除算防止のためのmax
    float3 pureColor = rawColor.rgb / max(rawColor.a, 0.00001);
    float3 originalColor = saturate(pureColor);

    // ハーフテクセル補正
    float scale = (lutSize - 1.0) / lutSize;
    float offset = 0.5 / lutSize;
    float3 lutCoord = originalColor * scale + offset;
    
    float3 lutColor;
    if (is3D > 0.5)
    {
        // 3D LUTサンプリング
        lutColor = LutTexture.Sample(LutSampler, lutCoord).rgb;
    }
    else
    {
        // 1D LUTサンプリング: RGB各チャンネルを個別にサンプリング
        // 1Dテクスチャを (Size, 1, 1) の 3Dとして扱っているため Y, Z は 0.5
        float r = LutTexture.Sample(LutSampler, float3(lutCoord.r, 0.5, 0.5)).r;
        float g = LutTexture.Sample(LutSampler, float3(lutCoord.g, 0.5, 0.5)).g;
        float b = LutTexture.Sample(LutSampler, float3(lutCoord.b, 0.5, 0.5)).b;
        lutColor = float3(r, g, b);
    }
    
    // 強度によるブレンド
    float3 blendedColor = lerp(pureColor, lutColor, value);

    // Remultiply: 再びアルファを掛ける
    return float4(blendedColor * rawColor.a, rawColor.a);
}