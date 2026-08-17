#ifndef PRISM_FANLIGHT_MOTION_INCLUDED
#define PRISM_FANLIGHT_MOTION_INCLUDED

#include "PrismFanlightComputeContext.hlsl"
#include "PrismFanlightPose.hlsl"

#define PRISM_FANLIGHT_MOTION_SAMPLE_COUNT 64u

FanlightMotionSample PrismSampleMotion(float cyclePhase)
{
    float samplePosition = frac(cyclePhase) * (float)PRISM_FANLIGHT_MOTION_SAMPLE_COUNT;
    uint sample0 = (uint)floor(samplePosition) % PRISM_FANLIGHT_MOTION_SAMPLE_COUNT;
    uint sample1 = (sample0 + 1u) % PRISM_FANLIGHT_MOTION_SAMPLE_COUNT;
    float weight = frac(samplePosition);
    FanlightMotionSample a = _MotionSamples[sample0];
    FanlightMotionSample b = _MotionSamples[sample1];
    FanlightMotionSample result;
    result.armDirectionExtension.xyz = PrismInterpolateDirection(
        a.armDirectionExtension.xyz,
        b.armDirectionExtension.xyz,
        weight);
    result.armDirectionExtension.w = lerp(a.armDirectionExtension.w, b.armDirectionExtension.w, weight);
    result.penlightDirectionBodyLean.xyz = PrismInterpolateDirection(
        a.penlightDirectionBodyLean.xyz,
        b.penlightDirectionBodyLean.xyz,
        weight);
    result.penlightDirectionBodyLean.w = lerp(
        a.penlightDirectionBodyLean.w,
        b.penlightDirectionBodyLean.w,
        weight);
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

float3 PrismComputeHandNoise(FanlightSeatData seat, PrismAudienceBasis basis, float motionActivity)
{
    if (_MotionNoise.x <= 0.000001 || motionActivity <= 0.000001)
    {
        return 0.0;
    }

    int noiseOctaves = clamp(_MotionNoiseOctaves, 1, 4);
    float noisePersistence = saturate(_MotionNoise.w);
    float noiseSide = FbmNoise21(
        float2(PrismRandom(seat, 21u) * 2000.0 - 1000.0, _FanlightTime * _MotionNoise.z),
        noiseOctaves,
        noisePersistence);
    float noiseUp = FbmNoise21(
        float2(PrismRandom(seat, 22u) * 2000.0 - 1000.0, _FanlightTime * _MotionNoise.z + 317.5),
        noiseOctaves,
        noisePersistence);
    return (basis.sideLocal * noiseSide + basis.upLocal * noiseUp)
        * _MotionNoise.x
        * motionActivity;
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

float3 PrismDirectionFromAudience(
    PrismAudienceBasis basis,
    float3 audienceDirection,
    bool worldSpace)
{
    audienceDirection = SafeNormalize(audienceDirection, float3(0.0, 0.0, 1.0));
    float3 direction = worldSpace
        ? basis.sideWorld * audienceDirection.x + basis.upWorld * audienceDirection.y + basis.forwardWorld * audienceDirection.z
        : basis.sideLocal * audienceDirection.x + basis.upLocal * audienceDirection.y + basis.forwardLocal * audienceDirection.z;
    return SafeNormalize(direction, worldSpace ? basis.upWorld : basis.upLocal);
}

float3 PrismApplyDirectionSpread(FanlightSeatData seat, float3 direction)
{
    float maximumAngle = saturate(_MotionVariation.z) * _MotionCycle.w * PRISM_FANLIGHT_PI * 0.25;
    float cosine = lerp(1.0, cos(maximumAngle), PrismRandom(seat, 17u));
    float sine = sqrt(max(0.0, 1.0 - cosine * cosine));
    float azimuth = PrismRandom(seat, 18u) * 2.0 * PRISM_FANLIGHT_PI;
    float3 tangent = SafePerp(direction);
    float3 bitangent = SafeNormalize(cross(direction, tangent), SafePerp(direction));
    float3 radial = tangent * cos(azimuth) + bitangent * sin(azimuth);
    return SafeNormalize(direction * cosine + radial * sine, direction);
}

float3 PrismComputePenlightDirection(
    FanlightSeatData seat,
    PrismAudienceBasis basis,
    FanlightMotionSample wristSample,
    float motionActivity)
{
    float3 motionAudienceDirection = SafeNormalize(
        wristSample.penlightDirectionBodyLean.xyz,
        _MotionReferencePenlight.xyz);
    motionAudienceDirection = PrismApplyDirectionSpread(seat, motionAudienceDirection);
    float3 motionDirection = PrismDirectionFromAudience(basis, motionAudienceDirection, true);
    float3 referenceDirection = PrismDirectionFromAudience(basis, _MotionReferencePenlight.xyz, true);
    float3 direction = PrismInterpolateDirection(referenceDirection, motionDirection, motionActivity);
    float3 fallback = direction;

    if (_MotionNoise.y <= 0.000001 || motionActivity <= 0.000001)
    {
        return direction;
    }

    float3 tangent = basis.sideWorld - direction * dot(basis.sideWorld, direction);
    tangent = SafeNormalize(tangent, SafePerp(direction));

    int noiseOctaves = clamp(_MotionNoiseOctaves, 1, 4);
    float noisePersistence = saturate(_MotionNoise.w);
    float directionNoise = FbmNoise21(
        float2(PrismRandom(seat, 19u) * 2000.0 - 1000.0, _FanlightTime * _MotionNoise.z + 631.0),
        noiseOctaves,
        noisePersistence);
    float noiseAngle = directionNoise * _MotionNoise.y * motionActivity;
    return SafeNormalize(
        direction * cos(noiseAngle) + tangent * sin(noiseAngle),
        fallback);
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

PrismArm PrismComputeArm(
    FanlightSeatData seat,
    PrismHumanPose pose,
    PrismAudienceBasis basis,
    FanlightMotionSample armSample,
    FanlightMotionSample wristSample,
    float motionActivity,
    float gripPivotY)
{
    float extensionVariation = max(
        0.0,
        1.0 + (PrismRandom(seat, 24u) * 2.0 - 1.0) * _MotionVariation.y * _MotionCycle.w);
    float motionExtension = saturate(armSample.armDirectionExtension.w * extensionVariation);
    float armExtension = lerp(_MotionReferenceArm.w, motionExtension, motionActivity);
    float armLength = _AudienceArm.w * saturate(armExtension);
    float3 armAudienceDirection = PrismInterpolateDirection(
        _MotionReferenceArm.xyz,
        armSample.armDirectionExtension.xyz,
        motionActivity);
    float3 armDirection = PrismDirectionFromAudience(basis, armAudienceDirection, false);
    float sideScale = lerp(1.0, _MotionParameters.z, motionActivity);
    float forwardScale = lerp(1.0, _MotionParameters.w, motionActivity);
    float sideDistance = dot(armDirection, basis.sideLocal) * armLength * sideScale;
    float upDistance = dot(armDirection, basis.upLocal) * armLength + _MotionParameters.y * motionActivity;
    float forwardDistance = dot(armDirection, basis.forwardLocal) * armLength * forwardScale;
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
        * _MotionCycle.w
        * motionActivity;
    handLocal += positionSpread;
    handLocal += PrismComputeHandNoise(seat, basis, motionActivity);
    handLocal = PrismClampHandToArmLimit(pose.shoulderLocal, handLocal);

    float3 penlightDirection = PrismComputePenlightDirection(seat, basis, wristSample, motionActivity);
    float4x4 handTranslation = Translate(handLocal);
    float4x4 penlightRotation = PrismPenlightRotation(penlightDirection, basis);
    float4x4 gripTranslation = Translate(float3(0.0, -gripPivotY, 0.0));

    PrismArm result = (PrismArm)0;
    result.worldMatrix = mul(_LocalToWorld, mul(handTranslation, mul(penlightRotation, gripTranslation)));
    result.handLocal = handLocal;
    result.shoulderLocal = pose.shoulderLocal;
    return result;
}

void PrismComputeFrameData(
    FanlightSeatData seat,
    float gripPivotY,
    out PrismHumanPose pose,
    out PrismArm arm)
{
    PrismCrowdRhythm rhythm = PrismComputeCrowdRhythm(seat);
    PrismAudienceBasis basis = PrismComputeAudienceBasis(seat);
    FanlightMotionSample motionSample = PrismSampleMotion(rhythm.cyclePhase);
    float wristPhase = frac(rhythm.cyclePhase - saturate(_MotionCycle.z));
    FanlightMotionSample wristSample = PrismSampleMotion(wristPhase);
    float motionActivity = saturate(_MotionParameters.x * PrismComputeMotionScale(seat));
    pose = PrismComputeHumanPose(seat, rhythm, basis, motionSample, motionActivity);
    arm = PrismComputeArm(seat, pose, basis, motionSample, wristSample, motionActivity, gripPivotY);
}

float4x4 PrismComputeMatrix(FanlightSeatData seat, float gripPivotY)
{
    PrismHumanPose pose;
    PrismArm arm;
    PrismComputeFrameData(seat, gripPivotY, pose, arm);
    return arm.worldMatrix;
}

#endif
