using UnityEngine;

public class TimeSpikeTrapPro : MonoBehaviour
{
    public int damage = 20;
    public bool isTimeStopped = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTimeStopped) return;

        if (collision.CompareTag("Player"))
        {
            Debug.Log("時間のトゲトラップに接触！ 被ダメージ: " + damage);
        }
    }
}
