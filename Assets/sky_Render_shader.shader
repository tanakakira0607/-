Shader "Unlit/RenderShader2_2" //shader名の宣言
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {} //マテリアルインスペクタに表示されるパラメータを定義
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" } //タグの定義
		LOD 100 //shaderの複数さを定義

		Pass //1回の描画
		{
			CGPROGRAM  //gpuの処理↓
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
float random(float2 st)
{
    return frac(
        sin(dot(st, float2(12.9898,78.233)))
        * 43758.5453
    );
}

float	noise(float2 st){
	float2 i = floor(st);
	float2 f = frac(st);
	float a = random(i);
	float b = random(i +  float2(1.0,0.0));
	float c = random(i + float2(0.0,1.0));
	float d = random(i + float2(1.0,1.0));

	f= f * f * (3.0 -2.0 *f);
	return lerp(
		lerp(a,b,f.x),
		lerp(c,d,f.x),
		f.y
		);
	}
			fixed4 frag (v2f i) : SV_Target
			{

				float2 uv = i.uv;
				uv.x += _Time.y * 0.03;
				float n =
(
    noise(uv * 4)
    + noise(uv * 8) * 0.5
    + noise(uv * 16) * 0.25
	+ noise(uv * 32) * 0.125
	+ noise(uv*64)*0.0625
) / 1.75;

float edgenoise = noise(uv *30);

float cloud =
    smoothstep(
        0.65,
        0.85,
        n + edgenoise *0.05
    );

float lihgt = saturate(n*1.5);


float4 sky =
    lerp(
        float4(0.7, 0.8, 1.0, 1.0),
        float4(0.2, 0.5, 1.0, 1.0),
        i.uv.y
    );


float4 cloudColor = float4(
    1.0,
    1.0,
    1.0,
    1.0
);

cloudColor *- lihgt;

return lerp(
    sky,
    cloudColor,
    cloud
);
				
				
			}
			ENDCG
		}
	}
}
