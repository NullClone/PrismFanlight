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

float3 PrismWorldVectorToLocal(float3 vectorWS)
{
    return mul((float3x3)_WorldToLocal, vectorWS);
}

float3 PrismComputeWorldDirection(FanlightSeatData seat)
{
    float3 worldDirection = SafeNormalize(_SwingAxis.xyz, float3(0.0, 0.0, 1.0));
    float aimStrength = saturate(_SwingTargetPos.w);

    if (_SwingMode == 1 && aimStrength > 0.001)
    {
        float3 seatWorldPos = mul(_LocalToWorld, float4(seat.localPositionSeed.xyz, 1.0)).xyz;
        float3 targetDirection = _SwingTargetPos.xyz - seatWorldPos;
        targetDirection.y = 0.0;
        targetDirection = SafeNormalize(targetDirection, worldDirection);
        worldDirection = SafeNormalize(lerp(worldDirection, targetDirection, aimStrength), worldDirection);
    }

    return worldDirection;
}

float3 PrismComputeBaseAxis(FanlightSeatData seat, bool horizontal)
{
    float3 worldDirection = PrismComputeWorldDirection(seat);
    float3 worldAxis = horizontal
        ? worldDirection
        : SafeNormalize(cross(float3(0.0, 1.0, 0.0), worldDirection), float3(1.0, 0.0, 0.0));
        
    return SafeNormalize(PrismWorldVectorToLocal(worldAxis), float3(1.0, 0.0, 0.0));
}

struct PrismArm
{
    float4x4 worldMatrix;
    float3 handLocal;
    float3 baseLocal;
};

PrismArm PrismComputeArm(FanlightSeatData seat)
{
    float seed = seat.localPositionSeed.w;
    float3 localPosition = seat.localPositionSeed.xyz;

    int noiseOctaves = clamp((int)round(_MotionNoise.z), 1, 4);
    float noisePersistence = max(0.001, _MotionNoise.w);

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
    phase += FbmNoise21(float2(Hash11(seed + 23.0) * 2000.0 - 1000.0, _FanlightTime * _MotionTiming.w), noiseOctaves, noisePersistence) * _MotionTiming.z;

    float2 jitter = float2(Hash11(seed + 31.0), Hash11(seed + 37.0)) * 2.0 - 1.0;
    localPosition.xz += jitter * _MotionVariation.x * _SeatPitch.xy;
    localPosition.y += (Hash11(seed + 41.0) * 2.0 - 1.0) * _MotionVariation.y;
    localPosition.y += _HandBaseHeight;

    bool isHorizontal = Hash11(seed + 131.0) < saturate(_SwingWrist.x);

    float angle = cos(phase);
    float snappedAngle = smoothstep(-1.0, 1.0, angle) * 2.0 - 1.0;
    angle = lerp(angle, snappedAngle, _MotionShape.w * Hash11(seed + 43.0));
    float heldAngle = sign(angle) * pow(abs(angle), lerp(1.0, 0.25, _MotionShape.x));
    angle = lerp(angle, heldAngle, _MotionShape.x);
    angle = clamp(angle + _MotionShape.z * 0.35, -1.0, 1.0);
    float angleAmplitude = lerp(_MotionSwing.z, _MotionSwing.w, Hash11(seed + 47.0));
    float angleNoiseVal = FbmNoise21(float2(Hash11(seed + 103.0) * 2000.0 - 1000.0, _FanlightTime * _MotionNoise.y), noiseOctaves, noisePersistence);
    angleAmplitude = max(0.0, angleAmplitude * (1.0 + angleNoiseVal * _MotionVariation.w));
    float armAngle = angle * angleAmplitude;

    float wristAngle;
    if (isHorizontal)
    {
        wristAngle = cos(phase * max(1.0, _SwingWrist.y)) * _SwingWrist.z;
    }
    else
    {
        wristAngle = sin(phase) * _MotionShape.y * 0.5 * angleAmplitude;
    }
    
    float3 baseAxis = PrismComputeBaseAxis(seat, isHorizontal);
    float3 perpU = SafePerp(baseAxis);
    float3 perpV = cross(baseAxis, perpU);

    float spreadDx = Hash11(seed + 53.0) * 2.0 - 1.0;
    float spreadDy = Hash11(seed + 55.0) * 2.0 - 1.0;
    float spreadLen = length(float2(spreadDx, spreadDy));
    float2 spreadDir = spreadLen > 0.001 ? float2(spreadDx, spreadDy) / spreadLen : float2(1.0, 0.0);
    float spreadAngle = Hash11(seed + 57.0) * _SwingAxis.w * PRISM_FANLIGHT_PI * 0.5;
    float cosSpread = cos(spreadAngle);
    float sinSpread = sin(spreadAngle);
    float3 axis = SafeNormalize(
        baseAxis * cosSpread + (perpU * spreadDir.x + perpV * spreadDir.y) * sinSpread,
        baseAxis);

    float noiseU = FbmNoise21(float2(Hash11(seed + 89.0) * 2000.0 - 1000.0, _FanlightTime * _MotionNoise.y), noiseOctaves, noisePersistence);
    float noiseV = FbmNoise21(float2(Hash11(seed + 97.0) * 2000.0 - 1000.0, _FanlightTime * _MotionNoise.y + 317.5), noiseOctaves, noisePersistence);
    float3 ap1 = SafePerp(axis);
    float3 ap2 = cross(axis, ap1);
    axis = SafeNormalize(axis + (ap1 * noiseU + ap2 * noiseV) * _MotionNoise.x, axis);
    
    float armLength = lerp(_MotionSwing.x, _MotionSwing.y, Hash11(seed + 105.0));
    float armJitter = 1.0 + (Hash11(seed + 59.0) * 2.0 - 1.0) * _MotionVariation.z;
    armLength = max(0.0, armLength * armJitter);

    float enthusiasm = _MotionHuman.x * lerp(1.0, lerp(0.65, 1.35, Hash11(seed + 61.0)), _MotionHuman.y);
    float restFactor = PrismComputeRestFactor(seed);
    float smallMotionFactor = Hash11(seed + 71.0) < _MotionRest.z ? 0.35 : 1.0;
    float downbeatPulse = pow(1.0 - saturate(_FanlightBeat.w), 8.0) * saturate(_FanlightTempo.x);
    float motionScale = enthusiasm * restFactor * smallMotionFactor * (1.0 + downbeatPulse * _MotionBeat.w);
    armAngle *= motionScale;
    wristAngle = clamp(wristAngle * motionScale, -1.4, 1.4);

    float armReach = armLength * max(0.0, enthusiasm);
    float4x4 m1 = Translate(localPosition);
    float4x4 mArm = AxisAngle(axis, armAngle);
    float4x4 m3 = Translate(float3(0.0, armReach, 0.0));
    float4x4 mWrist = AxisAngle(axis, wristAngle);
    float4x4 mGrip = Translate(float3(0.0, -_GripPivotY, 0.0));

    PrismArm result;
    result.worldMatrix = mul(_LocalToWorld, mul(m1, mul(mArm, mul(m3, mul(mWrist, mGrip)))));
    result.handLocal = localPosition + mul((float3x3)mArm, float3(0.0, armReach, 0.0));
    result.baseLocal = localPosition;
    return result;
}

float4x4 PrismComputeMatrix(FanlightSeatData seat)
{
    return PrismComputeArm(seat).worldMatrix;
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
