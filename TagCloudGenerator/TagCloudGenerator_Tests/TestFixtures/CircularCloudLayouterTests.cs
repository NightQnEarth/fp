﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using TagCloudGenerator.GeneratorCore.CloudLayouters;
using TagCloudGenerator_Tests.Extensions;
using TagCloudGenerator_Tests.WrongVisualization;

namespace TagCloudGenerator_Tests.TestFixtures
{
    [TestFixture]
    public class CircularCloudLayouterTests
    {
        private const double Precision = 0.7072; // sqrt(2)/2.
        private static readonly Point origin = Point.Empty;
        private static Size VisualizationImageSize => new Size(1000, 800);

        private static Point ImageCenter => new Point(VisualizationImageSize.Width / 2,
                                                      VisualizationImageSize.Height / 2);

        private WrongVisualizationCloud wrongVisualizationCloud;
        private CircularCloudLayouter circularCloudLayouter;

        [SetUp]
        public void SetUp()
        {
            circularCloudLayouter = new CircularCloudLayouter(ImageCenter);
            wrongVisualizationCloud = null;
        }

        [TearDown]
        public void TearDown()
        {
            const string failedTestsDirectoryName = "FailedVisualizationTests";

            if (TestContext.CurrentContext.Result.Outcome.Status is TestStatus.Failed &&
                wrongVisualizationCloud != null)
                WrongVisualizationSaver.SaveAndGetPathToWrongVisualization(wrongVisualizationCloud,
                                                                           VisualizationImageSize,
                                                                           failedTestsDirectoryName);
        }

        [Test]
        [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
        public void CircularCloudLayouterConstructor_GetCenterPoint() =>
            Assert.DoesNotThrow(() => new CircularCloudLayouter(new Point(10, 20)));

        [Test]
        public void PutNextRectangle_OnZeroSize_ThrowArgumentException()
        {
            var rectangleResult = circularCloudLayouter.PutNextRectangle(new Size(0, 0));

            rectangleResult.IsSuccess.Should().BeFalse();
            rectangleResult.Error.Should().Be("Was passed empty rectangle size.");
        }

        [TestCase(12, 8, TestName = "WhenEvenWidthAndHeight")]
        [TestCase(100, 5555, TestName = "WhenEvenWidthAndOddHeight")]
        [TestCase(1, 1, TestName = "WhenOddWidthAndHeight")]
        public void PutNextRectangle_OnFirstSize_ReturnsRectangleWithCenterInTheOrigin(int width, int height)
        {
            circularCloudLayouter = new CircularCloudLayouter(origin);
            var firstRectangle = circularCloudLayouter.PutNextRectangle(new Size(width, height));

            firstRectangle.Error.Should().BeNull();

            wrongVisualizationCloud = new WrongVisualizationCloud(
                TestsHelper.BackgroundColor,
                TestsHelper.TagStyleByTagType,
                (firstRectangle.Value, new Rectangle(origin, new Size(1, 1))));

            firstRectangle.Value.CheckIfPointIsCenterOfRectangle(origin, Precision).Should().BeTrue();
        }

        [TestCase(0, 0, TestName = "WhenOriginAsCenter")]
        [TestCase(11, 57, TestName = "WhenCenterWithDifferentCoordinates")]
        [TestCase(250, 250, TestName = "WhenCenterWithSameCoordinates")]
        public void PutNextRectangle_OnFirstSize_ReturnsRectangleWithCenterInSpecifiedPoint(int xCenter, int yCenter)
        {
            var center = new Point(xCenter, yCenter);
            var firstRectangle = new CircularCloudLayouter(center).PutNextRectangle(new Size(100, 50));

            firstRectangle.Error.Should().BeNull();

            wrongVisualizationCloud = new WrongVisualizationCloud(
                TestsHelper.BackgroundColor,
                TestsHelper.TagStyleByTagType,
                (firstRectangle.Value, new Rectangle(center, new Size(1, 1))));

            firstRectangle.Value.CheckIfPointIsCenterOfRectangle(center, Precision).Should().BeTrue();
        }

        [Test]
        public void PutNextRectangle_OnSecondSize_ReturnsNotIntersectedRectangle()
        {
            var firstRectangle = circularCloudLayouter.PutNextRectangle(new Size(10, 5));
            var secondRectangle = circularCloudLayouter.PutNextRectangle(new Size(7, 3));

            TestsHelper.HandleErrors(error => error.Should().BeNull(), firstRectangle, secondRectangle);

            wrongVisualizationCloud = new WrongVisualizationCloud(TestsHelper.BackgroundColor,
                                                                  TestsHelper.TagStyleByTagType,
                                                                  (firstRectangle.Value, secondRectangle.Value));

            firstRectangle.Value.IntersectsWith(secondRectangle.Value).Should().BeFalse();
        }

        [Test]
        public void PutNextRectangle_OnALotOfCalls_ReturnsNotIntersectedRectangles()
        {
            var randomizer = TestContext.CurrentContext.Random;

            var rectangleResults = Enumerable.Range(0, 60)
                .Select(_ => circularCloudLayouter.PutNextRectangle(
                            new Size(randomizer.Next(50, 100), randomizer.Next(50, 100))));

            var rectangles = TestsHelper.SelectValues(rectangleResults).ToArray();

            var intersectingRectangles = TestsHelper.GetAnyPairOfIntersectingRectangles(rectangles);

            if (intersectingRectangles.HasValue)
                wrongVisualizationCloud = new WrongVisualizationCloud(TestsHelper.BackgroundColor,
                                                                      TestsHelper.TagStyleByTagType,
                                                                      intersectingRectangles.Value, rectangles);

            intersectingRectangles.Should().BeNull();
        }

        [Test]
        public void PutNextRectangle_OnFirstSize_ReturnsRectangleWithSpecifiedSize([Random(1, 1000, 1)] int width,
                                                                                   [Random(1, 1000, 1)] int height)
        {
            var specifiedSize = new Size(width, height);
            var firstRectangle = circularCloudLayouter.PutNextRectangle(specifiedSize);

            firstRectangle.Error.Should().BeNull();

            firstRectangle.Value.Size.Should().Be(specifiedSize);
        }

        [Test]
        public void PutNextRectangle_OnALotOfCallsWithRandomSizes_ReturnsRectanglesWithSpecifiedSizes()
        {
            var randomizer = TestContext.CurrentContext.Random;

            var inputSizes = Enumerable.Range(0, 500)
                .Select(i => new Size(randomizer.Next(1, 500), randomizer.Next(1, 500)))
                .ToArray();

            var rectangleResults = inputSizes.Select(size => circularCloudLayouter.PutNextRectangle(size));
            var rectangles = TestsHelper.SelectValues(rectangleResults);

            rectangles.Select(rectangle => rectangle.Size).Should().Equal(inputSizes);
        }

        [Test]
        [Repeat(100)]
        public void PutNextRectangle_OnALotOfCallsWithRandomSize_ReturnsDenseRoundlyCloud()
        {
            var randomizer = TestContext.CurrentContext.Random;
            var rectangleResults = Enumerable.Range(0, randomizer.Next(50, 100))
                .Select(i => new Size(randomizer.Next(40, 100), randomizer.Next(40, 80)))
                .Select(size => circularCloudLayouter.PutNextRectangle(size));

            var rectangles = TestsHelper.SelectValues(rectangleResults).ToArray();

            wrongVisualizationCloud = new WrongVisualizationCloud(TestsHelper.BackgroundColor,
                                                                  TestsHelper.TagStyleByTagType,
                                                                  rectangles);

            var maxRadius = rectangles.Max(rectangle => ImageCenter.GetDistanceToPoint(rectangle.GetRectangleCenter()));
            var maxArea = maxRadius * maxRadius * Math.PI;
            var cloudArea = rectangles.Sum(rectangle => rectangle.Height * rectangle.Width);

            (cloudArea / maxArea).Should().BeGreaterOrEqualTo(0.65);
        }
    }
}