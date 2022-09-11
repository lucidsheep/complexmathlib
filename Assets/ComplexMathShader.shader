// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Sprites/CMath"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		[PerRendererData] _DotVals("Zeroes and Poles", 2DArray) = "" {}
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
		Blend One OneMinusSrcAlpha

		Pass
		{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ PIXELSNAP_ON
			#include "UnityCG.cginc"

			static const float PI = 3.14159265f;

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
				half2 texcoord  : TEXCOORD0;
				float4 screenPos : TEXCOORD1;
			};

			float3 hue2rgb(float hue) {
				hue = frac(hue); //only use fractional part of hue, making it loop
				float r = abs(hue * 6 - 3) - 1; //red
				float g = 2 - abs(hue * 6 - 2); //green
				float b = 2 - abs(hue * 6 - 4); //blue
				float3 rgb = float3(r,g,b); //combine components
				rgb = saturate(rgb); //clamp between 0 and 1
				return rgb;
			}

float2 sum(float2 summand1, float2 summand2){
    return float2(summand1[0]+summand2[0],summand1[1]+summand2[1]);
}

float2 diff(float2 summand1, float2 summand2){
    return float2(summand1[0]-summand2[0],summand1[1]-summand2[1]);
}

float2 prod(float2 multip1,float2 multip2){
    return float2((multip1[0]*multip2[0])-(multip1[1]*multip2[1]),(multip1[0]*multip2[1])+(multip1[1]*multip2[0]));
}

float2 div(float2 multip1,float2 multip2){
    float denom = multip2[0]*multip2[0]+multip2[1]*multip2[1];
    return float2(((multip1[0]*multip2[0])+(multip1[1]*multip2[1]))/denom , ((multip1[1]*multip2[0])-(multip1[0]*multip2[1]))/denom );
}

//converts val from ratios, old ratio = rangeConv[0], [1], new ratio = rangeConv[2], [3]
float map(float val, float4 rangeConv)
{
	return (((val - rangeConv[0]) / (rangeConv[1] - rangeConv[0])) * (rangeConv[3] - rangeConv[2])) + rangeConv[2];
}

float2 myFunction(float2 z)
{
/*
//todo
    let aX = zero1X;
    let aY = zero1Y;

    let bX = zero2X;
    let bY = zero2Y;

    let cX = poleX;
    let cY = poleY;

*/

float2 a = {0, 0};
float2 b = {0, 0};
float2 c = {0, 0};

bool extraZero = false;
bool extraPole = false;

    if(!extraZero&&!extraPole){

        return diff(z,a);

    }else if(extraZero&&!extraPole){

        return prod(diff(z,a),diff(z,b));

    }else if(!extraZero&&extraPole){

        return div(  diff(z,a) ,  diff(z,c )   );

    }else if(extraZero&&extraPole) {

        return div(
                    prod(diff(z,a),diff(z,b)), 
                    diff(z,c)
                );

		}


}
float2 findPhase(float2 pos){

    //float myCoordX = pos.x; //pixelToAxisX(pos.x);
    //float myCoordY = pos.y; // pixelToAxisY(pos.y);

    //float2 myVal = myFunction(pos);
	float2 myVal = {0,0};
    float myArg = atan2(myVal[1],myVal[0]);

    return map(myArg,float4(-PI,PI,0,255));

}





			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.screenPos = ComputeScreenPos(OUT.vertex);
				OUT.texcoord = IN.texcoord;
				OUT.color = IN.color;
				return OUT;
			}

			sampler2D _MainTex;
			sampler2D _AlphaTex;
			float _AlphaSplitEnabled;

			fixed4 frag(v2f IN) : SV_Target
			{
				fixed4 c = IN.color;
				fixed4 r = IN.color;
				fixed4 f = IN.color;
/*
				f.a = c.a;
				f.r = ((r.r * r.a) + (c.r * remainder)) * f.a;
				f.g = ((r.g * r.a) + (c.g * remainder)) * f.a;
				f.b = ((r.b * r.a) + (c.b * remainder)) * f.a;
*/
				return f;
			}
		ENDCG
		}
	}
}
