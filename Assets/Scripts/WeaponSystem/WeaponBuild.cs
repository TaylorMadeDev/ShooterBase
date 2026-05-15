using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scrapout.Weapons
{
    // A runtime class modeling an actively built weapon
    [Serializable]
    public class WeaponBuild
    {
        [Header("Base Weapon Stats")]
        public WeaponStats BaseStats;

        [Header("Equipped Parts")]
        public WeaponPartData Body;
        public WeaponPartData Barrel;
        public WeaponPartData Magazine;
        public WeaponPartData Grip;
        public WeaponPartData Stock;
        public WeaponPartData Optic;
        
        public List<WeaponPartData> ScrapModifiers = new List<WeaponPartData>();

        [Header("Calculated Results")]
        public WeaponStats FinalStats;
        public List<WeaponSpecialEffect> ActiveEffects = new List<WeaponSpecialEffect>();

        // Events for UI/Visual Assembler to hook into
        public event Action OnPartsChanged;
        public event Action OnStatsRecalculated;

        public bool IsValid()
        {
            // The bare minimum to be a functional weapon in Scrapout
            return Body != null && Barrel != null && Magazine != null;
        }

        public bool CanEquipPart(WeaponPartData part)
        {
            if (part == null) return false;
            // Add grid inventory checks here later!
            return true;
        }

        public void EquipPart(WeaponPartData part)
        {
            if (!CanEquipPart(part)) return;

            switch (part.PartType)
            {
                case WeaponPartType.Body: Body = part; break;
                case WeaponPartType.Barrel: Barrel = part; break;
                case WeaponPartType.Magazine: Magazine = part; break;
                case WeaponPartType.Grip: Grip = part; break;
                case WeaponPartType.Stock: Stock = part; break;
                case WeaponPartType.Optic: Optic = part; break;
                case WeaponPartType.ScrapModifier: ScrapModifiers.Add(part); break;
            }

            RecalculateStats();
            OnPartsChanged?.Invoke();
        }

        public void UnequipPart(WeaponPartType type, WeaponPartData specificModifier = null)
        {
            switch (type)
            {
                case WeaponPartType.Body: Body = null; break;
                case WeaponPartType.Barrel: Barrel = null; break;
                case WeaponPartType.Magazine: Magazine = null; break;
                case WeaponPartType.Grip: Grip = null; break;
                case WeaponPartType.Stock: Stock = null; break;
                case WeaponPartType.Optic: Optic = null; break;
                case WeaponPartType.ScrapModifier:
                    if (specificModifier != null) ScrapModifiers.Remove(specificModifier);
                    break;
            }

            RecalculateStats();
            OnPartsChanged?.Invoke();
        }

        public void RecalculateStats()
        {
            if (BaseStats == null) BaseStats = new WeaponStats();
            
            WeaponStats flatTotals = new WeaponStats() { Damage = 0, FireRate = 0, Accuracy = 0, Recoil = 0, BulletSpeed = 0, Range = 0, ReloadSpeed = 0, MagazineSize = 0, PelletsPerShot = 0, Spread = 0, Knockback = 0 };
            WeaponStats multiplierTotals = new WeaponStats() { Damage = 1, FireRate = 1, Accuracy = 1, Recoil = 1, BulletSpeed = 1, Range = 1, ReloadSpeed = 1, MagazineSize = 1, PelletsPerShot = 1, Spread = 1, Knockback = 1 };
            
            ActiveEffects.Clear();

            List<WeaponPartData> allParts = GetAllEquippedParts();

            foreach (var part in allParts)
            {
                flatTotals.AddFlatBonuses(part.FlatBonuses);
                multiplierTotals.ApplyMultipliers(part.Multipliers);

                foreach (var effect in part.SpecialEffects)
                {
                    if (effect != WeaponSpecialEffect.None && !ActiveEffects.Contains(effect))
                    {
                        ActiveEffects.Add(effect);
                    }
                }
            }

            FinalStats = BaseStats.Clone();
            FinalStats.AddFlatBonuses(flatTotals);
            FinalStats.ApplyMultipliers(multiplierTotals);

            OnStatsRecalculated?.Invoke();
        }

        public List<WeaponPartData> GetAllEquippedParts()
        {
            List<WeaponPartData> parts = new List<WeaponPartData>();
            if (Body != null) parts.Add(Body);
            if (Barrel != null) parts.Add(Barrel);
            if (Magazine != null) parts.Add(Magazine);
            if (Grip != null) parts.Add(Grip);
            if (Stock != null) parts.Add(Stock);
            if (Optic != null) parts.Add(Optic);
            parts.AddRange(ScrapModifiers);
            return parts;
        }
    }
}
