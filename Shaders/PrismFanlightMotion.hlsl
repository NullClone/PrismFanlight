#ifndef PRISM_FANLIGHT_MOTION_INCLUDED
#define PRISM_FANLIGHT_MOTION_INCLUDED

#include "PrismFanlightComputeContext.hlsl"

FanlightMotionSample PrismSampleMotion(float cyclePhase);

#include "PrismFanlightPose.hlsl"

#define PRISM_FANLIGHT_MOTION_SAMPLE_COUNT 64u

FanlightMotionSample PrismSampleMotionAsset(uint assetIndex, float cyclePhase)
{
    float samplePosition = frac(cyclePhase) * (float)PRISM_FANLIGHT_MOTION_SAMPLE_COUNT;
    uint sample0 = (uint)floor(samplePosition) % PRISM_FANLIGHT_MOTION_SAMPLE_COUNT;
    uint sample1 = (sample0 + 1u) % PRISM_FANLIGHT_MOTION_SAMPLE_COUNT;
    float weight = frac(samplePosition);
    uint baseIndex = assetIndex * PRISM_FANLIGHT_MOTION_SAMPLE_COUNT;
    FanlightMotionSample a = _MotionSamples[baseIndex + sample0];
    FanlightMotionSample b = _MotionSamples[baseIndex + sample1];
    FanlightMotionSample result;
    result.arm = lerp(a.arm, b.arm, weight);
    result.penlight = lerp(a.penlight, b.penlight, weight);
    return result;
}

FanlightMotionSample PrismSampleMotion(float cyclePhase)
{
    FanlightMotionSample a = PrismSampleMotionAsset(0u, cyclePhase);
    FanlightMotionSample b = PrismSampleMotionAsset(1u, cyclePhase);
    FanlightMotionSample c = PrismSampleMotionAsset(2u, cyclePhase);
    float3 weights = max(0.0, _MotionAssetWeights.xyz);
    weights /= max(0.000001, weights.x + weights.y + weights.z);
    FanlightMotionSample result;
    result.arm = a.arm * weights.x + b.arm * weights.y + c.arm * weights.z;
    result.penlight = a.penlight * weights.x + b.penlight * weights.y + c.penlight * weights.z;
    return result;
}

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

float PrismComputeMotionScale(FanlightSeatData seat)
{
    float energyResponse = lerp(1.0, lerp(0.65, 1.35, PrismRandom(seat, 25u)), _MotionHuman.y * _MotionCycle.w);
    float enthusiasm = max(0.0, _MotionHuman.x) * energyResponse;
    float restFactor = PrismComputeRestFactor(seat);
    float participationScale = PrismRandom(seat, 26u) < _MotionRest.z ? 0.35 : 1.0;
    return saturate(enthusiasm * restFactor * participationScale * saturate(_MotionTiming.x));
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
    return (basis.sideLocal * noiseSide + basis.upLocal * noiseUp)
        * _MotionNoise.x
        * _MotionCycle.w
        * 0.05
        * motionScale;
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

float3 PrismDirectionFromAngles(PrismAudienceBasis basis, float elevation, float side, bool worldSpace)
{
    float cosElevation = cos(elevation);
    float3 audienceDirection = float3(
        sin(side) * cosElevation,
        sin(elevation),
        cos(side) * cosElevation);
    float3 direction = worldSpace
        ? basis.sideWorld * audienceDirection.x + basis.upWorld * audienceDirection.y + basis.forwardWorld * audienceDirection.z
        : basis.sideLocal * audienceDirection.x + basis.upLocal * audienceDirection.y + basis.forwardLocal * audienceDirection.z;
    return SafeNormalize(direction, worldSpace ? basis.upWorld : basis.upLocal);
}

float3 PrismComputePenlightDirection(
    FanlightSeatData seat,
    PrismAudienceBasis basis,
    FanlightMotionSample wristSample,
    float motionScale)
{
    float variation = _MotionCycle.w;
    float elevationVariation = (PrismRandom(seat, 17u) * 2.0 - 1.0) * _MotionVariation.w * variation * 0.25;
    float sideVariation = (PrismRandom(seat, 18u) * 2.0 - 1.0) * _SwingAxis.w * variation * PRISM_FANLIGHT_PI * 0.25;
    float elevation = wristSample.penlight.x * _MotionParameters.x * motionScale + elevationVariation;
    float side = wristSample.penlight.y * _MotionParameters.x * motionScale + sideVariation;
    float3 direction = PrismDirectionFromAngles(basis, elevation, side, true);
    float3 fallback = direction;
    float3 tangent = basis.sideWorld - direction * dot(basis.sideWorld, direction);
    tangent = SafeNormalize(tangent, SafePerp(direction));

    int noiseOctaves = clamp((int)round(_MotionNoise.z), 1, 4);
    float noisePersistence = max(0.001, _MotionNoise.w);
    float directionNoise = FbmNoise21(
        float2(PrismRandom(seat, 19u) * 2000.0 - 1000.0, _FanlightTime * _MotionNoise.y + 631.0),
        noiseOctaves,
        noisePersistence);
    return SafeNormalize(direction + tangent * directionNoise * _MotionNoise.x * variation * 0.1, fallback);
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
    FanlightMotionSample armSample = PrismSampleMotion(rhythm.cyclePhase);
    float wristPhase = frac(rhythm.cyclePhase - saturate(_MotionCycle.z));
    FanlightMotionSample wristSample = PrismSampleMotion(wristPhase);
    float motionScale = PrismComputeMotionScale(seat);
    float extensionVariation = max(
        0.0,
        1.0 + (PrismRandom(seat, 24u) * 2.0 - 1.0) * _MotionVariation.z * _MotionCycle.w);
    float armLength = _AudienceArm.w * saturate(armSample.arm.z * extensionVariation);
    float elevation = armSample.arm.x * _MotionParameters.x * motionScale;
    float side = armSample.arm.y * _MotionParameters.x * motionScale;
    float3 armDirection = PrismDirectionFromAngles(basis, elevation, side, false);
    float sideDistance = dot(armDirection, basis.sideLocal) * armLength * _MotionParameters.z;
    float upDistance = dot(armDirection, basis.upLocal) * armLength + _MotionParameters.y;
    float forwardDistance = dot(armDirection, basis.forwardLocal) * armLength * _MotionParameters.w;
    float3 handLocal = pose.shoulderLocal
        + basis.sideLocal * sideDistance
        + basis.upLocal * upDistance
        + basis.forwardLocal * forwardDistance;
    float3 positionSpread = PrismTransformAudienceOffset(
        basis,
        float3(
            PrismRandom(seat, 29u) * 2.0 - 1.0,
            PrismRandom(seat, 30u) * 2.0 - 1.0,
            PrismRandom(seat, 31u) * 2.0 - 1.0))
        * max(0.0, _HandPositionSpread)
        * _MotionCycle.w;
    handLocal += positionSpread;
    handLocal += PrismComputeHandNoise(seat, basis, motionScale);
    handLocal = PrismClampHandToArmLimit(pose.shoulderLocal, handLocal);

    float3 penlightDirection = PrismComputePenlightDirection(seat, basis, wristSample, motionScale);
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
