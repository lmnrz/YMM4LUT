Texture2D InputTexture : register(t0);
SamplerState InputSampler : register(s0);

Texture3D LutTexture : register(t1);
SamplerState LutSampler : register(s1);

cbuffer constants : register(b0)
{
    float value : packoffset(c0.x);   // 強度
    float lutSize : packoffset(c0.y); // LUTサイズ
}; 

float4 main(
    float4 pos : SV_POSITION,
    float4 posScene : SCENE_POSITION,
    float4 uv0 : TEXCOORD0
) : SV_Target
{
    float4 rawColor = InputTexture.Sample(InputSampler, uv0.xy);
    
    if (rawColor.a <= 0.0)
        return float4(0, 0, 0, 0);
    
    if (value <= 0.0)
        return rawColor;
    
    float3 pureColor = rawColor.rgb / rawColor.a;

    // --- ここからLUT処理 ---
    
    float3 originalColor = saturate(pureColor);

    // ハーフテクセル補正
    float scale = (lutSize - 1.0) / lutSize;
    float offset = 0.5 / lutSize;
    float3 lutCoord = originalColor * scale + offset;
    
    // 3D LUTサンプリング
    float3 lutColor = LutTexture.Sample(LutSampler, lutCoord).rgb;
    
    // 強度によるブレンド (Unmultiplyされた空間で行う)
    float3 blendedColor = lerp(pureColor, lutColor, value);

    // --- LUT処理ここまで ---

    // 2. Remultiply: 再びアルファを掛けてPremultiplied Alphaの状態に戻す
    float3 finalColor = blendedColor * rawColor.a;
    
    return float4(finalColor, rawColor.a);
}