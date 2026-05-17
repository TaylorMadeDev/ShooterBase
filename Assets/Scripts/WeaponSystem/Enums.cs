using UnityEngine;

namespace Scrapout.Weapons
{
    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum WeaponPartType
    {
        Body, // The main body of the weapon, defines firing mechanism
        Barrel,
        Magazine,
        Grip,
        Stock,
        Optic,
        ScrapModifier
    }

    public enum WeaponBodyType
    {
        Pistol,
        AssaultRifle,
        Sniper,
        Shotgun,
        SMG,
        LMG,
        Marksman
    }

    public enum WeaponSpecialEffect
    {
        None,
        FireBullets,
        ElectricBullets,
        ExplosiveImpact,
        MultiShot,
        Ricochet,
        ChargeShot,
        EnemyScanner
    }
}
