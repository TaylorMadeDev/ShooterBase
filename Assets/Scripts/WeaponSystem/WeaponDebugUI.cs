using UnityEngine;
using System.Text;

namespace Scrapout.Weapons
{
    [RequireComponent(typeof(WeaponRuntime))]
    public class WeaponDebugUI : MonoBehaviour
    {
        [Header("Screen Overlay")]
        [Tooltip("Show weapon part and stat info in the top-left corner.")]
        public bool ShowOnScreen = true;

        [Tooltip("How far from the left edge the overlay appears.")]
        public float LeftMargin = 10f;

        [Tooltip("How far from the top edge the overlay appears.")]
        public float TopMargin = 10f;

        private WeaponRuntime _weaponRuntime;

        private void Awake()
        {
            _weaponRuntime = GetComponent<WeaponRuntime>();
        }

        private void OnGUI()
        {
            if (!ShowOnScreen || _weaponRuntime == null || _weaponRuntime.ActiveBuild == null) return;

            WeaponBuild build = _weaponRuntime.ActiveBuild;
            WeaponStats stats = build.FinalStats;

            if (stats == null) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<b>WEAPON STATS</b>");
            sb.AppendLine();
            sb.AppendLine($"Body: {(build.Body ? build.Body.PartName : "None")}");
            sb.AppendLine($"Barrel: {(build.Barrel ? build.Barrel.PartName : "None")}");
            sb.AppendLine($"Magazine: {(build.Magazine ? build.Magazine.PartName : "None")}");
            sb.AppendLine($"Grip: {(build.Grip ? build.Grip.PartName : "None")}");
            sb.AppendLine($"Stock: {(build.Stock ? build.Stock.PartName : "None")}");
            sb.AppendLine($"Optic: {(build.Optic ? build.Optic.PartName : "None")}");
            sb.AppendLine();
            sb.AppendLine($"Damage: {stats.Damage:F1}");
            sb.AppendLine($"Fire Rate: {stats.FireRate:F1}");
            sb.AppendLine($"Accuracy: {stats.Accuracy:F1}");
            sb.AppendLine($"Recoil: {stats.Recoil:F1}");
            sb.AppendLine($"Range: {stats.Range:F1}");
            sb.AppendLine($"Reload: {stats.ReloadSpeed:F1}s");
            sb.AppendLine($"Magazine: {stats.MagazineSize}");
            sb.AppendLine($"Spread: {stats.Spread:F1}");
            sb.AppendLine($"Pellets: {stats.PelletsPerShot}");
            sb.AppendLine($"Knockback: {stats.Knockback:F1}");

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                fontSize = 14,
                wordWrap = false
            };

            Rect shadowRect = new Rect(LeftMargin + 1, TopMargin + 1, 360, 420);
            Rect textRect = new Rect(LeftMargin, TopMargin, 360, 420);

            GUI.contentColor = Color.black;
            GUI.Label(shadowRect, sb.ToString(), style);

            GUI.contentColor = Color.white;
            GUI.Label(textRect, sb.ToString(), style);
        }
    }
}
