using UnityEngine;
using System.Collections.Generic;

namespace Scrapout.Weapons
{
    [CreateAssetMenu(fileName = "NewWeaponPart", menuName = "Scrapout/Weapon Part")]
    public class WeaponPartData : ScriptableObject
    {
        [Header("Basic Info")]
        public string PartName;
        [TextArea]
        public string Description;
        public ItemRarity Rarity;
        public WeaponPartType PartType;
        
        [Header("Requirements")]
        [Tooltip("Is this part required for the weapon to fire?")]
        public bool IsRequired = false;

        [Header("UI & Inventory")]
        public Sprite Icon;
        public Vector2Int GridSize = new Vector2Int(1, 1);

        [Header("VISUALS")]
        public GameObject Prefab;

        [Header("BODY SETTINGS")]
        [Tooltip("Only used when PartType is Body.")]
        public WeaponBodyType BodyType = WeaponBodyType.Pistol;

        [Header("BARREL VFX")]
        [Tooltip("Prefab spawned at the shoot point when this barrel fires.")]
        public GameObject MuzzleFlashPrefab;

        [Tooltip("Scale multiplier applied to the spawned muzzle flash.")]
        public float MuzzleFlashScale = 1f;

        [Header("MODIFIERS")]
        [Tooltip("Flat additions to the weapon's base stats.")]
        public WeaponStats FlatBonuses = new WeaponStats()
        {
            Damage = 0,
            FireRate = 0,
            Accuracy = 0,
            Recoil = 0,
            BulletSpeed = 0,
            Range = 0,
            ReloadSpeed = 0,
            MagazineSize = 0,
            PelletsPerShot = 0,
            Spread = 0,
            Knockback = 0
        };
        
        [Tooltip("Multipliers applied after flat bonuses. Leave at 1 for no change.")]
        public WeaponStats Multipliers = new WeaponStats()
        {
            Damage = 1, FireRate = 1, Accuracy = 1, Recoil = 1,
            BulletSpeed = 1, Range = 1, ReloadSpeed = 1, MagazineSize = 1,
            PelletsPerShot = 1, Spread = 1, Knockback = 1
        };

        [Header("Special Effects")]
        public List<WeaponSpecialEffect> SpecialEffects = new List<WeaponSpecialEffect>();
    }
}
