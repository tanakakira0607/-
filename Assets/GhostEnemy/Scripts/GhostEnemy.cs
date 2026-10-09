using UnityEngine;

/// <summary>
/// ひねくれ霊：Playerタグのキャラを追尾する敵。
/// スプライトのフレーム切り替えでアニメーションします（Animator不要）。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class GhostEnemy : MonoBehaviour
{
    public enum State { Idle, Alert, Chase, Attack, Damage, Vanish }
    PlayerHealth playerHealth;
    [Header("Sprites (Tools > Ghost > Create Ghost Prefab で自動設定)")]
    public Sprite[] idleFrames;
    public Sprite[] moveFrames;
    public Sprite[] alertFrames;
    public Sprite[] attackFrames;
    public Sprite[] damageFrames;
    public Sprite[] vanishFrames;

    [Header("追尾")]
    public string targetTag = "Player";
    public float moveSpeed = 1.2f;        // ゆっくり追ってくる
    public float detectRange = 6f;        // この距離に入ったら発見
    public float loseRange = 9f;          // この距離より離れたら見失う
    public float retargetInterval = 0.25f;

    [Header("攻撃")]
    public float attackRange = 0.9f;
    public float attackCooldown = 1.5f;
    public int attackDamage = 1;          // 対象に TakeDamage(int) があれば呼ぶ

    [Header("ステータス")]
    public int hp = 3;
    public float alertDuration = 0.6f;
    public float damageDuration = 0.35f;
    public float knockback = 1.5f;

    [Header("アニメ速度 (fps)")]
    public float idleFps = 4f;
    public float moveFps = 8f;
    public float alertFps = 10f;
    public float attackFps = 10f;
    public float damageFps = 8f;
    public float vanishFps = 6f;

    public State Current { get; private set; } = State.Idle;

    SpriteRenderer sr;
    Transform target;
    float stateTime, animTime, retargetTimer, attackTimer;
    Vector2 knockVel;
    bool attackHitDone;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    void Start()
    {
        playerHealth = GameObject.FindWithTag("Slider")
           .GetComponent<PlayerHealth>();
        ChangeState(State.Idle);
    }

    void Update()
    {
        stateTime += Time.deltaTime;
        attackTimer -= Time.deltaTime;

        if (Current != State.Vanish && Current != State.Damage)
        {
            retargetTimer -= Time.deltaTime;
            if (retargetTimer <= 0f)
            {
                retargetTimer = retargetInterval;
                target = FindNearestTarget();
            }
        }

        switch (Current)
        {
            case State.Idle:   UpdateIdle();   break;
            case State.Alert:  UpdateAlert();  break;
            case State.Chase:  UpdateChase();  break;
            case State.Attack: UpdateAttack(); break;
            case State.Damage: UpdateDamage(); break;
            case State.Vanish: UpdateVanish(); break;
        }
    }

    // ---------------- 状態ごとの処理 ----------------

    void UpdateIdle()
    {
        Animate(idleFrames, idleFps, true);
        if (target != null && Dist() <= detectRange) ChangeState(State.Alert);
    }

    void UpdateAlert()
    {
        FaceTarget();
        Animate(alertFrames, alertFps, true);
        if (stateTime >= alertDuration) ChangeState(State.Chase);
    }

    void UpdateChase()
    {
        if (target == null || Dist() > loseRange) { ChangeState(State.Idle); return; }

        FaceTarget();
        Animate(moveFrames, moveFps, true);

        if (Dist() <= attackRange)
        {
            if (attackTimer <= 0f) ChangeState(State.Attack);
            return;
        }
        Vector3 dir = (target.position - transform.position);
        dir.z = 0f;
        transform.position += dir.normalized * moveSpeed * Time.deltaTime;
    }

    void UpdateAttack()
    {
        FaceTarget();
        bool finished = Animate(attackFrames, attackFps, false);

        // 3フレーム目（口を大きく開けた瞬間）でヒット判定
        if (!attackHitDone && attackFrames != null && attackFrames.Length > 2 && CurrentFrameIndex(attackFrames, attackFps) >= 2)
        {

            playerHealth.TakeDamage(1);
            attackHitDone = true;
            if (target != null && Dist() <= attackRange * 1.3f)
                target.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
        }
        if (finished)
        {
            attackTimer = attackCooldown;
            ChangeState(State.Chase);
        }
    }

    void UpdateDamage()
    {
        Animate(damageFrames, damageFps, true);
        transform.position += (Vector3)(knockVel * Time.deltaTime);
        knockVel = Vector2.Lerp(knockVel, Vector2.zero, 10f * Time.deltaTime);
        if (stateTime >= damageDuration) ChangeState(target != null ? State.Chase : State.Idle);
    }

    void UpdateVanish()
    {
        bool finished = Animate(vanishFrames, vanishFps, false);
        var c = sr.color;
        c.a = Mathf.Clamp01(1f - stateTime / (vanishFrames.Length / vanishFps + 0.3f));
        sr.color = c;
        if (finished && c.a <= 0f) Destroy(gameObject);
    }

    // ---------------- 公開API ----------------

    /// <summary>外部（プレイヤーの攻撃など）から呼ぶ。</summary>
    public void TakeDamage(int amount)
    {
        if (Current == State.Vanish) return;
        hp -= amount;
        if (hp <= 0) { ChangeState(State.Vanish); return; }

        if (target != null)
            knockVel = ((Vector2)(transform.position - target.position)).normalized * knockback * 4f;
        ChangeState(State.Damage);
    }

    // ---------------- 内部ヘルパー ----------------

    void ChangeState(State s)
    {
        Current = s;
        stateTime = 0f;
        animTime = 0f;
        attackHitDone = false;
        if (s != State.Vanish) sr.color = Color.white;
    }

    Transform FindNearestTarget()
    {
        var players = GameObject.FindGameObjectsWithTag(targetTag);
        Transform best = null;
        float bestSqr = float.MaxValue;
        foreach (var p in players)
        {
            float d = (p.transform.position - transform.position).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = p.transform; }
        }
        return best;
    }

    float Dist()
    {
        Vector3 d = target.position - transform.position;
        d.z = 0f;
        return d.magnitude;
    }

    void FaceTarget()
    {
        if (target == null) return;
        float dx = target.position.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.05f) sr.flipX = dx < 0f;
    }

    int CurrentFrameIndex(Sprite[] frames, float fps) => Mathf.FloorToInt(animTime * fps);

    /// <summary>フレームを進める。ループしない場合、最後まで再生したら true。</summary>
    bool Animate(Sprite[] frames, float fps, bool loop)
    {
        if (frames == null || frames.Length == 0) return true;
        animTime += Time.deltaTime;
        int idx = Mathf.FloorToInt(animTime * fps);
        bool finished = idx >= frames.Length;
        idx = loop ? idx % frames.Length : Mathf.Min(idx, frames.Length - 1);
        sr.sprite = frames[idx];
        return !loop && finished;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;    Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
