using UnityEngine;
using System.Collections;

public class Bullet1 : MonoBehaviour
{
    public float warningTime = 1f;
    public float laserDuration = 0.3f;
    public float laserLength = 20f;
    public float laserWidth = 1.5f;

    PlayerHealth playerHealth;

    void Start()
    {
        playerHealth = GameObject.FindWithTag("Slider")
            .GetComponent<PlayerHealth>();

        StartCoroutine(FireLaser());
    }

    IEnumerator FireLaser()
    {
        GameObject player =
            GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Destroy(gameObject);
            yield break;
        }

        // プレイヤーをロックオン
        Vector2 direction =
            (player.transform.position - transform.position).normalized;

        // 予告線
        GameObject warning = new GameObject("WarningLine");

        LineRenderer lr =
            warning.AddComponent<LineRenderer>();

        lr.positionCount = 2;
        lr.startWidth = 0.15f;
        lr.endWidth = 0.15f;

        lr.material = new Material(
            Shader.Find("Sprites/Default"));

        lr.startColor = Color.red;
        lr.endColor = Color.red;

        lr.SetPosition(0, transform.position);
        lr.SetPosition(
            1,
            transform.position + (Vector3)direction * laserLength
        );

        yield return new WaitForSeconds(warningTime);

        Destroy(warning);

        // 極太レーザー
        GameObject laser =
            new GameObject("Laser");

        laser.transform.position =
            transform.position +
            (Vector3)(direction * laserLength * 0.5f);

        float angle =
            Mathf.Atan2(direction.y, direction.x)
            * Mathf.Rad2Deg;

        laser.transform.rotation =
            Quaternion.Euler(0, 0, angle);

        SpriteRenderer sr =
            laser.AddComponent<SpriteRenderer>();

        sr.color = Color.red;

        laser.transform.localScale =
            new Vector3(laserLength, laserWidth, 1f);

        BoxCollider2D col =
            laser.AddComponent<BoxCollider2D>();

        col.isTrigger = true;

        LaserDamage damage =
            laser.AddComponent<LaserDamage>();

        yield return new WaitForSeconds(laserDuration);

        Destroy(laser);
        Destroy(gameObject);
    }
}