using NUnit.Framework;
using PrismFanlight.Timeline;
using UnityEngine;

namespace PrismFanlight.Tests
{
    public sealed class FanlightWeightedAngleTests
    {
        // Fields

        private const float Tolerance = 1e-3f;


        // Methods

        [Test]
        public void ValueDegrees_AcrossZero_UsesShortestArc()
        {
            var angle = new FanlightWeightedAngle();
            angle.AddDegrees(350f, 1f);
            angle.AddDegrees(10f, 1f);

            Assert.That(Mathf.Abs(Mathf.DeltaAngle(angle.ValueDegrees(180f), 0f)), Is.LessThan(Tolerance));
        }

        [Test]
        public void ValueDegrees_WeightedTowardsHeavierInput()
        {
            var angle = new FanlightWeightedAngle();
            angle.AddDegrees(0f, 3f);
            angle.AddDegrees(90f, 1f);

            Assert.That(angle.ValueDegrees(180f), Is.EqualTo(22.5f).Within(Tolerance));
        }

        [Test]
        public void ValueDegrees_NegativeInput_IsNormalized()
        {
            var angle = new FanlightWeightedAngle();
            angle.AddDegrees(-90f, 1f);

            Assert.That(angle.ValueDegrees(0f), Is.EqualTo(270f).Within(Tolerance));
        }

        [Test]
        public void ValueDegrees_WithoutWeight_ReturnsFallback()
        {
            var angle = new FanlightWeightedAngle();
            angle.AddDegrees(90f, 0f);

            Assert.That(angle.ValueDegrees(45f), Is.EqualTo(45f));
        }
    }
}
