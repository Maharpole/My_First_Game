using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour
{
    public float rotateSpeed = 45f;

    [Header("Label")]
    public bool showNameLabel = true;
    public float nameHeight = 0.8f;
    public Color nameColor = Color.yellow;
    public int nameFontSize = 24;

    private TextMeshPro nameText;
    private RuntimeEquipmentItem runtime;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Awake()
    {
        runtime = GetComponent<RuntimeEquipmentItem>();
        if (showNameLabel)
        {
            CreateNameLabel();
        }
    }

    void Update()
    {
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        UpdateNameLabel();
    }

    void LateUpdate()
    {
        if (nameText != null && Camera.main != null)
        {
            nameText.transform.rotation = Quaternion.LookRotation(nameText.transform.position - Camera.main.transform.position);
        }
    }

    public void TryPickup(Player player, float maxDistance = 1.5f)
    {
        if (player == null) return;
        if ((player.transform.position - transform.position).sqrMagnitude > maxDistance * maxDistance) return;

        // Use the same logic as trigger pickup
        DoPickup(player);
    }

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<Player>();
        if (player == null)
        {
            player = other.GetComponentInParent<Player>();
        }

        if (player == null) return;
        DoPickup(player);
    }

    void DoPickup(Player player)
    {
        if (runtime == null || runtime.generated == null || runtime.generated.baseEquipment == null) return;

        bool equipped = player.TryEquip(runtime.generated.baseEquipment);
        if (equipped)
        {
            Destroy(gameObject);
        }
    }

    void CreateNameLabel()
    {
        var go = new GameObject("ItemName");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.up * nameHeight;

        nameText = go.AddComponent<TextMeshPro>();
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontSize = nameFontSize;
        nameText.color = nameColor;
        nameText.enableAutoSizing = false;
        nameText.text = GetDisplayName();
        nameText.raycastTarget = false;
    }

    void UpdateNameLabel()
    {
        if (nameText == null) return;
        string desired = GetDisplayName();
        if (nameText.text != desired)
        {
            nameText.text = desired;
        }
    }

    string GetDisplayName()
    {
        if (runtime != null && runtime.generated != null && runtime.generated.baseEquipment != null)
        {
            // Optionally prefix with rarity color
            return runtime.GetDisplayName();
        }
        return "Item";
    }
}
