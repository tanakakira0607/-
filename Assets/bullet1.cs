using UnityEngine;

public class Bullet2 : MonoBehaviour
{
    public float speed = 20f;
    public float warningTime = 1f;

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
        GameObject line = new GameObject("WarningLine");
        LineRenderer lr = line.AddComponent<LineRenderer>();

        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.positionCount = 2;

        lr.material = new Material(
            Shader.Find("Sprites/Default"));

        lr.startColor = Color.red;
        lr.endColor = Color.red;

        lr.SetPosition(0, transform.position);
        lr.SetPosition(1,
            transform.position + Vector3.down * 20f);

        // 1•b‘Ò‚Â
        yield return new WaitForSeconds(warningTime);

        Destroy(line);

        // ”­Ë
        sr.enabled = true;
        col.enabled = true;
        rb.simulated = true;

        rb.linearVelocity = Vector2.down * speed;

        Destroy(gameObject, 5f);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerHealth.TakeDamage(1);
        }
    }
}
