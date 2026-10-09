using UnityEngine;

public static class StageUnlock
{
    public static void ClearStage(string stageName)
    {
        PlayerPrefs.SetInt(stageName, 1);
        PlayerPrefs.Save();
    }

    public static bool IsCleared(string stageName)
    {
        return PlayerPrefs.GetInt(stageName, 0) == 1;
    }
}
