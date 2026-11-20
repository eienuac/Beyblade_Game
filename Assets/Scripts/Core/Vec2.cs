using System;

namespace BeybladeCore
{
    /// <summary>Arena düzleminde (XZ) 2B vektör. Unity'ye bağımlı olmadan savaş hesapları için.</summary>
    public struct Vec2
    {
        public float X;
        public float Z;

        public Vec2(float x, float z) { X = x; Z = z; }

        public static readonly Vec2 Zero = new Vec2(0f, 0f);

        public float SqrLength => X * X + Z * Z;
        public float Length => (float)Math.Sqrt(X * X + Z * Z);

        public Vec2 Normalized
        {
            get
            {
                float l = Length;
                return l > 1e-5f ? new Vec2(X / l, Z / l) : Zero;
            }
        }

        /// <summary>Saat yönünün tersine 90° (yukarıdan bakınca). Unity'deki Cross(dir, up) ile aynı yön.</summary>
        public Vec2 Perp => new Vec2(-Z, X);

        public static float Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Z * b.Z;

        public Vec2 ClampLength(float max)
        {
            float l = Length;
            return l > max && l > 1e-5f ? this * (max / l) : this;
        }

        public static Vec2 Lerp(Vec2 a, Vec2 b, float t) => a + (b - a) * t;

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Z + b.Z);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Z - b.Z);
        public static Vec2 operator -(Vec2 a) => new Vec2(-a.X, -a.Z);
        public static Vec2 operator *(Vec2 a, float s) => new Vec2(a.X * s, a.Z * s);
        public static Vec2 operator *(float s, Vec2 a) => new Vec2(a.X * s, a.Z * s);
        public static Vec2 operator /(Vec2 a, float s) => new Vec2(a.X / s, a.Z / s);

        public override string ToString() => $"({X:0.00}, {Z:0.00})";
    }
}
