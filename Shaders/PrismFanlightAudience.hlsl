// 観客 1 人を 3 パーツ（体・腕・頭）に分解して _AudienceParts へ書き出す。
// 肘の IK は持たず、腕は「肩 → 手」を直線で結ぶだけ。手が肩から maxReach
// 以上離れると体が水平に寄って（lean）追従する。
//
//   _AudienceShape = (bodyHeight, heightJitter, shoulderHeightRatio, bodyHalfWidth)
//   _AudienceArm   = (armHalfWidth, shoulderOffset, headHalfSize, maxReach)
//   _AudienceReach = (leanFactor, leanMax, worldScale, _)

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
    float maxReach = _AudienceArm.w;

    // 足元（席位置）と肩。手はペンライトのモーションが決めた位置。
    float3 feet = float3(arm.baseLocal.x, 0.0, arm.baseLocal.z);
    float3 hand = arm.handLocal;
    float3 shoulder = feet + float3(shoulderOffset, shoulderHeight, 0.0);

    // 手が遠いと体ごと水平に寄せる（lean）。
    float3 toHand = hand - shoulder;
    float d = length(toHand);
    float over = max(0.0, d - maxReach);
    float lean = min(over * saturate(_AudienceReach.x), _AudienceReach.y);
    float3 horiz = SafeNormalize(float3(toHand.x, 0.0, toHand.z), float3(0.0, 0.0, 1.0));
    float3 shift = horiz * lean;
    feet += shift;
    shoulder += shift;

    // 体（torso）は足元〜首まで。首から上を頭ビルボードが占める。
    float neckY = max(shoulderHeight, bodyHeight - headHalf * 2.0);
    float3 headCenterLocal = feet + float3(0.0, neckY + headHalf, 0.0);

    float scale = _AudienceReach.z;
    float3 feetW = mul(_LocalToWorld, float4(feet, 1.0)).xyz;
    float3 neckW = mul(_LocalToWorld, float4(feet + float3(0.0, neckY, 0.0), 1.0)).xyz;
    float3 shoulderW = mul(_LocalToWorld, float4(shoulder, 1.0)).xyz;
    float3 handW = mul(_LocalToWorld, float4(hand, 1.0)).xyz;
    float3 headW = mul(_LocalToWorld, float4(headCenterLocal, 1.0)).xyz;

    uint b = seatId * 3u;
    _AudienceParts[b + 0u] = PrismMakeAudiencePart(feetW, neckW, bodyHalfWidth * scale, 0.0); // 体リボン
    _AudienceParts[b + 1u] = PrismMakeAudiencePart(shoulderW, handW, armHalfWidth * scale, 1.0); // 腕リボン
    _AudienceParts[b + 2u] = PrismMakeAudiencePart(headW, headW, headHalf * scale, 2.0); // 頭ビルボード
}
