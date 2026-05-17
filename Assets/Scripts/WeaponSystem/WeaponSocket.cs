using UnityEngine;

namespace Scrapout.Weapons
{
    public class WeaponSocket : MonoBehaviour
    {
        public WeaponPartType SocketType;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(transform.position, 0.02f);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.1f);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + transform.up * 0.1f);
        }
    }
}
