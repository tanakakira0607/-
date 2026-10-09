using UnityEngine;

public class Shot_enemy : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint2;

    float timer = 0f;
    float interval = 3f; // 3•b

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= interval)
        {
            Shoot();
            timer = 0f;
        }
    }

    void Shoot()
    {
        Instantiate(
            bulletPrefab,
            firePoint2.position,
            firePoint2.rotation
        );
    }
}