using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SkillNodeBinding : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public SkillNodeDefinition data;
    public Toggle toggle;
    public Graphic lockOverlay;
    public Color lockedColor = new Color(0.5f,0.5f,0.5f,1f);
    public Color unlockedColor = Color.white;
    [TextArea] public string tooltipOverride;

    bool _applying;

    void Awake()
    {
        if (toggle == null) toggle = GetComponentInChildren<Toggle>(true);
        SyncFromState();
        Debug.Log($"[SkillNodeBinding] Awake node='{(data!=null?data.id:"<null>")}' isOn={ (toggle!=null?toggle.isOn:false)} parentsOk={SkillTreeState.ParentsSatisfied(data)} points={SkillTreeState.RemainingPoints}");
        UpdateVisual();
    }

    void OnEnable()
    {
        if (toggle != null) toggle.onValueChanged.AddListener(OnToggle);
        SkillTreeState.OnUnlocked += OnAnyUnlocked;
        SyncFromState();
        Debug.Log($"[SkillNodeBinding] OnEnable node='{(data!=null?data.id:"<null>")}' isOn={ (toggle!=null?toggle.isOn:false)}");
        UpdateVisual();
    }

    void OnDisable()
    {
        if (toggle != null) toggle.onValueChanged.RemoveListener(OnToggle);
        SkillTreeState.OnUnlocked -= OnAnyUnlocked;
    }

    void OnAnyUnlocked(SkillNodeDefinition _)
    {
        UpdateVisual();
    }

    void SyncFromState()
    {
        if (toggle == null) return;
        _applying = true;
        toggle.isOn = SkillTreeState.IsUnlocked(data);
        Debug.Log($"[SkillNodeBinding] SyncFromState node='{(data!=null?data.id:"<null>")}' set isOn={toggle.isOn}");
        _applying = false;
    }

    void OnToggle(bool on)
    {
        if (_applying || data == null || toggle == null) return;
        Debug.Log($"[SkillNodeBinding] OnToggle node='{data.id}' on={on} points={SkillTreeState.RemainingPoints}");
        if (on)
        {
            if (!SkillTreeState.TryUnlock(data))
            {
                _applying = true;
                toggle.isOn = false;
                _applying = false;
                Debug.Log($"[SkillNodeBinding] Revert toggle OFF for '{data.id}'");
            }
        }
        else
        {
            // No refunds for now; snap back if already unlocked
            if (SkillTreeState.IsUnlocked(data))
            {
                _applying = true;
                toggle.isOn = true;
                _applying = false;
                Debug.Log($"[SkillNodeBinding] Prevent turning OFF unlocked node '{data.id}'");
            }
        }
        UpdateVisual();
    }

    void UpdateVisual()
    {
        bool unlocked = SkillTreeState.IsUnlocked(data);
        bool parentsOk = SkillTreeState.ParentsSatisfied(data);
        bool canAfford = SkillTreeState.RemainingPoints >= Mathf.Max(1, data != null ? data.cost : 1);
        bool interactable = unlocked || (parentsOk && canAfford);
        if (toggle != null) toggle.interactable = interactable;
        if (lockOverlay != null) lockOverlay.color = unlocked ? unlockedColor : lockedColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        string tip = !string.IsNullOrWhiteSpace(tooltipOverride) ? tooltipOverride : data != null ? data.description : string.Empty;
        if (!string.IsNullOrEmpty(tip)) UITooltip.Show(tip, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UITooltip.Hide();
    }
}


