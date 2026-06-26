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

PrismHumanPose PrismComputeHumanPose(FanlightSeatData seat)
{
    float seed = seat.localPositionSeed.w;
    float3 anchor = PrismComputeSeatAnchor(seat);

    float heightJitter = (Hash11(seed + 211.0) * 2.0 - 1.0) * _AudienceShape.y;
    float bodyHeight = max(0.1, _AudienceShape.x * (1.0 + heightJitter));
    float shoulderHeight = bodyHeight * saturate(_AudienceShape.z);
    float bodyHalfWidth = _AudienceShape.w;
    float armHalfWidth = _AudienceArm.x;
    float shoulderOffset = _AudienceArm.y;
    float headHalf = _AudienceArm.z;

    float phaseSeed = Hash11(seed + 227.0);
    float phase = (_FanlightTime * max(0.01, _AudienceMotionBody.z) + phaseSeed) * 2.0 * PRISM_FANLIGHT_PI;
    float sway = sin(phase);
    float bounce = sway * 0.5 + 0.5;

    float3 bodyOffset = float3(sway * _AudienceMotionBody.y, bounce * _AudienceMotionBody.x, 0.0);
    float3 feet = float3(anchor.x, anchor.y, anchor.z) + bodyOffset;
    float neckY = max(shoulderHeight, bodyHeight - headHalf * 2.0);
    float shoulderLean = _AudienceUpperBody.y * saturate(_AudienceUpperBody.x) * saturate(_AudienceMotionBody.w);
    float3 leanAxis = PrismComputeBaseAxis(seat, false);
    float3 upperBodyLean = leanAxis * sway * shoulderLean;

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

float3 PrismGetArmBase(PrismHumanPose pose)
{
    return _HandBaseHeight > 0.0001 ? pose.shoulderLocal : pose.anchorLocal + float3(0.0, _HandBaseHeight, 0.0);
}

#endif
