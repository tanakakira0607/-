using UnityEngine;

public class DemonKingBossPro : MonoBehaviour
{
    public int maxHp = 500;
    public int currentHp;
    public int attackDamage = 30;

    [Header("時間操作機能")]
    public bool isTimeStopped = false;

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private float actionTimer = 0f;
    private Vector3 initialScale;

    void Start()
    {
        currentHp = maxHp;
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        initialScale = transform.localScale;
    }

    void Update()
    {
        if (isTimeStopped)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        actionTimer += Time.deltaTime;
        float interval = (currentHp < maxHp * 0.4f) ? 1.2f : 2.5f; // HP低下で狂乱モード

        if (actionTimer >= interval)
        {
            ExecuteBossPattern();
            actionTimer = 0f;
        }
    }

    void ExecuteBossPattern()
    {
        int pattern = Random.Range(0, 2);
        if (pattern == 0)
        {
            // 攻撃1: 大ジャンプ衝撃波攻撃
            rb.AddForce(Vector2.up * 9f, ForceMode2D.Impulse);
            StartCoroutine(FlashColor(Color.cyan));
        }
        else
        {
            // 攻撃2: 前方突進
            float dir = (Random.value > 0.5f) ? 1f : -1f;
            rb.AddForce(new Vector2(dir * 6f, 2f), ForceMode2D.Impulse);
            StartCoroutine(FlashColor(Color.red));
        }
    }

    System.Collections.IEnumerator FlashColor(Color c)
    {
        sr.color = c;
        yield return new WaitForSeconds(0.25f);
        sr.color = Color.white;
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        transform.localScale = initialScale * 0.9f;
        Invoke(nameof(ResetScale), 0.1f);

        if (currentHp <= 0)
        {
            Debug.Log("【祝】魔王を討伐し魔王城を攻略した！");
            Destroy(gameObject);
        }
    }

    void ResetScale() => transform.localScale = initialScale;
}
