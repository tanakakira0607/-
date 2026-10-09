using UnityEngine;

namespace CastleKit
{
    /// <summary>
    /// 短い演出(ニアミスの輪・残像・消滅の輪)。実時間で動くので、スロー中でもはっきり見えます。
    /// </summary>
    public class CastleFx : MonoBehaviour
    {
        public static Sprite RingSprite;

        float t, duration, startScale, endScale;
        Color col;
        SpriteRenderer sr;

        public static void Spawn(Sprite sprite, Vector2 pos, Color color, float startScale, float endScale, float duration, int sortingOrder = 20)
        {
            if (sprite == null) return;
            GameObject go = new GameObject("CastleFx");
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * startScale;

            SpriteRenderer s = go.AddComponent<SpriteRenderer>();
            s.sprite = sprite;
            s.color = color;
            s.sortingOrder = sortingOrder;

            CastleFx fx = go.AddComponent<CastleFx>();
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
