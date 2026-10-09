using UnityEngine;

public class HomingBullet : MonoBehaviour
{
    public float speed = 7f;          // 弾速
    public float rotateSpeed = 100000f;  // 追尾力

    private Transform player;
    private Rigidbody2D rb;
    PlayerHealth playerHealth;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        rb = GetComponent<Rigidbody2D>();
        playerHealth = GameObject.FindWithTag("Slider")
            .GetComponent<PlayerHealth>();
    }

    void FixedUpdate()
    {
        if (player == null) return;

        // プレイヤーへの方向
        Vector2 direction =
            ((Vector2)player.position - rb.position).normalized;

        // 現在向いている方向との角度差
        float rotateAmount =
            Vector3.Cross(direction, transform.up).z;

        // 旋回
        rb.angularVelocity = -rotateAmount * rotateSpeed;

        // 前進
        rb.linearVelocity = transform.up * speed;
        Destroy(gameObject, 5f);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Destroy(gameObject);
            if (collision.gameObject.CompareTag("Player"))
            {
                playerHealth.TakeDamage(1);
            }
        }
        
if (collision.gameObject.CompareTag("Ground"))
            
{
            
Destroy(gameObject);
            
}
    }
}