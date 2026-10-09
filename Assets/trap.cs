using UnityEngine;

public class trap : MonoBehaviour
{
    PlayerHealth playerHealth;
    private void Start()
    {
        playerHealth = GameObject.FindWithTag("Slider")
            .GetComponent<PlayerHealth>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerHealth.TakeDamage(1);
        }
    }
}
