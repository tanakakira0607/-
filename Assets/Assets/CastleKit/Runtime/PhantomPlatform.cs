using System;
using UnityEngine;

namespace CastleKit
{
    /// <summary>
    /// スロー中だけ実体化する幽玄の足場。通常時は薄く透けて、当たり判定もなし。
    /// 「スロー中か」の判定は SlowCheck で差し替え可能(既定は Time.timeScale が1未満)。
    ///   例: CastleKit.PhantomPlatform.SlowCheck = () => myPlayer.IsSlow;
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PhantomPlatform : MonoBehaviour
    {
        public static Func<bool> SlowCheck = () => Time.timeScale > 0f && Time.timeScale < 0.99f;

        public float solidAlpha = 0.95f;
        public float ghostAlpha = 0.25f;
        public float fadeSpeed = 8f;
        public float graceTime = 0.25f; // スロー解除後も足場を残す猶予

        Collider2D col;
        SpriteRenderer[] renderers;
        float alpha;
        float graceTimer;

        void Awake()
        {
            col = GetComponent<Collider2D>();
            renderers = GetComponentsInChildren<SpriteRenderer>();
            alpha = ghostAlpha;
            col.enabled = false;
            Apply();
        }

        void Update()
        {
            bool slow = SlowCheck != null && SlowCheck();
            if (slow) graceTimer = graceTime;
            else if (graceTimer > 0f) graceTimer -= Time.unscaledDeltaTime;

            bool solid = slow || graceTimer > 0f;
            col.enabled = solid;
            alpha = Mathf.MoveTowards(alpha, solid ? solidAlpha : ghostAlpha, fadeSpeed * Time.unscaledDeltaTime);
            Apply();
        }

        void Apply()
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Color c = renderers[i].color;
                c.a = alpha;
                renderers[i].color = c;
            }
        }
    }
}
