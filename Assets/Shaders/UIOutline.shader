Shader "UI/Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width (px)", Range(0,8)) = 0
        _OutlineEnabled ("Outline Enabled", Float) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _OutlineEnabled;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;

                // 描边：当本像素透明、但周围采样到不透明像素时，画描边色
                if (_OutlineEnabled > 0.5 && _OutlineWidth > 0.0 && col.a < 0.1)
                {
                    float2 d = _MainTex_TexelSize.xy * _OutlineWidth;
                    float a = 0;
                    a = max(a, tex2D(_MainTex, i.texcoord + float2( d.x, 0)).a);
                    a = max(a, tex2D(_MainTex, i.texcoord + float2(-d.x, 0)).a);
                    a = max(a, tex2D(_MainTex, i.texcoord + float2(0,  d.y)).a);
                    a = max(a, tex2D(_MainTex, i.texcoord + float2(0, -d.y)).a);
                    a = max(a, tex2D(_MainTex, i.texcoord + float2( d.x,  d.y)).a);
                    a = max(a, tex2D(_MainTex, i.texcoord + float2(-d.x,  d.y)).a);
                    a = max(a, tex2D(_MainTex, i.texcoord + float2( d.x, -d.y)).a);
                    a = max(a, tex2D(_MainTex, i.texcoord + float2(-d.x, -d.y)).a);
                    if (a > 0.1)
                        col = fixed4(_OutlineColor.rgb, _OutlineColor.a * a);
                }

                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                return col;
            }
            ENDCG
        }
    }
}
