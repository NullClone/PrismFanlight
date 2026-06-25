using System;
using Unity.Mathematics;
using UnityEngine;

namespace PrismFanlight
{
    // 観客 1 人の見た目（体・頭・腕のビルボード）の設定。
    // 手の位置はペンライトのモーション計算（PrismComputeArm）から取得し、
    // 腕は「肩 → 手」を結ぶ 1 本のリボンとして描く（肘の IK は持たない）。
    // 手が肩から maxReach 以上離れると、体が水平方向へ寄って（lean）追従する。
    [Serializable]
    public struct FanlightAudienceSettings
    {
        public bool enabled;

        [Min(0.1f)]
        public float bodyHeight;

        [Range(0f, 1f)]
        public float bodyHeightJitter;

        [Min(0.01f)]
        public float bodyWidth;

        [Min(0.01f)]
        public float headSize;

        [Range(0f, 1f)]
        public float shoulderHeight;

        [Range(-1f, 1f)]
        public float shoulderOffset;

        [Min(0.01f)]
        public float armWidth;

        [Min(0.01f)]
        public float maxReach;

        [Range(0f, 1f)]
        public float leanFactor;

        [Min(0f)]
        public float leanMax;


        public static FanlightAudienceSettings Default() => new()
        {
            enabled = true,
            bodyHeight = 1.5f,
            bodyHeightJitter = 0.08f,
            bodyWidth = 0.55f,
            headSize = 0.28f,
            shoulderHeight = 0.82f,
            shoulderOffset = 0.16f,
            armWidth = 0.14f,
            maxReach = 0.55f,
            leanFactor = 0.5f,
            leanMax = 0.4f
        };

        public FanlightAudienceSettings Validated() => new()
        {
            enabled = enabled,
            bodyHeight = math.max(0.1f, bodyHeight),
            bodyHeightJitter = math.saturate(bodyHeightJitter),
            bodyWidth = math.max(0.01f, bodyWidth),
            // headSize / maxReach は旧 Body 設定からの移行時に 0 で読み込まれうるため、
            // 未設定（0 以下）のときは既定値で埋めて見た目が壊れないようにする。
            headSize = headSize > 0f ? math.max(0.01f, headSize) : 0.28f,
            shoulderHeight = math.saturate(shoulderHeight),
            shoulderOffset = math.clamp(shoulderOffset, -1f, 1f),
            armWidth = math.max(0.01f, armWidth),
            maxReach = maxReach > 0f ? math.max(0.01f, maxReach) : 0.55f,
            leanFactor = math.saturate(leanFactor),
            leanMax = math.max(0f, leanMax)
        };
    }
}
