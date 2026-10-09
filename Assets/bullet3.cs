using UnityEngine;

public class Bullet3 : MonoBehaviour
{
    public float speed = 8f;
    public float warningTime = 2f;

    PlayerHealth playerHealth;
    Rigidbody2D rb;
    SpriteRenderer sr;
    Collider2D col;

    void Start()
    {
        playerHealth = GameObject.FindWithTag("Slider")
            .GetComponent<PlayerHealth>();

        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        StartCoroutine(FireAfterWarning());
    }

    System.Collections.IEnumerator FireAfterWarning()
    {
        // ”­Ë‘O‚Í’e‚ğ”ñ•\¦
        sr.enabled = false;
        col.enabled = false;
        rb.simulated = false;

        // —\ü‚ğ•\¦
        
        yield return new WaitForSeconds(warningTime);

     
        // ”­Ë
        sr.enabled = true;
        col.enabled = true;
        rb.simulated = true;

        rb.linearVelocity = Vector2.left * speed;

        Destroy(gameObject, 5f);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerHealth.TakeDamage(1);
        }
        Destroy(gameObject);
    }
}
