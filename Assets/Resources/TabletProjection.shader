Shader "Hidden/Lab/TabletProjection"
{
    Properties { _MainTex ("Tablet", 2D) = "white" {} _TopRatio ("Top to bottom width", Float) = 1 }
    SubShader
    {
        Pass
        {
            ZTest Always ZWrite Off Cull Off Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _TopRatio;
            struct Data { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            Data vert(appdata_img v)
            {
                Data o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord; return o;
            }
            fixed4 frag(Data i) : SV_Target
            {
                float y = 1 - i.uv.y;
                float w = lerp(_TopRatio, 1, y);
                float u = 0.5 + (i.uv.x - 0.5) / w;
                clip(min(u, 1 - u));
                return fixed4(tex2D(_MainTex, float2(u, 1 - y / w)).rgb, 1);
            }
            ENDCG
        }
    }
}
