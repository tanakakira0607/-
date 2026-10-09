using UnityEngine;
using UnityEngine.SceneManagement;

public class StageSelect1 : MonoBehaviour
{
    [SerializeField] private string stageName;

    public void LoadStage()
    {
        SceneManager.LoadScene(stageName);
    }
}