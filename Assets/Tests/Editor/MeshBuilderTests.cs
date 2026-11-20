using NUnit.Framework;

namespace BeybladeCore.Tests
{
    public class MeshBuilderTests
    {
        static readonly string[] Layers = { "layer_basic", "layer_ejderha", "layer_kale", "layer_ruzgar", "layer_yildirim" };
        static readonly string[] Disks = { "disk_basic", "disk_agir", "disk_hafif" };
        static readonly string[] Tips = { "tip_basic", "tip_duz", "tip_top", "tip_igne" };

        static void AssertValid(MeshData m, string name)
        {
            Assert.Greater(m.VertexCount, 0, name);
            Assert.AreEqual(m.VertexCount, m.Normals.Count, name);
            for (int s = 0; s < MeshData.SlotCount; s++)
            {
                Assert.AreEqual(0, m.Triangles[s].Count % 3, name + " slot " + s);
                foreach (int i in m.Triangles[s]) Assert.That(i, Is.InRange(0, m.VertexCount - 1), name);
            }
            foreach (Vec3 v in m.Vertices)
                Assert.IsFalse(float.IsNaN(v.X) || float.IsNaN(v.Y) || float.IsNaN(v.Z), name + " NaN");
            foreach (Vec3 n in m.Normals)
                Assert.AreEqual(1f, n.Length, 1e-3f, name + " normal");
            Assert.Less(m.VertexCount, 65000, name + " (16-bit index sınırı)");
        }

        [Test]
        public void AllParts_ProduceValidMeshes()
        {
            foreach (string id in Layers) AssertValid(BeyMeshBuilder.BuildLayer(BeyDesignCatalog.Layer(id)).Mesh, id);
            foreach (string id in Disks) AssertValid(BeyMeshBuilder.BuildDisk(BeyDesignCatalog.Disk(id)).Mesh, id);
            foreach (string id in Tips) AssertValid(BeyMeshBuilder.BuildTip(BeyDesignCatalog.Tip(id)).Mesh, id);
            AssertValid(BeyMeshBuilder.BuildStadium().Mesh, "stadium");
        }

        [Test]
        public void Parts_StackInTheRightOrder()
        {
            foreach (string id in Layers)
            {
                BeyMeshBuilder.BuildLayer(BeyDesignCatalog.Layer(id)).Mesh.GetBounds(out Vec3 min, out Vec3 max);
                Assert.GreaterOrEqual(min.Y, -1e-3f, id + " layer diskin üstünde başlar");
                Assert.LessOrEqual(System.Math.Max(max.X, max.Z), 1.01f, id + " dış yarıçap 1");
            }
            foreach (string id in Disks)
            {
                BeyMeshBuilder.BuildDisk(BeyDesignCatalog.Disk(id)).Mesh.GetBounds(out Vec3 min, out Vec3 max);
                Assert.LessOrEqual(max.Y, 0f, id + " disk layer'ın altında");
            }
            foreach (string id in Tips)
            {
                TipVisual v = BeyDesignCatalog.Tip(id);
                BeyMeshBuilder.BuildTip(v).Mesh.GetBounds(out Vec3 min, out Vec3 max);
                Assert.AreEqual(BeyMeshBuilder.TipContactY(v.Shape), min.Y, 1e-3f, id + " temas noktası en alt nokta");
            }
        }

        [Test]
        public void TopSurfaces_FaceUp()
        {
            // Layer'ın üst yüzündeki üçgenlerin çoğu yukarı bakmalı (yön/sarma kontrolü)
            MeshData m = BeyMeshBuilder.BuildLayer(BeyDesignCatalog.Layer("layer_ejderha")).Mesh;
            int up = 0, total = 0;
            var tris = m.Triangles[(int)MatSlot.Primary];
            for (int i = 0; i < tris.Count; i += 3)
            {
                Vec3 a = m.Vertices[tris[i]], b = m.Vertices[tris[i + 1]], c = m.Vertices[tris[i + 2]];
                Vec3 n = Vec3.Cross(b - a, c - a);
                if (n.Length < 1e-8f) continue;
                total++;
                if (n.Y > 0f) up++;
            }
            Assert.Greater(up, total * 0.6f, "ana renkli yüzeyler çoğunlukla yukarı bakar");
        }

        [Test]
        public void EveryPartInCatalog_HasADesign()
        {
            // Mağazadaki her parça kimliği bir görünüme eşlenir (varsayılana düşmez)
            Assert.AreEqual(LayerShape.Claw, BeyDesignCatalog.Layer("layer_ejderha").Shape);
            Assert.AreEqual(LayerShape.Shield, BeyDesignCatalog.Layer("layer_kale").Shape);
            Assert.AreEqual(LayerShape.Wing, BeyDesignCatalog.Layer("layer_ruzgar").Shape);
            Assert.AreEqual(LayerShape.Bolt, BeyDesignCatalog.Layer("layer_yildirim").Shape);
            Assert.AreEqual(DiskShape.Heavy, BeyDesignCatalog.Disk("disk_agir").Shape);
            Assert.AreEqual(TipShape.Needle, BeyDesignCatalog.Tip("tip_igne").Shape);
        }

        [Test]
        public void StepApi_MatchesRun()
        {
            var setup = new SimSetup
            {
                A = ArchetypePresets.Get(BeyArchetype.Attack), B = ArchetypePresets.Get(BeyArchetype.Stamina),
                PowerA = 0.85f, PowerB = 0.85f, BrainA = BotDifficulty.ForLevel(6), BrainB = BotDifficulty.ForLevel(6),
                Seed = 77, MaxTime = 120f
            };
            SimResult full = new BattleSim(setup).Run();
            var stepped = new BattleSim(setup);
            int guard = 0;
            while (!stepped.IsOver && guard++ < 100000) stepped.StepOnce();
            Assert.AreEqual(full.Duration, stepped.Time, 1e-3f);
            Assert.AreEqual(full.Collisions, stepped.Collisions);
        }
    }
}
