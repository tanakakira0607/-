using System.Collections;
using UnityEngine;

public class DropSpawner : MonoBehaviour
{
    [Header("プレハブ")]
    public GameObject fallingObjectPrefab;
    public GameObject warningPrefab;

    [Header("落下地点(Empty)")]
    public Transform dropPoint;
    private void Start()
    {
       SpawnFallingObject();
    }
    public void SpawnFallingObject()
    {
        StartCoroutine(WarningAndDrop());
    }

    IEnumerator WarningAndDrop()
    {
        // Emptyの位置を取得
        Vector3 pos = dropPoint.position;

        // 警告表示
        GameObject warning = Instantiate(warningPrefab, pos, Quaternion.identity);

        SpriteRenderer sr = warning.GetComponent<SpriteRenderer>();

        float timer = 0f;

        while (timer < 1f)
        {
            sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(0.1f);
            timer += 0.1f;
        }

        Destroy(warning);

        // Emptyの真上から落下
        Instantiate(
            fallingObjectPrefab,
            pos + Vector3.up * 10f,
            Quaternion.identity
        );
    }
}