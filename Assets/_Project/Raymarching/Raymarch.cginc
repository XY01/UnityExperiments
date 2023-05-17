// Calculate the dot product between two vectors
float Test(float3 a, float3 b)
{
    return a + b;
}

// returns distance to box and distance inside box. if ray misses dist insde box will be zero
float2 RayBoxDst(float3 boundsMin, float3 boundsMax, float3 rayOrigin, float3 rayDirection)
{
	float3 t0 = (boundsMin - rayOrigin) / rayDirection;
	float3 t1 = (boundsMax - rayOrigin) / rayDirection;
	float3 tmin = min(t0, t1);
	float3 tmax = max(t0, t1);

	float dstA = max(max(tmin.x, tmin.y), tmin.z);
	float dstB = min(min(tmax.x, tmax.y), tmax.z);

	float distToBox = max(0, dstA);
	float distInsideBox = max(0, dstB - distToBox);
	return float2(distToBox, distInsideBox);
}

float3 GetNormal(float3 samplePos, float3 rayDirection, sampler3D volumeTex, float currentDensity)
{
	float3 up = normalize(cross(float3(0,1,0), rayDirection));
	float3 right = cross(rayDirection, up);
	

	float3 gradient = float3( 
		currentDensity - tex3D(volumeTex,samplePos + float3(0.001, 0.0, 0.0)).r,
		currentDensity - tex3D(volumeTex,samplePos + float3(0.0, 0.001, 0.0)).r,
		currentDensity - tex3D(volumeTex,samplePos + float3(0.0, 0.0, 0.001)).r
		); 

	return normalize(gradient);
}

float3 GetGradient(float3 samplePos, sampler3D volumeTex, float offsetStep, float currentVal)
{		
	float3 gradient = float3( 
		tex3D(volumeTex,samplePos + float3(offsetStep, 0.0, 0.0)).r - currentVal,
		tex3D(volumeTex,samplePos + float3(0.0, offsetStep, 0.0)).r - currentVal,
		tex3D(volumeTex,samplePos + float3(0.0, 0.0, offsetStep)).r - currentVal
		);
	return gradient;// currentVal - tex3D(volumeTex, samplePos + float3(offsetStep, 0, 0)).r;

}


// cast ray in world space, increment length, test world pos against depth, 
// normalize world space for sample using bounds
float3 RaymarchVolumeTex(float3 rayOrigin, float3 rayDirection, int numSteps, float stepSize, sampler3D volumeTex, float densityScale, float maxDist, float3 boundsMin, float3 boundsSize )
{
	// RAY
	//
	float density = 0;
	float3 worldPos = rayOrigin;
	float dist = 0;
	int stepCount = 0;
	int maxSteps = 100;
	//stepSize = maxDist / maxSteps;


	// LIGHTING
	//
	float lightVal = 0;
	float3 lightDir = float3(1,0,0);
	float3 normal = float3(0,0,0);		
	int lightSampleCount = 0;



	while(stepCount < maxSteps)// && dist < maxDist)
	{
		stepCount++;
		dist += stepSize;
		worldPos += rayDirection * stepSize;
	
		//if(dist < maxDist)
		{
			float3 samplePos = (worldPos-boundsMin) / boundsSize;
			samplePos = saturate(samplePos);		
			float sampledDensity = tex3D(volumeTex, samplePos ).r;
			float inverseDensityScalar =1;// saturate(1 - (density*2));
			density += sampledDensity * densityScale * inverseDensityScalar;

			if(sampledDensity > 0)// && lightSampleCount < 10)
			{
				float3 gradient = GetGradient(samplePos, volumeTex, .02, sampledDensity);
				//float lightDot = dot(float3(0,-1,0),normal);
				//lightVal += lightDot;// * (1 - density);

				lightVal += saturate(gradient.r) * saturate(1 - (density*10)) * .1f;

				//lightVal += saturate(GetGradient(samplePos, volumeTex, .02, sampledDensity ).r * (1 - density));
				//lightSampleCount++;
			}
			//density += sampledDensity+gradient*densityScale;

			//if(sampledDensity > 0)
			//{
			//	if(length(normal) == 0)
			//	{
			//		normal = float3(0,1,0);// GetNormal(samplePos, rayDirection, volumeTex, sampledDensity);
			//		float sampledDensityOffsetY = tex3D(volumeTex, samplePos + float3(0,.01,0)).r;
			//		lightVal = sampledDensityOffsetY - sampledDensity;//  max(dot(normal, lightDir),0);
			//	}
			//}
		}
	}


	density = saturate(density);
	density = exp(-density);

	lightVal = saturate(lightVal);
	//lightVal = exp(lightVal);

	return float3(density, lightVal, 0); 
}


//float fireNoise(vec3 position) 
//{   
//	float flameNoise = snoise(position * 2.0);
//	float turbulence = snoise(position * 10.0) * 0.5 + 0.5;
//	float temperature = snoise(position * 5.0) * 0.5 + 0.5;
//	return flameNoise * turbulence * temperature; 
//}