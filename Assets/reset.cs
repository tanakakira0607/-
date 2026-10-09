using UnityEngine;

public class GameStart : MonoBehaviour
{
    private void Awake()
    {
        if (!PlayerPrefs.HasKey("GameInitialized"))
        {
            PlayerPrefs.DeleteAll();

            PlayerPrefs.SetInt("GameInitialized", 1);
            PlayerPrefs.Save();
        }
    }
}