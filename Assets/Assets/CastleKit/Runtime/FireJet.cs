using UnityEngine;

namespace CastleKit
{
    /// <summary>
    /// 床の噴出口。「静か → 予兆(火花・口が赤く光る) → 炎の柱が噴き上がる」を繰り返す。
    /// 予兆がはっきりあるので、スローで見極めて通り抜けやすい罠です。
    /// ゲーム内時間で進むので、スロー中は炎の周期もゆっくりになります。
    /// </summary>
    [RequireComponent(typeof(Hazard))]
    public class FireJet : MonoBehaviour
    {
        public float interval = 3.4f;     // 静かな時間
        public float warnTime = 0.9f;     // 予兆
        public float activeTime = 0.8f;   // 噴き上がる時間
        public float startDelay = 0f;     // 開始をずらす(複数並べるとき用)
        public float height = 3.2f;
        public float width = 0.9f;

        public SpriteRenderer vent;
        public SpriteRenderer flame;
        public Sprite[] flameFrames;

        enum State { Idle, Warn, Active }
        State state = State.Idle;
        Hazard hazard;
        float timer;
        float animT;

        void Awake()
        {
            hazard = GetComponent<Hazard>();
            hazard.active = false;
            timer = -startDelay;
            SetFlame(0f, 0f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            timer += dt;
            animT += dt;

            switch (state)
            {
                case State.Idle:
                    if (timer >= interval)
                    {
                        state = State.Warn;
                        timer = 0f;
                    }
                    break;

                case State.Warn:
                {
                    float k = Mathf.Clamp01(timer / warnTime);
                    float flick = 0.5f + 0.5f * Mathf.Sin(animT * 40f);
                    SetFlame(0.12f + 0.12f * flick, 0.55f);
                    if (vent != null) vent.color = Color.Lerp(Color.white, new Color(1f, 0.55f, 0.4f), 0.4f + 0.6f * flick * k);
                    if (timer >= warnTime)
                    {
                        state = State.Active;
                        timer = 0f;
                        hazard.active = true;
                    }
                    break;
                }

                case State.Active:
                {
                    float grow = Mathf.Clamp01(timer / 0.12f);
                    SetFlame(grow, 1f);
                    if (timer >= activeTime)
                    {
                        state = State.Idle;
                        timer = 0f;
                        hazard.active = false;
                        SetFlame(0f, 0f);
                        if (vent != null) vent.color = Color.white;
                    }
                    break;
                }
            }

            // 炎のコマ送り
            if (flame != null && flameFrames != null && flameFrames.Length > 0)
            {
                flame.sprite = flameFrames[Mathf.FloorToInt(animT * 14f) % flameFrames.Length];
            }
        }

        // 炎の高さ(0〜1)と透明度。炎のスプライトは足元が基準
        void SetFlame(float heightRatio, float alpha)
        {
            if (flame == null) return;
            flame.enabled = heightRatio > 0.001f;
            float h = height * heightRatio;
            flame.transform.localScale = new Vector3(width, Mathf.Max(0.0001f, h / 3f), 1f);
            Color c = flame.color;
            c.a = alpha;
            flame.color = c;
        }
    }
}
