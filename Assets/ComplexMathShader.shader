
//Shader boilerplate, I recommend ignoring it and not touching it :)
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
//actual start of the shader program


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

//Shader parameters set within C# code
uint CanvasSize;
uint NumPoles;
uint NumZeroes;
uint PixelZoom;
uint PixelStyle;
float Contours;
float Zeroes[16];
float Poles[16];
float4 Anchor;
float4 Scatter;
uint UseTexture;
uint Rivers;
float RiverWidth;
float ContourSpeed;

//Texture data (used in texture mode)
sampler2D _MainTex;

//Utility functions
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

//converts val from between rangeConv[0], rangeConv[1] to between rangeConv[2], rangeConv[3]
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

//transforms a complex number z through a series of zeroes and poles to a new complex number
float2 myFunction(float2 z)
{

    if(NumZeroes == 0)
        return prod(float2(Anchor.x, Anchor.y), z) + float2(Scatter.x, Scatter.y);

    float2 numerator = z - float2(Zeroes[0], Zeroes[1]);

    for(uint i = 2; i < NumZeroes * 2; i += 2)
    {
        numerator = prod(numerator, (z - float2(Zeroes[i], Zeroes[i+1])));
    }
    if(NumPoles == 0)
        return prod(float2(Anchor.x, Anchor.y), numerator) + float2(Scatter.x, Scatter.y);

    float2 denom = z - float2(Poles[0], Poles[1]); 
    for(uint j = 2; j < NumPoles * 2; j += 2)
    {
        denom = prod(denom, (z - float2(Poles[j], Poles[j+1])));
    }
    return prod(float2(Anchor.x, Anchor.y), div(numerator, denom)) + float2(Scatter.x, Scatter.y);

}
//Checks if a hue should be displayed in River mode
//Naive implmeentation - Rivers 1-5 are centered to even fractions of hue (.0, .2, .4, .6. .8)
//Needs better implementation to evenly space out rivers and allow for more than 5
float CheckRivers(float hue)
{
	//check if River mode is even on
	if(Rivers <= 0) return 1.0;

	int evenCheck = hue * 10;
	float frac = abs((hue * 10.0) - evenCheck);
	if(evenCheck % 2 == 0) return 0.0;
	if(((evenCheck +1) / 2 <= Rivers) && (frac <= RiverWidth)) return 1.0;
	return 0.0;
}

float findSawTooth(float2 myVal){

    if(Contours <= 0.0)
        return 1.0;

    float myMod = sqrt((myVal[0]*myVal[0])+(myVal[1]*myVal[1]));

    float myBaseChange = log(myMod)/log(Contours);

	myBaseChange += (ContourSpeed * _Time[1]);
    return map(floor(myBaseChange)-myBaseChange,float4(-1.0,0.0,1.0,0.58823));

}
//Core function to get a color for a given complex number
float3 getColor(float2 pos)
{
	float2 myVal = myFunction(pos); //transform the complex number using zeroes and poles
	float hue = map(atan2(myVal.y, myVal.x), float4(-PI, PI, 0.0, 1.0)); //convert it to a hue
	float3 rgb = hue2rgb(hue) * CheckRivers(hue); //convert hue to rgb, check for rivers
	return findSawTooth(myVal) * rgb; //adjust brightness based on sawtooth
}
float2 vertexToXY(float4 vertex)
{
	return float2(map(vertex.x, float4(0.0, 1.0, -1.0, 1.0)), map(vertex.y, float4(0.0, 1.0, -1.0, 1.0)));
}

float2 vertexToPixelCoords(float4 id)
{
	float2 pixelCoords = float2(id.x, CanvasSize - id.y); // unity does y coordinates backwards
	float2 centerPix = float2(((uint)pixelCoords.x / (uint)PixelZoom) * (float)PixelZoom, ((uint)pixelCoords.y / (uint)PixelZoom) * (float)PixelZoom);
	return float2(map(centerPix.x, float4(0, CanvasSize, -1.0, 1.0)), map(centerPix.y, float4(0, CanvasSize, -1.0, 1.0)));
}
//Simulation of pixel styles, checks if a given pixel should be black based on style
bool shouldOutputBlack(float4 id)
{
	//PixelStyle: 0 = no style, 1 = circles, 2 = squares
	if(PixelStyle == 0) return false;
    
    float radius = (float)PixelZoom / 2.0;
    float2 center = float2(((uint)id.x / (uint)PixelZoom) * (float)PixelZoom + radius, ((uint)id.y / (uint)PixelZoom) * (float)PixelZoom + radius);
    if(PixelStyle == 1 && distance(id, center) >= (radius * .95))
        return true;
    else if(PixelStyle == 2 && (abs(id.x - center.x) > radius - 1 || abs(id.y - center.y) > radius - 1))
        return true;
	return false;
}
//standard Unity vertex shader
v2f vert(appdata_t IN)
{
	v2f OUT;
	OUT.vertex = UnityObjectToClipPos(IN.vertex);
	OUT.screenPos = ComputeScreenPos(OUT.vertex);
	OUT.texcoord = IN.texcoord;
	OUT.color = IN.color;
	return OUT;
}

//function run on every pixel to set color
fixed4 frag(v2f IN) : SV_Target
{
	if(UseTexture > 0)
	{
		//in texture mode we transform the pixel position and sample the texture at the new position

		float2 functionPos = myFunction(vertexToXY(IN.screenPos));

		//while loops ensure we don't go off the edge of the texture and loop around instead
		while(functionPos.x < -1.0)
			functionPos.x += 2.0;
		while(functionPos.x > 1.0)
			functionPos.x -= 2.0;
		while(functionPos.y < -1.0)
			functionPos.y += 2.0;
		while(functionPos.y > 1.0)
			functionPos.y -= 2.0;
		float2 texPos = float2(map(functionPos.x, float4(-1.0, 1.0, 0.0, 1.0)), map(functionPos.y, float4(-1.0, 1.0, 0.0, 1.0)));
		//sample the texture
		return tex2D(_MainTex, texPos);
	} else
	{
		//converts the position to the nearest simulated pixel
		float2 pixelXY = vertexToPixelCoords(IN.vertex);
		//run core color function
		float3 color = shouldOutputBlack(IN.vertex) ? float3(0,0,0) : getColor(pixelXY);
		return fixed4(color.r, color.g, color.b, 1.0);
	}
}
ENDCG
}
}
}
