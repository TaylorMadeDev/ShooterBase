using UnityEngine;
using System.Text;

namespace Scrapout.Weapons
{
    [RequireComponent(typeof(WeaponRuntime))]
    public class WeaponDebugUI : MonoBehaviour
    {
        private WeaponRuntime _weaponRuntime;

        private void Awake()
        {
            _weaponRuntime = GetComponent<WeaponRuntime>();
        }

        private void OnGUI()
        {
            if (_weaponRuntime == null || _weaponRuntime.ActiveBuild == null) return;

            WeaponBuild build = _weaponRuntime.ActiveBuild;
            WeaponStats stats = build.FinalStats;

            if (stats == null) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<b>--- WEAPON DEBUG ---</b>");
            sb.AppendLine($"Valid Build: {build.IsValid()}");
            sb.AppendLine();
            sb.AppendLine("<b>[ EQUIPPED PARTS ]</b>");
            sb.AppendLine($"Body: {(build.Body ? build.Body.PartName : "None")}");
            sb.AppendLine($"Barrel: {(build.Barrel ? build.Barrel.PartName : "None")}");
            sb.AppendLine($"Magazine: {(build.Magazine ? build.Magazine.PartName : "None")}");
            sb.AppendLine($"Grip: {(build.Grip ? build.Grip.PartName : "None")}");
            sb.AppendLine($"Stock: {(build.Stock ? build.Stock.PartName : "None")}");
            sb.AppendLine($"Optic: {(build.Optic ? build.Optic.PartName : "None")}");
            sb.AppendLine();
            sb.AppendLine("<b>[ FINAL STATS ]</b>");
            sb.AppendLine($"Damage: {stats.Damage}");
            sb.AppendLine($"Fire Rate: {stats.FireRate}");
            sb.AppendLine($"Accuracy: {stats.Accuracy}");
            sb.AppendLine($"Recoil: {stats.Recoil}");
            sb.AppendLine($"Range: {stats.Range}");
            sb.AppendLine($"Reload Speed: {stats.ReloadSpeed}");
            sb.AppendLine($"Mag Size: {stats.MagazineSize}");
            sb.AppendLine($"Spread: {stats.Spread}");

            // Set up a clean GUI style
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.richText = true;
            style.fontSize = 14;

            // Draw a drop shadow so it's readable over any background
            GUI.contentColor = Color.black;
            GUI.Label(new Rect(11, 11, 300, 500), sb.ToString(), style);
            
            // Draw the actual white text
            GUI.contentColor = Color.white;
            GUI.Label(new Rect(10, 10, 300, 500), sb.ToString(), style);
        }
    }
}
