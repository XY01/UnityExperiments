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
    float y = dot(p, float2(345.6, 456.7));
    float2 noise = float2(x,y);
    noise = sin(noise);
    noise *= 43758.5453;
    noise = frac(noise);
    return  noise;
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