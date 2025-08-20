using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class SaveSystem
{
    const string Key_Profile = "save.profile.name";
    const string Key_Scene = "save.scene";
    const string Key_SkillCsv = "save.skills.csv";
    const string Key_Coins = "save.coins";
    const string Key_Unspent = "save.skillpoints";
    const string Key_Equip = "save.equip."; // per-slot keys

    public static void SavePlayer(Player player)
    {
        if (player == null) return;
        PlayerPrefs.SetString(Key_Profile, PlayerProfile.CharacterName);
        PlayerPrefs.SetString(Key_Scene, "Haven_Scene"); // always respawn here
        PlayerPrefs.SetString(Key_SkillCsv, SkillTreeState.ExportCsv());
        PlayerPrefs.SetInt(Key_Coins, player.Coins);
        PlayerPrefs.SetInt(Key_Unspent, PlayerProfile.UnspentSkillPoints);

        var equip = player.GetComponent<CharacterEquipment>();
        if (equip != null)
        {
            SaveSlot(Key_Equip + "Helmet", equip.helmet);
            SaveSlot(Key_Equip + "BodyArmour", equip.bodyArmour);
            SaveSlot(Key_Equip + "Amulet", equip.amulet);
            SaveSlot(Key_Equip + "Gloves", equip.gloves);
            SaveSlot(Key_Equip + "Ring1", equip.ring1);
            SaveSlot(Key_Equip + "Ring2", equip.ring2);
            SaveSlot(Key_Equip + "Boots", equip.boots);
            SaveSlot(Key_Equip + "Belt", equip.belt);
            SaveSlot(Key_Equip + "MainHand", equip.mainHand);
            SaveSlot(Key_Equip + "OffHand", equip.offHand);
        }
        PlayerPrefs.Save();
        Debug.Log("[SaveSystem] Saved player.");
    }

    public static void SaveNow()
    {
        var player = Object.FindFirstObjectByType<Player>();
        if (player != null) SavePlayer(player);
        else Debug.LogWarning("[SaveSystem] SaveNow: Player not found");
    }

    static void SaveSlot(string key, EquipmentSlot slot)
    {
        if (slot != null && slot.HasItem && slot.EquippedItem != null)
        {
            var path = AssetDatabaseUtility.GetResourcesPath(slot.EquippedItem);
            PlayerPrefs.SetString(key, path);
        }
        else
        {
            PlayerPrefs.DeleteKey(key);
        }
    }

    public static void LoadGame()
    {
        string scene = PlayerPrefs.GetString(Key_Scene, "Haven_Scene");
        SceneManager.LoadScene(scene);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public static bool HasSave()
    {
        return PlayerPrefs.HasKey(Key_Scene) || PlayerPrefs.HasKey(Key_SkillCsv);
    }

    static void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        var player = Object.FindFirstObjectByType<Player>();
        if (player == null)
        {
            Debug.LogWarning("[SaveSystem] Player not found after load.");
            return;
        }
        ApplyLoadedState(player);
    }

    static void ApplyLoadedState(Player player)
    {
        // Skill tree
        SkillTreeState.ImportCsv(PlayerPrefs.GetString(Key_SkillCsv, string.Empty), replace: true);
        PlayerProfile.UnspentSkillPoints = PlayerPrefs.GetInt(Key_Unspent, PlayerProfile.UnspentSkillPoints);

        // Equipment
        var equip = player.GetComponent<CharacterEquipment>();
        if (equip != null)
        {
            LoadSlot(equip.helmet, PlayerPrefs.GetString(Key_Equip + "Helmet", string.Empty));
            LoadSlot(equip.bodyArmour, PlayerPrefs.GetString(Key_Equip + "BodyArmour", string.Empty));
            LoadSlot(equip.amulet, PlayerPrefs.GetString(Key_Equip + "Amulet", string.Empty));
            LoadSlot(equip.gloves, PlayerPrefs.GetString(Key_Equip + "Gloves", string.Empty));
            LoadSlot(equip.ring1, PlayerPrefs.GetString(Key_Equip + "Ring1", string.Empty));
            LoadSlot(equip.ring2, PlayerPrefs.GetString(Key_Equip + "Ring2", string.Empty));
            LoadSlot(equip.boots, PlayerPrefs.GetString(Key_Equip + "Boots", string.Empty));
            LoadSlot(equip.belt, PlayerPrefs.GetString(Key_Equip + "Belt", string.Empty));
            LoadSlot(equip.mainHand, PlayerPrefs.GetString(Key_Equip + "MainHand", string.Empty));
            LoadSlot(equip.offHand, PlayerPrefs.GetString(Key_Equip + "OffHand", string.Empty));
        }

        // Spawn at Haven_Scene spawn point
        var spawn = GameObject.FindWithTag("PlayerSpawn");
        if (spawn != null)
        {
            player.transform.position = spawn.transform.position;
        }
        Debug.Log("[SaveSystem] Loaded player state.");
    }

    static void LoadSlot(EquipmentSlot slot, string resourcePath)
    {
        if (slot == null || string.IsNullOrEmpty(resourcePath)) return;
        var item = Resources.Load<EquipmentData>(resourcePath);
        if (item != null) slot.TryEquip(item);
    }
}

// Utility for mapping ScriptableObjects to a Resources-relative path at edit-time.
public static class AssetDatabaseUtility
{
#if UNITY_EDITOR
    public static string GetResourcesPath(Object obj)
    {
        if (obj == null) return string.Empty;
        string path = UnityEditor.AssetDatabase.GetAssetPath(obj);
        int index = path.IndexOf("Resources/");
        if (index < 0) { Debug.LogWarning($"[SaveSystem] Asset '{path}' not under a Resources folder."); return string.Empty; }
        string rel = path.Substring(index + "Resources/".Length);
        int dot = rel.LastIndexOf('.');
        if (dot >= 0) rel = rel.Substring(0, dot);
        return rel;
    }
#else
    public static string GetResourcesPath(Object obj) { return string.Empty; }
#endif
}


