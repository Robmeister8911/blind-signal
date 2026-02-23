Shader "BlindSignal/PingRipple"
{
    // -------------------------------------------------------------------------
    // Unlit transparent shader that renders a growing, fading circular ring.
    // The ring's radius, thickness, and alpha are controlled through material
    // properties so PingRipple.cs can animate them at runtime.
    //
    // UV space: the mesh should be a quad centred at (0.5, 0.5).
    // -------------------------------------------------------------------------
    Properties
    {
        _Color      ("Ring Colour",        Color)  = (0.2, 0.8, 1.0, 1.0)
        _Radius     ("Ring Radius (0-1)",  Range(0,1)) = 0.4
        _Thickness  ("Ring Thickness",     Range(0,0.5)) = 0.05
        _Alpha      ("Alpha Multiplier",   Range(0,1)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            fixed4  _Color;
            float   _Radius;
            float   _Thickness;
            float   _Alpha;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Distance from the quad centre (0.5, 0.5) in UV space.
                float2 centred = i.uv - float2(0.5, 0.5);
                float  dist    = length(centred);

                // Soft ring mask: 1 inside the ring, 0 outside.
                float inner = smoothstep(_Radius - _Thickness,
                                         _Radius - _Thickness * 0.5,
                                         dist);
                float outer = smoothstep(_Radius,
                                         _Radius - _Thickness * 0.5,
                                         dist);
                float ring  = inner * outer;

                fixed4 col = _Color;
                col.a      = col.a * ring * _Alpha;
                return col;
            }
            ENDCG
        }
    }

    FallBack "Unlit/Transparent"
}
