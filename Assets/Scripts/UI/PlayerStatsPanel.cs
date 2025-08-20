using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStatsPanel : MonoBehaviour
{
    public Player player;
    public TextMeshProUGUI text;
    public TextMeshProUGUI NameText;
    public TextMeshProUGUI ClassText;

    [Header("Refresh")]
    public float refreshInterval = 0.25f;

    void Awake()
    {
        if (player == null) player = FindFirstObjectByType<Player>();
        if (text == null)
        {
            var tgo = new GameObject("StatsText");
            text = tgo.AddComponent<TextMeshProUGUI>();
            text.fontSize = 20f;
            text.color = Color.white;
            var rt = text.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0, 1);
            rt.offsetMin = new Vector2(8, -300);
            rt.offsetMax = new Vector2(-8, -8);
        }

        // Create name label if missing
        if (NameText == null)
        {
            var nameGo = new GameObject("NameText");
            NameText = nameGo.AddComponent<TextMeshProUGUI>();
            NameText.fontSize = 24f;
            NameText.color = Color.white;
            var nrt = NameText.rectTransform;
            nrt.anchorMin = new Vector2(0, 1);
            nrt.anchorMax = new Vector2(1, 1);
            nrt.pivot = new Vector2(0, 1);
            nrt.offsetMin = new Vector2(8, -8);
            nrt.offsetMax = new Vector2(-8, -40);
        }

        // Create class label if missing
        if (ClassText == null)
        {
            var classGo = new GameObject("ClassText");
            ClassText = classGo.AddComponent<TextMeshProUGUI>();
            ClassText.fontSize = 20f;
            ClassText.color = Color.yellow;
            var crt = ClassText.rectTransform;
            crt.anchorMin = new Vector2(0, 1);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0, 1);
            crt.offsetMin = new Vector2(8, -44);
            crt.offsetMax = new Vector2(-8, -72);
        }
    }

    void OnEnable()
    {
        CancelInvoke();
        InvokeRepeating(nameof(Refresh), 0f, Mathf.Max(0.05f, refreshInterval));
    }

    void OnDisable()
    {
        CancelInvoke();
    }

    void Refresh()
    {
        if (player == null) return;
        var s = player.GetComputedStats();
        // Header info
        if (NameText != null) NameText.text = PlayerProfile.CharacterName;
        if (ClassText != null) ClassText.text = PlayerProfile.StartingClass.ToString();
        text.text =
            $"Health: {player.CurrentHealth}/{s.finalMaxHealth}\n" +
            $"Move Speed: {s.finalMoveSpeed:F2}\n" +
            $"Increased Damage: +{s.damageFlat}\n" +
            $"Damage Modifier: {s.damagePercent}%\n" +
            $"Percent IncreasedAttack Speed: {s.attackSpeedPercent}%\n" +
            $"Crit Chance: {s.critChancePercent}%\n" +
            $"Crit Multiplier: {s.critMultiplierPercent}%\n" +
            $"Reflect: +{s.reflectFlat} flat, {s.reflectPercent}%\n" +
            $"Regen: +{s.regenPerTick} hp / {s.regenTickSeconds:F2}s";
        // auto-size panel to text
        text.ForceMeshUpdate();
        var size = text.GetPreferredValues(text.text, 480, 0);
    }
}


