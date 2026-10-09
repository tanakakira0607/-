using UnityEngine;

public class TimeMovingPlatform : MonoBehaviour
{
    [Header("移動パラメータ")]
    public Vector3 moveOffset = new Vector3(4f, 0f, 0f);
    public float duration = 3.0f;

    [Header("時間操作フラグ")]
    public bool isTimeStopped = false;
    public bool isTimeReversed = false;

    private Vector3 startPos;
    private Vector3 targetPos;
    private float progress = 0f;
    private int direction = 1;

    void Start()
    {
        startPos = transform.position;
        targetPos = startPos + moveOffset;
    }

    void Update()
    {
        if (isTimeStopped) return; // 時間停止中は足場が固定

        float step = (Time.deltaTime / duration) * (isTimeReversed ? -1f : 1f);
        progress += step * direction;

        if (progress >= 1.0f)
        {
            progress = 1.0f;
            direction = -1;
        }
        else if (progress <= 0.0f)
        {
            progress = 0.0f;
            direction = 1;
        }

        transform.position = Vector3.Lerp(startPos, targetPos, progress);
    }

    // プレイヤーが足場に乗った時に一緒に移動させる処理
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(transform);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(null);
        }
    }
}
