using NUnit.Framework;
using PrismFanlight.Core;
using PrismFanlight.Live;
using UnityEngine;
using static PrismFanlight.Tests.FanlightTestInputs;

namespace PrismFanlight.Tests
{
    public sealed class FanlightLiveLookQueueTests
    {
        // Fields

        private FanlightLiveLook _first;
        private FanlightLiveLook _second;
        private FanlightLiveLookQueue _queue;


        // Methods

        [SetUp]
        public void SetUp()
        {
            _first = ScriptableObject.CreateInstance<FanlightLiveLook>();
            _second = ScriptableObject.CreateInstance<FanlightLiveLook>();
            _queue = new FanlightLiveLookQueue();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_first);
            Object.DestroyImmediate(_second);
        }

        [Test]
        public void TryTakeDue_Immediate_IsDueAtNextEvaluation()
        {
            _queue.Request(_first, FanlightLiveQuantize.Immediate);

            Assert.That(_queue.TryTakeDue(AtBeat(2.3d), out var look), Is.True);
            Assert.That(look, Is.SameAs(_first));
            Assert.That(_queue.PendingLook, Is.Null);
        }

        [Test]
        public void TryTakeDue_Beat_WaitsForNextBeat()
        {
            _queue.Request(_first, FanlightLiveQuantize.Beat);

            Assert.That(_queue.TryTakeDue(AtBeat(2.3d), out _), Is.False);
            Assert.That(_queue.TryTakeDue(AtBeat(2.9d), out _), Is.False);
            Assert.That(_queue.TryTakeDue(AtBeat(3d), out var look), Is.True);
            Assert.That(look, Is.SameAs(_first));
        }

        [Test]
        public void TryTakeDue_BeatRequestedOnBoundary_IsDueImmediately()
        {
            _queue.Request(_first, FanlightLiveQuantize.Beat);

            Assert.That(_queue.TryTakeDue(AtBeat(3d), out _), Is.True);
        }

        [Test]
        public void TryTakeDue_Bar_WaitsForNextBar()
        {
            _queue.Request(_first, FanlightLiveQuantize.Bar);

            Assert.That(_queue.TryTakeDue(AtBeat(5d), out _), Is.False);
            Assert.That(_queue.TryTakeDue(AtBeat(7.99d), out _), Is.False);
            Assert.That(_queue.TryTakeDue(AtBeat(8d), out _), Is.True);
        }

        [Test]
        public void TryTakeDue_BarWithThreeBeats_UsesBeatsPerBar()
        {
            _queue.Request(_first, FanlightLiveQuantize.Bar);

            Assert.That(_queue.TryTakeDue(AtBeat(4d, 3), out _), Is.False);
            Assert.That(_queue.TryTakeDue(AtBeat(6d, 3), out _), Is.True);
        }

        [Test]
        public void TryTakeDue_BarRequestedOnBoundary_IsDueImmediately()
        {
            _queue.Request(_first, FanlightLiveQuantize.Bar);

            Assert.That(_queue.TryTakeDue(AtBeat(8d), out _), Is.True);
        }

        [Test]
        public void Request_LaterRequest_ReplacesPendingLook()
        {
            _queue.Request(_first, FanlightLiveQuantize.Bar);
            _queue.Request(_second, FanlightLiveQuantize.Immediate);

            Assert.That(_queue.TryTakeDue(AtBeat(5d), out var look), Is.True);
            Assert.That(look, Is.SameAs(_second));
        }

        [Test]
        public void TryTakeDue_BackwardBeat_ResolvesTargetAgain()
        {
            _queue.Request(_first, FanlightLiveQuantize.Beat);

            Assert.That(_queue.TryTakeDue(AtBeat(2.3d), out _), Is.False);
            Assert.That(_queue.TryTakeDue(AtBeat(0.5d, discontinuity: FanlightTimeDiscontinuity.Seek), out _), Is.False);
            Assert.That(_queue.TryTakeDue(AtBeat(1d), out _), Is.True);
        }

        [Test]
        public void TryTakeDue_ForwardDiscontinuity_KeepsTarget()
        {
            _queue.Request(_first, FanlightLiveQuantize.Beat);

            Assert.That(_queue.TryTakeDue(AtBeat(2.3d), out _), Is.False);
            Assert.That(_queue.TryTakeDue(AtBeat(5.5d, discontinuity: FanlightTimeDiscontinuity.Seek), out _), Is.True);
        }

        [Test]
        public void Clear_DropsPendingLook()
        {
            _queue.Request(_first, FanlightLiveQuantize.Immediate);
            _queue.Clear();

            Assert.That(_queue.PendingLook, Is.Null);
            Assert.That(_queue.TryTakeDue(AtBeat(1d), out _), Is.False);
        }
    }
}
