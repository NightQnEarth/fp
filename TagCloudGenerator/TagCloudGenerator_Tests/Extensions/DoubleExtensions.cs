﻿using System;

namespace TagCloudGenerator_Tests.Extensions
{
    public static class DoubleExtensions
    {
        public static bool IsApproximatelyEqual(this double number, double otherNumber, double precision) =>
            Math.Abs(number - otherNumber) < precision;
    }
}