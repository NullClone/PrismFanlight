// Builds three billboard parts for each audience seat:
//   part 0: body ribbon, feet -> neck
//   part 1: arm ribbon, shoulder -> hand
//   part 2: head billboard, center + radius
//
// Audience motion is intentionally minimal. Body/head stay anchored to the
// seat, while only the arm and a tiny shoulder offset connect to the penlight.
//
//   _AudienceShape      = (bodyHeight, heightJitter, shoulderHeightRatio, bodyHalfWidth)
//   _AudienceArm        = (armHalfWidth, shoulderOffset, headHalfSize, maxReach)
//   _AudienceReach      = (leanFactor, leanMax, worldScale, _)
//   _AudienceMotionBody = (bodyBounce, bodySway, bodyMotionSpeed, shoulderFollow)
//   _AudienceShoulder   = (shoulderFollowMax, _, _, _)

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
    PrismArm arm = PrismComputeArm(seat);
    float seed = seat.localPositionSeed.w;

    float heightJitter = (Hash11(seed + 211.0) * 2.0 - 1.0) * _AudienceShape.y;
    float bodyHeight = max(0.1, _AudienceShape.x * (1.0 + heightJitter));
    float shoulderHeight = bodyHeight * saturate(_AudienceShape.z);
    float bodyHalfWidth = _AudienceShape.w;

    float armHalfWidth = _AudienceArm.x;
    float shoulderOffset = _AudienceArm.y;
    float headHalf = _AudienceArm.z;
    float maxReach = max(_AudienceArm.w, 0.001);

    float3 anchor = float3(arm.baseLocal.x, 0.0, arm.baseLocal.z);
    float3 hand = arm.handLocal;
    float3 baseShoulder = anchor + float3(shoulderOffset, shoulderHeight, 0.0);

    float phaseSeed = Hash11(seed + 227.0);
    float phase = (_FanlightTime * max(0.01, _AudienceMotionBody.z) + phaseSeed) * 2.0 * PRISM_FANLIGHT_PI;
    float sway = sin(phase);
    float bounce = sway * 0.5 + 0.5;

    // Use local X for the crowd sway. Keeping the axis fixed avoids high-frequency
    // changes from the penlight hand direction and removes extra trig work.
    float3 bodyOffset = float3(sway * _AudienceMotionBody.y, bounce * _AudienceMotionBody.x, 0.0);
    float3 feet = anchor + bodyOffset;
    float neckY = max(shoulderHeight, bodyHeight - headHalf * 2.0);
    float3 neckLocal = feet + float3(0.0, neckY, 0.0);
    float3 headCenterLocal = feet + float3(0.0, neckY + headHalf, 0.0);

    float3 handDelta = hand - baseShoulder;
    float3 handDir = SafeNormalize(float3(handDelta.x, 0.0, handDelta.z), float3(0.0, 0.0, 1.0));
    float reachOver = max(0.0, length(handDelta) - maxReach);
    float reachFollow = saturate(reachOver / maxReach) * _AudienceReach.y * saturate(_AudienceReach.x);
    float followDistance = min(_AudienceShoulder.x, reachFollow) * saturate(_AudienceMotionBody.w);
    float3 shoulder = baseShoulder + bodyOffset + handDir * followDistance;

    float scale = _AudienceReach.z;
    float3 feetW = mul(_LocalToWorld, float4(feet, 1.0)).xyz;
    float3 neckW = mul(_LocalToWorld, float4(neckLocal, 1.0)).xyz;
    float3 shoulderW = mul(_LocalToWorld, float4(shoulder, 1.0)).xyz;
    float3 handW = mul(_LocalToWorld, float4(hand, 1.0)).xyz;
    float3 headW = mul(_LocalToWorld, float4(headCenterLocal, 1.0)).xyz;

    uint b = seatId * 3u;
    _AudienceParts[b + 0u] = PrismMakeAudiencePart(feetW, neckW, bodyHalfWidth * scale, 0.0);
    _AudienceParts[b + 1u] = PrismMakeAudiencePart(shoulderW, handW, armHalfWidth * scale, 1.0);
    _AudienceParts[b + 2u] = PrismMakeAudiencePart(headW, headW, headHalf * scale, 2.0);
}
