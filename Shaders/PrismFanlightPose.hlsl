#ifndef PRISM_FANLIGHT_POSE_INCLUDED
#define PRISM_FANLIGHT_POSE_INCLUDED

#include "PrismFanlightDirection.hlsl"

float3 PrismComputeSeatAnchor(FanlightSeatData seat)
{
    float3 localPosition = seat.localPositionSeed.xyz;
    float2 jitter = float2(PrismRandom(seat, 0u), PrismRandom(seat, 1u)) * 2.0 - 1.0;
    localPosition.xz += jitter * _MotionVariation.x * _SeatPitch.xy;
    return localPosition;
}

PrismCrowdRhythm PrismComputeCrowdRhythm(FanlightSeatData seat)
{
    float reactionDelay = PrismRandom(seat, 3u) * _MotionHuman.z * _MotionCycle.w;
    float beatReaction = reactionDelay * max(1.0, _FanlightTempo.y) / 60.0;
    float seatBeatJitter = (PrismRandom(seat, 5u) * 2.0 - 1.0) * _MotionBeatSpread.x * _MotionCycle.w;
    float2 block01 = float2(
        _BlockCount.x > 1.0 ? seat.planePositionBlock.z / max(1.0, _BlockCount.x - 1.0) : 0.5,
        _BlockCount.y > 1.0 ? seat.planePositionBlock.w / max(1.0, _BlockCount.y - 1.0) : 0.5);
    float blockBeatDelay = dot(block01 - 0.5, _MotionBeatSpread.yz);
    float delayedBeat = _FanlightBeat.y - beatReaction - seatBeatJitter - blockBeatDelay;
    float personaTiming = (PrismRandom(seat, 6u) * 2.0 - 1.0) * 0.5 * _MotionTiming.y * _MotionCycle.w;
    float phaseNoise = 0.0;
    if (_MotionTiming.z > 0.000001)
    {
        phaseNoise = FbmNoise21(
            float2(PrismRandom(seat, 7u) * 2000.0 - 1000.0, _FanlightTime * _MotionTiming.w),
            clamp(_MotionNoiseOctaves, 1, 4),
            saturate(_MotionNoise.w)) * _MotionTiming.z / (2.0 * PRISM_FANLIGHT_PI);
    }
    float cyclePhase = frac((delayedBeat + _MotionCycle.y) / max(0.001, _MotionCycle.x) + personaTiming + phaseNoise);
    float bodyPhase = cyclePhase * 2.0 * PRISM_FANLIGHT_PI;

    PrismCrowdRhythm rhythm = (PrismCrowdRhythm)0;
    rhythm.cyclePhase = cyclePhase;
    rhythm.bodyPhase = bodyPhase + 0.35;
    return rhythm;
}

PrismHumanPose PrismComputeHumanPose(
    FanlightSeatData seat,
    PrismCrowdRhythm rhythm,
    PrismAudienceBasis basis,
    FanlightMotionSample motionSample,
    float motionActivity)
{
    float3 anchor = PrismComputeSeatAnchor(seat);
    float heightJitter = (PrismRandom(seat, 8u) * 2.0 - 1.0) * _AudienceShape.y;
    float bodyHeight = max(0.1, _AudienceShape.x * (1.0 + heightJitter));
    float shoulderHeight = bodyHeight * saturate(_AudienceShape.z);
    float bodyHalfWidth = _AudienceShape.w;
    float armHalfWidth = _AudienceArm.x;
    float shoulderOffset = _AudienceArm.y;
    float headHalf = _AudienceArm.z;
    float sway = sin(rhythm.bodyPhase);
    float bounce = sway * 0.5 + 0.5;
    float3 bodyOffset = basis.sideLocal * sway * _AudienceMotionBody.y
        + basis.upLocal * bounce * _AudienceMotionBody.x;
    float3 feet = anchor + bodyOffset;
    float neckHeight = max(shoulderHeight, bodyHeight - headHalf * 2.0);
    float motionLean = lerp(
        _MotionReferencePenlight.w,
        motionSample.penlightDirectionBodyLean.w,
        motionActivity);
    float3 leanUp = basis.upLocal * cos(motionLean) + basis.forwardLocal * sin(motionLean);

    PrismHumanPose pose = (PrismHumanPose)0;
    pose.anchorLocal = anchor;
    pose.feetLocal = feet;
    pose.shoulderLocal = feet + basis.sideLocal * shoulderOffset + leanUp * shoulderHeight;
    pose.neckLocal = feet + leanUp * neckHeight;
    pose.headCenterLocal = feet + leanUp * (neckHeight + headHalf);
    pose.bodyHalfWidth = bodyHalfWidth;
    pose.armHalfWidth = armHalfWidth;
    pose.headHalf = headHalf;
    return pose;
}

#endif
