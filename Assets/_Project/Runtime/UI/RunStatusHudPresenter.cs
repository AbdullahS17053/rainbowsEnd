using RainbowsEnd.Gameplay.Orchestration;
using RainbowsEnd.Gameplay.Player;
using TMPro;
using UnityEngine;

namespace RainbowsEnd.UI
{
    public sealed class RunStatusHudPresenter : MonoBehaviour
    {
        [SerializeField] private TMP_Text ammoText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text killsText;
        [SerializeField] private TMP_Text escapesText;
        [SerializeField] private PlayerWeaponInventory weaponInventory;
        [SerializeField] private RunDirector runDirector;

        private string _lastAmmo;
        private string _lastTime;
        private string _lastKills;
        private string _lastEscapes;

        private void Awake()
        {
            ResolveReferences();
            Refresh();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Refresh();
        }

        private void Update()
        {
            if (weaponInventory == null || runDirector == null)
            {
                ResolveReferences();
            }

            Refresh();
        }

        public void Configure(
            TMP_Text ammoLabel,
            TMP_Text timeLabel,
            TMP_Text killsLabel,
            TMP_Text escapesLabel,
            PlayerWeaponInventory inventory,
            RunDirector director)
        {
            ammoText = ammoLabel;
            timeText = timeLabel;
            killsText = killsLabel;
            escapesText = escapesLabel;
            weaponInventory = inventory;
            runDirector = director;
            _lastAmmo = null;
            _lastTime = null;
            _lastKills = null;
            _lastEscapes = null;
            Refresh();
        }

        public static string FormatAmmo(PlayerWeaponInventory inventory)
        {
            return inventory != null && inventory.HasTemporaryWeapon
                ? $"Ammo: {inventory.RemainingTemporaryShots}"
                : "Ammo: ∞";
        }

        public static string FormatTime(float elapsedSeconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(elapsedSeconds));
            return $"Time: {totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        public static string FormatKills(int count)
        {
            return $"Kills: {Mathf.Max(0, count)}";
        }

        public static string FormatEscapes(int count)
        {
            return $"Escapes: {Mathf.Max(0, count)}";
        }

        private void ResolveReferences()
        {
            weaponInventory ??= FindFirstObjectByType<PlayerWeaponInventory>(FindObjectsInactive.Include);
            runDirector ??= FindFirstObjectByType<RunDirector>(FindObjectsInactive.Include);
        }

        private void Refresh()
        {
            SetText(ammoText, FormatAmmo(weaponInventory), ref _lastAmmo);
            SetText(timeText, FormatTime(runDirector != null ? runDirector.ElapsedRunSeconds : 0f), ref _lastTime);
            int streak = Gameplay.Combat.KillstreakManager.Instance != null && Gameplay.Combat.KillstreakManager.Instance.IsStreakActive
                ? Gameplay.Combat.KillstreakManager.Instance.CurrentStreak
                : 0;
            string killsString = streak > 1
                ? $"{FormatKills(runDirector != null ? runDirector.KillCount : 0)}  <color=#FFD700>[Streak: {streak}]</color>"
                : FormatKills(runDirector != null ? runDirector.KillCount : 0);
            SetText(killsText, killsString, ref _lastKills);
            SetText(escapesText, FormatEscapes(runDirector != null ? runDirector.SuccessfulTheftEscapeCount : 0), ref _lastEscapes);
        }

        private static void SetText(TMP_Text label, string value, ref string lastValue)
        {
            if (label == null || value == lastValue)
            {
                return;
            }

            lastValue = value;
            label.text = value;
        }
    }
}
