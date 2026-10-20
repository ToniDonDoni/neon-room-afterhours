// Procedural soft-glow shader for the mobile control sticks.
// Draws a translucent disc, a thin neon ring and a soft halo in one pass.
// No textures are sampled: the whole dial is computed from the UV radius.
Shader "NeonRoom/UI Stick Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Dial Color", Color) = (0.22, 0.83, 1, 1)
        _GlowColor ("Glow Color", Color) = (0.22, 0.83, 1, 1)
        _Fill ("Fill Alpha", Range(0, 1)) = 0.14
        _RingRadius ("Ring Radius", Range(0, 1)) = 0.78
        _RingWidth ("Ring Width", Range(0.005, 0.3)) = 0.035
        _RingIntensity ("Ring Intensity", Range(0, 4)) = 1.6
        _GlowPower ("Glow Falloff", Range(1, 40)) = 9
        _GlowIntensity ("Glow Intensity", Range(0, 3)) = 0.85
        _InnerGlow ("Inner Glow", Range(0, 2)) = 0.35
        _Knob ("Knob Mode", Range(0, 1)) = 0
        _Pulse ("Touch Boost", Range(0, 2)) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
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

        Cull [_Cull]
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend [_SrcBlend] [_DstBlend]
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            fixed4 _Color;
            fixed4 _GlowColor;
            float _Fill;
            float _RingRadius;
            float _RingWidth;
            float _RingIntensity;
            float _GlowPower;
            float _GlowIntensity;
            float _InnerGlow;
            float _Knob;
            float _Pulse;
            float4 _ClipRect;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 p = i.texcoord * 2.0 - 1.0;
                float r = length(p);
                fixed4 col = fixed4(0, 0, 0, 0);

                if (_Knob > 0.5)
                {
                    // Radial gradient knob: bright core, soft falloff to the rim.
                    float core = saturate(1.0 - r);
                    float a = pow(core, 1.7);
                    col.rgb = _Color.rgb * (1.0 + _Pulse * 0.45);
                    col.a = a * _Color.a * (0.9 + 0.1 * core);
                }
                else
                {
                    float ring = 1.0 - smoothstep(_RingWidth * 0.5, _RingWidth, abs(r - _RingRadius));
                    float fill = (1.0 - smoothstep(_RingRadius - _RingWidth, _RingRadius, r)) * _Fill;
                    float outerDistance = saturate(r - _RingRadius);
                    float glow = exp(-outerDistance * _GlowPower) * step(_RingRadius - _RingWidth * 0.5, r) * _GlowIntensity;
                    float inner = exp(-max(0.0, _RingRadius - r) * _GlowPower * 0.55) * _InnerGlow * 0.35;
                    float edgeFade = 1.0 - smoothstep(0.965, 1.0, r);

                    col.rgb = _Color.rgb * (fill + inner) + _GlowColor.rgb * (ring * _RingIntensity + glow) * (1.0 + _Pulse);
                    col.a = saturate(fill + ring * _RingIntensity + glow + inner) * edgeFade * _Color.a;
                }

                col *= i.color;
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                return col;
            }
            ENDCG
        }
    }

    Fallback "UI/Default"
}
