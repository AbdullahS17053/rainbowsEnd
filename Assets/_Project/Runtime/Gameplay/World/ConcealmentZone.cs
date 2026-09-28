using System.Collections.Generic;
using UnityEngine;

namespace RainbowsEnd.Gameplay.World
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class ConcealmentZone : MonoBehaviour
    {
        private static readonly List<ConcealmentZone> ActiveZones = new List<ConcealmentZone>();

        [SerializeField] private Collider2D zoneCollider;

        public Collider2D ZoneCollider => zoneCollider;

        public static bool IsConcealed(Vector2 point, Collider2D actorCollider = null)
        {
            for (int i = 0; i < ActiveZones.Count; i++)
            {
                ConcealmentZone zone = ActiveZones[i];
                if (zone == null || !zone.gameObject.activeInHierarchy) continue;
                if (zone.Contains(point) || (actorCollider != null && zone.Overlaps(actorCollider)))
                {
                    return true;
                }
            }
            return false;
        }

        public bool Contains(Vector2 point)
        {
            EnsureCollider();
            return zoneCollider != null && zoneCollider.OverlapPoint(point);
        }

        public bool Overlaps(Collider2D collider)
        {
            EnsureCollider();
            return zoneCollider != null && collider != null && zoneCollider.IsTouching(collider);
        }

        private void Reset()
        {
            EnsureCollider();
        }

        private void OnValidate()
        {
            EnsureCollider();
        }

        private void Awake()
        {
            EnsureCollider();
        }

        private void OnEnable()
        {
            EnsureCollider();
            if (!ActiveZones.Contains(this))
            {
                ActiveZones.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveZones.Remove(this);
        }

        private void EnsureCollider()
        {
            zoneCollider ??= GetComponent<Collider2D>();
            if (zoneCollider != null)
            {
                zoneCollider.isTrigger = true;
            }
        }
    }
}