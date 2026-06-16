using UnityEngine;

namespace RainbowsEnd.Core.Config
{
    [CreateAssetMenu(fileName = "ProjectConfigRegistry", menuName = "Rainbows End/Config/Project Config Registry")]
    public sealed class ProjectConfigRegistry : ScriptableObject
    {
        [SerializeField] private PlayerConfig playerConfig;
        [SerializeField] private EnemyConfig travellerConfig;
        [SerializeField] private EnemyConfig policeConfig;
        [SerializeField] private EnemyConfig militaryConfig;
        [SerializeField] private UnicornConfig unicornConfig;
        [SerializeField] private PotConfig potConfig;
        [SerializeField] private WeaponConfig defaultWeaponConfig;
        [SerializeField] private PortalConfig portalConfig;
        [SerializeField] private EconomyConfig economyConfig;
        [SerializeField] private WaveConfig waveConfig;

        public PlayerConfig PlayerConfig => playerConfig;

        public EnemyConfig TravellerConfig => travellerConfig;

        public EnemyConfig PoliceConfig => policeConfig;

        public EnemyConfig MilitaryConfig => militaryConfig;

        public UnicornConfig UnicornConfig => unicornConfig;

        public PotConfig PotConfig => potConfig;

        public WeaponConfig DefaultWeaponConfig => defaultWeaponConfig;

        public PortalConfig PortalConfig => portalConfig;

        public EconomyConfig EconomyConfig => economyConfig;

        public WaveConfig WaveConfig => waveConfig;

        public bool HasCoreReferences =>
            playerConfig != null &&
            travellerConfig != null &&
            unicornConfig != null &&
            potConfig != null &&
            defaultWeaponConfig != null &&
            portalConfig != null &&
            economyConfig != null &&
            waveConfig != null;

        public void Configure(
            PlayerConfig player,
            EnemyConfig traveller,
            UnicornConfig unicorn,
            PotConfig pot,
            WeaponConfig weapon,
            PortalConfig portal,
            EconomyConfig economy,
            WaveConfig wave)
        {
            playerConfig = player;
            travellerConfig = traveller;
            unicornConfig = unicorn;
            potConfig = pot;
            defaultWeaponConfig = weapon;
            portalConfig = portal;
            economyConfig = economy;
            waveConfig = wave;
        }

        public void SetEnemyConfigs(EnemyConfig police, EnemyConfig military)
        {
            policeConfig = police;
            militaryConfig = military;
        }
    }
}