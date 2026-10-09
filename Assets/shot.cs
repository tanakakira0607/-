using UnityEngine;

public class Shot : MonoBehaviour
{
    public GameObject bulletPrefab; // 弾プレハブ
    public Transform firePoint;     // 発射位置

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // 左クリック
        {
            Shoot();
            Timer.time -= 0.5f;
        }
    }

    void Shoot()
    {
        Instantiate(
            bulletPrefab,
            firePoint.position,
            firePoint.rotation
        );
    }
}