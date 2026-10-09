using UnityEngine;

namespace AttackKit
{
    /// <summary>
    /// 発射の閃光・命中の火花などの短いコマ送り演出。再生が終わると自動で消えます。
    /// 既定では実時間で動くので、スロー中でも一瞬でパッと弾けます。
    /// </summary>
    public class AttackFx : MonoBehaviour
    {
        Sprite[] frames;
        float fps;
        float t;
        bool unscaled;
        SpriteRenderer sr;

        public static void Spawn(Sprite[] frames, Vector2 pos, float fps = 20f, float scale = 1f,
                                 float angle = 0f, int sortingOrder = 20, bool unscaled = true)
        {
            if (frames == null || frames.Length == 0) return;

            GameObject go = new GameObject("AttackFx");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            go.transform.localScale = Vector3.one * scale;

            SpriteRenderer s = go.AddComponent<SpriteRenderer>();
            s.sprite = frames[0];
            s.sortingOrder = sortingOrder;

            AttackFx fx = go.AddComponent<AttackFx>();
            fx.frames = frames;
            fx.fps = Mathf.Max(1f, fps);
            fx.sr = s;
            fx.unscaled = unscaled;
        }

        void Update()
        {
            t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            int idx = Mathf.FloorToInt(t * fps);
            if (idx >= frames.Length)
            {
                Destroy(gameObject);
                return;
            }
            sr.sprite = frames[idx];
        }
    }
}
