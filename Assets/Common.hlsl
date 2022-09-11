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
