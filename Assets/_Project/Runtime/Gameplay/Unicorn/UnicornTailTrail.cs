using System;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Unicorn
{
    [DisallowMultipleComponent]
    public sealed class UnicornTailTrail : MonoBehaviour
    {
        [Header("Trail Visuals")]
        [Tooltip("The Particle System generating the upward tail trail.")]
        [SerializeField] private ParticleSystem tailParticleSystem;

        [Tooltip("Transform representing the unicorn's tail bone/anchor where the trail emits from.")]
        [SerializeField] private Transform tailAnchor;

        [Header("Material & Shader Configuration")]
        [Tooltip("Assign the shader of the trail here in the inspector. It will automatically be assigned to the trail material.")]
        [SerializeField] private Shader trailShader;

        [Tooltip("The material used for the particle system and trail. You can also assign the shader directly in this material's inspector.")]
        [SerializeField] private Material trailMaterial;

        [Header("Upward Motion & Trail Settings")]
        [Tooltip("Speed at which the trail particles drift upwards in world space (+Y axis).")]
        [SerializeField, Min(0f)] private float upwardSpeed = 1.8f;

        [Tooltip("Lifetime of each trail segment/particle in seconds.")]
        [SerializeField, Min(0.1f)] private float lifetime = 1.2f;

        [Tooltip("Emission rate of trail particles per second.")]
        [SerializeField, Min(1f)] private float emissionRate = 30f;

        [Tooltip("Initial width of the trail at the tail anchor.")]
        [SerializeField, Min(0.01f)] private float startWidth = 0.35f;

        [Tooltip("Width of the trail at the end of its lifetime as it fades upwards.")]
        [SerializeField, Min(0f)] private float endWidth = 0.02f;

        [Header("Rendering")]
        [Tooltip("Sorting order for the trail renderer to appear properly layered in 2D.")]
        [SerializeField] private int sortingOrder = 15;

        [SerializeField] private string sortingLayerName = "Default";

        [Header("Tail Bone Emission (Disabled in favor of ground trail)")]
        [Tooltip("Whether the unicorn body tail bone emits particles. Default is false.")]
        [SerializeField] private bool emitFromTail = false;

        public ParticleSystem TailParticleSystem => tailParticleSystem;
        public Transform TailAnchor => tailAnchor;
        public Material TrailMaterial => trailMaterial;
        public Shader TrailShader => trailShader;
        public float UpwardSpeed => upwardSpeed;
        public float Lifetime => lifetime;
        public float EmissionRate => emissionRate;
        public float StartWidth => startWidth;
        public float EndWidth => endWidth;
        public int SortingOrder => sortingOrder;
        public bool EmitFromTail => emitFromTail;

        public void Configure(Transform anchor, ParticleSystem particleSystem, Material material, Shader shader = null)
        {
            tailAnchor = anchor;
            tailParticleSystem = particleSystem;
            trailMaterial = material;
            trailShader = shader;

            if (trailShader != null && trailMaterial != null)
            {
                trailMaterial.shader = trailShader;
            }

            if (tailParticleSystem != null)
            {
                ConfigureParticleSystem(tailParticleSystem);
            }
        }

        public void SetTrailShader(Shader shader)
        {
            trailShader = shader;
            ApplyShaderToMaterial();
        }

        public void SetTrailMaterial(Material material)
        {
            trailMaterial = material;
            if (tailParticleSystem != null && tailParticleSystem.TryGetComponent(out ParticleSystemRenderer renderer))
            {
                renderer.trailMaterial = trailMaterial;
                renderer.material = trailMaterial;
            }
        }

        public void ClearTrail()
        {
            if (tailParticleSystem != null)
            {
                tailParticleSystem.Clear();
            }
        }

        private void Reset()
        {
            ResolveTailAnchor();
            ResolveParticleSystem();
        }

        private void OnValidate()
        {
            if (trailShader != null)
            {
                ApplyShaderToMaterial();
            }
            else if (trailMaterial != null && tailParticleSystem != null && tailParticleSystem.TryGetComponent(out ParticleSystemRenderer renderer))
            {
                renderer.trailMaterial = trailMaterial;
                renderer.material = trailMaterial;
                renderer.sortingOrder = sortingOrder;
                if (!string.IsNullOrEmpty(sortingLayerName))
                {
                    renderer.sortingLayerName = sortingLayerName;
                }
            }

            if (tailParticleSystem != null)
            {
                var main = tailParticleSystem.main;
                main.startLifetime = lifetime;
                main.startSize = startWidth;

                var emission = tailParticleSystem.emission;
                emission.rateOverTime = emissionRate;

                var vel = tailParticleSystem.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.World;
                vel.y = new ParticleSystem.MinMaxCurve(upwardSpeed);
            }
        }

        private void Awake()
        {
            ResolveTailAnchor();
            ResolveParticleSystem();

            if (Application.isPlaying && !emitFromTail && tailParticleSystem != null)
            {
                var emission = tailParticleSystem.emission;
                emission.enabled = false;
                tailParticleSystem.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (trailShader != null && trailMaterial != null && trailMaterial.shader != trailShader)
            {
                ApplyShaderToMaterial();
            }
        }

        public Transform ResolveTailAnchor()
        {
            if (tailAnchor != null)
            {
                return tailAnchor;
            }

            string[] candidateNames =
            {
                "Horse_Rig_Tail_TopSHJnt",
                "Horse_Rig_Tail_02SHJnt",
                "Horse_Rig_Tail_01SHJnt"
            };

            for (int i = 0; i < candidateNames.Length; i++)
            {
                Transform found = FindRecursive(transform, candidateNames[i]);
                if (found != null)
                {
                    tailAnchor = found;
                    return tailAnchor;
                }
            }

            Transform tailFuzzy = FindTailFuzzy(transform);
            if (tailFuzzy != null)
            {
                tailAnchor = tailFuzzy;
                return tailAnchor;
            }

            Transform existingAnchor = transform.Find("TailAnchor");
            if (existingAnchor != null)
            {
                tailAnchor = existingAnchor;
                return tailAnchor;
            }

            GameObject newAnchor = new GameObject("TailAnchor");
            newAnchor.transform.SetParent(transform, false);
            newAnchor.transform.localPosition = new Vector3(0f, -0.6f, 0f);
            tailAnchor = newAnchor.transform;
            return tailAnchor;
        }

        public ParticleSystem ResolveParticleSystem()
        {
            if (tailParticleSystem != null)
            {
                ConfigureParticleSystem(tailParticleSystem);
                return tailParticleSystem;
            }

            Transform anchor = ResolveTailAnchor();
            Transform psTransform = anchor.Find("TailTrail");
            if (psTransform == null)
            {
                psTransform = transform.Find("TailTrail");
            }

            if (psTransform != null && psTransform.TryGetComponent(out ParticleSystem existingPs))
            {
                tailParticleSystem = existingPs;
                ConfigureParticleSystem(tailParticleSystem);
                return tailParticleSystem;
            }

            GameObject trailObject = new GameObject("TailTrail");
            trailObject.transform.SetParent(anchor, false);
            trailObject.transform.localPosition = Vector3.zero;
            trailObject.transform.localRotation = Quaternion.identity;

            tailParticleSystem = trailObject.AddComponent<ParticleSystem>();
            ConfigureParticleSystem(tailParticleSystem);
            return tailParticleSystem;
        }

        public void ConfigureParticleSystem(ParticleSystem ps)
        {
            if (ps == null)
            {
                return;
            }

            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = lifetime;
            main.startSpeed = 0f;
            main.startSize = startWidth;
            main.maxParticles = 1000;
            main.playOnAwake = true;
            main.loop = true;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = emissionRate;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;

            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(0f);
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(upwardSpeed);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0f);

            var trails = ps.trails;
            trails.enabled = true;
            trails.ratio = 1.0f;
            trails.lifetimeMultiplier = 1.0f;
            trails.minVertexDistance = 0.05f;
            trails.worldSpace = true;
            trails.dieWithParticles = true;
            trails.textureMode = ParticleSystemTrailTextureMode.Stretch;

            float endRatio = Mathf.Clamp01(endWidth / Mathf.Max(0.001f, startWidth));
            AnimationCurve widthCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(1f, endRatio)
            );
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, widthCurve);

            EnsureTrailMaterial();

            if (ps.TryGetComponent(out ParticleSystemRenderer psRenderer))
            {
                psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                if (!string.IsNullOrEmpty(sortingLayerName))
                {
                    psRenderer.sortingLayerName = sortingLayerName;
                }
                psRenderer.sortingOrder = sortingOrder;

                if (trailMaterial != null)
                {
                    psRenderer.trailMaterial = trailMaterial;
                    psRenderer.material = trailMaterial;
                }
            }
        }

        private void EnsureTrailMaterial()
        {
            if (trailMaterial != null)
            {
                if (trailShader != null && trailMaterial.shader != trailShader)
                {
                    trailMaterial.shader = trailShader;
                }
                return;
            }

            Shader shader = trailShader;
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                         ?? Shader.Find("Sprites/Default");
            }

            if (shader != null)
            {
                trailMaterial = new Material(shader)
                {
                    name = "M_UnicornTailTrail_Runtime"
                };
            }
        }

        private void ApplyShaderToMaterial()
        {
            if (trailShader == null)
            {
                return;
            }

            if (trailMaterial == null)
            {
                trailMaterial = new Material(trailShader)
                {
                    name = "M_UnicornTailTrail_Runtime"
                };
            }
            else if (trailMaterial.shader != trailShader)
            {
                trailMaterial.shader = trailShader;
            }

            if (tailParticleSystem != null && tailParticleSystem.TryGetComponent(out ParticleSystemRenderer renderer))
            {
                renderer.trailMaterial = trailMaterial;
                renderer.material = trailMaterial;
            }
        }

        private static Transform FindRecursive(Transform parent, string targetName)
        {
            if (parent.name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindRecursive(parent.GetChild(i), targetName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindTailFuzzy(Transform parent)
        {
            if (parent.name.IndexOf("tail", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindTailFuzzy(parent.GetChild(i));
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
