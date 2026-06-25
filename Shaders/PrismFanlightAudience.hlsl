// Builds three billboard parts for each audience seat:
//   part 0: body ribbon, feet -> neck
//   part 1: arm ribbon, shoulder -> hand
//   part 2: head billboard, center + radius
//
// Body and head are anchored to the seat and only receive low-frequency motion.
// The penlight hand can move quickly, but only the shoulder gets a small,
// clamped follow offset so the audience silhouette remains stable in closeups.
//
//   _AudienceShape      = (bodyHeight, heightJitter, shoulderHeightRatio, bodyHalfWidth)
//   _AudienceArm        = (armHalfWidth, shoulderOffset, headHalfSize, maxReach)
//   _AudienceReach      = (leanFactor, leanMax, worldScale, _)
//   _AudienceMotionBody = (bodyBounce, bodySway, bodyMotionSpeed, shoulderFollow)
//   _AudienceMotionHead = (shoulderBounce, headBob, headSway, headCounterMotion)
//   _AudienceShoulder   = (shoulderFollowMax, headMotionSpeed, _, _)
//   _AudienceVariation  = (enthusiasmVariation, bodyVariation, headVariation, reactionDelay)
//   _AudienceCrowd      = (quietProbability, quietMotionLevel, _, _)

FanlightAudiencePart PrismMakeAudiencePart(float3 p0, float3 p1, float halfWidth, float type)
{
    FanlightAudiencePart part;
    part.p0HalfWidth = float4(p0, halfWidth);
    part.p1Type = float4(p1, type);
    return part;
}

float PrismAudienceSlowPulse(float phase)
{
    return sin(phase) * 0.5 + 0.5;
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

    float delaySeconds = Hash11(seed + 223.0) * _AudienceVariation.w;
    float localTime = max(0.0, _FanlightTime - delaySeconds);
    float phaseSeed = Hash11(seed + 227.0);
    float bodySpeed = max(0.01, _AudienceMotionBody.z);
    float headSpeed = max(0.01, _AudienceShoulder.y);
    float bodyPhase = (localTime * bodySpeed + phaseSeed) * 2.0 * PRISM_FANLIGHT_PI;
    float headPhase = (localTime * headSpeed + phaseSeed * 1.37 + 0.21) * 2.0 * PRISM_FANLIGHT_PI;

    float beatBlend = saturate(_FanlightTempo.x);
    float slowBeat = _FanlightBeat.y / 2.0 + phaseSeed * 0.18;
    float beatPhase = frac(slowBeat) * 2.0 * PRISM_FANLIGHT_PI;
    bodyPhase = lerp(bodyPhase, beatPhase, beatBlend * 0.35);
    headPhase = lerp(headPhase, beatPhase + 0.7, beatBlend * 0.2);

    float enthusiasm = max(0.0, 1.0 + (Hash11(seed + 233.0) * 2.0 - 1.0) * _AudienceVariation.x);
    float bodyVariation = max(0.0, 1.0 + (Hash11(seed + 239.0) * 2.0 - 1.0) * _AudienceVariation.y);
    float headVariation = max(0.0, 1.0 + (Hash11(seed + 241.0) * 2.0 - 1.0) * _AudienceVariation.z);
    float quietMultiplier = Hash11(seed + 251.0) < _AudienceCrowd.x ? saturate(_AudienceCrowd.y) : 1.0;
    float bodyAmp = enthusiasm * bodyVariation * quietMultiplier;
    float headAmp = enthusiasm * headVariation * quietMultiplier;

    float3 handDelta = hand - baseShoulder;
    float3 handDir = SafeNormalize(float3(handDelta.x, 0.0, handDelta.z), float3(0.0, 0.0, 1.0));
    float bodyAxis = Hash11(seed + 257.0) * 2.0 * PRISM_FANLIGHT_PI;
    float3 bodySideDir = float3(cos(bodyAxis), 0.0, sin(bodyAxis));

    float3 bodyOffset = float3(0.0, 0.0, 0.0);
    bodyOffset += bodySideDir * (sin(bodyPhase) * _AudienceMotionBody.y * bodyAmp);
    bodyOffset += float3(0.0, PrismAudienceSlowPulse(bodyPhase + 0.4) * _AudienceMotionBody.x * bodyAmp, 0.0);

    float3 feet = anchor + bodyOffset;
    float neckY = max(shoulderHeight, bodyHeight - headHalf * 2.0);
    float3 neckLocal = feet + float3(0.0, neckY, 0.0);

    float reachOver = max(0.0, length(handDelta) - maxReach);
    float reach01 = saturate(reachOver / maxReach);
    float reachFollow = reach01 * _AudienceReach.y * saturate(_AudienceReach.x);
    float followDistance = min(_AudienceShoulder.x, reachFollow) * saturate(_AudienceMotionBody.w);
    float3 shoulderFollow = handDir * followDistance;

    float shoulderPulse = PrismAudienceSlowPulse(bodyPhase + 1.1) * _AudienceMotionHead.x * bodyAmp;
    float3 shoulder = baseShoulder + bodyOffset + shoulderFollow + float3(0.0, shoulderPulse, 0.0);

    float3 headOffset = float3(0.0, 0.0, 0.0);
    headOffset += bodySideDir * (sin(headPhase) * _AudienceMotionHead.z * headAmp);
    headOffset += float3(0.0, PrismAudienceSlowPulse(headPhase + 0.8) * _AudienceMotionHead.y * headAmp, 0.0);
    headOffset -= bodyOffset * saturate(_AudienceMotionHead.w) * 0.35;

    float3 headCenterLocal = feet + float3(0.0, neckY + headHalf, 0.0) + headOffset;

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
