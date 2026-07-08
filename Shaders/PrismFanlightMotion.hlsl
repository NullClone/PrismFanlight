#ifndef PRISM_FANLIGHT_MOTION_INCLUDED
#define PRISM_FANLIGHT_MOTION_INCLUDED

#include "PrismFanlightPose.hlsl"

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

PrismArm PrismComputeArm(FanlightSeatData seat, PrismHumanPose pose)
{
    float seed = seat.localPositionSeed.w;
    float3 shoulderLocal = PrismGetArmBase(pose);
    PrismCrowdRhythm rhythm = PrismComputeCrowdRhythm(seat);

    int noiseOctaves = clamp((int)round(_MotionNoise.z), 1, 4);
    float noisePersistence = max(0.001, _MotionNoise.w);

    bool isHorizontal = Hash11(seed + 131.0) < saturate(_SwingWrist.x);

    float angle = cos(rhythm.armPhase);
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
        wristAngle = cos(rhythm.wristPhase * max(1.0, _SwingWrist.y)) * _SwingWrist.z;
    }
    else
    {
        wristAngle = sin(rhythm.wristPhase) * _MotionShape.y * 0.5 * angleAmplitude;
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
    float motionScale = enthusiasm * restFactor * smallMotionFactor * (1.0 + rhythm.downbeatPulse * _MotionBeat.w);
    armAngle *= motionScale;
    wristAngle = clamp(wristAngle * motionScale, -1.4, 1.4);

    float armLengthLimit = _AudienceArm.w;
    float armReach = min(armLength * max(0.0, enthusiasm), armLengthLimit);
    float4x4 m1 = Translate(shoulderLocal);
    float4x4 mArm = AxisAngle(axis, armAngle);
    float4x4 m3 = Translate(float3(0.0, armReach, 0.0));
    float4x4 mWrist = AxisAngle(axis, wristAngle);
    float4x4 mGrip = Translate(float3(0.0, -_GripPivotY, 0.0));

    PrismArm result = (PrismArm)0;
    result.worldMatrix = mul(_LocalToWorld, mul(m1, mul(mArm, mul(m3, mul(mWrist, mGrip)))));
    result.handLocal = shoulderLocal + mul((float3x3)mArm, float3(0.0, armReach, 0.0));
    result.shoulderLocal = shoulderLocal;
    return result;
}

float4x4 PrismComputeMatrix(FanlightSeatData seat)
{
    PrismHumanPose pose = PrismComputeHumanPose(seat);
    return PrismComputeArm(seat, pose).worldMatrix;
}

#endif
