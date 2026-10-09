using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BossKit
{
    /// <summary>
    /// ボス本体。プレイヤーが近づくと戦闘開始。
    ///
    /// 【スローで避けて気持ちいい設計】
    ///  - 攻撃は必ず「予兆(溜めモーション・警告ゾーン)」→「発射」の順。
    ///  - 弾は速い(通常速度だと厳しい)が、隙間は広め。スローにすると弾が止まって見え、隙間を抜けられる。
    ///  - スロー中にギリギリをかすめるとニアミス演出(輪っか)が出る。
    ///  - 攻撃の後は必ず「ぐったり」する時間があり、青白い絵になる = 反撃のチャンス。
    ///  - HPが半分になると怒りフェーズ(攻撃が増え、少し速くなる)。
    ///
    /// 全ての動きは Time.deltaTime を使うので、Time.timeScale を下げれば弾もボスも遅くなります。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BossAnimator))]
    public class BossController : MonoBehaviour
    {
        [Header("体力")]
        public int maxHp = 30;

        [Header("戦闘エリア(ボスの初期位置が中心)")]
        public float arenaHalfWidth = 15f;
        public float groundY = 0f;
        // プレイヤーがこの横距離まで近づくと戦闘開始
        public float activateDistance = 16f;

        [Header("プレイヤー")]
        public string playerTag = "Player";

        [Header("見た目(自動で入ります)")]
        public Sprite bulletSprite;
        public Sprite warnSprite;
        public Sprite ringSprite;

        [Header("イベント")]
        public UnityEvent onFightStart;
        public UnityEvent onPhase2;
        public UnityEvent onDefeated;

        public int Hp { get { return hp; } }
        public int Phase { get { return phase; } }
        public bool Fighting { get { return fighting; } }
        public bool IsDead { get { return dead; } }
        // ボスが「ぐったり」している(反撃のチャンス)
        public bool IsTired { get { return tired; } }

        int hp;
        int phase = 1;
        bool fighting, dead, invulnerable, moveLocked, tired, dashGrazed;
        float arenaMinX, arenaMaxX;
        Vector3 homePos, baseScale;
        float bobT, hitFlash;
        Color tint = Color.white;
        SpriteRenderer sr;
        BossAnimator anim;
        Transform player;
        Coroutine fightRoutine;
        readonly List<GameObject> warns = new List<GameObject>();
        Transform barRoot, barFill;
        const float BarWidth = 4f;

        // ------------------------------------------------------------
        // 基本
        // ------------------------------------------------------------
        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            anim = GetComponent<BossAnimator>();
            hp = maxHp;
            homePos = transform.position;
            baseScale = transform.localScale;
            arenaMinX = homePos.x - arenaHalfWidth;
            arenaMaxX = homePos.x + arenaHalfWidth;
            BossFx.RingSprite = ringSprite;
            BuildBar();
            anim.Play(BossAnimState.Idle);
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

        void Update()
        {
            Transform pl = Player;
            if (!fighting && !dead && pl != null && Mathf.Abs(pl.position.x - homePos.x) <= activateDistance)
            {
                StartFight();
            }

            float dt = Time.deltaTime;
            bobT += dt;

            if (!moveLocked && !dead)
            {
                float targetX = homePos.x;
                if (fighting && pl != null)
                {
                    float lo, hi, top;
                    GetRange(out lo, out hi, out top);
                    targetX = Mathf.Clamp(pl.position.x, lo + 3f, hi - 3f);
                }
                float follow = phase >= 2 ? 4.5f : 3f;
                Vector3 p = transform.position;
                p.x = Mathf.MoveTowards(p.x, targetX, follow * dt);
                float targetY = homePos.y + Mathf.Sin(bobT * 1.6f) * 0.4f;
                p.y = Mathf.MoveTowards(p.y, targetY, 6f * dt);
                transform.position = p;
            }

            UpdateBar();
        }

        void LateUpdate()
        {
            hitFlash -= Time.unscaledDeltaTime;
            sr.color = hitFlash > 0f ? new Color(1f, 0.65f, 0.65f, 1f) : tint;
        }

        void OnDestroy()
        {
            if (barRoot != null) Destroy(barRoot.gameObject);
        }

        void StartFight()
        {
            fighting = true;
            if (onFightStart != null) onFightStart.Invoke();
            fightRoutine = StartCoroutine(FightLoop());
        }

        // ------------------------------------------------------------
        // ダメージ(プレイヤーの攻撃から呼んでください)
        //   例: other.GetComponent<BossKit.BossController>()?.TakeDamage(1);
        // ------------------------------------------------------------
        public void TakeDamage(int amount)
        {
            if (dead || !fighting || invulnerable) return;

            hp -= amount;
            hitFlash = 0.1f;

            if (hp <= 0)
            {
                Die();
                return;
            }

            anim.PlayOneShot(BossAnimState.Hurt);

            if (phase == 1 && hp <= maxHp / 2)
            {
                StartCoroutine(Phase2Routine());
            }
        }

        void Die()
        {
            dead = true;
            fighting = false;
            StopAllCoroutines();
            ClearWarns();
            ClearBullets();
            moveLocked = true;
            tired = false;
            transform.localScale = baseScale;
            if (onDefeated != null) onDefeated.Invoke();
            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            if (barRoot != null) barRoot.gameObject.SetActive(false);
            anim.useUnscaledTime = true; // スロー中でも最後まで演出が進む
            anim.Play(BossAnimState.Death, false);

            Vector3 p = transform.position;
            float t = 0f;
            const float dur = 2f;
            float ringTimer = 0f;
            while (t < dur)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                ringTimer += dt;
                transform.position = p + (Vector3)(Random.insideUnitCircle * 0.1f);
                if (t > 1f) tint = new Color(1f, 1f, 1f, 1f - (t - 1f) / (dur - 1f));
                if (ringTimer > 0.25f)
                {
                    ringTimer = 0f;
                    BossFx.Spawn(ringSprite, p + (Vector3)(Random.insideUnitCircle * 1.2f), new Color(0.8f, 0.6f, 1f, 1f), 0.3f, 2.5f, 0.5f);
                }
                yield return null;
            }
            gameObject.SetActive(false);
        }

        IEnumerator Phase2Routine()
        {
            if (fightRoutine != null) StopCoroutine(fightRoutine);
            invulnerable = true;
            moveLocked = true;
            tired = false;
            ClearWarns();
            ClearBullets();
            phase = 2;
            transform.localScale = baseScale;
            anim.rage = true;
            anim.Play(BossAnimState.Charge);
            if (onPhase2 != null) onPhase2.Invoke();

            float lo, hi, top;
            GetRange(out lo, out hi, out top);
            transform.position = new Vector3((lo + hi) * 0.5f, homePos.y, transform.position.z);

            // 咆哮(実時間で揺れる)
            Vector3 p = transform.position;
            float t = 0f;
            while (t < 1.4f)
            {
                t += Time.unscaledDeltaTime;
                transform.position = p + (Vector3)(Random.insideUnitCircle * 0.08f);
                yield return null;
            }
            transform.position = p;
            anim.Play(BossAnimState.Idle);
            moveLocked = false;
            invulnerable = false;
            fightRoutine = StartCoroutine(FightLoop());
        }

        // ------------------------------------------------------------
        // 戦闘ループ
        // ------------------------------------------------------------
        IEnumerator FightLoop()
        {
            anim.Play(BossAnimState.Idle);
            yield return Wait(1.0f);

            int last = -1;
            while (!dead)
            {
                int n = phase >= 2 ? 4 : 3;
                int pick;
                int guard = 0;
                do { pick = Random.Range(0, n); guard++; } while (pick == last && guard < 10);
                last = pick;

                switch (pick)
                {
                    case 0: yield return RingBurst(); break;
                    case 1: yield return SoulRain(); break;
                    case 2: yield return PhantomDash(); break;
                    default: yield return Spiral(); break;
                }

                // 攻撃のあとは「ぐったり」= 反撃チャンス
                tired = true;
                anim.Play(BossAnimState.Tired);
                yield return Wait(phase >= 2 ? 1.3f : 1.9f);
                tired = false;
                anim.Play(BossAnimState.Idle);
            }
        }

        // ------------------------------------------------------------
        // 攻撃1: リングバースト
        //  予兆で光が収束 → 隙間のある弾の輪が広がる。次の輪は隙間がずれる(編み込み)。
        // ------------------------------------------------------------
        IEnumerator RingBurst()
        {
            bool p2 = phase >= 2;
            int rings = p2 ? 3 : 2;
            int count = p2 ? 26 : 22;
            float speed = p2 ? 9f : 7.5f;
            float gapDeg = p2 ? 46f : 54f;

            StartCoroutine(WarnRing(0.9f, new Color(1f, 0.3f, 0.5f, 0.9f)));
            yield return Telegraph(0.9f);

            anim.Play(BossAnimState.Attack, false);

            float side = Random.value < 0.5f ? -1f : 1f;
            float gap = AimAngle() + Random.Range(25f, 45f) * side;

            for (int r = 0; r < rings; r++)
            {
                Vector2 origin = transform.position;
                float step = 360f / count;
                float offset = (r % 2 == 1) ? step * 0.5f : 0f;
                for (int i = 0; i < count; i++)
                {
                    float ang = i * step + offset;
                    if (Mathf.Abs(Mathf.DeltaAngle(ang, gap)) < gapDeg * 0.5f) continue;
                    SpawnBullet(origin, Dir(ang) * speed, 1f);
                }
                side = -side;
                gap += Random.Range(22f, 38f) * side;
                yield return Wait(0.55f);
            }
        }

        // ------------------------------------------------------------
        // 攻撃2: ソウルレイン
        //  赤い警告ゾーン(危険な列)→ 弾の柱が一気に落ちる。安全な列は毎波ずれていく。
        //  通常速度だと列の移動が間に合いにくく、スローなら余裕で移れる。
        // ------------------------------------------------------------
        IEnumerator SoulRain()
        {
            bool p2 = phase >= 2;
            int waves = p2 ? 4 : 3;
            float tele = p2 ? 0.8f : 1.0f;
            float fall = p2 ? 16f : 14f;
            int safeWidth = p2 ? 1 : 2;

            float lo, hi, top;
            GetRange(out lo, out hi, out top);
            float step = 3.5f;
            int lanes = Mathf.Max(4, Mathf.FloorToInt((hi - lo - 2f) / step) + 1);
            float startX = (lo + hi) * 0.5f - (lanes - 1) * step * 0.5f;
            float height = top - groundY;

            int dirSign = Random.value < 0.5f ? -1 : 1;
            int safe = Random.Range(0, lanes - safeWidth + 1);
            Transform pl = Player;
            if (pl != null)
            {
                int playerLane = Mathf.RoundToInt((pl.position.x - startX) / step);
                safe = Mathf.Clamp(playerLane + dirSign * Random.Range(1, 3), 0, lanes - safeWidth);
            }

            for (int w = 0; w < waves; w++)
            {
                anim.Play(BossAnimState.Charge);

                // 警告ゾーン(危険な列だけ赤く)
                List<GameObject> mine = new List<GameObject>();
                for (int l = 0; l < lanes; l++)
                {
                    if (l >= safe && l < safe + safeWidth) continue;
                    Vector2 c = new Vector2(startX + l * step, groundY + height * 0.5f);
                    mine.Add(MakeWarn(c, new Vector2(step - 0.2f, height), new Color(1f, 0.15f, 0.3f, 0.18f)));
                }

                float dur = (w == 0) ? tele : tele * 0.8f;
                float t = 0f;
                while (t < dur)
                {
                    t += Time.deltaTime;
                    float a = 0.12f + 0.12f * Mathf.PingPong(t * 5f, 1f);
                    for (int i = 0; i < mine.Count; i++)
                    {
                        if (mine[i] == null) continue;
                        SpriteRenderer s = mine[i].GetComponent<SpriteRenderer>();
                        Color col = s.color;
                        col.a = a;
                        s.color = col;
                    }
                    yield return null;
                }
                for (int i = 0; i < mine.Count; i++) DestroyWarn(mine[i]);

                // 発射: 1レーンを3発で隙間なく埋める
                anim.Play(BossAnimState.Attack, false);
                for (int l = 0; l < lanes; l++)
                {
                    if (l >= safe && l < safe + safeWidth) continue;
                    for (int k = -1; k <= 1; k++)
                    {
                        SpawnBullet(new Vector2(startX + l * step + k * 1.2f, top), Vector2.down * fall, 1.2f);
                    }
                }

                // 次の安全レーン
                int move = (p2 ? Random.Range(1, 3) : 1) * dirSign;
                safe = Mathf.Clamp(safe + move, 0, lanes - safeWidth);
                if (safe <= 0 || safe >= lanes - safeWidth) dirSign = -dirSign;
            }

            yield return Wait(0.8f);
        }

        // ------------------------------------------------------------
        // 攻撃3: ファントムダッシュ
        //  画面端でボスが赤い帯を出して溜める → 一気に横切る。ジャンプかスローで避ける。
        //  怒りフェーズでは往復する。
        // ------------------------------------------------------------
        IEnumerator PhantomDash()
        {
            bool p2 = phase >= 2;
            int dashes = p2 ? 2 : 1;

            float lo, hi, top;
            GetRange(out lo, out hi, out top);
            float y = groundY + 0.9f;

            Transform pl = Player;
            bool startRight = pl == null || pl.position.x < (lo + hi) * 0.5f;

            moveLocked = true;
            for (int d = 0; d < dashes; d++)
            {
                float fromX = startRight ? hi - 0.5f : lo + 0.5f;
                float toX = startRight ? lo - 2f : hi + 2f;

                anim.Play(BossAnimState.Idle);
                yield return MoveTo(new Vector2(fromX, y), 0.5f);

                // 予兆: 通る道に赤い帯 + 溜めモーション
                anim.Play(BossAnimState.Charge);
                float len = (hi + 2f) - (lo - 2f);
                GameObject line = MakeWarn(new Vector2((lo + hi) * 0.5f, y), new Vector2(len, 1.4f), new Color(1f, 0.2f, 0.3f, 0.2f));
                SpriteRenderer ls = line.GetComponent<SpriteRenderer>();
                float tele = (d == 0) ? 1.0f : 0.6f;
                float t = 0f;
                while (t < tele)
                {
                    t += Time.deltaTime;
                    float k = Mathf.PingPong(t * 6f, 1f);
                    if (ls != null)
                    {
                        Color col = ls.color;
                        col.a = 0.12f + 0.18f * k;
                        ls.color = col;
                    }
                    yield return null;
                }
                DestroyWarn(line);

                // ダッシュ
                anim.Play(BossAnimState.Dash);
                float speed = p2 ? 30f : 26f;
                Vector3 pos = transform.position;
                float dirX = Mathf.Sign(toX - pos.x);
                float trail = 0f;
                dashGrazed = false;
                while ((toX - pos.x) * dirX > 0f)
                {
                    float dt = Time.deltaTime;
                    pos.x += dirX * speed * dt;
                    transform.position = pos;
                    CheckContact(1.0f);

                    trail += dt;
                    if (trail > 0.035f)
                    {
                        trail = 0f;
                        BossFx.Spawn(sr.sprite, transform.position, new Color(0.7f, 0.5f, 1f, 0.5f),
                                     baseScale.x, baseScale.x, 0.3f, 6);
                    }
                    yield return null;
                }

                startRight = !startRight;
                if (d < dashes - 1) yield return Wait(0.2f);
            }

            anim.Play(BossAnimState.Idle);
            yield return MoveTo(new Vector2((lo + hi) * 0.5f, homePos.y), 0.8f);
            moveLocked = false;
        }

        void CheckContact(float radius)
        {
            Collider2D pc = BossHit.GetPlayerCollider(playerTag);
            if (pc == null) return;

            Vector2 pos = transform.position;
            Vector2 closest = pc.ClosestPoint(pos);
            float d = Vector2.Distance(closest, pos);

            if (d <= radius)
            {
                BossHit.TryDamage(pc, 1);
            }
            else if (!dashGrazed && d <= radius + 0.9f && Time.timeScale < 0.5f)
            {
                dashGrazed = true;
                BossProjectile.RaiseGraze(closest);
            }
        }

        // ------------------------------------------------------------
        // 攻撃4(怒りフェーズのみ): ウィスプスパイラル
        //  回転する弾の腕。途中で回転方向が逆になる。スローで腕の間を縫う。
        // ------------------------------------------------------------
        IEnumerator Spiral()
        {
            yield return Telegraph(0.8f);
            anim.Play(BossAnimState.Attack, false);

            float dur = 4f;
            float interval = 0.075f;
            float spin = 95f;
            int arms = 3;
            float speed = 6.5f;

            float angle = Random.Range(0f, 360f);
            float dirSpin = Random.value < 0.5f ? -1f : 1f;
            bool flipped = false;
            float acc = 0f;
            float t = 0f;

            while (t < dur)
            {
                float dt = Time.deltaTime;
                t += dt;
                acc += dt;
                angle += spin * dirSpin * dt;

                if (!flipped && t > dur * 0.5f)
                {
                    flipped = true;
                    dirSpin = -dirSpin;
                }

                while (acc >= interval)
                {
                    acc -= interval;
                    for (int a = 0; a < arms; a++)
                    {
                        SpawnBullet(transform.position, Dir(angle + 360f / arms * a) * speed, 0.9f);
                    }
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

        // 予兆: 溜めモーション + 小刻みに震える
        IEnumerator Telegraph(float duration)
        {
            anim.Play(BossAnimState.Charge);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                transform.localScale = baseScale * (1f + 0.05f * Mathf.Sin(t * 20f));
                yield return null;
            }
            transform.localScale = baseScale;
        }

        // 予兆: 外側から収束する輪
        IEnumerator WarnRing(float duration, Color c)
        {
            if (ringSprite == null) yield break;
            GameObject go = new GameObject("BossWarnRing");
            warns.Add(go);
            SpriteRenderer s = go.AddComponent<SpriteRenderer>();
            s.sprite = ringSprite;
            s.sortingOrder = 6;

            float t = 0f;
            while (t < duration)
            {
                if (go == null) yield break;
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                go.transform.position = transform.position;
                go.transform.localScale = Vector3.one * Mathf.Lerp(7f, 1.2f, k);
                Color cc = c;
                cc.a = c.a * k;
                s.color = cc;
                yield return null;
            }
            DestroyWarn(go);
        }

        IEnumerator MoveTo(Vector2 target, float duration)
        {
            Vector3 a = transform.position;
            Vector3 b = new Vector3(target.x, target.y, a.z);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            transform.position = b;
        }

        BossProjectile SpawnBullet(Vector2 pos, Vector2 vel, float scale)
        {
            GameObject go = new GameObject("BossProjectile");
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;

            SpriteRenderer s = go.AddComponent<SpriteRenderer>();
            s.sprite = bulletSprite;
            s.sortingOrder = 8;

            BossProjectile b = go.AddComponent<BossProjectile>();
            b.velocity = vel;
            b.hitRadius = 0.28f * scale; // 見た目より少し小さい判定(ギリギリで当たらない気持ちよさ)
            b.killBelowY = groundY - 0.4f;
            b.killMinX = arenaMinX - 15f;
            b.killMaxX = arenaMaxX + 15f;
            b.playerTag = playerTag;
            return b;
        }

        GameObject MakeWarn(Vector2 center, Vector2 size, Color c)
        {
            GameObject go = new GameObject("BossWarn");
            go.transform.position = center;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            SpriteRenderer s = go.AddComponent<SpriteRenderer>();
            s.sprite = warnSprite;
            s.color = c;
            s.sortingOrder = 6;
            warns.Add(go);
            return go;
        }

        void DestroyWarn(GameObject go)
        {
            warns.Remove(go);
            if (go != null) Destroy(go);
        }

        void ClearWarns()
        {
            for (int i = 0; i < warns.Count; i++)
            {
                if (warns[i] != null) Destroy(warns[i]);
            }
            warns.Clear();
        }

        void ClearBullets()
        {
            List<BossProjectile> copy = new List<BossProjectile>(BossProjectile.All);
            for (int i = 0; i < copy.Count; i++)
            {
                if (copy[i] != null) Destroy(copy[i].gameObject);
            }
        }

        static Vector2 Dir(float deg)
        {
            float r = deg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
        }

        float AimAngle()
        {
            Transform pl = Player;
            if (pl == null) return -90f;
            Vector2 d = pl.position - transform.position;
            return Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        }

        // 画面に見えている範囲に攻撃を収める(カメラが追従する想定)
        void GetRange(out float min, out float max, out float top)
        {
            min = arenaMinX;
            max = arenaMaxX;
            top = groundY + 9f;

            Camera cam = Camera.main;
            if (cam != null && cam.orthographic)
            {
                float halfW = cam.orthographicSize * cam.aspect;
                float cx = cam.transform.position.x;
                float lo = Mathf.Max(arenaMinX, cx - halfW + 1.5f);
                float hi = Mathf.Min(arenaMaxX, cx + halfW - 1.5f);
                if (hi - lo >= 10f)
                {
                    min = lo;
                    max = hi;
                }
                top = cam.transform.position.y + cam.orthographicSize + 0.5f;
            }
        }

        // ------------------------------------------------------------
        // HPバー(ボスの頭上)
        // ------------------------------------------------------------
        void BuildBar()
        {
            GameObject root = new GameObject("BossHpBar");
            barRoot = root.transform;

            GameObject back = new GameObject("bg");
            back.transform.SetParent(barRoot, false);
            back.transform.localScale = new Vector3(BarWidth + 0.12f, 0.34f, 1f);
            SpriteRenderer sb = back.AddComponent<SpriteRenderer>();
            sb.sprite = warnSprite;
            sb.color = new Color(0f, 0f, 0f, 0.7f);
            sb.sortingOrder = 30;

            GameObject fill = new GameObject("fill");
            fill.transform.SetParent(barRoot, false);
            barFill = fill.transform;
            SpriteRenderer sf = fill.AddComponent<SpriteRenderer>();
            sf.sprite = warnSprite;
            sf.color = new Color(0.9f, 0.2f, 0.4f, 1f);
            sf.sortingOrder = 31;

            root.SetActive(false);
        }

        void UpdateBar()
        {
            if (barRoot == null) return;
            bool show = fighting && !dead;
            if (barRoot.gameObject.activeSelf != show) barRoot.gameObject.SetActive(show);
            if (!show) return;

            barRoot.position = transform.position + Vector3.up * 2.6f;
            float r = Mathf.Clamp01((float)hp / Mathf.Max(1, maxHp));
            barFill.localScale = new Vector3(BarWidth * r, 0.22f, 1f);
            barFill.localPosition = new Vector3(-BarWidth * 0.5f + BarWidth * r * 0.5f, 0f, 0f);
        }
    }
}
