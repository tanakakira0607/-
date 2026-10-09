using UnityEngine;
using UnityEngine.UI;
public class Move : MonoBehaviour
{
    Rigidbody2D rb;

    float walkSpeed = 5f;
    float runSpeed = 10f;
    float targetTimeScale = 1f;

    public Sprite[] walkSprites;
    public Sprite[] jumpSprites;
    public Sprite[] idlSprites;
    private bool isGrounded;
    float time = 0;
    int idx = 0;
    public Slider slowGauge;
    
public Camera mainCamera;

 

public float normalCameraSize = 5f;

public float slowCameraSize = 3f;

public float zoomSpeed = 5f;


    public Image grayEffect;

    float slowTime = 6f; // 残り使用可能時間

float maxSlowTime = 6f; // 最大時間

float rechargeTimer = 0f;

    SpriteRenderer spriteRenderer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        float speed = 0f;

        float currentSpeed =
            Input.GetKey(KeyCode.LeftShift)
            ? runSpeed
            : walkSpeed;

        bool isWalking = false;

        // 左移動
        if (Input.GetKey(KeyCode.A))
        {
            speed = -currentSpeed;
            isWalking = true;
        }
        // 右移動
        else if (Input.GetKey(KeyCode.D))
        {
            speed = currentSpeed;
            isWalking = true;
        }

        // ジャンプ
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)

        {

            rb.linearVelocity =

            new Vector2(rb.linearVelocity.x, 8f);



            isGrounded = false;

        }

        // スローシステム（元のまま）

        // --------------------
        // スローシステム
        // --------------------

        if (Input.GetKey(KeyCode.S) && slowTime > 0f)
        {
            // スロー発動
            targetTimeScale = 0.05f;

            // スロー時間消費
            slowTime -= Time.unscaledDeltaTime;

            if (slowTime < 0f)
            {
                slowTime = 0f;
            }
        }
        else
        {
            // 通常速度
            targetTimeScale = 1f;

            // 10秒で満タンまで回復
            if (slowTime < maxSlowTime)
            {
                slowTime += (maxSlowTime / 5f) * Time.unscaledDeltaTime;

                if (slowTime > maxSlowTime)
                {
                    slowTime = maxSlowTime;
                }
            }
        }

        // 時間をなめらかに変化
        Time.timeScale = Mathf.Lerp(
            Time.timeScale,
            targetTimeScale,
            15f * Time.unscaledDeltaTime
        );

        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // ゲージ更新
        slowGauge.maxValue = maxSlowTime;
        slowGauge.value = slowTime;

        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // 移動
        rb.linearVelocity =
            new Vector2(speed, rb.linearVelocity.y);

        // アニメーション
        time += Time.unscaledDeltaTime;

        if (time > 0.3f)
        {
            time = 0;

            // ジャンプ中
            if (Mathf.Abs(rb.linearVelocity.y) > 0.1f)
            {
                spriteRenderer.sprite =
                    jumpSprites[idx];
            }
            // 歩行中
            else if (isWalking)
            {
                spriteRenderer.sprite =
                    walkSprites[idx];
            }
            // 待機中
            else
            {
                spriteRenderer.sprite =
                    idlSprites[idx];
            }

            idx = 1 - idx;
        }

        
        // スロー中なら濃くする
        

        // カメラフォーカス
        float targetCameraSize =
            (Input.GetKey(KeyCode.S) && slowTime > 0f)
            ? slowCameraSize
            : normalCameraSize;

        mainCamera.orthographicSize = Mathf.Lerp(
            mainCamera.orthographicSize,
            targetCameraSize,
            zoomSpeed * Time.unscaledDeltaTime
        );
    }


    private void OnCollisionEnter2D(Collision2D collision)

{

if (collision.gameObject.CompareTag("Ground"))

{

isGrounded = true;

}

}
}