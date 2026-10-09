using UnityEngine;

public class LaserDamage : MonoBehaviour
{
    public int damage = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth hp =
                other.GetComponent<PlayerHealth>();

            if (hp != null)
            {
                hp.TakeDamage(damage);
            }
        }
    }
}