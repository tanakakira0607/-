using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 20f;

    void Start()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        rb.linearVelocity = transform.right * speed;

        // 5•bŒã‚Éíœ
        Destroy(gameObject, 5f);
    }
}