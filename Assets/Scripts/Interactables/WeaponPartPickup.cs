using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Highly recommended for UI text in Unity!
using Scrapout.Weapons;

namespace Scrapout.Interactables
{
    [RequireComponent(typeof(SphereCollider))]
    public class WeaponPartPickup : MonoBehaviour
    {
        [Header("Item Data")]
        public WeaponPartData PartData;

        [Header("UI References")]
        [Tooltip("The 'PickupBillboard' transform that will rotate to face the camera.")]
        public Transform BillboardRoot;
        public Canvas UICanvas;
        
        // These should be TextMeshProUGUI components for crisp text
        public TextMeshProUGUI PartNameText;
        public TextMeshProUGUI RarityText;
        public TextMeshProUGUI PartTypeText;
        public Image PartTypeIconImage;
        public Image BackgroundImage;

        [Header("Part Type Icons")]
        public Sprite BodyIcon;
        public Sprite BarrelIcon;
        public Sprite MagazineIcon;
        public Sprite GripIcon;
        public Sprite StockIcon;
        public Sprite OpticIcon;

        [Header("Rarity Backgrounds")]
        public Sprite CommonBackground;
        public Sprite UncommonBackground;
        public Sprite RareBackground;
        public Sprite EpicBackground;
        public Sprite LegendaryBackground;

        [Header("Visuals (Optional)")]
        [Tooltip("Where to spawn the 3D model of the part on the ground")]
        public Transform ModelContainer;

        [Header("Audio & SFX")]
        public AudioSource Source;
        public AudioClip OpenSound;
        public AudioClip CloseSound;
        public AudioClip IdleLoopSound;

        [Header("Hologram Projector Beam")]
        public bool EnableProjectorBeam = false; // Turned off by default now!
        [Tooltip("Assign your Hologram material here, or a standard transparent material!")]
        public Material BeamMaterial;
        public float MaxBeamWidth = 0.5f;

        [Header("VFX (Particle Systems)")]
        [Tooltip("A looping particle system that will match the rarity color")]
        public ParticleSystem LoopingVFX;
        [Tooltip("Plays once when the UI opens!")]
        public ParticleSystem OpenBurstVFX;

        private Transform _mainCamera;
        private bool _isPlayerNear = false;
        private GameObject _spawnedModel;
        
        private Vector3 _originalUIScale;
        private Coroutine _uiAnimationCoroutine;
        private LineRenderer _beamRenderer;

        private void Start()
        {
            _mainCamera = Camera.main.transform;
            
            // Setup trigger collider for interaction distance
            SphereCollider col = GetComponent<SphereCollider>();
            col.isTrigger = true;
            if (col.radius < 1f) col.radius = 2f; 

            // Hide UI by default until player gets close
            if (UICanvas != null) 
            {
                _originalUIScale = UICanvas.transform.localScale;
                UICanvas.transform.localScale = Vector3.zero;
                UICanvas.gameObject.SetActive(false);
            }

            // Setup Projector Beam
            if (EnableProjectorBeam)
            {
                _beamRenderer = gameObject.AddComponent<LineRenderer>();
                _beamRenderer.positionCount = 2;
                _beamRenderer.startWidth = 0f;
                _beamRenderer.endWidth = 0f;
                _beamRenderer.useWorldSpace = true;
                if (BeamMaterial != null) _beamRenderer.material = BeamMaterial;
            }

            // Auto-fallback for audio source
            if (Source == null) Source = GetComponent<AudioSource>();

            InitializeDrop();
        }

        private void InitializeDrop()
        {
            if (PartData == null) return;

            // 1. Setup UI Text
            if (PartNameText != null) PartNameText.text = PartData.PartName.ToUpper();
            if (PartTypeText != null) PartTypeText.text = PartData.PartType.ToString();
            if (RarityText != null) 
            {
                RarityText.text = PartData.Rarity.ToString();
                Color rarityColor = GetRarityColor(PartData.Rarity);
                RarityText.color = rarityColor;
                PartNameText.color = rarityColor; // Color the name based on rarity too!

                if (_beamRenderer != null)
                {
                    _beamRenderer.startColor = rarityColor;
                    
                    // Make the top part of the beam fade out transparently
                    Color clearColor = rarityColor;
                    clearColor.a = 0f;
                    _beamRenderer.endColor = clearColor;
                }

                // Sync Particle VFX Colors to Rarity
                if (LoopingVFX != null)
                {
                    foreach (var ps in LoopingVFX.GetComponentsInChildren<ParticleSystem>())
                    {
                        var main = ps.main;
                        main.startColor = rarityColor;
                    }
                }
                if (OpenBurstVFX != null)
                {
                    foreach (var ps in OpenBurstVFX.GetComponentsInChildren<ParticleSystem>())
                    {
                        var main = ps.main;
                        main.startColor = rarityColor;
                    }
                }

                // Change the actual background sprite based on rarity
                if (BackgroundImage != null)
                {
                    BackgroundImage.sprite = GetRarityBackgroundSprite(PartData.Rarity);
                    BackgroundImage.color = Color.white; // Ensure it's not accidentally transparent from old settings!
                }
            }

            // 2. Setup UI Icon based on Part Type
            if (PartTypeIconImage != null)
            {
                Sprite typeIcon = GetPartTypeIcon(PartData.PartType);
                if (typeIcon != null)
                {
                    PartTypeIconImage.sprite = typeIcon;
                    PartTypeIconImage.enabled = true;
                }
                else
                {
                    PartTypeIconImage.enabled = false;
                }
            }

            // 3. Spawn the actual 3D model so it's not just an invisible box on the ground
            if (ModelContainer != null && PartData.Prefab != null)
            {
                _spawnedModel = Instantiate(PartData.Prefab, ModelContainer);
                _spawnedModel.transform.localPosition = Vector3.zero;
                // Optional: add a slow rotation script to the ModelContainer to make it float and spin
            }
        }

        private void Update()
        {
            // Billboard effect: Make the UI face the camera whenever it is visible
            if (_isPlayerNear && BillboardRoot != null && _mainCamera != null)
            {
                // This math makes it perfectly face the camera without flipping backwards
                BillboardRoot.forward = _mainCamera.forward;

                if (_beamRenderer != null)
                {
                    // Bottom of beam at the item on the ground
                    Vector3 startPos = ModelContainer != null ? ModelContainer.position : transform.position;
                    _beamRenderer.SetPosition(0, startPos);
                    
                    // Top of beam at the bottom edge of the Billboard UI Canvas
                    Vector3 endPos = BillboardRoot.position - new Vector3(0f, 0.4f, 0f); // adjust the 0.4 offset to hit bottom of your UI
                    _beamRenderer.SetPosition(1, endPos);
                }
            }

            if (_isPlayerNear && Input.GetKeyDown(KeyCode.E))
            {
                Interact();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"Something entered the trigger! It was: {other.gameObject.name} with Tag: {other.tag}");

            // Check if what walked into the trigger is the Player
            if (other.CompareTag("Player") || other.gameObject.name.Contains("Player"))
            {
                if (!_isPlayerNear)
                {
                    _isPlayerNear = true;
                    if (_uiAnimationCoroutine != null) StopCoroutine(_uiAnimationCoroutine);
                    _uiAnimationCoroutine = StartCoroutine(AnimateUI(true));
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") || other.gameObject.name.Contains("Player"))
            {
                if (_isPlayerNear)
                {
                    _isPlayerNear = false;
                    if (_uiAnimationCoroutine != null) StopCoroutine(_uiAnimationCoroutine);
                    _uiAnimationCoroutine = StartCoroutine(AnimateUI(false));
                }
            }
        }

        private IEnumerator AnimateUI(bool show)
        {
            if (UICanvas == null) yield break;

            float duration = 0.25f; // Fast, snappy animation
            float t = 0f;

            if (show)
            {
                UICanvas.gameObject.SetActive(true);
                Vector3 startScale = UICanvas.transform.localScale;

                // Handle Audio
                if (Source != null)
                {
                    if (OpenSound != null) Source.PlayOneShot(OpenSound);
                    if (IdleLoopSound != null)
                    {
                        Source.clip = IdleLoopSound;
                        Source.loop = true;
                        Source.Play();
                    }
                }

                if (OpenBurstVFX != null) OpenBurstVFX.Play();

                // Smooth Hologram Scale Up (EaseOutBack-like effect)
                while (t < 1f)
                {
                    t += Time.deltaTime / duration;
                    // Creates a bouncy overshoot effect
                    float curve = Mathf.Clamp01(t);
                    float overshoot = 1f - Mathf.Pow(1f - curve, 3f); // Cubic ease out
                    
                    UICanvas.transform.localScale = Vector3.LerpUnclamped(startScale, _originalUIScale, overshoot);

                    // Animate the beam getting wider
                    if (_beamRenderer != null)
                    {
                        _beamRenderer.startWidth = Mathf.Lerp(0f, MaxBeamWidth, overshoot);
                        _beamRenderer.endWidth = Mathf.Lerp(0f, MaxBeamWidth, overshoot);
                    }

                    yield return null;
                }
                UICanvas.transform.localScale = _originalUIScale;
                if (_beamRenderer != null) _beamRenderer.startWidth = _beamRenderer.endWidth = MaxBeamWidth;
            }
            else
            {
                Vector3 startScale = UICanvas.transform.localScale;
                float startBeamWidth = _beamRenderer != null ? _beamRenderer.startWidth : 0f;

                // Handle Audio
                if (Source != null)
                {
                    Source.Stop(); // Kill the idle loop
                    if (CloseSound != null) Source.PlayOneShot(CloseSound);
                }

                // Smooth Hologram Scale Down
                while (t < 1f)
                {
                    t += Time.deltaTime / duration;
                    float curve = Mathf.Clamp01(t);
                    float easeIn = curve * curve; // Quadratic ease in
                    
                    UICanvas.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, easeIn);

                    // Animate the beam getting thinner
                    if (_beamRenderer != null)
                    {
                        _beamRenderer.startWidth = Mathf.Lerp(startBeamWidth, 0f, easeIn);
                        _beamRenderer.endWidth = Mathf.Lerp(startBeamWidth, 0f, easeIn);
                    }

                    yield return null;
                }
                UICanvas.transform.localScale = Vector3.zero;
                if (_beamRenderer != null) _beamRenderer.startWidth = _beamRenderer.endWidth = 0f;
                UICanvas.gameObject.SetActive(false);
            }
        }

        private void Interact()
        {
            // TODO: Here is where you tell the Player's inventory logic to grab 'PartData'
            // For example: other.GetComponent<PlayerInventory>().EquipOrStore(PartData);
            
            Debug.Log($"Picked up a {PartData.Rarity} {PartData.PartName}!");
            Destroy(gameObject);
        }

        private Color GetRarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return Color.white;
                case ItemRarity.Uncommon: return Color.green;
                case ItemRarity.Rare: return new Color(0.2f, 0.6f, 1f); // Blue
                case ItemRarity.Epic: return new Color(0.7f, 0.2f, 1f); // Purple
                case ItemRarity.Legendary: return new Color(1f, 0.6f, 0f); // Orange
                default: return Color.white;
            }
        }

        private Sprite GetRarityBackgroundSprite(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return CommonBackground;
                case ItemRarity.Uncommon: return UncommonBackground;
                case ItemRarity.Rare: return RareBackground;
                case ItemRarity.Epic: return EpicBackground;
                case ItemRarity.Legendary: return LegendaryBackground;
                default: return CommonBackground;
            }
        }

        private Sprite GetPartTypeIcon(WeaponPartType type)
        {
            switch (type)
            {
                case WeaponPartType.Body: return BodyIcon;
                case WeaponPartType.Barrel: return BarrelIcon;
                case WeaponPartType.Magazine: return MagazineIcon;
                case WeaponPartType.Grip: return GripIcon;
                case WeaponPartType.Stock: return StockIcon;
                case WeaponPartType.Optic: return OpticIcon;
                default: return null;
            }
        }
    }
}