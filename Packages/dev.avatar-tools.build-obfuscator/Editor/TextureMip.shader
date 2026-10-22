// SPDX-License-Identifier: GPL-3.0-only
// Build-time readback only. Never referenced by an avatar material.
Shader "Hidden/AvatarBuildObfuscator/TextureMip"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} }
    SubShader
    {
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Mip;
            float4 frag(v2f_img input) : SV_Target
            {
                return tex2Dlod(_MainTex, float4(input.uv, 0, _Mip));
            }
            ENDCG
        }
    }
    Fallback Off
}
