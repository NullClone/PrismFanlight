#ifndef PRISM_FANLIGHT_POSE_INCLUDED
#define PRISM_FANLIGHT_POSE_INCLUDED

#include "PrismFanlightDirection.hlsl"

float3 PrismComputeSeatAnchor(FanlightSeatData seat)
{
    float3 localPosition = seat.localPositionSeed.xyz;
    float2 jitter = float2(PrismRandom(seat, 0u), PrismRandom(seat, 1u)) * 2.0 - 1.0;
    localPosition.xz += jitter * _MotionVariation.x * _SeatPitch.xy;
    localPosition.y += (PrismRandom(seat, 2u) * 2.0 - 1.0) * _MotionVariation.y;
    return localPosition;
}

PrismCrowdRhythm PrismComputeCrowdRhythm(FanlightSeatData seat)
{
    int noiseOctaves = clamp((int)round(_MotionNoise.z), 1, 4);
    float noisePersistence = max(0.001, _MotionNoise.w);
    float reactionDelay = PrismRandom(seat, 3u) * _MotionHuman.z;
    float beatReaction = reactionDelay * max(1.0, _FanlightTempo.y) / 60.0;
    float randomBeatReaction = PrismRandom(seat, 4u) * _MotionBeatSpread.x;
    float seatBeatJitter = (PrismRandom(seat, 5u) * 2.0 - 1.0) * _MotionBeatSpread.y;
    float2 block01 = float2(
        _BlockCount.x > 1.0 ? seat.planePositionBlock.z / max(1.0, _BlockCount.x - 1.0) : 0.5,
        _BlockCount.y > 1.0 ? seat.planePositionBlock.w / max(1.0, _BlockCount.y - 1.0) : 0.5);
    float blockBeatDelay = dot(block01 - 0.5, _MotionBeatSpread.zw);
    float delayedBeat = _FanlightBeat.y - beatReaction - randomBeatReaction - seatBeatJitter - blockBeatDelay;
    float personaTiming = (PrismRandom(seat, 6u) * 2.0 - 1.0) * 0.5 * _MotionTiming.y;
    float phaseNoise = FbmNoise21(
        float2(PrismRandom(seat, 7u) * 2000.0 - 1000.0, _FanlightTime * _MotionTiming.w),
        noiseOctaves,
        noisePersistence) * _MotionTiming.z / (2.0 * PRISM_FANLIGHT_PI);
    float cyclePhase = frac((delayedBeat + _GestureTiming.y) / max(0.001, _GestureTiming.x) + personaTiming + phaseNoise);
    float bodyPhase = cyclePhase * 2.0 * PRISM_FANLIGHT_PI;

    PrismCrowdRhythm rhythm = (PrismCrowdRhythm)0;
    rhythm.cyclePhase = cyclePhase;
    rhythm.bodyPhase = bodyPhase + 0.35;
    rhythm.shoulderPhase = bodyPhase + 0.18;
    rhythm.downbeatPulse = pow(1.0 - saturate(_FanlightBeat.w), 8.0) * saturate(_FanlightTempo.x);
    return rhythm;
}

PrismHumanPose PrismComputeHumanPose(FanlightSeatData seat)
{
    float3 anchor = PrismComputeSeatAnchor(seat);
    PrismCrowdRhythm rhythm = PrismComputeCrowdRhythm(seat);
    PrismAudienceBasis basis = PrismComputeAudienceBasis(seat);
    float heightJitter = (PrismRandom(seat, 8u) * 2.0 - 1.0) * _AudienceShape.y;
    float bodyHeight = max(0.1, _AudienceShape.x * (1.0 + heightJitter));
    float shoulderHeight = bodyHeight * saturate(_AudienceShape.z);
    float bodyHalfWidth = _AudienceShape.w;
    float armHalfWidth = _AudienceArm.x;
    float shoulderOffset = _AudienceArm.y;
    float headHalf = _AudienceArm.z;
    float sway = sin(rhythm.bodyPhase);
    float shoulderSway = sin(rhythm.shoulderPhase);
    float bounce = sway * 0.5 + 0.5;
    float3 bodyOffset = basis.sideLocal * sway * _AudienceMotionBody.y
        + basis.upLocal * bounce * _AudienceMotionBody.x;
    float3 feet = anchor + bodyOffset;
    float neckHeight = max(shoulderHeight, bodyHeight - headHalf * 2.0);
    float shoulderLean = _AudienceUpperBody.y * saturate(_AudienceUpperBody.x) * saturate(_AudienceMotionBody.w);
    float3 upperBodyLean = basis.forwardLocal * shoulderSway * shoulderLean;

    PrismHumanPose pose = (PrismHumanPose)0;
    pose.anchorLocal = anchor;
    pose.feetLocal = feet;
    pose.shoulderLocal = feet + basis.sideLocal * shoulderOffset + basis.upLocal * shoulderHeight + upperBodyLean;
    pose.neckLocal = feet + basis.upLocal * neckHeight + upperBodyLean * 0.35;
    pose.headCenterLocal = feet + basis.upLocal * (neckHeight + headHalf) + upperBodyLean * 0.35;
    pose.bodyHalfWidth = bodyHalfWidth;
    pose.armHalfWidth = armHalfWidth;
    pose.headHalf = headHalf;
    return pose;
}

#endif
