using UnityEngine;

namespace BossKit
{
    /// <summary>
    /// ボス戦用の短い演出(ニアミスの輪・残像)。実時間で動くので、スロー中でもはっきり見えます。
    /// </summary>
    public class BossFx : MonoBehaviour
    {
        public static Sprite RingSprite;

        float t, duration, startScale, endScale;
        Color col;
        SpriteRenderer sr;

        public static void Spawn(Sprite sprite, Vector2 pos, Color color, float startScale, float endScale, float duration, int sortingOrder = 20)
        {
            if (sprite == null) return;
            GameObject go = new GameObject("BossFx");
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * startScale;

            SpriteRenderer s = go.AddComponent<SpriteRenderer>();
            s.sprite = sprite;
            s.color = color;
            s.sortingOrder = sortingOrder;

            BossFx fx = go.AddComponent<BossFx>();
            fx.sr = s;
            fx.col = color;
            fx.startScale = startScale;
            fx.endScale = endScale;
            fx.duration = Mathf.Max(0.01f, duration);
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            float e = 1f - (1f - k) * (1f - k);
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, e);

            Color c = col;
            c.a = col.a * (1f - k);
            sr.color = c;

            if (k >= 1f) Destroy(gameObject);
        }
    }
}
