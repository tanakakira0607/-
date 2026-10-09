using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CastleKit
{
    /// <summary>
    /// 魔王。プレイヤーが近づくと戦闘開始。
    ///
    /// 【スローで避けて気持ちいい設計】
    ///  - 攻撃は必ず「予兆(溜めモーション・警告ゾーン)」→「発動」の順。
    ///  - 弾・炎は速い(通常速度だと厳しい)が、隙間は広め。スローにすると止まって見え、隙間を抜けられる。
    ///  - スロー中にギリギリをかすめるとニアミス演出(輪っか)が出る。
    ///  - 攻撃の後は必ず「ぐったり」する時間があり、青白い絵になる = 反撃のチャンス。
    ///  - HPが半分になると怒りフェーズ(攻撃が増え、少し速くなる)。
    ///
    /// 攻撃: 魔炎輪 / 獄炎柱 / 魔剣突進 / 魔炎の渦(怒りフェーズのみ)
    /// 全ての動きは Time.deltaTime を使うので、Time.timeScale を下げれば遅くなります。
    /// ダメージは CastleHit 経由で playerHealth.TakeDamage(1)。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(FrameAnimator))]
    public class DemonLord : MonoBehaviour
    {
        [Header("体力")]
        public int maxHp = 40;

        [Header("戦闘エリア(魔王の初期位置が中心)")]
        public float arenaHalfWidth = 15f;
        public float groundY = 0f;
        // プレイヤーがこの横距離まで近づくと戦闘開始
        public float activateDistance = 16f;

        [Header("プレイヤー")]
        public string playerTag = "Player";

        [Header("見た目(自動で入ります)")]
        public Sprite fireSprite;
        public Sprite warnSprite;
        public Sprite ringSprite;
        public Sprite[] pillarFrames;

        [Header("イベント")]
        public UnityEvent onFightStart;
        public UnityEvent onPhase2;
        public UnityEvent onDefeated;

        public int Hp { get { return hp; } }
        public int Phase { get { return phase; } }
        public bool Fighting { get { return fighting; } }
        public bool IsDead { get { return dead; } }
        // 魔王が「ぐったり」している(反撃のチャンス)
        public bool IsTired { get { return tired; } }

        int hp;
        int phase = 1;
        bool fighting, dead, invulnerable, moveLocked, tired, dashGrazed;
        float arenaMinX, arenaMaxX;
        Vector3 homePos, baseScale;
        float bobT, hitFlash;
        Color tint = Color.white;
        SpriteRenderer sr;
        FrameAnimator anim;
        Transform player;
        Coroutine fightRoutine;
        readonly List<GameObject> temps = new List<GameObject>();
        Transform barRoot, barFill;
        const float BarWidth = 5f;

        // ------------------------------------------------------------
        // 基本
        // ------------------------------------------------------------
        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            anim = GetComponent<FrameAnimator>();
            hp = maxHp;
            homePos = transform.position;
            baseScale = transform.localScale;
            arenaMinX = homePos.x - arenaHalfWidth;
            arenaMaxX = homePos.x + arenaHalfWidth;
            CastleFx.RingSprite = ringSprite;
            BuildBar();
        }

        void Start()
        {
            PlayIdle();
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

        void PlayIdle()
        {
            anim.Play(phase >= 2 ? "idlerage" : "idle");
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
        //   例: other.GetComponent<CastleKit.DemonLord>()?.TakeDamage(1);
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

            anim.PlayOnce("hurt");

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
            ClearTemps();
            CastleProjectile.ClearAll();
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
            anim.Play("death");

            Vector3 p = transform.position;
            float t = 0f;
            const float dur = 2.4f;
            float ringTimer = 0f;
            while (t < dur)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                ringTimer += dt;
                transform.position = p + (Vector3)(Random.insideUnitCircle * 0.12f);
                if (t > 1.2f) tint = new Color(1f, 1f, 1f, 1f - (t - 1.2f) / (dur - 1.2f));
                if (ringTimer > 0.22f)
                {
                    ringTimer = 0f;
                    CastleFx.Spawn(ringSprite, p + (Vector3)(Random.insideUnitCircle * 1.6f), new Color(1f, 0.6f, 0.4f, 1f), 0.3f, 3f, 0.5f);
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
            ClearTemps();
            CastleProjectile.ClearAll();
            phase = 2;
            transform.localScale = baseScale;
            anim.Play("charge");
            if (onPhase2 != null) onPhase2.Invoke();

            float lo, hi, top;
            GetRange(out lo, out hi, out top);
            transform.position = new Vector3((lo + hi) * 0.5f, homePos.y, transform.position.z);

            // 咆哮(実時間で揺れる)
            Vector3 p = transform.position;
            float t = 0f;
            while (t < 1.6f)
            {
                t += Time.unscaledDeltaTime;
                transform.position = p + (Vector3)(Random.insideUnitCircle * 0.1f);
                if (Mathf.Repeat(t, 0.4f) < Time.unscaledDeltaTime)
                    CastleFx.Spawn(ringSprite, p, new Color(1f, 0.4f, 0.3f, 1f), 0.5f, 5f, 0.5f);
                yield return null;
            }
            transform.position = p;
            PlayIdle();
            moveLocked = false;
            invulnerable = false;
            fightRoutine = StartCoroutine(FightLoop());
        }

        // ------------------------------------------------------------
        // 戦闘ループ
        // ------------------------------------------------------------
        IEnumerator FightLoop()
        {
            PlayIdle();
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
                    case 0: yield return FlameRing(); break;
                    case 1: yield return HellPillars(); break;
                    case 2: yield return DarkCharge(); break;
                    default: yield return FlameSpiral(); break;
                }

                // 攻撃のあとは「ぐったり」= 反撃チャンス
                tired = true;
                anim.Play("tired");
                yield return Wait(phase >= 2 ? 1.4f : 2.0f);
                tired = false;
                PlayIdle();
            }
        }

        // ------------------------------------------------------------
        // 攻撃1: 魔炎輪
        //  予兆で光が収束 → 隙間のある火球の輪が広がる。次の輪は隙間がずれる(編み込み)。
        // ------------------------------------------------------------
        IEnumerator FlameRing()
        {
            bool p2 = phase >= 2;
            int rings = p2 ? 3 : 2;
            int count = p2 ? 26 : 22;
            float speed = p2 ? 9f : 7.5f;
            float gapDeg = p2 ? 46f : 54f;

            StartCoroutine(WarnRing(1.0f, new Color(1f, 0.35f, 0.2f, 0.9f)));
            yield return Telegraph(1.0f);

            anim.Play("attack");

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
                    SpawnBullet(origin, Dir(ang) * speed, 1.2f);
                }
                side = -side;
                gap += Random.Range(22f, 38f) * side;
                yield return Wait(0.55f);
            }
        }

        // ------------------------------------------------------------
        // 攻撃2: 獄炎柱
        //  床が赤く光る(危険な列)→ 炎の柱が一気に噴き上がる。安全な列は毎波ずれていく。
        //  通常速度だと列の移動が間に合いにくく、スローなら余裕で移れる。
        // ------------------------------------------------------------
        IEnumerator HellPillars()
        {
            bool p2 = phase >= 2;
            int waves = p2 ? 4 : 3;
            float tele = p2 ? 0.85f : 1.05f;
            int safeWidth = p2 ? 1 : 2;
            const float height = 7f;

            float lo, hi, top;
            GetRange(out lo, out hi, out top);
            float step = 3.5f;
            int lanes = Mathf.Max(4, Mathf.FloorToInt((hi - lo - 2f) / step) + 1);
            float startX = (lo + hi) * 0.5f - (lanes - 1) * step * 0.5f;

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
                anim.Play("charge");

                // 警告ゾーン(危険な列だけ赤く、床は特に強く)
                List<GameObject> zones = new List<GameObject>();
                List<GameObject> floors = new List<GameObject>();
                for (int l = 0; l < lanes; l++)
                {
                    if (l >= safe && l < safe + safeWidth) continue;
                    float x = startX + l * step;
                    zones.Add(MakeWarn(new Vector2(x, groundY + height * 0.5f), new Vector2(step - 0.3f, height), new Color(1f, 0.25f, 0.1f, 0.12f)));
                    floors.Add(MakeWarn(new Vector2(x, groundY + 0.2f), new Vector2(step - 0.3f, 0.4f), new Color(1f, 0.5f, 0.2f, 0.7f)));
                }

                float dur = (w == 0) ? tele : tele * 0.8f;
                float t = 0f;
                while (t < dur)
                {
                    t += Time.deltaTime;
                    float k = Mathf.PingPong(t * 6f, 1f);
                    for (int i = 0; i < zones.Count; i++)
                    {
                        SetAlpha(zones[i], 0.08f + 0.1f * k);
                        SetAlpha(floors[i], 0.4f + 0.5f * k);
                    }
                    yield return null;
                }
                for (int i = 0; i < zones.Count; i++) { DestroyTemp(zones[i]); DestroyTemp(floors[i]); }

                // 噴き上がる炎の柱
                anim.Play("attack");
                for (int l = 0; l < lanes; l++)
                {
                    if (l >= safe && l < safe + safeWidth) continue;
                    SpawnPillar(startX + l * step, step - 0.4f, height, 0.75f);
                }

                // 次の安全レーン
                int move = (p2 ? Random.Range(1, 3) : 1) * dirSign;
                safe = Mathf.Clamp(safe + move, 0, lanes - safeWidth);
                if (safe <= 0 || safe >= lanes - safeWidth) dirSign = -dirSign;
            }

            yield return Wait(0.9f);
        }

        void SpawnPillar(float x, float width, float height, float life)
        {
            GameObject go = new GameObject("LordPillar");
            go.transform.position = new Vector3(x, groundY, 0f);
            go.transform.localScale = new Vector3(width, height / 3f, 1f); // 炎のスプライトは 1×3 ユニット、足元が基準
            SpriteRenderer s = go.AddComponent<SpriteRenderer>();
            s.sortingOrder = 6;
            if (pillarFrames != null && pillarFrames.Length > 0) s.sprite = pillarFrames[0];

            Hazard h = go.AddComponent<Hazard>();
            h.size = new Vector2(0.75f, 3f);     // スケール後に 幅×0.75 / 高さ=height になる
            h.offset = new Vector2(0f, 1.5f);
            h.playerTag = playerTag;
            h.active = true;

            temps.Add(go);
            StartCoroutine(PillarLife(go, s, life));
        }

        IEnumerator PillarLife(GameObject go, SpriteRenderer s, float life)
        {
            float t = 0f;
            while (t < life)
            {
                if (go == null) yield break;
                t += Time.deltaTime;
                if (pillarFrames != null && pillarFrames.Length > 0)
                    s.sprite = pillarFrames[Mathf.FloorToInt(t * 14f) % pillarFrames.Length];
                if (t > life * 0.75f)
                {
                    Color c = s.color;
                    c.a = Mathf.Clamp01(1f - (t - life * 0.75f) / (life * 0.25f));
                    s.color = c;
                }
                yield return null;
            }
            DestroyTemp(go);
        }

        // ------------------------------------------------------------
        // 攻撃3: 魔剣突進
        //  画面端で赤い帯を出して溜める → 一気に横切る。ジャンプかスローで避ける。
        //  怒りフェーズでは往復する。
        // ------------------------------------------------------------
        IEnumerator DarkCharge()
        {
            bool p2 = phase >= 2;
            int dashes = p2 ? 2 : 1;

            float lo, hi, top;
            GetRange(out lo, out hi, out top);
            float y = groundY + 1.3f;

            Transform pl = Player;
            bool startRight = pl == null || pl.position.x < (lo + hi) * 0.5f;

            moveLocked = true;
            for (int d = 0; d < dashes; d++)
            {
                float fromX = startRight ? hi - 0.5f : lo + 0.5f;
                float toX = startRight ? lo - 3f : hi + 3f;

                PlayIdle();
                yield return MoveTo(new Vector2(fromX, y), 0.5f);

                // 予兆: 通る道に赤い帯 + 溜めモーション
                anim.Play("charge");
                sr.flipX = !startRight; // 進む向きを向く(絵は右向き)
                float len = (hi + 3f) - (lo - 3f);
                GameObject line = MakeWarn(new Vector2((lo + hi) * 0.5f, y), new Vector2(len, 2.2f), new Color(1f, 0.2f, 0.15f, 0.2f));
                float tele = (d == 0) ? 1.1f : 0.65f;
                float t = 0f;
                while (t < tele)
                {
                    t += Time.deltaTime;
                    SetAlpha(line, 0.1f + 0.18f * Mathf.PingPong(t * 6f, 1f));
                    yield return null;
                }
                DestroyTemp(line);

                // 突進
                anim.Play("dash");
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
                    CheckContact(1.4f);

                    trail += dt;
                    if (trail > 0.035f)
                    {
                        trail = 0f;
                        CastleFx.Spawn(sr.sprite, transform.position, new Color(1f, 0.4f, 0.5f, 0.5f),
                                       baseScale.x, baseScale.x, 0.3f, 6);
                    }
                    yield return null;
                }

                startRight = !startRight;
                if (d < dashes - 1) yield return Wait(0.2f);
            }

            sr.flipX = false;
            PlayIdle();
            yield return MoveTo(new Vector2((lo + hi) * 0.5f, homePos.y), 0.8f);
            moveLocked = false;
        }

        void CheckContact(float radius)
        {
            Vector2 pos = transform.position;
            if (CastleHit.Touches(pos, radius, playerTag))
            {
                CastleHit.TryDamage(1);
                return;
            }
            if (!dashGrazed && Time.timeScale < 0.5f && CastleHit.Touches(pos, radius + 0.9f, playerTag))
            {
                dashGrazed = true;
                Collider2D pc = CastleHit.GetPlayerCollider(playerTag);
                if (pc != null) CastleProjectile.RaiseGraze(pc.ClosestPoint(pos));
            }
        }

        // ------------------------------------------------------------
        // 攻撃4(怒りフェーズのみ): 魔炎の渦
        //  回転する火球の腕。途中で回転方向が逆になる。スローで腕の間を縫う。
        // ------------------------------------------------------------
        IEnumerator FlameSpiral()
        {
            yield return Telegraph(0.9f);
            anim.Play("attack");

            float dur = 4.2f;
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
                        SpawnBullet(transform.position, Dir(angle + 360f / arms * a) * speed, 1f);
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
            anim.Play("charge");
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                transform.localScale = baseScale * (1f + 0.04f * Mathf.Sin(t * 20f));
                yield return null;
            }
            transform.localScale = baseScale;
        }

        // 予兆: 外側から収束する輪
        IEnumerator WarnRing(float duration, Color c)
        {
            if (ringSprite == null) yield break;
            GameObject go = new GameObject("LordWarnRing");
            temps.Add(go);
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
                go.transform.localScale = Vector3.one * Mathf.Lerp(9f, 1.5f, k);
                Color cc = c;
                cc.a = c.a * k;
                s.color = cc;
                yield return null;
            }
            DestroyTemp(go);
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

        void SpawnBullet(Vector2 pos, Vector2 vel, float scale)
        {
            CastleProjectile.Spawn(fireSprite, pos, vel, scale, groundY - 0.4f, arenaMinX - 15f, arenaMaxX + 15f, playerTag);
        }

        GameObject MakeWarn(Vector2 center, Vector2 size, Color c)
        {
            GameObject go = new GameObject("LordWarn");
            go.transform.position = center;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            SpriteRenderer s = go.AddComponent<SpriteRenderer>();
            s.sprite = warnSprite;
            s.color = c;
            s.sortingOrder = 6;
            temps.Add(go);
            return go;
        }

        static void SetAlpha(GameObject go, float a)
        {
            if (go == null) return;
            SpriteRenderer s = go.GetComponent<SpriteRenderer>();
            if (s == null) return;
            Color c = s.color;
            c.a = a;
            s.color = c;
        }

        void DestroyTemp(GameObject go)
        {
            temps.Remove(go);
            if (go != null) Destroy(go);
        }

        void ClearTemps()
        {
            for (int i = 0; i < temps.Count; i++)
            {
                if (temps[i] != null) Destroy(temps[i]);
            }
            temps.Clear();
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
        // HPバー(魔王の頭上)
        // ------------------------------------------------------------
        void BuildBar()
        {
            GameObject root = new GameObject("LordHpBar");
            barRoot = root.transform;

            GameObject back = new GameObject("bg");
            back.transform.SetParent(barRoot, false);
            back.transform.localScale = new Vector3(BarWidth + 0.14f, 0.38f, 1f);
            SpriteRenderer sb = back.AddComponent<SpriteRenderer>();
            sb.sprite = warnSprite;
            sb.color = new Color(0f, 0f, 0f, 0.75f);
            sb.sortingOrder = 30;

            GameObject fill = new GameObject("fill");
            fill.transform.SetParent(barRoot, false);
            barFill = fill.transform;
            SpriteRenderer sf = fill.AddComponent<SpriteRenderer>();
            sf.sprite = warnSprite;
            sf.color = new Color(0.8f, 0.15f, 0.55f, 1f);
            sf.sortingOrder = 31;

            root.SetActive(false);
        }

        void UpdateBar()
        {
            if (barRoot == null) return;
            bool show = fighting && !dead;
            if (barRoot.gameObject.activeSelf != show) barRoot.gameObject.SetActive(show);
            if (!show) return;

            barRoot.position = transform.position + Vector3.up * 3.4f;
            float r = Mathf.Clamp01((float)hp / Mathf.Max(1, maxHp));
            barFill.localScale = new Vector3(BarWidth * r, 0.24f, 1f);
            barFill.localPosition = new Vector3(-BarWidth * 0.5f + BarWidth * r * 0.5f, 0f, 0f);
        }
    }
}
