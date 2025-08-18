using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStatsPanel : MonoBehaviour
{
    public Player player;
    public TextMeshProUGUI text;
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


