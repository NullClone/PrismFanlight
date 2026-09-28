using NUnit.Framework;
using PrismFanlight.Core;
using PrismFanlight.Live;
using PrismFanlight.Rendering;

namespace PrismFanlight.Tests
{
    public sealed class FanlightStatusMonitorTests
    {
        // Fields

        private FanlightStatusMonitor _monitor;


        // Methods

        [SetUp]
        public void SetUp()
        {
            _monitor = new FanlightStatusMonitor();
        }

        [Test]
        public void Update_WithoutFaults_ReturnsEmptyFaults()
        {
            var status = Update(FanlightShowTimeFault.None, string.Empty);

            Assert.That(status.HasFault, Is.False);
            Assert.That(status.TimeFault, Is.Empty);
            Assert.That(status.SequenceFault, Is.Empty);
            Assert.That(status.RendererFault, Is.Empty);
            Assert.That(status.LiveFault, Is.Empty);
        }

        [Test]
        public void Update_WithFaults_ReturnsFaultText()
        {
            var status = _monitor.Update(
                true,
                true,
                true,
                FanlightShowTimeFault.TempoConflict,
                "Conflict",
                FanlightRendererFault.InvalidLayout,
                "Look: Invalid",
                null);

            Assert.That(status.HasFault, Is.True);
            Assert.That(status.IsRendering, Is.True);
            Assert.That(status.IsHolding, Is.True);
            Assert.That(status.IsTimeFallbackActive, Is.True);
            Assert.That(status.TimeFault, Is.EqualTo(nameof(FanlightShowTimeFault.TempoConflict)));
            Assert.That(status.SequenceFault, Is.EqualTo("Conflict"));
            Assert.That(status.RendererFault, Is.EqualTo(nameof(FanlightRendererFault.InvalidLayout)));
            Assert.That(status.LiveFault, Is.EqualTo("Look: Invalid"));
        }

        [Test]
        public void Update_UnchangedFault_ReusesFaultText()
        {
            var first = Update(FanlightShowTimeFault.TempoConflict, string.Empty);
            var second = Update(FanlightShowTimeFault.TempoConflict, string.Empty);

            Assert.That(second.TimeFault, Is.SameAs(first.TimeFault));
        }

        [Test]
        public void Update_ClearedFault_ReturnsEmptyFault()
        {
            Update(FanlightShowTimeFault.TempoConflict, "Conflict");

            var status = Update(FanlightShowTimeFault.None, string.Empty);

            Assert.That(status.HasFault, Is.False);
            Assert.That(status.TimeFault, Is.Empty);
            Assert.That(status.SequenceFault, Is.Empty);
        }

        [Test]
        public void Update_NullSequenceFault_IsTreatedAsNoFault()
        {
            var status = Update(FanlightShowTimeFault.None, null);

            Assert.That(status.SequenceFault, Is.Empty);
            Assert.That(status.HasFault, Is.False);
        }

        [Test]
        public void DefaultStatus_ReturnsEmptyFaults()
        {
            var status = default(FanlightLiveStatus);

            Assert.That(status.HasFault, Is.False);
            Assert.That(status.IsRendering, Is.False);
            Assert.That(status.TimeFault, Is.Empty);
            Assert.That(status.LiveFault, Is.Empty);
        }


        private FanlightLiveStatus Update(FanlightShowTimeFault timeFault, string sequenceFault)
        {
            return _monitor.Update(
                false,
                false,
                false,
                timeFault,
                sequenceFault,
                FanlightRendererFault.None,
                string.Empty,
                null);
        }
    }
}
