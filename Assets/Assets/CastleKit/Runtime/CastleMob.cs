using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace CastleKit
{
    public enum MobKind { Imp, Bat, Mage }

    /// <summary>
    /// 魔王城のモブの魔物。1つのスクリプトで3種類をこなします。
    ///
    ///  Imp(小悪魔)  : 床を往復。近づくと「かがんで溜め → 突進 → 隙」。突進はスローで見切れる。
    ///  Bat(コウモリ): 天井付近で漂う。近づくと「羽を広げて震える → 急降下 → 元の位置へ戻る」。
    ///  Mage(魔導士) : その場で杖を構え、「詠唱 → 3方向の火球」。弾は速いが予兆が長い。
    ///
    /// 全ての動きは Time.deltaTime を使うので、Time.timeScale を下げれば遅くなります。
    /// 触れるとダメージ(CastleHit経由で playerHealth.TakeDamage(1))。
    /// プレイヤーの攻撃からは mob.TakeDamage(1) を呼んでください。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(FrameAnimator))]
    public class CastleMob : MonoBehaviour
    {
        public MobKind kind = MobKind.Imp;
        public int hp = 2;
        public string playerTag = "Player";

        [Header("動き")]
        public float patrolRange = 4f;     // Imp: 往復する範囲(左右)
        public float moveSpeed = 1.8f;
        public float detectRange = 4f;     // 攻撃を始める距離
        public float minY = 0.8f;          // Bat: 急降下で下がりすぎない高さ

        [Header("当たり判定(スプライト単位 × 拡大率)")]
        public float contactRadius = 0.85f;
        public Vector2 centerOffset = new Vector2(0f, 0.5f);
        public Vector2 muzzleOffset = new Vector2(0.35f, 1.25f); // Mage: 弾の出る位置

        [Header("見た目(自動で入ります)")]
        public Sprite fireSprite;
        public Sprite ringSprite;

        public UnityEvent onDefeated;

        public bool IsDead { get { return dead; } }

        SpriteRenderer sr;
        FrameAnimator anim;
        Transform player;
        Vector3 home;
        bool dead;
        float hitFlash;
        Color tint = Color.white;
        float cooldown;
        int dir = 1;
        float hoverT;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            anim = GetComponent<FrameAnimator>();
            home = transform.position;
            hoverT = Random.value * 6f;
            CastleFx.RingSprite = ringSprite != null ? ringSprite : CastleFx.RingSprite;
        }

        void Start()
        {
            switch (kind)
            {
                case MobKind.Imp: StartCoroutine(ImpLoop()); break;
                case MobKind.Bat: StartCoroutine(BatLoop()); break;
                default: StartCoroutine(MageLoop()); break;
            }
        }

        Transform Player
        {
            get
            {
                if (player == null)
                {
                    GameObject go = GameObject.FindGameObjectWithTag(playerTag);
                    if (go != null) player = go.transform;
                }
                return player;
            }
        }

        Vector2 Center
        {
            get
            {
                float s = Mathf.Abs(transform.lossyScale.x);
                return (Vector2)transform.position + centerOffset * s;
            }
        }

        void Update()
        {
            if (dead) return;
            // 触れたらダメージ
            if (CastleHit.Touches(Center, contactRadius, playerTag)) CastleHit.TryDamage(1);
        }

        void LateUpdate()
        {
            hitFlash -= Time.unscaledDeltaTime;
            sr.color = hitFlash > 0f ? new Color(1f, 0.6f, 0.6f, 1f) : tint;
        }

        // ------------------------------------------------------------
        // ダメージ(プレイヤーの攻撃から呼んでください)
        //   例: other.GetComponent<CastleKit.CastleMob>()?.TakeDamage(1);
        // ------------------------------------------------------------
        public void TakeDamage(int amount)
        {
            if (dead) return;
            hp -= amount;
            hitFlash = 0.1f;
            if (hp <= 0) Die();
        }

        void Die()
        {
            dead = true;
            StopAllCoroutines();
            tint = Color.white;
            anim.useUnscaledTime = true; // スロー中でも消滅演出は進む
            anim.Play("death");
            if (onDefeated != null) onDefeated.Invoke();
            Collider2D[] cols = GetComponents<Collider2D>();
            for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;
            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            CastleFx.Spawn(CastleFx.RingSprite, Center, new Color(1f, 0.8f, 0.5f, 1f), 0.3f, 1.8f, 0.4f);
            float t = 0f;
            while (t < 0.7f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            Destroy(gameObject);
        }

        // ------------------------------------------------------------
        // Imp: 往復 → 溜め → 突進 → 隙
        // ------------------------------------------------------------
        IEnumerator ImpLoop()
        {
            float minX = home.x - patrolRange;
            float maxX = home.x + patrolRange;
            anim.Play("move");

            while (!dead)
            {
                float dt = Time.deltaTime;
                Vector3 p = transform.position;
                p.x += dir * moveSpeed * dt;
                if (p.x > maxX) { p.x = maxX; dir = -1; }
                else if (p.x < minX) { p.x = minX; dir = 1; }
                transform.position = p;
                sr.flipX = dir < 0;
                cooldown -= dt;

                Transform pl = Player;
                if (pl != null && cooldown <= 0f
                    && Mathf.Abs(pl.position.x - p.x) <= detectRange
                    && Mathf.Abs(pl.position.y - p.y) <= 2.2f)
                {
                    // 溜め(かがんで赤く点滅)
                    dir = pl.position.x >= p.x ? 1 : -1;
                    sr.flipX = dir < 0;
                    anim.Play("windup");
                    yield return Flash(0.55f);

                    // 突進
                    anim.Play("lunge");
                    float t = 0f;
                    while (t < 0.5f)
                    {
                        float d2 = Time.deltaTime;
                        t += d2;
                        Vector3 q = transform.position;
                        q.x = Mathf.Clamp(q.x + dir * 9f * d2, minX - 3f, maxX + 3f);
                        transform.position = q;
                        yield return null;
                    }

                    // 隙(反撃のチャンス)
                    anim.Play("windup");
                    yield return Wait(0.9f);
                    anim.Play("move");
                    cooldown = 1.2f;
                }
                yield return null;
            }
        }

        // ------------------------------------------------------------
        // Bat: 漂う → 震える → 急降下 → 戻る
        // ------------------------------------------------------------
        IEnumerator BatLoop()
        {
            anim.Play("fly");
            float phase = Random.value * 6f;

            while (!dead)
            {
                float dt = Time.deltaTime;
                hoverT += dt;
                transform.position = home + new Vector3(Mathf.Sin(hoverT * 0.8f + phase) * 1.2f,
                                                        Mathf.Sin(hoverT * 2f + phase) * 0.3f, 0f);
                cooldown -= dt;

                Transform pl = Player;
                if (pl != null && cooldown <= 0f
                    && Vector2.Distance(pl.position, transform.position) <= detectRange)
                {
                    // 予兆: 羽を広げて震える
                    anim.Play("windup");
                    Vector3 baseP = transform.position;
                    float t = 0f;
                    while (t < 0.7f)
                    {
                        t += Time.deltaTime;
                        transform.position = baseP + (Vector3)(Random.insideUnitCircle * 0.06f);
                        yield return null;
                    }

                    // 急降下(予兆の終わりにいた位置へ一直線)
                    Vector3 target = pl != null ? pl.position + Vector3.up * 0.5f : baseP + Vector3.down * 4f;
                    Vector3 start = transform.position;
                    Vector3 d = (target - start).normalized;
                    float maxTravel = Vector3.Distance(start, target) + 3f;
                    float travel = 0f;
                    anim.Play("dive");
                    sr.flipX = d.x < 0f;
                    while (travel < maxTravel)
                    {
                        float step = 11f * Time.deltaTime;
                        Vector3 pos = transform.position + d * step;
                        pos.y = Mathf.Max(pos.y, minY);
                        transform.position = pos;
                        travel += step;
                        yield return null;
                    }

                    // 元の位置へ戻る
                    anim.Play("fly");
                    while (Vector3.Distance(transform.position, home) > 0.2f)
                    {
                        transform.position = Vector3.MoveTowards(transform.position, home, 4.5f * Time.deltaTime);
                        yield return null;
                    }
                    cooldown = 1.5f;
                }
                yield return null;
            }
        }

        // ------------------------------------------------------------
        // Mage: 構える → 詠唱 → 3方向の火球 → 隙
        // ------------------------------------------------------------
        IEnumerator MageLoop()
        {
            anim.Play("idle");
            float bobT = Random.value * 6f;

            while (!dead)
            {
                float dt = Time.deltaTime;
                bobT += dt;
                transform.position = home + Vector3.up * Mathf.Sin(bobT * 2f) * 0.06f;
                cooldown -= dt;

                Transform pl = Player;
                if (pl != null) sr.flipX = pl.position.x < transform.position.x;

                if (pl != null && cooldown <= 0f
                    && Mathf.Abs(pl.position.x - home.x) <= detectRange
                    && Mathf.Abs(pl.position.y - home.y) <= detectRange * 0.8f)
                {
                    // 詠唱(予兆)
                    anim.Play("cast");
                    yield return Flash(0.8f);

                    // 発射: プレイヤーへ向けて3方向
                    float s = Mathf.Abs(transform.lossyScale.x);
                    Vector2 origin = (Vector2)transform.position + new Vector2(sr.flipX ? -muzzleOffset.x : muzzleOffset.x, muzzleOffset.y) * s;
                    Vector2 aim = pl != null ? (Vector2)pl.position - origin : Vector2.down;
                    float baseAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
                    for (int k = -1; k <= 1; k++)
                    {
                        float rad = (baseAngle + k * 18f) * Mathf.Deg2Rad;
                        Vector2 vel = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * 9f;
                        CastleProjectile.Spawn(fireSprite, origin, vel, 1f, home.y - 6f, home.x - 40f, home.x + 40f, playerTag);
                    }

                    yield return Wait(0.5f);
                    anim.Play("idle");
                    cooldown = 2.2f;
                }
                yield return null;
            }
        }

        // ------------------------------------------------------------
        // ヘルパー
        // ------------------------------------------------------------
        IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        // 予兆の点滅(赤くなる)
        IEnumerator Flash(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                tint = Color.Lerp(Color.white, new Color(1f, 0.55f, 0.55f), Mathf.PingPong(t * 8f, 1f));
                yield return null;
            }
            tint = Color.white;
        }
    }
}
