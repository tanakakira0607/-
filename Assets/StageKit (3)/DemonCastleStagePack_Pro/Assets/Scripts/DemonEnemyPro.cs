using UnityEngine;

public class DemonEnemyPro : MonoBehaviour
{
    public int maxHp = 50;
    public int currentHp;
    public int attackDamage = 15;
    public float moveSpeed = 2.5f;

    [Header("時間操作機能")]
    public bool isTimeStopped = false;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private int moveDirection = 1;
    private float moveTimer = 0f;

    void Start()
    {
        currentHp = maxHp;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (isTimeStopped)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        moveTimer += Time.deltaTime;
        if (moveTimer > 2.5f)
        {
            moveDirection *= -1;
            moveTimer = 0f;
        }

        rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);

        // 歩行時のアニメーション（上下・左右伸縮）
        float scaleY = 1.0f + Mathf.Sin(Time.time * 10f) * 0.15f;
        transform.localScale = new Vector3(moveDirection > 0 ? 1 : -1, scaleY, 1);
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        StartCoroutine(HitFlash());
        if (currentHp <= 0)
        {
            Destroy(gameObject);
        }
    }

    System.Collections.IEnumerator HitFlash()
    {
        sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        sr.color = Color.white;
    }
}
