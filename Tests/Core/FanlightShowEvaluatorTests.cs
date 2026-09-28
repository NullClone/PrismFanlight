using System;
using NUnit.Framework;
using PrismFanlight.Core;
using static PrismFanlight.Tests.FanlightTestInputs;

namespace PrismFanlight.Tests
{
    public sealed class FanlightShowEvaluatorTests
    {
        // Fields

        private const float Tolerance = 1e-5f;

        private FanlightShowEvaluator _evaluator;


        // Methods

        [SetUp]
        public void SetUp()
        {
            _evaluator = new FanlightShowEvaluator();
        }

        [Test]
        public void Evaluate_WithoutContributions_ReturnsBaseline()
        {
            var sample = Evaluate(BaseState(energy: 0.3f), 1d);

            Assert.That(sample.State.Intent.Energy, Is.EqualTo(0.3f).Within(Tolerance));
        }

        [Test]
        public void Evaluate_HigherTrackPriority_IsAppliedLast()
        {
            var context = new FanlightSequenceContext(null);

            var sample = Evaluate(
                BaseState(),
                1d,
                Contribution(context, 1, 0, IntentPatch(FanlightIntentFields.Energy, 0.8f)),
                Contribution(context, 0, 1, IntentPatch(FanlightIntentFields.Energy, 0.2f)));

            Assert.That(sample.State.Intent.Energy, Is.EqualTo(0.8f).Within(Tolerance));
        }

        [Test]
        public void Evaluate_SameTrackPriority_UsesTrackOrder()
        {
            var context = new FanlightSequenceContext(null);

            var sample = Evaluate(
                BaseState(),
                1d,
                Contribution(context, 0, 1, IntentPatch(FanlightIntentFields.Energy, 0.8f)),
                Contribution(context, 0, 0, IntentPatch(FanlightIntentFields.Energy, 0.2f)));

            Assert.That(sample.State.Intent.Energy, Is.EqualTo(0.8f).Within(Tolerance));
        }

        [Test]
        public void Evaluate_IsIndependentOfRegistrationOrder()
        {
            var context = new FanlightSequenceContext(null);
            var lower = Contribution(context, 0, 0, IntentPatch(FanlightIntentFields.Energy, 0.2f), 0.5f);
            var upper = Contribution(context, 0, 1, IntentPatch(FanlightIntentFields.Energy, 0.8f), 0.5f);

            var forward = Evaluate(BaseState(), 1d, lower, upper);
            var reversed = Evaluate(BaseState(), 1d, upper, lower);

            Assert.That(reversed.State.Intent.Energy, Is.EqualTo(forward.State.Intent.Energy));
        }

        [Test]
        public void Evaluate_PartialWeight_BlendsFromBaseline()
        {
            var context = new FanlightSequenceContext(null);

            var sample = Evaluate(
                BaseState(energy: 1f),
                1d,
                Contribution(context, 0, 0, IntentPatch(FanlightIntentFields.Energy, 0f), 0.25f));

            Assert.That(sample.State.Intent.Energy, Is.EqualTo(0.75f).Within(Tolerance));
        }

        [Test]
        public void Evaluate_FieldsOutsideMask_KeepBaseline()
        {
            var context = new FanlightSequenceContext(null);

            var sample = Evaluate(
                BaseState(participation: 0.5f),
                1d,
                Contribution(context, 0, 0, IntentPatch(FanlightIntentFields.Energy, 0.2f, participation: 0.9f)));

            Assert.That(sample.State.Intent.Energy, Is.EqualTo(0.2f).Within(Tolerance));
            Assert.That(sample.State.Intent.Participation, Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void Evaluate_ContributionEnd_IsExclusive()
        {
            var context = new FanlightSequenceContext(null);
            var contribution = Contribution(context, 0, 0, IntentPatch(FanlightIntentFields.Energy, 0.2f), 1f, 0d, 1d);

            var inside = Evaluate(BaseState(energy: 1f), 0.5d, contribution);
            var atEnd = Evaluate(BaseState(energy: 1f), 1d, contribution);

            Assert.That(inside.State.Intent.Energy, Is.EqualTo(0.2f).Within(Tolerance));
            Assert.That(atEnd.State.Intent.Energy, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Evaluate_ZeroWeight_IsIgnored()
        {
            var context = new FanlightSequenceContext(null);
            var other = new FanlightSequenceContext(null);

            var sample = Evaluate(
                BaseState(energy: 1f),
                1d,
                Contribution(context, 0, 0, IntentPatch(FanlightIntentFields.Energy, 0.2f)),
                Contribution(other, 0, 0, IntentPatch(FanlightIntentFields.Energy, 0.9f), 0f));

            Assert.That(sample.State.Intent.Energy, Is.EqualTo(0.2f).Within(Tolerance));
        }

        [Test]
        public void Evaluate_UnrelatedSequencesOwningSameField_Throws()
        {
            var first = new FanlightSequenceContext(null);
            var second = new FanlightSequenceContext(null);

            Assert.Throws<InvalidOperationException>(() => Evaluate(
                BaseState(),
                1d,
                Contribution(first, 0, 0, IntentPatch(FanlightIntentFields.Energy, 0.2f)),
                Contribution(second, 0, 0, IntentPatch(FanlightIntentFields.Energy, 0.8f))));
        }

        [Test]
        public void Evaluate_UnrelatedSequencesOwningDifferentFields_Combine()
        {
            var first = new FanlightSequenceContext(null);
            var second = new FanlightSequenceContext(null);

            var sample = Evaluate(
                BaseState(),
                1d,
                Contribution(first, 0, 0, IntentPatch(FanlightIntentFields.Energy, 0.2f)),
                Contribution(second, 0, 0, IntentPatch(FanlightIntentFields.Participation, 0f, participation: 0.9f)));

            Assert.That(sample.State.Intent.Energy, Is.EqualTo(0.2f).Within(Tolerance));
            Assert.That(sample.State.Intent.Participation, Is.EqualTo(0.9f).Within(Tolerance));
        }

        [Test]
        public void Evaluate_AncestorSequence_OwnsSharedField()
        {
            var parent = new FanlightSequenceContext(null);
            var child = new FanlightSequenceContext(parent);

            var sample = Evaluate(
                BaseState(),
                1d,
                Contribution(parent, 0, 0, IntentPatch(FanlightIntentFields.Energy, 0.2f)),
                Contribution(child, 10, 0, IntentPatch(FanlightIntentFields.Energy, 0.8f)));

            Assert.That(sample.State.Intent.Energy, Is.EqualTo(0.2f).Within(Tolerance));
        }

        [Test]
        public void Evaluate_PartialParentAndChildOnSameField_Throws()
        {
            var parent = new FanlightSequenceContext(null);
            var child = new FanlightSequenceContext(parent);

            Assert.Throws<InvalidOperationException>(() => Evaluate(
                BaseState(),
                1d,
                Contribution(parent, 0, 0, IntentPatch(FanlightIntentFields.Energy, 0.2f), 0.5f),
                Contribution(child, 0, 0, IntentPatch(FanlightIntentFields.Energy, 0.8f))));
        }

        [Test]
        public void Evaluate_DuplicateTrackOrderInSequence_Throws()
        {
            var context = new FanlightSequenceContext(null);

            Assert.Throws<InvalidOperationException>(() => Evaluate(
                BaseState(),
                1d,
                Contribution(context, 0, 0, IntentPatch(FanlightIntentFields.Energy, 0.2f)),
                Contribution(context, 1, 0, IntentPatch(FanlightIntentFields.Participation, 0f, participation: 0.9f))));
        }

        [Test]
        public void Evaluate_ReleasedSequenceContext_Throws()
        {
            var context = new FanlightSequenceContext(null);
            var contribution = Contribution(context, 0, 0, IntentPatch(FanlightIntentFields.Energy, 0.2f));

            context.Release();

            Assert.Throws<InvalidOperationException>(() => Evaluate(BaseState(), 1d, contribution));
        }

        [Test]
        public void Contribution_WithReleasedSequenceContext_Throws()
        {
            var context = new FanlightSequenceContext(null);
            context.Release();

            Assert.Throws<ArgumentException>(() => Contribution(context, 0, 0, IntentPatch(FanlightIntentFields.Energy, 0.2f)));
        }

        [Test]
        public void Evaluate_IncompleteTime_Throws()
        {
            var request = new FanlightShowEvaluationRequest(default, BaseState(), ReadOnlyMemory<FanlightShowContribution>.Empty, FanlightEvaluationOptions.Default);

            Assert.Throws<ArgumentException>(() => _evaluator.Evaluate(request));
        }

        [Test]
        public void Evaluate_AnimationSampleRate_QuantizesAnimationSeconds()
        {
            var time = AtSeconds(1.0199d);

            var quantized = _evaluator.Evaluate(new FanlightShowEvaluationRequest(
                time,
                BaseState(),
                ReadOnlyMemory<FanlightShowContribution>.Empty,
                new FanlightEvaluationOptions(60d, 1e-6d)));
            var continuous = _evaluator.Evaluate(new FanlightShowEvaluationRequest(
                time,
                BaseState(),
                ReadOnlyMemory<FanlightShowContribution>.Empty,
                new FanlightEvaluationOptions(0d, 1e-6d)));

            Assert.That(quantized.AnimationSampleSeconds, Is.EqualTo(61d / 60d).Within(1e-9d));
            Assert.That(quantized.ShowSeconds, Is.EqualTo(time.Seconds));
            Assert.That(continuous.AnimationSampleSeconds, Is.EqualTo(time.Seconds));
        }


        private FanlightShowSample Evaluate(FanlightShowState baseState, double seconds, params FanlightShowContribution[] contributions)
        {
            var request = new FanlightShowEvaluationRequest(
                AtSeconds(seconds),
                baseState,
                contributions,
                FanlightEvaluationOptions.Default);

            return _evaluator.Evaluate(request);
        }
    }
}
