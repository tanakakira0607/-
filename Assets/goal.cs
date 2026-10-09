using UnityEngine;
using UnityEngine.SceneManagement;

public class Goal : MonoBehaviour
{
    [Header("クリアするステージ名")]
    [SerializeField] private string stageName;

    [Header("クリア後に移動するシーン")]
    [SerializeField] private string nextSceneName = "StageSelect";

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // ステージクリア保存
            StageUnlock.ClearStage(stageName);

            Debug.Log(stageName + " をクリアしました");

            // 次のシーンへ
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
