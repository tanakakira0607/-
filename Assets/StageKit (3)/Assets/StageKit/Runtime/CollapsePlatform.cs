using System.Collections;
using UnityEngine;

namespace StageKit
{
    /// <summary>
    /// 乗ると少し揺れてから崩れ落ち、しばらくして復活する足場。
    /// ゲーム内時間(Time.deltaTime)で進むので、スロー中は崩れるのも遅くなります。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CollapsePlatform : MonoBehaviour
    {
        public float shakeTime = 0.6f;
        public float shakeAmount = 0.05f;
        public float fallTime = 0.8f;
        public float respawnTime = 3f;
        public string playerTag = "Player";

        Collider2D col;
        SpriteRenderer[] renderers;
        Vector3 origin;
        bool running;

        void Awake()
        {
            col = GetComponent<Collider2D>();
            renderers = GetComponentsInChildren<SpriteRenderer>();
            origin = transform.position;
        }

        void OnCollisionEnter2D(Collision2D c)
        {
            if (running || !c.collider.CompareTag(playerTag)) return;
            if (c.collider.transform.position.y < transform.position.y) return; // 上から乗ったときだけ
            StartCoroutine(Collapse());
        }

        IEnumerator Collapse()
        {
            running = true;

            float t = 0f;
            while (t < shakeTime)
            {
                t += Time.deltaTime;
                transform.position = origin + new Vector3(Random.Range(-1f, 1f) * shakeAmount, 0f, 0f);
                yield return null;
            }
            transform.position = origin;

            col.enabled = false;
            float v = 0f;
            t = 0f;
            while (t < fallTime)
            {
                float dt = Time.deltaTime;
                t += dt;
                v += 20f * dt;
                transform.position += Vector3.down * v * dt;
                SetAlpha(1f - t / fallTime);
                yield return null;
            }
            SetAlpha(0f);

            t = 0f;
            while (t < respawnTime)
            {
                t += Time.deltaTime;
                yield return null;
            }

            transform.position = origin;
            SetAlpha(1f);
            col.enabled = true;
            running = false;
        }

        void SetAlpha(float a)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Color c = renderers[i].color;
                c.a = Mathf.Clamp01(a);
                renderers[i].color = c;
            }
        }
    }
}
