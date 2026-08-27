using System;
using System.Collections.Generic;
using RainbowsEnd.Core.Config;
using UnityEngine;

namespace RainbowsEnd.Gameplay.Enemies
{
    [Serializable]
    public struct GoldDropOption
    {
        [SerializeField] [Min(0)] private int goldAmount;

        public GoldDropOption(int goldAmount)
        {
            this.goldAmount = Mathf.Max(0, goldAmount);
        }

        public int GoldAmount => Mathf.Max(0, goldAmount);

        public bool IsValid => GoldAmount > 0;
    }

    [Serializable]
    public struct WeaponDropOption
    {
        [SerializeField] private WeaponConfig weaponConfig;
        [SerializeField] [Min(0)] private int ammoCount;

        public WeaponDropOption(WeaponConfig weaponConfig, int ammoCount)
        {
            this.weaponConfig = weaponConfig;
            this.ammoCount = Mathf.Max(0, ammoCount);
        }

        public WeaponConfig WeaponConfig => weaponConfig;

        public int AmmoCount => Mathf.Max(0, ammoCount);

        public bool IsValid => weaponConfig != null && AmmoCount > 0;
    }

        public enum MilitaryDropType
    {
        None,
        Weapon,
        Gold
    }

public static class EnemyDropSelectionUtility
    {
        public static int SelectGoldDropIndex(IReadOnlyList<GoldDropOption> options, float roll01)
        {
            if (options == null || options.Count == 0)
            {
                return -1;
            }

            int validCount = 0;
            for (int index = 0; index < options.Count; index++)
            {
                if (options[index].IsValid)
                {
                    validCount++;
                }
            }

            if (validCount == 0)
            {
                return -1;
            }

            int selectedOrdinal = ResolveSelectedOrdinal(validCount, roll01);
            int currentOrdinal = 0;
            for (int index = 0; index < options.Count; index++)
            {
                if (!options[index].IsValid)
                {
                    continue;
                }

                if (currentOrdinal == selectedOrdinal)
                {
                    return index;
                }

                currentOrdinal++;
            }

            return -1;
        }

        public static int SelectGoldAmount(IReadOnlyList<GoldDropOption> options, float roll01)
        {
            int selectedIndex = SelectGoldDropIndex(options, roll01);
            return selectedIndex >= 0 ? options[selectedIndex].GoldAmount : 0;
        }

        public static int SelectWeaponDropIndex(IReadOnlyList<WeaponDropOption> options, float roll01)
        {
            if (options == null || options.Count == 0)
            {
                return -1;
            }

            int validCount = 0;
            for (int index = 0; index < options.Count; index++)
            {
                if (options[index].IsValid)
                {
                    validCount++;
                }
            }

            if (validCount == 0)
            {
                return -1;
            }

            int selectedOrdinal = ResolveSelectedOrdinal(validCount, roll01);
            int currentOrdinal = 0;
            for (int index = 0; index < options.Count; index++)
            {
                if (!options[index].IsValid)
                {
                    continue;
                }

                if (currentOrdinal == selectedOrdinal)
                {
                    return index;
                }

                currentOrdinal++;
            }

            return -1;
        }

        public static bool TrySelectWeaponDrop(IReadOnlyList<WeaponDropOption> options, float roll01, out WeaponConfig weaponConfig, out int ammoCount)
        {
            int selectedIndex = SelectWeaponDropIndex(options, roll01);
            if (selectedIndex < 0)
            {
                weaponConfig = null;
                ammoCount = 0;
                return false;
            }

            WeaponDropOption selectedOption = options[selectedIndex];
            weaponConfig = selectedOption.WeaponConfig;
            ammoCount = selectedOption.AmmoCount;
            return true;
        }

        private static int ResolveSelectedOrdinal(int validCount, float roll01)
        {
            float clampedRoll = Mathf.Clamp01(roll01);
            return Mathf.Min(Mathf.FloorToInt(clampedRoll * validCount), validCount - 1);
        }


public static MilitaryDropType SelectMilitaryDropType(float dropRoll01, float rewardRoll01, float dropChance, float weaponRewardChance)
        {
            if (Mathf.Clamp01(dropRoll01) >= Mathf.Clamp01(dropChance))
            {
                return MilitaryDropType.None;
            }

            return Mathf.Clamp01(rewardRoll01) < Mathf.Clamp01(weaponRewardChance)
                ? MilitaryDropType.Weapon
                : MilitaryDropType.Gold;
        }
}
}