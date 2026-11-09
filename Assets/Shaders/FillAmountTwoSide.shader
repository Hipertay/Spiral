Shader "Custom/WiperFill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FillAmount ("Fill Amount", Range(0, 1)) = 0.5
        [Toggle] _Symmetric ("Symmetric", Float) = 1   // в обе стороны или только по часовой
        [Toggle] _FlipSide ("Reverse Side", Float) = 0 // в какую сторону растёт/уменьшается сектор
    }

    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _FillAmount;
            float _Symmetric;
            float _FlipSide;
            #define PI 3.14159265359

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Центр UV (0.5, 0.5)
                float2 center = float2(0.5, 0.5);
                float2 dir = i.uv - center;

                // Угол от вертикали вверх (против часовой)
                float angle = atan2(dir.x, dir.y); // -PI .. PI
                if (angle < 0) angle += 2 * PI;    // 0 .. 2PI

                // Отражаем угол по вертикали: рост/сжатие сектора идёт в другую сторону.
                if (_FlipSide > 0.5)
                    angle = (2 * PI - angle) % (2 * PI);

                if (_Symmetric > 0.5)
                {
                    // === СИММЕТРИЧНЫЙ РЕЖИМ ===
                    // Расширение от верхней точки (0°) в обе стороны
                    // delta = angle (так как startAngle = 0)
                    float delta = angle;
                    // Приводим к [-PI, PI] (на случай, если angle > PI)
                    delta = fmod(delta + PI, 2 * PI) - PI;
                    float halfAngle = _FillAmount * PI;
                    if (abs(delta) > halfAngle)
                        discard;
                }
                else
                {
                    // === ОДНОСТОРОННИЙ РЕЖИМ ===
                    // По часовой стрелке от верхней точки
                    float clockwiseAngle = (2 * PI - angle) % (2 * PI);
                    float maxAngle = _FillAmount * 2 * PI;
                    if (clockwiseAngle > maxAngle)
                        discard;
                }

                // Вывод текстуры
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                return col;
            }
            ENDCG
        }
    }
}