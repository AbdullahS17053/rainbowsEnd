using NUnit.Framework;
using RainbowsEnd.Gameplay.Unicorn;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Unicorn
{
    public sealed class UnicornTailTrailTests
    {
        [Test]
        public void ResolveTailAnchor_FindsTailBone_WhenPresent()
        {
            GameObject root = new GameObject("UnicornRoot");
            try
            {
                GameObject visualRoot = new GameObject("VisualRoot");
                visualRoot.transform.SetParent(root.transform);

                GameObject horseRoot = new GameObject("HorseRoot");
                horseRoot.transform.SetParent(visualRoot.transform);

                GameObject tailBone = new GameObject("Horse_Rig_Tail_TopSHJnt");
                tailBone.transform.SetParent(horseRoot.transform);

                UnicornTailTrail trail = root.AddComponent<UnicornTailTrail>();
                Transform resolvedAnchor = trail.ResolveTailAnchor();

                Assert.That(resolvedAnchor, Is.EqualTo(tailBone.transform));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ResolveTailAnchor_CreatesFallbackAnchor_WhenNoTailBonePresent()
        {
            GameObject root = new GameObject("UnicornRoot");
            try
            {
                UnicornTailTrail trail = root.AddComponent<UnicornTailTrail>();
                Transform resolvedAnchor = trail.ResolveTailAnchor();

                Assert.That(resolvedAnchor, Is.Not.Null);
                Assert.That(resolvedAnchor.name, Is.EqualTo("TailAnchor"));
                Assert.That(resolvedAnchor.parent, Is.EqualTo(root.transform));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ResolveParticleSystem_ConfiguresUpwardMotionAndTrails()
        {
            GameObject root = new GameObject("UnicornRoot");
            try
            {
                UnicornTailTrail trail = root.AddComponent<UnicornTailTrail>();
                ParticleSystem ps = trail.ResolveParticleSystem();

                Assert.That(ps, Is.Not.Null);
                Assert.That(trail.TailParticleSystem, Is.EqualTo(ps));

                var main = ps.main;
                Assert.That(main.simulationSpace, Is.EqualTo(ParticleSystemSimulationSpace.World));

                var vel = ps.velocityOverLifetime;
                Assert.That(vel.enabled, Is.True);
                Assert.That(vel.space, Is.EqualTo(ParticleSystemSimulationSpace.World));
                Assert.That(vel.y.constant, Is.GreaterThan(0f));

                var trails = ps.trails;
                Assert.That(trails.enabled, Is.True);
                Assert.That(trails.ratio, Is.EqualTo(1f));
                Assert.That(trails.worldSpace, Is.True);

                ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
                Assert.That(renderer, Is.Not.Null);
                Assert.That(renderer.sortingOrder, Is.EqualTo(trail.SortingOrder));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SetTrailShader_AppliesShaderToMaterial()
        {
            GameObject root = new GameObject("UnicornRoot");
            try
            {
                UnicornTailTrail trail = root.AddComponent<UnicornTailTrail>();
                trail.ResolveParticleSystem();

                Shader testShader = Shader.Find("Sprites/Default");
                Assert.That(testShader, Is.Not.Null);

                trail.SetTrailShader(testShader);

                Assert.That(trail.TrailMaterial, Is.Not.Null);
                Assert.That(trail.TrailMaterial.shader, Is.EqualTo(testShader));
                Assert.That(trail.TrailShader, Is.EqualTo(testShader));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ClearTrail_DoesNotThrow_WhenCalled()
        {
            GameObject root = new GameObject("UnicornRoot");
            try
            {
                UnicornTailTrail trail = root.AddComponent<UnicornTailTrail>();
                trail.ResolveParticleSystem();

                Assert.DoesNotThrow(() => trail.ClearTrail());
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
