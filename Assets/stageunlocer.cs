using UnityEngine;
using UnityEngine.UI;

public class StageButton : MonoBehaviour
{
    public string requiredStage;

    public Button button;
    public Image thumbnail;

    public GameObject blackMask;
    public GameObject lockIcon;

    void Start()
    {
        bool unlocked =
            string.IsNullOrEmpty(requiredStage) ||
            StageUnlock.IsCleared(requiredStage);

        button.interactable = unlocked;

        blackMask.SetActive(!unlocked);
        lockIcon.SetActive(!unlocked);
    }
}