using System;
using UnityEngine;

namespace Scrapout.Weapons
{
    [Serializable]
    public class WeaponStats
    {
        public float Damage = 10f;
        public float FireRate = 5f; // Shots per second
        public float Accuracy = 90f; // 0 to 100
        public float Recoil = 2f;
        public float BulletSpeed = 100f;
        public float Range = 50f;
        public float ReloadSpeed = 2f;
        public int MagazineSize = 30;
        public int PelletsPerShot = 1;
        public float Spread = 1f;
        public float Knockback = 5f;

        // Clone to avoid modifying the base template
        public WeaponStats Clone()
        {
            return (WeaponStats)this.MemberwiseClone();
        }

        // Add flat bonuses
        public void AddFlatBonuses(WeaponStats bonuses)
        {
            if (bonuses == null) return;
            Damage += bonuses.Damage;
            FireRate += bonuses.FireRate;
            Accuracy += bonuses.Accuracy;
            Recoil += bonuses.Recoil;
            BulletSpeed += bonuses.BulletSpeed;
            Range += bonuses.Range;
            ReloadSpeed += bonuses.ReloadSpeed;
            MagazineSize += bonuses.MagazineSize;
            PelletsPerShot += bonuses.PelletsPerShot;
            Spread += bonuses.Spread;
            Knockback += bonuses.Knockback;
        }

        // Apply multipliers (e.g. 1.15 for +15%)
        public void ApplyMultipliers(WeaponStats multipliers)
        {
            if (multipliers == null) return;
            Damage *= multipliers.Damage;
            FireRate *= multipliers.FireRate;
            Accuracy *= multipliers.Accuracy;
            Recoil *= multipliers.Recoil;
            BulletSpeed *= multipliers.BulletSpeed;
            Range *= multipliers.Range;
            ReloadSpeed *= multipliers.ReloadSpeed;
            MagazineSize = Mathf.RoundToInt(MagazineSize * multipliers.MagazineSize); // Assuming mag multiplier exists, else keep it float internally
            PelletsPerShot = Mathf.RoundToInt(PelletsPerShot * multipliers.PelletsPerShot);
            Spread *= multipliers.Spread;
            Knockback *= multipliers.Knockback;
        }
    }
}
