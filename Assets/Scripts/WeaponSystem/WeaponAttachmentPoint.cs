using UnityEngine;

namespace Scrapout.Weapons
{
    /// <summary>
    /// Place this on an empty child GameObject inside a part prefab (like a Grip or Barrel).
    /// Move and rotate this child to define exactly where this part attaches to the Body's socket.
    /// </summary>
    public class WeaponAttachmentPoint : MonoBehaviour
    {
        // This is purely a marker script. The Transform's position and rotation are what matter!
        
        private void OnDrawGizmos()
        {
            // Draw a helpful visual in the editor so you know which way is "Forward" mapping to the socket
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(transform.position, 0.02f);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.1f);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + transform.up * 0.1f);
        }
    }
}