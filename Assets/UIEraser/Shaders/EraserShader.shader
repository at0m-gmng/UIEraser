Shader "UI/UIEraser"
{
    Properties
    {
        _MainTex ("Base Image", 2D) = "white" {}
        _Color   ("Tint Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex; float4 _MainTex_ST;
            sampler2D _MaskTex; float4 _MaskTex_ST;
            fixed4   _Color;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f     { float4 pos : SV_POSITION; float2 uv  : TEXCOORD0; fixed4 col   : COLOR; };

            v2f vert(appdata IN)
            {
                v2f OUT;
                OUT.pos = UnityObjectToClipPos(IN.vertex);
                OUT.uv  = IN.uv;
                OUT.col = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.uv;
                fixed4 baseCol = tex2D(_MainTex, TRANSFORM_TEX(uv, _MainTex)) * IN.col;
                fixed mask   = tex2D(_MaskTex, TRANSFORM_TEX(uv, _MaskTex)).r;
                baseCol.a *= mask;
                return baseCol;
            }
            ENDCG
        }
    }
}
