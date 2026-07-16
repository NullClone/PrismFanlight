using System;
using System.IO;
using NUnit.Framework;
using PrismFanlight.Authoring;
using PrismFanlight.Editor;
using Unity.Mathematics;
using UnityEngine;

namespace PrismFanlight.Tests
{
    public sealed class FanlightLayoutBakeTests
    {
        [Test]
        public void BlockPlacement_RebuildsOnlyTargetRange_AndPreservesStableIds()
        {
            var layout = CreateLayout();
            var before = new FanlightCompiledLayout(layout);
            var beforeSeats = (FanlightBakedSeatRecord[])before.Seats.Clone();

            Assert.That(layout.SetBlockPlacement(1, new FanlightBlockPlacement
            {
                position = new Vector3(2f, 1f, -3f),
                eulerRotation = new Vector3(0f, 35f, 0f)
            }), Is.True);
            before.CompileBlock(1);

            var blockSeatCount = layout.BlockSeatCount;
            for (var i = 0; i < before.Seats.Length; i++)
            {
                Assert.That(before.Seats[i].stableSeatId, Is.EqualTo(beforeSeats[i].stableSeatId));
                if (i < blockSeatCount) Assert.That(before.Seats[i].localPosition, Is.EqualTo(beforeSeats[i].localPosition));
                else Assert.That(before.Seats[i].localPosition, Is.Not.EqualTo(beforeSeats[i].localPosition));
            }
        }

        [Test]
        public void BinaryArtifact_RoundTripsDeterministically()
        {
            var layout = CreateLayout();
            var compiled = new FanlightCompiledLayout(layout);
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pflayoutbake");
            try
            {
                FanlightLayoutBakeFile.Write(path, compiled);
                var data = FanlightLayoutBakeFile.Read(path);
                Assert.That(data.LayoutId, Is.EqualTo(layout.LayoutId.Value));
                Assert.That(data.SourceLayoutVersion, Is.EqualTo(layout.LayoutVersion));
                Assert.That(data.ContentHash, Is.EqualTo(compiled.ContentHash));
                Assert.That(data.Seats.Length, Is.EqualTo(layout.TotalSeatCount));
                Assert.That(data.Blocks.Length, Is.EqualTo(layout.TotalBlockCount));
                for (var i = 0; i < data.Seats.Length; i++)
                {
                    Assert.That(data.Seats[i].stableSeatId, Is.EqualTo(layout.GetStableSeatId(i)));
                }
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
            }
        }

        [Test]
        public void IncrementalHash_MatchesFullRebuild()
        {
            var layout = CreateLayout();
            var session = FanlightLayoutEditSession.Get(layout);
            Assert.That(session, Is.Not.Null);
            Assert.That(session.SetBlockPlacement(1, new FanlightBlockPlacement
            {
                position = new Vector3(1f, 2f, 3f),
                eulerRotation = new Vector3(5f, 15f, 25f)
            }, "Test Layout Edit"), Is.True);

            var rebuilt = new FanlightCompiledLayout(layout);
            Assert.That(session.RuntimeLayout.ContentHash, Is.EqualTo(rebuilt.ContentHash));
        }

        [Test]
        public void Initialize_RejectsDuplicateStableSeatIds()
        {
            var layout = ScriptableObject.CreateInstance<FanlightLayoutAsset>();
            Assert.Throws<ArgumentException>(() => layout.Initialize(
                "00112233445566778899aabbccddeeff",
                math.int2(2, 1),
                math.float2(1f, 1f),
                math.int2(1, 1),
                float2.zero,
                new[] { "11112222333344445555666677778888" },
                new[] { 1UL, 1UL }));
        }

        [Test]
        public void Topology_CannotBeInitializedTwice()
        {
            var layout = CreateLayout();
            Assert.Throws<InvalidOperationException>(() => layout.Initialize(
                Guid.NewGuid().ToString("N"),
                math.int2(1, 1),
                math.float2(1f, 1f),
                math.int2(1, 1),
                float2.zero,
                new[] { Guid.NewGuid().ToString("N") },
                new[] { 1UL }));
        }

        private static FanlightLayoutAsset CreateLayout()
        {
            var layout = ScriptableObject.CreateInstance<FanlightLayoutAsset>();
            layout.Initialize(
                "00112233445566778899aabbccddeeff",
                math.int2(2, 2),
                math.float2(0.5f, 0.75f),
                math.int2(2, 1),
                math.float2(1f, 1f),
                new[]
                {
                    "11112222333344445555666677778888",
                    "9999aaaabbbbccccddddeeeeffff0000"
                },
                new[] { 10UL, 11UL, 12UL, 13UL, 20UL, 21UL, 22UL, 23UL });
            return layout;
        }
    }
}
