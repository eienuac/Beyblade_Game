using System;
using System.Collections.Generic;

namespace BeybladeCore
{
    /// <summary>3B vektör (Unity'den bağımsız; Unity'nin sol-el koordinatlarıyla aynı: y yukarı).</summary>
    public struct Vec3
    {
        public float X, Y, Z;
        public Vec3(float x, float y, float z) { X = x; Y = y; Z = z; }

        public static readonly Vec3 Zero = new Vec3(0f, 0f, 0f);
        public static readonly Vec3 Up = new Vec3(0f, 1f, 0f);
        public static readonly Vec3 Down = new Vec3(0f, -1f, 0f);

        public float Length => (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        public Vec3 Normalized
        {
            get
            {
                float l = Length;
                return l > 1e-6f ? new Vec3(X / l, Y / l, Z / l) : Up;
            }
        }

        public static Vec3 Cross(Vec3 a, Vec3 b) => new Vec3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        public static float Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator -(Vec3 a) => new Vec3(-a.X, -a.Y, -a.Z);
        public static Vec3 operator *(Vec3 a, float s) => new Vec3(a.X * s, a.Y * s, a.Z * s);
        public override string ToString() => $"({X:0.00}, {Y:0.00}, {Z:0.00})";
    }

    /// <summary>0..1 aralığında renk.</summary>
    public struct RGBA
    {
        public float R, G, B, A;
        public RGBA(float r, float g, float b, float a = 1f) { R = r; G = g; B = b; A = a; }

        /// <summary>"#RRGGBB" → renk.</summary>
        public static RGBA Hex(string hex)
        {
            hex = hex.TrimStart('#');
            int v = Convert.ToInt32(hex, 16);
            return new RGBA(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f);
        }

        public RGBA Mul(float k) => new RGBA(R * k, G * k, B * k, A);
    }

    /// <summary>Malzeme yuvaları: her yuva Unity'de ayrı submesh/malzeme olur.</summary>
    public enum MatSlot { Primary = 0, Secondary = 1, Metal = 2, Accent = 3, Dark = 4 }

    /// <summary>
    /// Basit mesh: köşe, normal ve malzeme yuvası başına üçgen listeleri.
    /// Üçgen yönü Unity kuralı: dışarıdan bakınca saat yönü (Cross(b-a, c-a) dışa bakar).
    /// </summary>
    public class MeshData
    {
        public const int SlotCount = 5;

        public readonly List<Vec3> Vertices = new List<Vec3>();
        public readonly List<Vec3> Normals = new List<Vec3>();
        public readonly List<int>[] Triangles;

        public MeshData()
        {
            Triangles = new List<int>[SlotCount];
            for (int i = 0; i < SlotCount; i++) Triangles[i] = new List<int>();
        }

        public int VertexCount => Vertices.Count;

        public int TriangleCount
        {
            get
            {
                int n = 0;
                foreach (List<int> t in Triangles) n += t.Count;
                return n / 3;
            }
        }

        public int Vertex(Vec3 p, Vec3 n)
        {
            Vertices.Add(p);
            Normals.Add(n.Normalized);
            return Vertices.Count - 1;
        }

        /// <summary>Üçgeni, yüzü 'outward' yönüne bakacak şekilde ekler (gerekirse sırayı çevirir).</summary>
        public void Tri(MatSlot slot, int a, int b, int c, Vec3 outward)
        {
            Vec3 n = Vec3.Cross(Vertices[b] - Vertices[a], Vertices[c] - Vertices[a]);
            List<int> list = Triangles[(int)slot];
            if (Vec3.Dot(n, outward) >= 0f) { list.Add(a); list.Add(b); list.Add(c); }
            else { list.Add(a); list.Add(c); list.Add(b); }
        }

        /// <summary>a-b-c-d dörtgeni (çevre sırasıyla).</summary>
        public void Quad(MatSlot slot, int a, int b, int c, int d, Vec3 outward)
        {
            Tri(slot, a, b, c, outward);
            Tri(slot, a, c, d, outward);
        }

        /// <summary>Başka bir mesh'i kaydırıp ekler; slotMap verilirse yuvalar yeniden eşlenir.</summary>
        public void Append(MeshData other, Vec3 offset, float scale = 1f)
        {
            int baseIndex = Vertices.Count;
            for (int i = 0; i < other.Vertices.Count; i++)
            {
                Vertices.Add(other.Vertices[i] * scale + offset);
                Normals.Add(other.Normals[i]);
            }
            for (int s = 0; s < SlotCount; s++)
                foreach (int idx in other.Triangles[s]) Triangles[s].Add(idx + baseIndex);
        }

        public void GetBounds(out Vec3 min, out Vec3 max)
        {
            min = new Vec3(float.MaxValue, float.MaxValue, float.MaxValue);
            max = new Vec3(float.MinValue, float.MinValue, float.MinValue);
            foreach (Vec3 v in Vertices)
            {
                if (v.X < min.X) min.X = v.X; if (v.Y < min.Y) min.Y = v.Y; if (v.Z < min.Z) min.Z = v.Z;
                if (v.X > max.X) max.X = v.X; if (v.Y > max.Y) max.Y = v.Y; if (v.Z > max.Z) max.Z = v.Z;
            }
        }
    }
}
