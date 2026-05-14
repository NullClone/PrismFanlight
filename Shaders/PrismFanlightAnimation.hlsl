float4x4 PrismComputeMatrix(FanlightSeatData seat)
{
    float seed = seat.localPositionSeed.w;
    float3 localPosition = seat.localPositionSeed.xyz;

    float phase = 2.0 * PRISM_FANLIGHT_PI * _MotionTiming.x * _FanlightTime;
    phase += Hash11(seed + 11.0) * 2.0 * PRISM_FANLIGHT_PI * _MotionTiming.y;
    phase += Noise21(float2(Hash11(seed + 23.0) * 2000.0 - 1000.0, _FanlightTime * _MotionTiming.w)) * _MotionTiming.z;

    float2 jitter = float2(Hash11(seed + 31.0), Hash11(seed + 37.0)) * 2.0 - 1.0;
    localPosition.xz += jitter * _MotionVariation.x * _SeatPitch.xy;
    localPosition.y += (Hash11(seed + 41.0) * 2.0 - 1.0) * _MotionVariation.y;

    float angle = cos(phase);
    float snappedAngle = smoothstep(-1.0, 1.0, angle) * 2.0 - 1.0;
    angle = lerp(angle, snappedAngle, _MotionSwing.w * Hash11(seed + 43.0));
    angle *= lerp(_MotionSwing.y, _MotionSwing.z, Hash11(seed + 47.0));

    float axisNoise = Noise21(float2(Hash11(seed + 53.0) * 2000.0 - 1000.0, _FanlightTime * _MotionNoise.y + 100.0));
    float3 axis = normalize(float3(axisNoise * _MotionNoise.x, 0.0, 1.0));
    float armJitter = 1.0 + (Hash11(seed + 59.0) * 2.0 - 1.0) * _MotionVariation.z;

    float4x4 m1 = Translate(localPosition);
    float4x4 m2 = AxisAngle(axis, angle);
    float4x4 m3 = Translate(float3(0.0, _MotionSwing.x * armJitter, 0.0));
    return mul(_LocalToWorld, mul(m1, mul(m2, m3)));
}

float4 PrismComputeColor(FanlightSeatData seat)
{
    float seed = seat.localPositionSeed.w;
    float2 pos = seat.planePositionBlock.xy;
    float2 block = seat.planePositionBlock.zw;

    float randomIntensityFactor = lerp(1.0, lerp(0.65, 1.35, Hash11(seed + 101.0)), _Brightness.z);
    float brightness = _Brightness.x;
    float3 rgb = _PrimaryColor.rgb;

    if (_ColorMode == 0)
    {
        brightness += _Brightness.y;
    }
    else if (_ColorMode == 1)
    {
        rgb = HsvToRgb(frac(Hash11(seed + 107.0) * _Hue.y + _FanlightTime * _Hue.x), _Brightness.w, 1.0);
        brightness += _Brightness.y;
    }
    else if (_ColorMode == 2)
    {
        rgb = HsvToRgb(frac(pos.x * 0.035 + pos.y * 0.02 + _FanlightTime * _Hue.x + Hash11(seed + 109.0) * _Hue.y), _Brightness.w, 1.0);
        brightness += _Brightness.y;
    }
    else if (_ColorMode == 3 || _ColorMode == 4)
    {
        float distanceValue = _ColorMode == 4 ? distance(pos, _Wave.xy) : pos.y - _Wave.y;
        float wave = sin(distanceValue * _Wave.z - _FanlightTime * _Wave.w) * 0.5 + 0.5;
        rgb = HsvToRgb(frac(Hash11(seed + 113.0) * _Hue.y + _FanlightTime * _Hue.x), _Brightness.w, 1.0);
        brightness += pow(wave, max(0.001, _WaveShape.x)) * _Brightness.y;
    }
    else if (_ColorMode == 5)
    {
        float denom = max(_BlockCount.x - 1.0, 1.0);
        rgb = lerp(_PrimaryColor.rgb, _SecondaryColor.rgb, block.x / denom);
        brightness += _Brightness.y;
    }

    return float4(rgb * brightness * randomIntensityFactor, _PrimaryColor.a);
}
