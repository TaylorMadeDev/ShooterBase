using UnityEngine;
using System.Collections;

namespace Scrapout.Weapons
{
    [RequireComponent(typeof(WeaponVisualAssembler))]
    public class WeaponRuntime : MonoBehaviour
    {
        [Header("Configuration")]
        public WeaponBuild ActiveBuild;
        [Tooltip("Fallback point if the active barrel/body doesn't have a WeaponShootPoint component.")]
        public Transform FallbackShootPoint; 

        private WeaponVisualAssembler _assembler;
        private int _currentAmmo;
        private bool _isReloading;
        private float _lastFireTime;

        private void Awake()
        {
            _assembler = GetComponent<WeaponVisualAssembler>();
            
            // Subscribe to build changes so visuals auto-update
            if (ActiveBuild != null)
            {
                ActiveBuild.OnPartsChanged += HandlePartsChanged;
                ActiveBuild.RecalculateStats();
            }
        }

        private void Start()
        {
            if (ActiveBuild != null && ActiveBuild.IsValid())
            {
                _currentAmmo = ActiveBuild.FinalStats.MagazineSize;
                _assembler.AssembleVisuals(ActiveBuild);
            }
        }

        private void OnDestroy()
        {
            if (ActiveBuild != null)
                ActiveBuild.OnPartsChanged -= HandlePartsChanged;
        }

        private void HandlePartsChanged()
        {
            _assembler.AssembleVisuals(ActiveBuild);
            // Cap ammo to new magazine size if necessary
            if (_currentAmmo > ActiveBuild.FinalStats.MagazineSize)
                _currentAmmo = ActiveBuild.FinalStats.MagazineSize;
        }

        private void Update()
        {
            // Replace with your input system calls
            // Input example:
            // if (Input.GetButton("Fire1")) TryShoot();
            // if (Input.GetKeyDown(KeyCode.R)) TryReload();
        }

        public void TryShoot()
        {
            if (!ActiveBuild.IsValid() || _isReloading) return;
            if (_currentAmmo <= 0)
            {
                TryReload();
                return;
            }

            float timeBetweenShots = 1f / ActiveBuild.FinalStats.FireRate;
            if (Time.time >= _lastFireTime + timeBetweenShots)
            {
                Shoot();
            }
        }

        private void Shoot()
        {
            _lastFireTime = Time.time;
            _currentAmmo--;

            int pellets = ActiveBuild.FinalStats.PelletsPerShot;
            for (int i = 0; i < pellets; i++)
            {
                FireSingleProjectile();
            }

            ApplyRecoilPlaceholder();
            HandleSpecialEffects();
        }

        private Transform GetActiveShootPoint()
        {
            // Scans the assembled visuals for a specific shoot point attached to the barrel (or body)
            WeaponShootPoint dynamicPoint = GetComponentInChildren<WeaponShootPoint>();
            if (dynamicPoint != null) return dynamicPoint.transform;
            
            // Ultimate fallback so the game doesn't crash if a designer forgets to add one
            return FallbackShootPoint != null ? FallbackShootPoint : transform; 
        }

        private void FireSingleProjectile()
        {
            // Simple Raycast implementation
            // Add spread logic here based on ActiveBuild.FinalStats.Spread
            Transform shootTransform = GetActiveShootPoint();
            Vector3 direction = shootTransform.forward;
            
            if (Physics.Raycast(shootTransform.position, direction, out RaycastHit hit, ActiveBuild.FinalStats.Range))
            {
                Debug.Log($"Hit {hit.collider.name} for {ActiveBuild.FinalStats.Damage} damage!");
                // Apply damage to health system...
            }
        }

        private void ApplyRecoilPlaceholder()
        {
            // Simple camera shake or weapon kick placeholder based on FinalStats.Recoil
            Debug.Log($"Applying Recoil: {ActiveBuild.FinalStats.Recoil}");
        }

        private void HandleSpecialEffects()
        {
            if (ActiveBuild.ActiveEffects.Contains(WeaponSpecialEffect.ExplosiveImpact))
            {
                Debug.Log("Playing Explosive Impact Logic!");
            }
            if (ActiveBuild.ActiveEffects.Contains(WeaponSpecialEffect.ElectricBullets))
            {
                Debug.Log("Spawning Electric Arc!");
            }
        }

        public void TryReload()
        {
            if (_isReloading || _currentAmmo == ActiveBuild.FinalStats.MagazineSize || !ActiveBuild.IsValid()) return;
            StartCoroutine(ReloadRoutine());
        }

        private IEnumerator ReloadRoutine()
        {
            _isReloading = true;
            Debug.Log("Reloading...");
            
            yield return new WaitForSeconds(ActiveBuild.FinalStats.ReloadSpeed);
            
            _currentAmmo = ActiveBuild.FinalStats.MagazineSize;
            _isReloading = false;
            Debug.Log("Reload Complete!");
        }
    }
}
