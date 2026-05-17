using UnityEngine;
using System.Collections.Generic;

namespace Scrapout.Weapons
{
    public class WeaponVisualAssembler : MonoBehaviour
    {
        [Header("Root")]
        [Tooltip("The root transform where the Body will be spawned (e.g. the player's hand).")]
        public Transform RootSocket;

        [Header("Procedural Visuals")]
        [Tooltip("Material used to generate procedural duct tape at connection joints")]
        public Material DuctTapeMaterial;
        public bool EnableDuctTapeJoints = true;

        private Dictionary<WeaponPartType, GameObject> _spawnedVisuals = new Dictionary<WeaponPartType, GameObject>();

        public void AssembleVisuals(WeaponBuild build)
        {
            ClearAllVisuals();

            // 1. Spawn the Body first at the RootSocket
            TrySpawnVisual(build.Body, RootSocket, out GameObject bodyObj);

            if (bodyObj == null) return; // Can't attach other parts without a body

            // 2. Find all sockets on the spawned Body
            WeaponSocket[] socketsOnBody = bodyObj.GetComponentsInChildren<WeaponSocket>();
            
            Dictionary<WeaponPartType, Transform> bodySockets = new Dictionary<WeaponPartType, Transform>();
            foreach (WeaponSocket socket in socketsOnBody)
            {
                bodySockets[socket.SocketType] = socket.transform;
            }

            // 3. Spawn the rest of the parts on their respective sockets found on the Body
            if (bodySockets.TryGetValue(WeaponPartType.Barrel, out Transform barrelSocket))
            {
                TrySpawnVisual(build.Barrel, barrelSocket, out _);
            }
            if (bodySockets.TryGetValue(WeaponPartType.Magazine, out Transform magazineSocket))
            {
                TrySpawnVisual(build.Magazine, magazineSocket, out _);
            }
            if (bodySockets.TryGetValue(WeaponPartType.Grip, out Transform gripSocket))
            {
                TrySpawnVisual(build.Grip, gripSocket, out _);
            }
            if (bodySockets.TryGetValue(WeaponPartType.Stock, out Transform stockSocket))
            {
                TrySpawnVisual(build.Stock, stockSocket, out _);
            }
            if (bodySockets.TryGetValue(WeaponPartType.Optic, out Transform opticSocket))
            {
                TrySpawnVisual(build.Optic, opticSocket, out _);
            }
        }

        private void TrySpawnVisual(WeaponPartData part, Transform socket, out GameObject spawnedObj)
        {
            spawnedObj = null;
            if (part == null || part.Prefab == null || socket == null) return;

            // Spawn the part at the socket's world transform, then parent it while preserving world scale.
            spawnedObj = Instantiate(part.Prefab, socket.position, socket.rotation);
            spawnedObj.transform.SetParent(socket, true);

            // Check if the part has a specific attachment point defined
            WeaponAttachmentPoint attachment = spawnedObj.GetComponentInChildren<WeaponAttachmentPoint>();

            if (part.PartType == WeaponPartType.Body)
            {
                if (attachment != null)
                {
                    // Keep the body's rotation and only move it so the attachment point lands on the root socket.
                    Vector3 posOffset = socket.position - attachment.transform.position;
                    spawnedObj.transform.position += posOffset;
                }
                else
                {
                    // Body fallback: keep the prefab's rotation, only snap it to the root socket.
                    spawnedObj.transform.localPosition = Vector3.zero;
                }

                _spawnedVisuals[part.PartType] = spawnedObj;
                return;
            }
            
            if (attachment != null)
            {
                // BULLETPROOF WORLD-SPACE ALIGNMENT:
                // 1. Align Rotation: Find the difference between the socket's rotation and the attachment's current rotation,
                // and apply that difference to the root spawned object.
                Quaternion rotOffset = socket.rotation * Quaternion.Inverse(attachment.transform.rotation);
                spawnedObj.transform.rotation = rotOffset * spawnedObj.transform.rotation;

                // 2. Align Position: Move the root spawned object by the distance between the socket and the attachment point.
                Vector3 posOffset = socket.position - attachment.transform.position;
                spawnedObj.transform.position += posOffset;
            }
            else
            {
                // Fallback: If no attachment point is found, just use the prefab's root pivot
                spawnedObj.transform.localPosition = Vector3.zero;
                spawnedObj.transform.localRotation = Quaternion.identity;
            }

            _spawnedVisuals[part.PartType] = spawnedObj;

            // Generate duct tape at the joint seam (only for non-body parts attaching to the body)
            if (EnableDuctTapeJoints && DuctTapeMaterial != null && part.PartType != WeaponPartType.Body)
            {
                JointTapeGenerator.GenerateTape(socket, spawnedObj, DuctTapeMaterial);
            }
        }

        public void ClearAllVisuals()
        {
            foreach (var kvp in _spawnedVisuals)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
            }
            _spawnedVisuals.Clear();
        }
    }
}
