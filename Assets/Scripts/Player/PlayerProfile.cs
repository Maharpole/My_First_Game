using UnityEngine;

public static class PlayerProfile
{
    private const string Key_Class = "profile.class";
    private const string Key_Name = "profile.name";
    private const string Key_SkillPoints = "profile.skillpoints";

    public static bool HasProfile => PlayerPrefs.HasKey(Key_Class);

    public static StartingClass StartingClass
    {
        get
        {
            if (!HasProfile) return StartingClass.Strength;
            return (StartingClass)PlayerPrefs.GetInt(Key_Class, (int)StartingClass.Strength);
        }
    }

    public static string CharacterName => PlayerPrefs.GetString(Key_Name, "Hero");

    public static int UnspentSkillPoints
    {
        get => PlayerPrefs.GetInt(Key_SkillPoints, 0);
        set { PlayerPrefs.SetInt(Key_SkillPoints, Mathf.Max(0, value)); PlayerPrefs.Save(); }
    }

    public static void NewGame(StartingClass chosen, string characterName)
    {
        PlayerPrefs.SetInt(Key_Class, (int)chosen);
        PlayerPrefs.SetString(Key_Name, string.IsNullOrWhiteSpace(characterName) ? "Hero" : characterName);
        PlayerPrefs.SetInt(Key_SkillPoints, 0);
        PlayerPrefs.Save();
    }
}


