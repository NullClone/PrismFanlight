using NUnit.Framework;
using PrismFanlight.Core;
using PrismFanlight.Time;
using static PrismFanlight.Tests.FanlightTestInputs;

namespace PrismFanlight.Tests
{
    public sealed class FanlightTempoScopeResolverTests
    {
        // Fields

        private const double Tolerance = 1e-9d;


        // Methods

        [Test]
        public void TryResolve_WithoutCandidates_UsesFallbackTempo()
        {
            var resolver = new FanlightTempoScopeResolver(120d, 4, 4, 0d);

            Assert.That(resolver.TryResolve(Clock(2.5d), default, out var sample, out var fault), Is.True);
            Assert.That(fault, Is.EqualTo(FanlightShowTimeFault.None));
            Assert.That(sample.MusicalPosition.Beat, Is.EqualTo(5d).Within(Tolerance));
            Assert.That(sample.MusicalPosition.Bar, Is.EqualTo(2L));
            Assert.That(sample.MusicalPosition.BeatInBar, Is.EqualTo(1d).Within(Tolerance));
            Assert.That(sample.MusicalPosition.BeatPhase, Is.EqualTo(0d).Within(Tolerance));
            Assert.That(sample.IsComplete, Is.True);
        }

        [Test]
        public void TryResolve_BeforeMusicalOrigin_ReturnsNegativeBeat()
        {
            var resolver = new FanlightTempoScopeResolver(120d, 4, 4, 1d);

            Assert.That(resolver.TryResolve(Clock(0.5d), default, out var sample, out _), Is.True);
            Assert.That(sample.MusicalPosition.Beat, Is.EqualTo(-1d).Within(Tolerance));
            Assert.That(sample.MusicalPosition.Bar, Is.EqualTo(0L));
            Assert.That(sample.MusicalPosition.BeatInBar, Is.EqualTo(3d).Within(Tolerance));
        }

        [Test]
        public void TryResolve_TempoSections_AreContinuous()
        {
            var resolver = new FanlightTempoScopeResolver(120d, 4, 4, 0d);
            var definition = TwoSectionDefinition(8d);

            Assert.That(resolver.TryResolve(Clock(4d), new[] { new FanlightTempoCandidate(4d, definition) }, out var boundary, out _), Is.True);
            Assert.That(boundary.MusicalPosition.Beat, Is.EqualTo(8d).Within(Tolerance));
            Assert.That(boundary.MusicalPosition.Bar, Is.EqualTo(3L));
            Assert.That(boundary.MusicalPosition.BeatInBar, Is.EqualTo(0d).Within(Tolerance));

            Assert.That(resolver.TryResolve(Clock(5d), new[] { new FanlightTempoCandidate(5d, definition) }, out var after, out _), Is.True);
            Assert.That(after.MusicalPosition.Beat, Is.EqualTo(9d).Within(Tolerance));
            Assert.That(after.MusicalPosition.Bar, Is.EqualTo(3L));
            Assert.That(after.MusicalPosition.BeatInBar, Is.EqualTo(1d).Within(Tolerance));
            Assert.That(after.MusicalPosition.Bpm, Is.EqualTo(60d));
        }

        [Test]
        public void TryResolve_DiscontinuousSections_ReturnsInvalidTempoDefinition()
        {
            var resolver = new FanlightTempoScopeResolver(120d, 4, 4, 0d);
            var definition = TwoSectionDefinition(7d);

            Assert.That(resolver.TryResolve(Clock(5d), new[] { new FanlightTempoCandidate(5d, definition) }, out _, out var fault), Is.False);
            Assert.That(fault, Is.EqualTo(FanlightShowTimeFault.InvalidTempoDefinition));
        }

        [Test]
        public void TryResolve_FiniteLastSection_ReturnsInvalidTempoDefinition()
        {
            var resolver = new FanlightTempoScopeResolver(120d, 4, 4, 0d);
            var definition = new FanlightTempoRuntimeDefinition(new[]
            {
                new FanlightTempoSection(0d, 10d, 0d, 1L, 0d, 120d, 4, 4)
            });

            Assert.That(resolver.TryResolve(Clock(1d), new[] { new FanlightTempoCandidate(1d, definition) }, out _, out var fault), Is.False);
            Assert.That(fault, Is.EqualTo(FanlightShowTimeFault.InvalidTempoDefinition));
        }

        [Test]
        public void TryResolve_MultipleCandidates_ReturnsTempoConflict()
        {
            var resolver = new FanlightTempoScopeResolver(120d, 4, 4, 0d);
            var definition = TwoSectionDefinition(8d);
            var candidates = new[]
            {
                new FanlightTempoCandidate(1d, definition),
                new FanlightTempoCandidate(1d, definition)
            };

            Assert.That(resolver.TryResolve(Clock(1d), candidates, out _, out var fault), Is.False);
            Assert.That(fault, Is.EqualTo(FanlightShowTimeFault.TempoConflict));
        }

        [Test]
        public void TryResolve_NonFiniteClock_ReturnsInvalidPrimarySample()
        {
            var resolver = new FanlightTempoScopeResolver(120d, 4, 4, 0d);

            Assert.That(resolver.TryResolve(Clock(double.NaN), default, out _, out var fault), Is.False);
            Assert.That(fault, Is.EqualTo(FanlightShowTimeFault.InvalidPrimarySample));
        }

        [Test]
        public void TryResolve_ReadyClockWithZeroRate_ReturnsInvalidPrimarySample()
        {
            var resolver = new FanlightTempoScopeResolver(120d, 4, 4, 0d);

            Assert.That(resolver.TryResolve(Clock(1d, 0d), default, out _, out var fault), Is.False);
            Assert.That(fault, Is.EqualTo(FanlightShowTimeFault.InvalidPrimarySample));
        }

        [Test]
        public void TryResolve_HoldingClock_Succeeds()
        {
            var resolver = new FanlightTempoScopeResolver(120d, 4, 4, 0d);

            Assert.That(resolver.TryResolve(Clock(1d, 0d, FanlightClockStatus.Holding), default, out var sample, out _), Is.True);
            Assert.That(sample.Status, Is.EqualTo(FanlightClockStatus.Holding));
            Assert.That(sample.IsComplete, Is.True);
        }

        [Test]
        public void TryResolve_InvalidFallbackTempo_ReturnsInvalidTempoDefinition()
        {
            var resolver = new FanlightTempoScopeResolver(0d, 4, 4, 0d);

            Assert.That(resolver.TryResolve(Clock(1d), default, out _, out var fault), Is.False);
            Assert.That(fault, Is.EqualTo(FanlightShowTimeFault.InvalidTempoDefinition));
        }


        private static FanlightTempoRuntimeDefinition TwoSectionDefinition(double secondStartBeat)
        {
            return new FanlightTempoRuntimeDefinition(new[]
            {
                new FanlightTempoSection(0d, 4d, 0d, 1L, 0d, 120d, 4, 4),
                new FanlightTempoSection(4d, double.PositiveInfinity, secondStartBeat, 3L, 0d, 60d, 4, 4)
            });
        }
    }
}
