float PrismComputeRestFactor(float seed)
{
    if (Hash11(seed + 67.0) >= _MotionRest.x)
    {
        return 1.0;
    }

    float cycleDuration = max(0.0, _MotionRestTiming.x);
    float restDuration = max(0.0, _MotionRestTiming.y);

    if (cycleDuration <= 0.0001 || restDuration <= 0.0001)
    {
        return _MotionRest.y;
    }

    restDuration = min(restDuration, cycleDuration);
    float phaseOffset = Hash11(seed + 83.0) * cycleDuration * saturate(_MotionRestTiming.w);
    float cycleTime = fmod(max(0.0, _FanlightTime + phaseOffset), cycleDuration);

    if (cycleTime >= restDuration)
    {
        return 1.0;
    }

    float fade = min(max(0.0, _MotionRestTiming.z), restDuration * 0.5);
    float restWeight = fade > 0.0001
        ? saturate(cycleTime / fade) * saturate((restDuration - cycleTime) / fade)
        : 1.0;
    return lerp(1.0, _MotionRest.y, restWeight);
}

float4x4 PrismComputeMatrix(FanlightSeatData seat)
{
    float seed = seat.localPositionSeed.w;
    float3 localPosition = seat.localPositionSeed.xyz;

    float reactionDelay = Hash11(seed + 17.0) * _MotionHuman.z;
    float tempoDrift = (Hash11(seed + 19.0) * 2.0 - 1.0) * _MotionHuman.w;
    float phaseTime = max(0.0, _FanlightTime - reactionDelay);
    float legacyPhase = 2.0 * PRISM_FANLIGHT_PI * max(0.0, _MotionTiming.x + tempoDrift) * phaseTime;
    float beatSync = saturate(_MotionBeat.x) * saturate(_FanlightTempo.x);
    float beatReaction = reactionDelay * max(1.0, _FanlightTempo.y) / 60.0;
    float randomBeatReaction = Hash11(seed + 73.0) * _MotionBeatSpread.x;
    float seatBeatJitter = (Hash11(seed + 79.0) * 2.0 - 1.0) * _MotionBeatSpread.y;
    float2 block01 = float2(
        _BlockCount.x > 1.0 ? seat.planePositionBlock.z / max(1.0, _BlockCount.x - 1.0) : 0.5,
        _BlockCount.y > 1.0 ? seat.planePositionBlock.w / max(1.0, _BlockCount.y - 1.0) : 0.5);
    float blockBeatDelay = dot(block01 - 0.5, _MotionBeatSpread.zw);
    float delayedBeat = max(0.0, _FanlightBeat.y - beatReaction - randomBeatReaction - seatBeatJitter - blockBeatDelay);
    float beatPhase = 2.0 * PRISM_FANLIGHT_PI * ((delayedBeat / max(0.001, _MotionBeat.y)) + _MotionBeat.z);
    float phase = lerp(legacyPhase, beatPhase, beatSync);
    phase += Hash11(seed + 11.0) * 2.0 * PRISM_FANLIGHT_PI * _MotionTiming.y;
    phase += Noise21(float2(Hash11(seed + 23.0) * 2000.0 - 1000.0, _FanlightTime * _MotionTiming.w)) * _MotionTiming.z;

    float2 jitter = float2(Hash11(seed + 31.0), Hash11(seed + 37.0)) * 2.0 - 1.0;
    localPosition.xz += jitter * _MotionVariation.x * _SeatPitch.xy;
    localPosition.y += (Hash11(seed + 41.0) * 2.0 - 1.0) * _MotionVariation.y;

    float angle = cos(phase);
    float snappedAngle = smoothstep(-1.0, 1.0, angle) * 2.0 - 1.0;
    angle = lerp(angle, snappedAngle, _MotionSwing.w * Hash11(seed + 43.0));
    float heldAngle = sign(angle) * pow(abs(angle), lerp(1.0, 0.25, _MotionShape.x));
    angle = lerp(angle, heldAngle, _MotionShape.x);
    float flickWave = sin(phase * 2.0) * (1.0 - abs(angle));
    angle = clamp(angle + flickWave * _MotionShape.y * 0.35, -1.0, 1.0);
    angle = clamp(angle + _MotionShape.z * 0.35, -1.0, 1.0);
    angle *= lerp(_MotionSwing.y, _MotionSwing.z, Hash11(seed + 47.0));

    float axisNoise = Noise21(float2(Hash11(seed + 53.0) * 2000.0 - 1000.0, _FanlightTime * _MotionNoise.y + 100.0));
    float3 baseAxis = SafeNormalize(_MotionDirection.xyz, float3(0.0, 0.0, 1.0));
    float3 expressiveAxis = SafeNormalize(float3(axisNoise * _MotionNoise.x, _MotionDirectionBlend.y, _MotionDirectionBlend.x), baseAxis);
    float3 axis = SafeNormalize(lerp(baseAxis, expressiveAxis, _MotionDirection.w), baseAxis);
    float armJitter = 1.0 + (Hash11(seed + 59.0) * 2.0 - 1.0) * _MotionVariation.z;

    float enthusiasm = _MotionHuman.x * lerp(1.0, lerp(0.65, 1.35, Hash11(seed + 61.0)), _MotionHuman.y);
    float restFactor = PrismComputeRestFactor(seed);
    float smallMotionFactor = Hash11(seed + 71.0) < _MotionRest.z ? 0.35 : 1.0;
    float downbeatPulse = pow(1.0 - saturate(_FanlightBeat.w), 8.0) * saturate(_FanlightTempo.x);
    float motionScale = enthusiasm * restFactor * smallMotionFactor * (1.0 + downbeatPulse * _MotionBeat.w);
    angle *= motionScale;

    float4x4 m1 = Translate(localPosition);
    float4x4 m2 = AxisAngle(axis, angle);
    float4x4 m3 = Translate(float3(0.0, _MotionSwing.x * armJitter * max(0.0, enthusiasm), 0.0));
    return mul(_LocalToWorld, mul(m1, mul(m2, m3)));
}

float4 PrismComputeColor(FanlightSeatData seat)
{
    float seed = seat.localPositionSeed.w;
    float2 block = seat.planePositionBlock.zw;

    float3 rgb = _PrimaryColor.rgb;

    if (_ColorMode == 0)
    {
    }
    else if (_ColorMode == 1)
    {
        int count = clamp(_PaletteColorCount, 1, 16);
        int paletteIndex = min((int)floor(Hash11(seed + 107.0) * count), count - 1);
        rgb = _PaletteColors[paletteIndex].rgb;
    }
    else if (_ColorMode == 2)
    {
        float denom = max(_BlockCount.x - 1.0, 1.0);
        rgb = lerp(_PrimaryColor.rgb, _SecondaryColor.rgb, block.x / denom);
    }

    float randomIntensityFactor = max(0.0, 1.0 + (Hash11(seed + 101.0) * 2.0 - 1.0) * _Brightness.y);
    return float4(rgb * randomIntensityFactor, _PrimaryColor.a);
}
