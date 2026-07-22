#ifndef PRISM_FANLIGHT_MOTION_INCLUDED
#define PRISM_FANLIGHT_MOTION_INCLUDED

#include "PrismFanlightPose.hlsl"

float PrismComputeRestFactor(FanlightSeatData seat)
{
    if (PrismRandom(seat, 12u) >= _MotionRest.x)
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
    float phaseOffset = PrismRandom(seat, 13u) * cycleDuration * saturate(_MotionRestTiming.w);
    float cycleTime = frac((_FanlightTime + phaseOffset) / cycleDuration) * cycleDuration;

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

float PrismShapeProgress(float progress)
{
    float p = saturate(progress);
    float smooth = p * p * (3.0 - 2.0 * p);
    float gain = lerp(1.0, 3.0, saturate(_GestureShape.x));
    return saturate((smooth - 0.5) * gain + 0.5);
}

float PrismCycleProgress(float cyclePhase)
{
    float strokeDuration = saturate(_GestureTiming.z);
    float nonStrokeDuration = max(0.0, 1.0 - strokeDuration);
    float holdDuration = nonStrokeDuration * saturate(_GestureTiming.w);
    float recoveryDuration = max(0.0, nonStrokeDuration - holdDuration);
    float phase = frac(cyclePhase);

    if (holdDuration > 0.000001 && phase < holdDuration)
    {
        return 1.0;
    }

    if (recoveryDuration > 0.000001 && phase < holdDuration + recoveryDuration)
    {
        return 1.0 - PrismShapeProgress((phase - holdDuration) / recoveryDuration);
    }

    if (strokeDuration > 0.000001)
    {
        return PrismShapeProgress((phase - holdDuration - recoveryDuration) / strokeDuration);
    }

    return 1.0;
}

float PrismFollowThroughWeight(float cyclePhase)
{
    float nonStrokeDuration = max(0.0, 1.0 - saturate(_GestureTiming.z));
    float holdDuration = nonStrokeDuration * saturate(_GestureTiming.w);
    float phase = frac(cyclePhase);
    if (holdDuration <= 0.000001 || phase >= holdDuration) return 0.0;
    return sin(PRISM_FANLIGHT_PI * saturate(phase / holdDuration));
}

float PrismComputeMotionScale(FanlightSeatData seat, PrismCrowdRhythm rhythm)
{
    float energyResponse = lerp(1.0, lerp(0.65, 1.35, PrismRandom(seat, 25u)), _MotionHuman.y);
    float enthusiasm = max(0.0, _MotionHuman.x) * energyResponse;
    float restFactor = PrismComputeRestFactor(seat);
    float participationScale = PrismRandom(seat, 26u) < _MotionRest.z ? 0.35 : 1.0;
    float reachVariation = max(0.0, 1.0 + (PrismRandom(seat, 24u) * 2.0 - 1.0) * _MotionVariation.z);
    float downbeatScale = 1.0 + rhythm.downbeatPulse * max(0.0, _GestureShape.w);
    return max(0.0, enthusiasm * restFactor * participationScale * saturate(_MotionTiming.x) * reachVariation * downbeatScale);
}

float3 PrismComputeHandNoise(FanlightSeatData seat, PrismAudienceBasis basis, float motionScale)
{
    int noiseOctaves = clamp((int)round(_MotionNoise.z), 1, 4);
    float noisePersistence = max(0.001, _MotionNoise.w);
    float noiseSide = FbmNoise21(
        float2(PrismRandom(seat, 21u) * 2000.0 - 1000.0, _FanlightTime * _MotionNoise.y),
        noiseOctaves,
        noisePersistence);
    float noiseUp = FbmNoise21(
        float2(PrismRandom(seat, 22u) * 2000.0 - 1000.0, _FanlightTime * _MotionNoise.y + 317.5),
        noiseOctaves,
        noisePersistence);
    return (basis.sideLocal * noiseSide + basis.upLocal * noiseUp) * _MotionNoise.x * 0.05 * motionScale;
}

float3 PrismClampHandToArmLimit(float3 shoulderLocal, float3 handLocal)
{
    float armLengthLimit = max(0.0001, _AudienceArm.w);
    float3 shoulderToHand = handLocal - shoulderLocal;
    float distance = length(shoulderToHand);
    return distance > armLengthLimit
        ? shoulderLocal + shoulderToHand * (armLengthLimit / max(distance, 0.0001))
        : handLocal;
}

float3 PrismComputePenlightDirection(
    FanlightSeatData seat,
    PrismAudienceBasis basis,
    float wristProgress)
{
    float3 localDirection = PrismInterpolateDirection(
        _PoseReadyDirection.xyz,
        _PoseAccentDirection.xyz,
        wristProgress);
    float3 direction = SafeNormalize(
        PrismTransformAudienceDirectionWorld(basis, localDirection),
        basis.upWorld);
    float3 fallback = direction;
    float spread = max(0.0, _SwingAxis.w) + max(0.0, _MotionVariation.w) * 0.5;
    float spreadAngle = (PrismRandom(seat, 18u) * 2.0 - 1.0) * spread * PRISM_FANLIGHT_PI * 0.25;
    float3 tangent = basis.sideWorld - direction * dot(basis.sideWorld, direction);
    tangent = SafeNormalize(tangent, SafePerp(direction));
    direction = SafeNormalize(direction * cos(spreadAngle) + tangent * sin(spreadAngle), fallback);

    int noiseOctaves = clamp((int)round(_MotionNoise.z), 1, 4);
    float noisePersistence = max(0.001, _MotionNoise.w);
    float directionNoise = FbmNoise21(
        float2(PrismRandom(seat, 19u) * 2000.0 - 1000.0, _FanlightTime * _MotionNoise.y + 631.0),
        noiseOctaves,
        noisePersistence);
    return SafeNormalize(direction + tangent * directionNoise * _MotionNoise.x * 0.1, fallback);
}

float4x4 PrismPenlightRotation(float3 directionWorld, PrismAudienceBasis basis)
{
    float3 yLocal = SafeNormalize(PrismWorldVectorToLocal(directionWorld), basis.upLocal);
    float3 xLocal = basis.sideLocal - yLocal * dot(basis.sideLocal, yLocal);
    xLocal = SafeNormalize(xLocal, SafePerp(yLocal));
    float3 zLocal = SafeNormalize(cross(xLocal, yLocal), basis.forwardLocal);
    xLocal = SafeNormalize(cross(yLocal, zLocal), xLocal);

    return float4x4(
        xLocal.x, yLocal.x, zLocal.x, 0.0,
        xLocal.y, yLocal.y, zLocal.y, 0.0,
        xLocal.z, yLocal.z, zLocal.z, 0.0,
        0.0, 0.0, 0.0, 1.0);
}

PrismArm PrismComputeArm(FanlightSeatData seat, PrismHumanPose pose, float gripPivotY)
{
    PrismCrowdRhythm rhythm = PrismComputeCrowdRhythm(seat);
    PrismAudienceBasis basis = PrismComputeAudienceBasis(seat);
    float handProgress = PrismCycleProgress(rhythm.cyclePhase);
    float wristPhase = frac(rhythm.cyclePhase - saturate(_GestureShape.z));
    float wristProgress = PrismCycleProgress(wristPhase);
    float motionScale = PrismComputeMotionScale(seat, rhythm);
    float3 positionSpread = PrismTransformAudienceOffset(
        basis,
        float3(
            PrismRandom(seat, 29u) * 2.0 - 1.0,
            PrismRandom(seat, 30u) * 2.0 - 1.0,
            PrismRandom(seat, 31u) * 2.0 - 1.0)) * max(0.0, _HandPositionSpread);
    float3 bodyLeanOffset = basis.forwardLocal * _PoseHandArc.w * 0.15 * motionScale;
    float3 readyHand = pose.shoulderLocal
        + PrismTransformAudienceOffset(basis, _PoseReadyHand.xyz)
        + positionSpread
        + bodyLeanOffset;
    float3 accentTarget = pose.shoulderLocal
        + PrismTransformAudienceOffset(basis, _PoseAccentHand.xyz)
        + positionSpread
        + bodyLeanOffset;
    float3 accentHand = readyHand + (accentTarget - readyHand) * motionScale;
    float3 arcOffset = PrismTransformAudienceOffset(basis, _PoseHandArc.xyz) * motionScale;
    float3 handLocal = lerp(readyHand, accentHand, handProgress)
        + arcOffset * 4.0 * handProgress * (1.0 - handProgress);
    handLocal += (accentHand - readyHand)
        * saturate(_GestureShape.y)
        * PrismFollowThroughWeight(rhythm.cyclePhase);
    handLocal += PrismComputeHandNoise(seat, basis, motionScale);
    handLocal = PrismClampHandToArmLimit(pose.shoulderLocal, handLocal);

    float3 penlightDirection = PrismComputePenlightDirection(seat, basis, wristProgress);
    float4x4 handTranslation = Translate(handLocal);
    float4x4 penlightRotation = PrismPenlightRotation(penlightDirection, basis);
    float4x4 gripTranslation = Translate(float3(0.0, -gripPivotY, 0.0));

    PrismArm result = (PrismArm)0;
    result.worldMatrix = mul(_LocalToWorld, mul(handTranslation, mul(penlightRotation, gripTranslation)));
    result.handLocal = handLocal;
    result.shoulderLocal = pose.shoulderLocal;
    return result;
}

float4x4 PrismComputeMatrix(FanlightSeatData seat, float gripPivotY)
{
    PrismHumanPose pose = PrismComputeHumanPose(seat);
    return PrismComputeArm(seat, pose, gripPivotY).worldMatrix;
}

#endif
