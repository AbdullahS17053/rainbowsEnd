using System.Collections.Generic;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Gameplay.Unicorn;
using UnityEngine;

namespace RainbowsEnd.Gameplay.World
{
    public enum TerrainType
    {
        Mountain = 0,
        Forest = 1,
        TallGrass = 2,
        Water = 3
    }

    [RequireComponent(typeof(Collider2D))]
    public sealed class TerrainZone : MonoBehaviour
    {
        private static readonly List<TerrainZone> ActiveZones = new List<TerrainZone>();

        [SerializeField] private TerrainType terrainType;
        [SerializeField] private Collider2D zoneCollider;
        [SerializeField] private float forestSpeedMultiplier = 0.90f;

        public TerrainType Type => terrainType;
        public Collider2D ZoneCollider => zoneCollider;
        public float ForestSpeedMultiplier => forestSpeedMultiplier;

        public static bool IsConcealed(Vector2 position)
        {
            for (int i = 0; i < ActiveZones.Count; i++)
            {
                TerrainZone zone = ActiveZones[i];
                if (zone == null || !zone.gameObject.activeInHierarchy) continue;
                if ((zone.terrainType == TerrainType.Forest || zone.terrainType == TerrainType.TallGrass)
                    && zone.Contains(position))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsInForest(Vector2 position)
        {
            for (int i = 0; i < ActiveZones.Count; i++)
            {
                TerrainZone zone = ActiveZones[i];
                if (zone == null || !zone.gameObject.activeInHierarchy) continue;
                if (zone.terrainType == TerrainType.Forest && zone.Contains(position))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsInWater(Vector2 position)
        {
            for (int i = 0; i < ActiveZones.Count; i++)
            {
                TerrainZone zone = ActiveZones[i];
                if (zone == null || !zone.gameObject.activeInHierarchy) continue;
                if (zone.terrainType == TerrainType.Water && zone.Contains(position))
                {
                    return true;
                }
            }
            return false;
        }

        public static TerrainZone GetZoneAt(Vector2 position)
        {
            for (int i = 0; i < ActiveZones.Count; i++)
            {
                TerrainZone zone = ActiveZones[i];
                if (zone != null && zone.gameObject.activeInHierarchy && zone.Contains(position))
                {
                    return zone;
                }
            }
            return null;
        }

        public bool Contains(Vector2 point)
        {
            return zoneCollider != null && zoneCollider.OverlapPoint(point);
        }

        private void Reset()
        {
            zoneCollider ??= GetComponent<Collider2D>();
        }

        private void OnValidate()
        {
            zoneCollider ??= GetComponent<Collider2D>();
        }

        private void Awake()
        {
            zoneCollider ??= GetComponent<Collider2D>();
        }

        private void OnEnable()
        {
            if (!ActiveZones.Contains(this))
            {
                ActiveZones.Add(this);
            }
            ConfigureCollisions();
        }

        private void OnDisable()
        {
            ActiveZones.Remove(this);
        }

        private void Start()
        {
            ConfigureCollisions();
        }

        public void ConfigureCollisions()
        {
            if (zoneCollider == null) return;

            // Mountain rules: Unicorn phases through Mountain
            if (terrainType == TerrainType.Mountain)
            {
                UnicornController unicorn = FindFirstObjectByType<UnicornController>(FindObjectsInactive.Include);
                if (unicorn != null)
                {
                    Collider2D unicornCol = unicorn.GetComponent<Collider2D>();
                    if (unicornCol != null)
                    {
                        Physics2D.IgnoreCollision(unicornCol, zoneCollider, true);
                    }
                }
            }
            // Water rules: Only Player can walk on Water, blocks enemies & unicorn
            else if (terrainType == TerrainType.Water)
            {
                PlayerRoot player = FindFirstObjectByType<PlayerRoot>(FindObjectsInactive.Include);
                if (player != null && player.BodyCollider != null)
                {
                    Physics2D.IgnoreCollision(player.BodyCollider, zoneCollider, true);
                }
            }
            // Forest and TallGrass are triggers for soft cover / concealment
            else if (terrainType == TerrainType.Forest || terrainType == TerrainType.TallGrass)
            {
                zoneCollider.isTrigger = true;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = terrainType switch
            {
                TerrainType.Mountain => new Color(0.4f, 0.4f, 0.4f, 0.4f),
                TerrainType.Forest => new Color(0.1f, 0.6f, 0.1f, 0.35f),
                TerrainType.TallGrass => new Color(0.6f, 0.85f, 0.2f, 0.35f),
                TerrainType.Water => new Color(0.1f, 0.4f, 0.9f, 0.35f),
                _ => Color.clear
            };

            if (zoneCollider != null)
            {
                Gizmos.DrawCube(zoneCollider.bounds.center, zoneCollider.bounds.size);
            }
        }
    }
}
