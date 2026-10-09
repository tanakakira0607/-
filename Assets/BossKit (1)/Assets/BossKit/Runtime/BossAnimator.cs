using UnityEngine;

namespace BossKit
{
    public enum BossAnimState { Idle, Charge, Attack, Dash, Tired, Hurt, Death }

    /// <summary>
    /// ドット絵のコマ送りアニメーション(Animatorコントローラー不要)。
    /// 既定ではゲーム内時間で進むので、スロー中はアニメーションもゆっくりになります。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BossAnimator : MonoBehaviour
    {
        public Sprite[] idle, idleRage, charge, attack, dash, tired, hurt, death;

        public float idleFps = 6f;
        public float chargeFps = 14f;
        public float attackFps = 14f;
        public float dashFps = 16f;
        public float tiredFps = 5f;
        public float hurtFps = 12f;
        public float deathFps = 8f;

        // フェーズ2の怒り状態(待機の絵が変わります)
        public bool rage;
        public bool useUnscaledTime = false;

        public BossAnimState State { get { return state; } }

        SpriteRenderer sr;
        BossAnimState state = BossAnimState.Idle;
        bool loop = true;
        float t;
        bool oneShot;
        BossAnimState savedState;
        bool savedLoop;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        // 状態を切り替える。loopAnim=false なら最後のコマで止まる
        public void Play(BossAnimState s, bool loopAnim = true)
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (state == s && loop == loopAnim && !oneShot) return;
            state = s;
            loop = loopAnim;
            oneShot = false;
            t = 0f;
            Apply();
        }

        // 1回だけ再生して、終わったら元の状態に戻る(被弾など)
        public void PlayOneShot(BossAnimState s)
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (state == BossAnimState.Death) return;
            if (!oneShot)
            {
                savedState = state;
                savedLoop = loop;
            }
            state = s;
            loop = false;
            oneShot = true;
            t = 0f;
            Apply();
        }

        void Update()
        {
            Sprite[] f = Frames();
            if (f == null || f.Length == 0 || sr == null) return;

            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            int idx = Mathf.FloorToInt(t * Fps());

            if (loop)
            {
                idx %= f.Length;
            }
            else if (idx >= f.Length)
            {
                idx = f.Length - 1;
                if (oneShot)
                {
                    oneShot = false;
                    state = savedState;
                    loop = savedLoop;
                    t = 0f;
                    Apply();
                    return;
                }
            }

            if (sr.sprite != f[idx]) sr.sprite = f[idx];
        }

        void Apply()
        {
            Sprite[] f = Frames();
            if (f != null && f.Length > 0 && sr != null) sr.sprite = f[0];
        }

        Sprite[] Frames()
        {
            switch (state)
            {
                case BossAnimState.Idle: return (rage && idleRage != null && idleRage.Length > 0) ? idleRage : idle;
                case BossAnimState.Charge: return charge;
                case BossAnimState.Attack: return attack;
                case BossAnimState.Dash: return dash;
                case BossAnimState.Tired: return tired;
                case BossAnimState.Hurt: return hurt;
                default: return death;
            }
        }

        float Fps()
        {
            switch (state)
            {
                case BossAnimState.Idle: return idleFps;
                case BossAnimState.Charge: return chargeFps;
                case BossAnimState.Attack: return attackFps;
                case BossAnimState.Dash: return dashFps;
                case BossAnimState.Tired: return tiredFps;
                case BossAnimState.Hurt: return hurtFps;
                default: return deathFps;
            }
        }
    }
}
