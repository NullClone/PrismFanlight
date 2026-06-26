#ifndef PRISM_FANLIGHT_AUDIENCE_INCLUDED
#define PRISM_FANLIGHT_AUDIENCE_INCLUDED

#include "PrismFanlightMotion.hlsl"

// Builds three billboard parts for each audience seat:
//   part 0: body ribbon, feet -> neck
//   part 1: arm ribbon, shoulder -> hand
//   part 2: head billboard, center + radius
//
// Body/head are the parent pose for the arm. The penlight matrix is generated
// from the same shoulder point, so the light reads as held by the audience.
//
//   _AudienceShape      = (bodyHeight, heightJitter, shoulderHeightRatio, bodyHalfWidth)
//   _AudienceArm        = (armHalfWidth, shoulderOffset, headHalfSize, armLengthLimit)
//   _AudienceUpperBody  = (upperBodyLean, upperBodyLeanMax, worldScale, _)
//   _AudienceMotionBody = (bodyBounce, bodySway, bodyMotionSpeed, upperBodyLeanMotion)

FanlightAudiencePart PrismMakeAudiencePart(float3 p0, float3 p1, float halfWidth, float type)
{
    FanlightAudiencePart part;
    part.p0HalfWidth = float4(p0, halfWidth);
    part.p1Type = float4(p1, type);
    return part;
}

void PrismBuildAudienceParts(uint seatId)
{
    FanlightSeatData seat = _Seats[seatId];
    PrismHumanPose human = PrismComputeHumanPose(seat);
    PrismArm arm = PrismComputeArm(seat, human);

    float scale = _AudienceUpperBody.z;
    float3 feetW = mul(_LocalToWorld, float4(human.feetLocal, 1.0)).xyz;
    float3 neckW = mul(_LocalToWorld, float4(human.neckLocal, 1.0)).xyz;
    float3 shoulderW = mul(_LocalToWorld, float4(human.shoulderLocal, 1.0)).xyz;
    float3 handW = mul(_LocalToWorld, float4(arm.handLocal, 1.0)).xyz;
    float3 headW = mul(_LocalToWorld, float4(human.headCenterLocal, 1.0)).xyz;

    uint b = seatId * 3u;
    _AudienceParts[b + 0u] = PrismMakeAudiencePart(feetW, neckW, human.bodyHalfWidth * scale, 0.0);
    _AudienceParts[b + 1u] = PrismMakeAudiencePart(shoulderW, handW, human.armHalfWidth * scale, 1.0);
    _AudienceParts[b + 2u] = PrismMakeAudiencePart(headW, headW, human.headHalf * scale, 2.0);
}

#endif
