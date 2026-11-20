using System;
using System.Globalization;
using GunPowdered.Core;

namespace GunPowdered.Features.Grenades.Domain
{
    /// <summary>A position or velocity for the flight maths, without Unity types (the domain stays game-free; the adapter converts).</summary>
    internal readonly struct ArcVector
    {
        internal ArcVector(float x, float y, float z) { X = x; Y = y; Z = z; }

        internal float X { get; }
        internal float Y { get; }
        internal float Z { get; }

        internal float Length => (float)Math.Sqrt((X * X) + (Y * Y) + (Z * Z));

        /// <summary>Distance on the ground plane (y ignored).</summary>
        internal float FlatDistanceTo(ArcVector other)
        {
            if (RuleLog.On) RuleLog.Every("ArcVector.FlatDistanceTo", 2d, $"ArcVector.FlatDistanceTo(other={other})");
            float dx = other.X - X, dz = other.Z - Z;
            { var returnedAt23 = (float)Math.Sqrt((dx * dx) + (dz * dz)); if (RuleLog.On) RuleLog.Every("ArcVector.FlatDistanceTo:23", 2d, $"ArcVector.FlatDistanceTo: returns {returnedAt23} (line 23)"); return returnedAt23; }
        }

        internal float DistanceTo(ArcVector other) => (other - this).Length;

        public static ArcVector operator +(ArcVector a, ArcVector b) => new ArcVector(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static ArcVector operator -(ArcVector a, ArcVector b) => new ArcVector(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static ArcVector operator *(ArcVector a, float f) => new ArcVector(a.X * f, a.Y * f, a.Z * f);

        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "({0:0.00}, {1:0.00}, {2:0.00})", X, Y, Z);
    }
}
