﻿//------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, 2008
//
//  File: CircleEase.cs
//------------------------------------------------------------------------------

namespace System.Windows.Media.Animation
{
    /// <summary>
    ///     This class implements an easing function that gives a circular curve toward the destination.
    /// </summary>
    public class CircleEase : EasingFunctionBase
    {
        protected override double EaseInCore(double normalizedTime)
        {
            normalizedTime = Math.Max(0.0, Math.Min(1.0, normalizedTime));
            return 1.0 - Math.Sqrt(1.0 - normalizedTime * normalizedTime);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new CircleEase();
        }
    }
}
