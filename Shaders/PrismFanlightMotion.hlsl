#ifndef PRISM_FANLIGHT_MOTION_INCLUDED
#define PRISM_FANLIGHT_MOTION_INCLUDED

#include "PrismFanlightComputeContext.hlsl"
#include "PrismFanlightPose.hlsl"

#define PRISM_FANLIGHT_MOTION_SAMPLE_COUNT 128u

FanlightMotionSample PrismSampleMotion(float cyclePhase)
{
    float samplePosition = frac(cyclePhase) * (float)PRISM_FANLIGHT_MOTION_SAMPLE_COUNT;
    uint sample0 = (uint)floor(samplePosition) % PRISM_FANLIGHT_MOTION_SAMPLE_COUNT;
    uint sample1 = (sample0 + 1u) % PRISM_FANLIGHT_MOTION_SAMPLE_COUNT;
    float weight = frac(samplePosition);
    FanlightMotionSample a = _MotionSamples[sample0];
    FanlightMotionSample b = _MotionSamples[sample1];
    FanlightMotionSample result;
    result.bodyPosition = lerp(a.bodyPosition, b.bodyPosition, weight);
    result.bodyRotation = PrismNlerpQuaternion(a.bodyRotation, b.bodyRotation, weight);
    result.handPosition = lerp(a.handPosition, b.handPosition, weight);
    result.penlightRotation = PrismNlerpQuaternion(a.penlightRotation, b.penlightRotation, weight);
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
    float energyResponse = lerp(1.0, lerp(0.65, 1.35, PrismRandom(seat, 25u)), _MotionHuman.y);
    float enthusiasm = max(0.0, _MotionHuman.x) * energyResponse;
    float restFactor = PrismComputeRestFactor(seat);
    float participationScale = PrismRandom(seat, 26u) < _MotionRest.z ? 0.35 : 1.0;
    return saturate(enthusiasm * restFactor * participationScale);
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
    float maximumAngle = saturate(_MotionVariation.z) * PRISM_FANLIGHT_PI * 0.25;
    float cosine = lerp(1.0, cos(maximumAngle), PrismRandom(seat, 17u));
    float sine = sqrt(max(0.0, 1.0 - cosine * cosine));
    float azimuth = PrismRandom(seat, 18u) * 2.0 * PRISM_FANLIGHT_PI;
    float3 tangent = SafePerp(direction);
    float3 bitangent = SafeNormalize(cross(direction, tangent), SafePerp(direction));
    float3 radial = tangent * cos(azimuth) + bitangent * sin(azimuth);
    return SafeNormalize(direction * cosine + radial * sine, direction);
}

float4x4 PrismComputePenlightRotation(
    FanlightSeatData seat,
    PrismAudienceBasis basis,
    FanlightMotionSample motionSample,
    float motionActivity)
{
    float4 rotation = PrismNlerpQuaternion(
        _MotionReferencePenlightRotation,
        motionSample.penlightRotation,
        motionActivity);
    float3 upAudience = PrismRotateByQuaternion(rotation, float3(0.0, 1.0, 0.0));
    float3 forwardAudience = PrismRotateByQuaternion(rotation, float3(0.0, 0.0, 1.0));
    upAudience = PrismApplyDirectionSpread(seat, upAudience);

    if (_MotionNoise.y > 0.000001 && motionActivity > 0.000001)
    {
        float3 tangent = SafeNormalize(
            forwardAudience - upAudience * dot(forwardAudience, upAudience),
            SafePerp(upAudience));
        int noiseOctaves = clamp(_MotionNoiseOctaves, 1, 4);
        float directionNoise = FbmNoise21(
            float2(PrismRandom(seat, 19u) * 2000.0 - 1000.0, _FanlightTime * _MotionNoise.z + 631.0),
            noiseOctaves,
            saturate(_MotionNoise.w));
        float noiseAngle = directionNoise * _MotionNoise.y * motionActivity;
        upAudience = SafeNormalize(
            upAudience * cos(noiseAngle) + tangent * sin(noiseAngle),
            upAudience);
    }

    float3 yLocal = SafeNormalize(
        PrismTransformAudienceOffset(basis, upAudience),
        basis.upLocal);
    float3 zLocal = PrismTransformAudienceOffset(basis, forwardAudience);
    zLocal = SafeNormalize(zLocal - yLocal * dot(zLocal, yLocal), SafePerp(yLocal));
    float3 xLocal = SafeNormalize(cross(yLocal, zLocal), basis.sideLocal);
    zLocal = SafeNormalize(cross(xLocal, yLocal), zLocal);

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
    float motionActivity,
    float gripPivotY)
{
    float reachVariation = max(
        0.0,
        1.0 + (PrismRandom(seat, 24u) * 2.0 - 1.0) * _MotionVariation.y);
    float3 handAudiencePosition = lerp(
        _MotionReferenceHandPosition.xyz,
        armSample.handPosition.xyz,
        motionActivity) * reachVariation;
    float armLength = max(0.0001, _AudienceArm.w);
    float sideDistance = handAudiencePosition.x * armLength;
    float upDistance = handAudiencePosition.y * armLength;
    float forwardDistance = handAudiencePosition.z * armLength;
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

        * motionActivity;
    handLocal += positionSpread;
    handLocal += PrismComputeHandNoise(seat, basis, motionActivity);
    handLocal = PrismClampHandToArmLimit(pose.shoulderLocal, handLocal);

    float4x4 handTranslation = Translate(handLocal);
    float4x4 penlightRotation = PrismComputePenlightRotation(seat, basis, armSample, motionActivity);
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
    float motionActivity = PrismComputeMotionScale(seat);
    pose = PrismComputeHumanPose(seat, rhythm, basis, motionSample, motionActivity);
    arm = PrismComputeArm(seat, pose, basis, motionSample, motionActivity, gripPivotY);
}

float4x4 PrismComputeMatrix(FanlightSeatData seat, float gripPivotY)
{
    PrismHumanPose pose;
    PrismArm arm;
    PrismComputeFrameData(seat, gripPivotY, pose, arm);
    return arm.worldMatrix;
}

#endif
