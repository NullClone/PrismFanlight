#ifndef PRISM_FANLIGHT_POSE_INCLUDED
#define PRISM_FANLIGHT_POSE_INCLUDED

#include "PrismFanlightDirection.hlsl"

float3 PrismComputeSeatAnchor(FanlightSeatData seat)
{
    float3 localPosition = seat.localPositionSeed.xyz;
    float seed = seat.localPositionSeed.w;

    float2 jitter = float2(Hash11(seed + 31.0), Hash11(seed + 37.0)) * 2.0 - 1.0;
    localPosition.xz += jitter * _MotionVariation.x * _SeatPitch.xy;
    localPosition.y += (Hash11(seed + 41.0) * 2.0 - 1.0) * _MotionVariation.y;

    return localPosition;
}

PrismCrowdRhythm PrismComputeCrowdRhythm(FanlightSeatData seat)
{
    float seed = seat.localPositionSeed.w;

    int noiseOctaves = clamp((int)round(_MotionNoise.z), 1, 4);
    float noisePersistence = max(0.001, _MotionNoise.w);

    float reactionDelay = Hash11(seed + 17.0) * _MotionHuman.z;
    float beatReaction = reactionDelay * max(1.0, _FanlightTempo.y) / 60.0;
    float randomBeatReaction = Hash11(seed + 73.0) * _MotionBeatSpread.x;
    float seatBeatJitter = (Hash11(seed + 79.0) * 2.0 - 1.0) * _MotionBeatSpread.y;
    float2 block01 = float2(
        _BlockCount.x > 1.0 ? seat.planePositionBlock.z / max(1.0, _BlockCount.x - 1.0) : 0.5,
        _BlockCount.y > 1.0 ? seat.planePositionBlock.w / max(1.0, _BlockCount.y - 1.0) : 0.5);
    float blockBeatDelay = dot(block01 - 0.5, _MotionBeatSpread.zw);
    float delayedBeat = max(0.0, _FanlightBeat.y - beatReaction - randomBeatReaction - seatBeatJitter - blockBeatDelay);
    float beatPhase = 2.0 * PRISM_FANLIGHT_PI * ((delayedBeat / max(0.001, _MotionBeat.y)) + _MotionBeat.z);
    beatPhase += Hash11(seed + 11.0) * 2.0 * PRISM_FANLIGHT_PI * _MotionTiming.y;
    beatPhase += FbmNoise21(float2(Hash11(seed + 23.0) * 2000.0 - 1000.0, _FanlightTime * _MotionTiming.w), noiseOctaves, noisePersistence) * _MotionTiming.z;

    PrismCrowdRhythm rhythm = (PrismCrowdRhythm)0;
    rhythm.basePhase = beatPhase;
    rhythm.bodyPhase = beatPhase + 0.35;
    rhythm.shoulderPhase = beatPhase + 0.18;
    rhythm.armPhase = beatPhase;
    rhythm.wristPhase = beatPhase - 0.16;
    rhythm.downbeatPulse = pow(1.0 - saturate(_FanlightBeat.w), 8.0) * saturate(_FanlightTempo.x);
    return rhythm;
}

PrismHumanPose PrismComputeHumanPose(FanlightSeatData seat)
{
    float seed = seat.localPositionSeed.w;
    float3 anchor = PrismComputeSeatAnchor(seat);
    PrismCrowdRhythm rhythm = PrismComputeCrowdRhythm(seat);

    float heightJitter = (Hash11(seed + 211.0) * 2.0 - 1.0) * _AudienceShape.y;
    float bodyHeight = max(0.1, _AudienceShape.x * (1.0 + heightJitter));
    float shoulderHeight = bodyHeight * saturate(_AudienceShape.z);
    float bodyHalfWidth = _AudienceShape.w;
    float armHalfWidth = _AudienceArm.x;
    float shoulderOffset = _AudienceArm.y;
    float headHalf = _AudienceArm.z;

    float sway = sin(rhythm.bodyPhase);
    float shoulderSway = sin(rhythm.shoulderPhase);
    float bounce = sway * 0.5 + 0.5;

    float3 bodyOffset = float3(sway * _AudienceMotionBody.y, bounce * _AudienceMotionBody.x, 0.0);
    float3 feet = float3(anchor.x, anchor.y, anchor.z) + bodyOffset;
    float neckY = max(shoulderHeight, bodyHeight - headHalf * 2.0);
    float shoulderLean = _AudienceUpperBody.y * saturate(_AudienceUpperBody.x) * saturate(_AudienceMotionBody.w);
    float3 leanAxis = PrismComputeBaseAxis(seat, false);
    float3 upperBodyLean = leanAxis * shoulderSway * shoulderLean;

    PrismHumanPose pose = (PrismHumanPose)0;
    pose.anchorLocal = anchor;
    pose.feetLocal = feet;
    pose.shoulderLocal = feet + float3(shoulderOffset, shoulderHeight, 0.0) + upperBodyLean;
    pose.neckLocal = feet + float3(0.0, neckY, 0.0) + upperBodyLean * 0.35;
    pose.headCenterLocal = feet + float3(0.0, neckY + headHalf, 0.0) + upperBodyLean * 0.35;
    pose.bodyHalfWidth = bodyHalfWidth;
    pose.armHalfWidth = armHalfWidth;
    pose.headHalf = headHalf;
    return pose;
}

float3 PrismComputeHandZoneBase(FanlightSeatData seat, PrismHumanPose pose)
{
    float seed = seat.localPositionSeed.w;
    float variation = max(0.0, _HandZone.w);

    float3 forwardLocal = PrismWorldVectorToLocal(PrismComputeWorldDirection(seat));
    forwardLocal.y = 0.0;
    forwardLocal = SafeNormalize(forwardLocal, float3(0.0, 0.0, -1.0));

    float3 sideLocal = SafeNormalize(cross(float3(0.0, 1.0, 0.0), forwardLocal), float3(1.0, 0.0, 0.0));
    float heightOffset = _HandZone.x + (Hash11(seed + 151.0) * 2.0 - 1.0) * variation;
    float forwardOffset = _HandZone.y + (Hash11(seed + 157.0) * 2.0 - 1.0) * variation;
    float sideOffset = (Hash11(seed + 163.0) * 2.0 - 1.0) * variation * 0.5;

    return pose.shoulderLocal
        + float3(0.0, heightOffset, 0.0)
        + forwardLocal * forwardOffset
        + sideLocal * sideOffset;
}

#endif
