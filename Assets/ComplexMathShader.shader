// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Sprites/CMath"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
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
// Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
#pragma exclude_renderers d3d11 gles
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
				half2 texcoord  : TEXCOORD0;
				float4 screenPos : TEXCOORD1;
			};

static const float PI = 3.14159265f;

float2 sum(float2 summand1, float2 summand2){
    return summand1 + summand2;
    //return float2(summand1[0]+summand2[0],summand1[1]+summand2[1]);
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

float3 hue2rgb(float hue) {
    hue = frac(hue); //only use fractional part of hue, making it loop
    float r = abs(hue * 6 - 3) - 1; //red
    float g = 2 - abs(hue * 6 - 2); //green
    float b = 2 - abs(hue * 6 - 4); //blue
    float3 rgb = float3(r,g,b); //combine components
    rgb = saturate(rgb); //clamp between 0 and 1
    return rgb;
}

float dist(float2 a, float2 b)
{
    return distance(a, b);
}


			uint NumPoles;
uint NumZeroes;
uint InputSize;
uint PixelZoom;
uint PixelStyle;
float Contours;
float Zeroes[16];
float Poles[16];
float4 Anchor;
float4 Constant;
uint UseTexture;

float2 myFunction(float2 z)
{

    if(NumZeroes == 0)
        return z;

    float2 numerator = z - float2(Zeroes[0], Zeroes[1]);

    for(uint i = 2; i < NumZeroes * 2; i += 2)
    {
        numerator = prod(numerator, (z - float2(Zeroes[i], Zeroes[i+1])));
    }
    if(NumPoles == 0)
        return numerator;

    float2 denom = z - float2(Poles[0], Poles[1]); 
    for(uint j = 2; j < NumPoles * 2; j += 2)
    {
        denom = prod(denom, (z - float2(Poles[j], Poles[j+1])));
    }
    return div(numerator, denom);

}

float getHue(float2 pos){
    //pos = float2(-1.0 + (pos.x * 2.0), -1.0 + (pos.y * 2.0));

    float2 myVal = myFunction(pos);
    float myArg = atan2(myVal[1],myVal[0]);

    return map(myArg,float4(-PI,PI,0.0, 1.0));

}

float findSawTooth(float2 myVal){

    if(Contours <= 0.0)
        return 1.0;

    float myMod = sqrt((myVal[0]*myVal[0])+(myVal[1]*myVal[1]));

    float myBaseChange = log(myMod)/log(Contours);

    return map(floor(myBaseChange)-myBaseChange,float4(-1.0,0.0,1.0,0.58823));

}

float3 getColor(float2 pos)
{
	float2 myVal = myFunction(pos);
	//hue
	float hue = map(atan2(myVal.y, myVal.x), float4(-PI, PI, 0.0, 1.0));
	float3 rgb = hue2rgb(hue);
	//sawtooth
	return findSawTooth(myVal) * rgb;
}
float2 vertexToXY(float4 vertex)
{
	return float2(map(vertex.x, float4(0.0, 1.0, -1.0, 1.0)), map(vertex.y, float4(0.0, 1.0, -1.0, 1.0)));
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
				if(UseTexture > 0)
				{
					float2 functionPos = myFunction(vertexToXY(IN.screenPos));
					while(functionPos.x < -1.0)
						functionPos.x += 2.0;
					while(functionPos.x > 1.0)
						functionPos.x -= 2.0;
					while(functionPos.y < -1.0)
						functionPos.y += 2.0;
					while(functionPos.y > 1.0)
						functionPos.y -= 2.0;
					float2 texPos = float2(map(functionPos.x, float4(-1.0, 1.0, 0.0, 1.0)), map(functionPos.y, float4(-1.0, 1.0, 0.0, 1.0)));
					return tex2D(_MainTex, texPos);
				} else
				{
					float2 pixelXY = vertexToXY(IN.screenPos);
					if(Anchor.z >= 0.0)
						pixelXY = prod(pixelXY, float2(Anchor.x, Anchor.y));
					if(Constant.z >= 0.0)
						pixelXY += float2(Constant.x, Constant.y);
					//float2 pixelXY = float2(((uint)id.x / (uint)PixelZoom) * (float)PixelZoom, ((uint)id.y / (uint)PixelZoom) * (float)PixelZoom);
					
					float3 color = getColor(pixelXY);
					return fixed4(color.r, color.g, color.b, 1.0);
				}
			}
		ENDCG
		}
	}
}
