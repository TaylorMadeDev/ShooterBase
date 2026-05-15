using UnityEngine;
using Scrapout.Weapons;

namespace Scrapout.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class SimplePlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float MoveSpeed = 5f;
        public float JumpForce = 5f;
        public float Gravity = -9.81f;

        [Header("Look")]
        public Transform CameraTransform;
        public float MouseSensitivity = 2f;
        private float _verticalLookRotation = 0f;

        [Header("Weapon")]
        public WeaponRuntime EquippedWeapon;

        private CharacterController _characterController;
        private Vector3 _velocity;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            
            // Lock the cursor to the center of the screen
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            HandleLook();
            HandleMovement();
            HandleWeaponInput();
        }

        private void HandleLook()
        {
            if (CameraTransform == null) return;

            float mouseX = Input.GetAxisRaw("Mouse X") * MouseSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * MouseSensitivity;

            // Rotate the player body left and right
            transform.Rotate(Vector3.up * mouseX);

            // Rotate the camera up and down independently
            _verticalLookRotation -= mouseY;
            _verticalLookRotation = Mathf.Clamp(_verticalLookRotation, -90f, 90f);
            CameraTransform.localEulerAngles = new Vector3(_verticalLookRotation, 0f, 0f);
        }

        private void HandleMovement()
        {
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            // Calculate movement direction relative to where the player is looking
            Vector3 move = transform.right * moveX + transform.forward * moveZ;
            _characterController.Move(move * MoveSpeed * Time.deltaTime);

            // Jumping & Gravity
            bool isGrounded = _characterController.isGrounded;
            if (isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; // Keep the player grounded
            }

            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                _velocity.y = Mathf.Sqrt(JumpForce * -2f * Gravity);
            }

            _velocity.y += Gravity * Time.deltaTime;
            _characterController.Move(_velocity * Time.deltaTime);
        }

        private void HandleWeaponInput()
        {
            if (EquippedWeapon == null) return;

            // Hold left click to shoot
            if (Input.GetButton("Fire1"))
            {
                EquippedWeapon.TryShoot();
            }

            // Press R to reload
            if (Input.GetKeyDown(KeyCode.R))
            {
                EquippedWeapon.TryReload();
            }
        }
    }
}
