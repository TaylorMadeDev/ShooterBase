using UnityEngine;

namespace Scrapout.Weapons
{
    /// <summary>
    /// Place this on an empty child GameObject at the end of your Barrel prefab (or Body prefab, if no barrel is used).
    /// This defines exactly where the bullets/raycasts will exit the weapon.
    /// </summary>
    public class WeaponShootPoint : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            // Draw a red indicator in the editor to show where bullets come out and which way they go
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, 0.02f);
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange-ish ray
            Gizmos.DrawRay(transform.position, transform.forward * 0.5f);
        }
    }
}