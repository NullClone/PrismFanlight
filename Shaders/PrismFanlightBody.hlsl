float3 PrismRotateAroundAxis(float3 v, float3 axis, float angle)
{
    float s = sin(angle);
    float c = cos(angle);
    return v * c + cross(axis, v) * s + axis * dot(axis, v) * (1.0 - c);
}

float3 PrismSolveElbow(float3 shoulder, float3 hand, float l1, float l2, float3 pole)
{
    float3 toHand = hand - shoulder;
    float d = length(toHand);
    float3 dir = d > 1e-5 ? toHand / d : float3(0.0, 1.0, 0.0);

    if (d >= l1 + l2)
    {
        return shoulder + dir * l1;
    }

    float dClamped = max(d, abs(l1 - l2) + 1e-4);
    float cosA = clamp((dClamped * dClamped + l1 * l1 - l2 * l2) / (2.0 * l1 * dClamped), -1.0, 1.0);
    float a = acos(cosA);
    float3 bendAxis = SafeNormalize(cross(dir, pole), float3(1.0, 0.0, 0.0));
    float3 upperDir = PrismRotateAroundAxis(dir, bendAxis, a);
    return shoulder + upperDir * l1;
}

FanlightBodyPart PrismMakeBodyPart(float3 p0, float3 p1, float halfWidth, float type)
{
    FanlightBodyPart part;
    part.p0HalfWidth = float4(p0, halfWidth);
    part.p1Type = float4(p1, type);
    return part;
}

void PrismBuildBodyParts(uint seatId)
{
    FanlightSeatData seat = _Seats[seatId];
    PrismArm arm = PrismComputeArm(seat);
    float seed = seat.localPositionSeed.w;

    float heightJitter = (Hash11(seed + 211.0) * 2.0 - 1.0) * _BodyShape.y;
    float bodyHeight = max(0.1, _BodyShape.x * (1.0 + heightJitter));
    float shoulderHeight = bodyHeight * saturate(_BodyShape.z);
    float bodyHalfWidth = _BodyShape.w;

    float l1 = _BodyArm.x;
    float l2 = _BodyArm.y;
    float armHalfWidth = _BodyArm.z;
    float shoulderOffset = _BodyArm.w;

    float3 feet = float3(arm.baseLocal.x, 0.0, arm.baseLocal.z);
    float3 hand = arm.handLocal;
    float3 shoulder = feet + float3(shoulderOffset, shoulderHeight, 0.0);

    float3 toHand = hand - shoulder;
    float d = length(toHand);
    float reach = l1 + l2;
    float over = max(0.0, d - reach);
    float lean = min(over * saturate(_BodyReach.x), _BodyReach.y);
    float3 horiz = SafeNormalize(float3(toHand.x, 0.0, toHand.z), float3(0.0, 0.0, 1.0));
    float3 shift = horiz * lean;
    feet += shift;
    shoulder += shift;

    float3 pole = SafeNormalize(float3(sign(shoulderOffset) * _BodyReach.z, -1.0, 0.0), float3(0.0, -1.0, 0.0));
    float3 elbow = PrismSolveElbow(shoulder, hand, l1, l2, pole);

    float scale = _BodyReach.w;
    float3 feetW = mul(_LocalToWorld, float4(feet, 1.0)).xyz;
    float3 headW = mul(_LocalToWorld, float4(feet + float3(0.0, bodyHeight, 0.0), 1.0)).xyz;
    float3 shoulderW = mul(_LocalToWorld, float4(shoulder, 1.0)).xyz;
    float3 elbowW = mul(_LocalToWorld, float4(elbow, 1.0)).xyz;
    float3 handW = mul(_LocalToWorld, float4(hand, 1.0)).xyz;

    uint b = seatId * 3u;
    _BodyParts[b + 0u] = PrismMakeBodyPart(feetW, headW, bodyHalfWidth * scale, 0.0);
    _BodyParts[b + 1u] = PrismMakeBodyPart(shoulderW, elbowW, armHalfWidth * scale, 1.0);
    _BodyParts[b + 2u] = PrismMakeBodyPart(elbowW, handW, armHalfWidth * scale, 1.0);
}
