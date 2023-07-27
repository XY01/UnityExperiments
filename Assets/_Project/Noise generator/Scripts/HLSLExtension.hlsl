// MATH HELPERS
//
float inverseLerp(float a, float b, float value)
{
    return (value - a) / (b - a);
}

// DISTANCE FUNCTIONS
//
float ManhattanDistance(float2 p1, float2 p2) {
    return abs(p1.x - p2.x) + abs(p1.y - p2.y);
}

float ChebyshevDistance(float2 p1, float2 p2) {
    return max(abs(p1.x - p2.x), abs(p1.y - p2.y));
}

float MinkowskiDistance(float2 p1, float2 p2, float p) {
    return pow(pow(abs(p1.x - p2.x), p) + pow(abs(p1.y - p2.y), p), 1.0 / p);
}

float HammingDistance(float2 p1, float2 p2) {
    float2 diff = abs(p1 - p2);
    return diff.x + diff.y;
}

float CosineDistance(float2 p1, float2 p2) {
    return 1.0 - dot(normalize(p1), normalize(p2));
}


// NOISE FUNCTIONS
//
float noise2x1(float2 p)
{
    float random = dot(p, float2(23.3,78.324));
    random = sin(random);
    random = random * 43758.5453;
    random = frac(random*63.71);
    return  random;
}

float2 noise2x2(float2 p)
{
    float x = dot(p, float2(123.4, 234.5));
    float y = dot(p, float2(234.5, 345.6));
    float2 noise = float2(x,y);
    noise = sin(noise);
    noise *= 43758.5453;
    noise = frac(noise);
    return  noise;
}

float whiteNoise3x3(float3 p)
{
    return frac(sin(dot(p, float3(12.9898, 78.233, 37.719)))* 43758.5453);
}

float2 noise2x2Time(float2 id, float time)
{
    float x = dot(id, float2(123.4, 234.5));
    float y = dot(id, float2(123.4, 234.5));
    float2 gradient = float2(x,y);
    gradient = sin(gradient);
    gradient *= 143758;
    gradient = sin(gradient + time);
    return gradient;
}

float valueNoise(float2 uv, float freq, float time, bool tiling)
{
    float2 scaledUv = uv * freq;
    float2 gridID = floor(scaledUv);
    //gridID += time;

    
    
    float2 gridUV = float2(frac(scaledUv.x), frac(scaledUv.y));
    gridUV = smoothstep(0,1,gridUV);

    float botLeft;
    float botRight;
    float topLeft;
    float topRight;
   
   
    // Get noise values at grid corners
    if(tiling)
    {
       botLeft = noise2x1((gridID+time)%freq);
       botRight = noise2x1((gridID+time+float2(1.0,0))%freq);
       topLeft = noise2x1((gridID+time+float2(0.0,1.0))%freq);
       topRight = noise2x1((gridID+time+float2(1.0,1.0))%freq);
    }
    else
    {
        
        botLeft = noise2x1(gridID+time);
        botRight = noise2x1(gridID+time+float2(1.0,0));
        topLeft = noise2x1(gridID+time+float2(0.0,1.0));
        topRight = noise2x1(gridID+time+float2(1.0,1.0));
    }

    float botLerp = lerp(botLeft, botRight, gridUV.x);
    float topLerp = lerp(topLeft, topRight, gridUV.x);
   
    return lerp(botLerp, topLerp, gridUV.y);
}

float valueNoise3D(float3 uvw, float freq, float time, bool tiling)
{
    float3 scaledUv = uvw * freq;
    float3 gridID = floor(scaledUv);
    //gridID += time;

    
    
    float3 gridUVW = float3(frac(scaledUv.x), frac(scaledUv.y), frac(scaledUv.z));
    gridUVW = smoothstep(0,1,gridUVW);

    float nearBotLeft;
    float nearBotRight;
    float nearTopLeft;
    float nearTopRight;
    float farBotLeft;
    float farBotRight;
    float farTopLeft;
    float farTopRight;
   
   
    // Get noise values at grid corners
    if(tiling)
    {
        nearBotLeft = whiteNoise3x3((gridID+time)%freq);
        nearBotRight = whiteNoise3x3((gridID+time+  float3(1.0, 0   ,0))%freq);
        nearTopLeft = whiteNoise3x3((gridID+time+   float3(0.0, 1.0 ,0))%freq);
        nearTopRight = whiteNoise3x3((gridID+time+  float3(1.0, 1.0 ,0))%freq);

        farBotLeft = whiteNoise3x3((gridID+time+   float3( 0,  0   ,1))%freq);
        farBotRight = whiteNoise3x3((gridID+time+  float3(1.0, 0   ,1))%freq);
        farTopLeft = whiteNoise3x3((gridID+time+   float3(0.0, 1.0 ,1))%freq);
        farTopRight = whiteNoise3x3((gridID+time+  float3(1.0, 1.0 ,1))%freq);
    }
    else
    {
        nearBotLeft = whiteNoise3x3(gridID+time);
        nearBotRight = whiteNoise3x3(gridID+time+  float3(1.0, 0   ,0));
        nearTopLeft = whiteNoise3x3(gridID+time+   float3(0.0, 1.0 ,0));
        nearTopRight = whiteNoise3x3(gridID+time+  float3(1.0, 1.0 ,0));

        farBotLeft = whiteNoise3x3(gridID+time+   float3( 0,  0   ,1));
        farBotRight = whiteNoise3x3(gridID+time+  float3(1.0, 0   ,1));
        farTopLeft = whiteNoise3x3(gridID+time+   float3(0.0, 1.0 ,1));
        farTopRight = whiteNoise3x3(gridID+time+  float3(1.0, 1.0 ,1));
    }

    float nearBotLerp = lerp(nearBotLeft, nearBotRight, gridUVW.x);
    float nearTopLerp = lerp(nearTopLeft, nearTopRight, gridUVW.x);   
    float nearBtmTopLerp = lerp(nearBotLerp, nearTopLerp, gridUVW.y);

    float farBotLerp = lerp(farBotLeft, farBotRight, gridUVW.x);
    float farTopLerp = lerp(farTopLeft, farTopRight, gridUVW.x);   
    float farBtmTopLerp = lerp(farBotLerp, farTopLerp, gridUVW.y);

    return lerp(nearBtmTopLerp, farBtmTopLerp, gridUVW.z);
}

// Also Voronoi noise. Currently offsetting in the z to get better depth
// Change to 2D for more traditional worley
float worleyNoise(float2 uv, float freq, float time, bool tiling)
{
    float2 scaledUv = uv * freq;
    float2 gridID = floor(scaledUv);
    float2 centeredGridUV = frac(scaledUv) - 0.5;

    float minDistFromPixel = 1000;
    
    for(int i = -1.0; i <= 1.0; i++)
    {
        for(int j = -1.0; j <= 1.0; j++)
        {
            float2 adjGridCoord = float2(i,j);            

            // Get noise sample in adjacent grid // DEBUG - Stable
            float2 adjGridID = gridID + adjGridCoord + 10;
            if(tiling)
            {
                adjGridID.x %= freq;
                adjGridID.y %= freq;
            }
            float2 noise = noise2x2(adjGridID);

            // Add sin of noise ot both components (modulates X and Y kind of like the ABC logo)
            float2 pointOnAdjGrid = adjGridCoord + sin(time * noise) * .5;
            float3 point3D = float3(pointOnAdjGrid, sin(time * noise.x) * 1);
            float3 centeredGrid3D = float3(centeredGridUV, 0);

            // Get min dist to point
            //float dist = length(centeredGridUV - pointOnAdjGrid);
            float dist = length(point3D - centeredGrid3D);
            //float dist = ManhattanDistance(centeredGridUV, pointOnAdjGrid);
            //float dist = ChebyshevDistance(centeredGridUV, pointOnAdjGrid);
            //float dist = MinkowskiDistance(centeredGridUV, pointOnAdjGrid, .5f);
            //float dist = HammingDistance(centeredGridUV, pointOnAdjGrid);
            minDistFromPixel = min(dist, minDistFromPixel);
        }
    }

    return  minDistFromPixel;
}

float perlinNoise(float2 uv, float freq, float time, bool tiling)
{
    float2 scaledUv = uv * freq;
    float2 gridID = floor(scaledUv);
    float2 gridUV = frac(scaledUv);

    // Corners
    float2 bl = gridID + float2(0,0);
    float2 br = gridID + float2(1,0);
    float2 tl = gridID + float2(0,1);
    float2 tr = gridID + float2(1,1);

    // Corners
    float2 blGrad;
    float2 brGrad;
    float2 tlGrad;
    float2 trGrad;
    
    if(tiling)
    {
        blGrad = noise2x2Time((bl)%freq, time);
        brGrad = noise2x2Time((br)%freq, time);
        tlGrad = noise2x2Time((tl)%freq, time);
        trGrad = noise2x2Time((tr)%freq, time);
    }
    else
    {
        blGrad = noise2x2Time(bl, time);
        brGrad = noise2x2Time(br, time);
        tlGrad = noise2x2Time(tl, time);
        trGrad = noise2x2Time(tr, time);        
    }

    // Vec to corners
    float2 distFromBL = gridUV - float2(0, 0);
    float2 distFromBR = gridUV - float2(1, 0);
    float2 distFromTL = gridUV - float2(0, 1);
    float2 distFromTR = gridUV - float2(1, 1);

    // Dot gradient to corner vec
    float2 dotBL = dot(blGrad, distFromBL);
    float2 dotBR = dot(brGrad, distFromBR);
    float2 dotTL = dot(tlGrad, distFromTL);
    float2 dotTR = dot(trGrad, distFromTR);


    float gridU = smoothstep(0,1,gridUV.x);
    float gridV = smoothstep(0,1,gridUV.y);
    
    float lerpBtm = lerp(dotBL, dotBR, gridU);
    float lerpTop = lerp(dotTL, dotTR, gridU);
    float perlin = lerp(lerpBtm, lerpTop, gridV);

    return  perlin;
}

// UV HELPERS
//
float2 pixIndexToGridUV(float freq, uint2 pixelIndex, uint res)
{
    float2 uv = float2((float)pixelIndex.x/res,
                      (float)pixelIndex.y/res);
    
    float2 scaledUv = uv * freq;
    return float2(frac(scaledUv.x), frac(scaledUv.y));
}


// SMOOTHING FUNCTIONS
//
float smoothstepCubic( float x )
{
    return x*x*(3.0-2.0*x);
}

float smoothstepQuatric( float x )
{
    return x*x*(2.0-x*x);
}

float smoothstepQuintic( float x )
{
    return x*x*x*(x*(x*6.0-15.0)+10.0);
}