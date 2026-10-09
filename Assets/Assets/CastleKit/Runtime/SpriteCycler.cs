using UnityEngine;

namespace CastleKit
{
    /// <summary>
    /// 子を含む全スプライトを、同じコマで一斉に切り替える(松明の炎・溶岩の波など)。
    /// ゲーム内時間で進むので、スロー中はゆらぎもゆっくりになります。
    /// </summary>
    public class SpriteCycler : MonoBehaviour
    {
        public Sprite[] frames;
        public float fps = 6f;
        public bool randomPhase = true;
        public bool useUnscaledTime = false;

        SpriteRenderer[] renderers;
        float t;

        void Awake()
        {
            renderers = GetComponentsInChildren<SpriteRenderer>();
            if (randomPhase) t = Random.value * 10f;
        }

        void Update()
        {
            if (frames == null || frames.Length == 0 || renderers == null) return;
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            int idx = Mathf.FloorToInt(t * fps) % frames.Length;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null) renderers[i].sprite = frames[idx];
            }
        }
    }
}
