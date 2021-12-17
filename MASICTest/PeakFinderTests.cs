using System;
using System.Collections.Generic;
using MASICPeakFinder;
using NUnit.Framework;

namespace MASICTest
{
    [TestFixture]
    public class PeakFinderTests
    {
        private clsMASICPeakFinder mMASICPeakFinder;

        [OneTimeSetUp]
        public void Setup()
        {
            mMASICPeakFinder = new clsMASICPeakFinder();
        }

        [Test]
        public void TestFindMinimumPositiveValue()
        {
            const int ABSOLUTE_MINIMUM_VALUE = 4;

            var sicData = new List<SICDataPoint>();
            var values = new List<double>();

            var minimumPositiveValueNoData = mMASICPeakFinder.FindMinimumPositiveValue(sicData, ABSOLUTE_MINIMUM_VALUE);

            Assert.AreEqual(ABSOLUTE_MINIMUM_VALUE, minimumPositiveValueNoData);

            for (var i = 1; i <= 10; i++)
            {
                var intensity = i - 3;
                var sicPoint = new SICDataPoint(i, intensity, i * 100);
                sicData.Add(sicPoint);
                values.Add(intensity);

                var sicMinimumPositiveValue = mMASICPeakFinder.FindMinimumPositiveValue(sicData, ABSOLUTE_MINIMUM_VALUE);
                var doubleMinimumPositiveValue = mMASICPeakFinder.FindMinimumPositiveValue(values, ABSOLUTE_MINIMUM_VALUE);

                Assert.AreEqual(ABSOLUTE_MINIMUM_VALUE, sicMinimumPositiveValue);
                Assert.AreEqual(ABSOLUTE_MINIMUM_VALUE, doubleMinimumPositiveValue);
            }

            Console.WriteLine();

            // Step through 10 more points, but now limit sicData and values to just 10 points
            for (var i = 11; i <= 20; i++)
            {
                var intensity = i - 3;
                var sicPoint = new SICDataPoint(i, intensity, i * 100);
                sicData.Add(sicPoint);
                values.Add(intensity);

                while (sicData.Count > 10)
                {
                    sicData.RemoveAt(0);
                    values.RemoveAt(0);
                }

                var sicMinimumPositiveValue = mMASICPeakFinder.FindMinimumPositiveValue(sicData, ABSOLUTE_MINIMUM_VALUE);
                var doubleMinimumPositiveValue = mMASICPeakFinder.FindMinimumPositiveValue(values, ABSOLUTE_MINIMUM_VALUE);

                if (i < 17)
                {
                    Assert.AreEqual(ABSOLUTE_MINIMUM_VALUE, sicMinimumPositiveValue);
                    Assert.AreEqual(ABSOLUTE_MINIMUM_VALUE, doubleMinimumPositiveValue);
                }
                else
                {
                    Assert.AreEqual(i - 12, sicMinimumPositiveValue);
                    Assert.AreEqual(i - 12, doubleMinimumPositiveValue);
                }
            }

            // Call the overloaded variant that accepts the number of data points
            values.Clear();
            for (var i = 1; i <= 20; i++)
            {
                var intensity = i - 3.5;
                values.Add(intensity);
            }

            for (var i = 0; i < 20; i++)
            {
                var dataCount = i + 1;
                var minimumPositiveValue = mMASICPeakFinder.FindMinimumPositiveValue(dataCount, values, ABSOLUTE_MINIMUM_VALUE);

                if (i >= 10)
                {
                    values.RemoveAt(0);
                }

                if (i < 17)
                {
                    Assert.AreEqual(ABSOLUTE_MINIMUM_VALUE, minimumPositiveValue);
                }
                else
                {
                    Assert.AreEqual(i - 12.5, minimumPositiveValue);
                }
            }
        }
    }
}
