Shader "Custom/SpriteWindSway"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        
        [Space(10)]
        [Header(Wind Settings)]
        _Speed ("Wind Speed", Float) = 1.0
        _MinStrength ("Min Strength", Range(0.0, 1.0)) = 0.01
        _MaxStrength ("Max Strength", Range(0.0, 1.0)) = 0.05
        _StrengthScale ("Strength Scale", Float) = 100.0
        _Interval ("Interval", Float) = 3.5
        _Detail ("Detail", Float) = 1.0
        _HeightOffset ("Height Offset (0=bottom, 1=top)", Range(0.0, 1.0)) = 0.0
        _Offset ("Time Offset", Float) = 0.0
    }

    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha // 유니티 스프라이트 기본 블렌딩(Pre-multiplied Alpha)

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"

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
            };

            fixed4 _Color;
            float _Speed;
            float _MinStrength;
            float _MaxStrength;
            float _StrengthScale;
            float _Interval;
            float _Detail;
            float _HeightOffset;
            float _Offset;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                
                // 타일맵 렌더링 시 모든 타일이 동일하게 흔들리는 것을 방지하려면
                // 아래처럼 버텍스의 월드 X 좌표를 오프셋에 더해 변주를 줄 수 있습니다.
                // float worldPosX = mul(unity_ObjectToWorld, IN.vertex).x;
                // float t = _Time.y * _Speed + _Offset + worldPosX * 0.5;
                
                float t = _Time.y * _Speed + _Offset;

                // 버텍스 오버헤드를 최소화하기 위해 pow() 대신 단순 곱셈 연산 적용
                float diffStr = _MaxStrength - _MinStrength;
                float diffStrSq = diffStr * diffStr;

                float strength = clamp(_MinStrength + diffStrSq + sin(t / _Interval) * diffStrSq, _MinStrength, _MaxStrength) * _StrengthScale;
                
                // Godot은 UV의 Y축이 위에서 아래로(0->1) 향하지만, 
                // Unity는 아래에서 위로(0->1) 향하기 때문에 원본의 (1.0 - uv.y) 식을 uv.y로 수정했습니다.
                float swayMask = max(0.0, IN.texcoord.y - _HeightOffset);
                float wind = (sin(t) + cos(t * _Detail)) * strength * swayMask;

                // 로컬 X축 버텍스 이동
                float4 localPos = IN.vertex;
                localPos.x += wind;

                OUT.vertex = UnityObjectToClipPos(localPos);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap (OUT.vertex);
                #endif

                return OUT;
            }

            sampler2D _MainTex;

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                // SpriteRenderer의 정상적인 색상 출력을 위한 RGB-Alpha 곱연산
                c.rgb *= c.a; 
                return c;
            }
            ENDCG
        }
    }
}