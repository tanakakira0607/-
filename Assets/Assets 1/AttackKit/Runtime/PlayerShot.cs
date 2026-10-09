using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace AttackKit
{
    /// <summary>
    /// プレイヤーの弾。Rigidbody2D の速度で飛びます(元の Bullet と同じ方式)。
    ///
    /// 当たった相手に TakeDamage(int または float) というメソッドがあれば、自動でそれを呼びます。
    /// 親オブジェクトも探すので、あなたの幽霊・CastleKit のモブ・魔王・BossKit のボスに、そのまま当たります。
    ///  - 壁・地面(トリガーでないコライダー)に当たると消えます。
    ///  - トリガーのアイテムやチェックポイントは素通りします。
    ///  - pierce が 1 以上なら、その数だけ敵を貫通します(チャージ弾)。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerShot : MonoBehaviour
    {
        public float speed = 20f;
        public int damage = 1;
        public int pierce = 0;            // 貫通できる敵の数
        public float lifeTime = 5f;
        public float maxDistance = 60f;

        // 当たらないタグ(プレイヤー自身と体力ゲージ)
        public string[] ignoreTags = { "Player", "Slider" };

        [Header("見た目")]
        public Sprite[] frames;
        public float fps = 14f;
        public Sprite[] hitFrames;
        public float hitScale = 1f;
        public AudioClip hitClip;

        Rigidbody2D body;
        SpriteRenderer sr;
        Vector2 dir = Vector2.right;
        Vector3 startPos;
        int pierceLeft;
        float animT;
        bool launched, dead;
        readonly HashSet<int> hitIds = new HashSet<int>();
        static readonly Dictionary<Type, MethodInfo> methodCache = new Dictionary<Type, MethodInfo>();

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            sr = GetComponent<SpriteRenderer>();
        }

        // 発射(向きを指定)
        public void Launch(Vector2 direction)
        {
            dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            Begin();
        }

        void Start()
        {
            // シーンに直接置かれた場合は、向き(transform.right)に飛ぶ
            if (!launched)
            {
                dir = transform.right;
                Begin();
            }
        }

        void Begin()
        {
            launched = true;
            startPos = transform.position;
            pierceLeft = pierce;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            SetVelocity(dir * speed);
            Destroy(gameObject, lifeTime);
        }

        void SetVelocity(Vector2 v)
        {
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = v;
#else
            body.velocity = v;
#endif
        }

        void Update()
        {
            // 弾の見た目をコマ送り(実時間)
            if (frames != null && frames.Length > 0 && sr != null)
            {
                animT += Time.unscaledDeltaTime;
                sr.sprite = frames[Mathf.FloorToInt(animT * fps) % frames.Length];
            }

            if (!dead && Vector3.Distance(startPos, transform.position) > maxDistance) Kill();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (dead || IsIgnored(other)) return;

            int id = other.gameObject.GetInstanceID();
            if (hitIds.Contains(id)) return;

            // 敵(TakeDamage を持つもの)
            if (TryDamage(other))
            {
                hitIds.Add(id);
                SpawnHit();
                pierceLeft--;
                if (pierceLeft < 0) Kill();
                return;
            }

            // 壁・地面(トリガーでないコライダー)
            if (!other.isTrigger)
            {
                SpawnHit();
                Kill();
            }
        }

        bool IsIgnored(Collider2D other)
        {
            for (int i = 0; i < ignoreTags.Length; i++)
            {
                if (string.IsNullOrEmpty(ignoreTags[i])) continue;
                try
                {
                    if (other.CompareTag(ignoreTags[i])) return true;
                }
                catch (UnityException)
                {
                    // プロジェクトに無いタグ名は無視
                }
            }
            return false;
        }

        bool TryDamage(Collider2D other)
        {
            MonoBehaviour[] list = other.GetComponentsInParent<MonoBehaviour>();
            for (int i = 0; i < list.Length; i++)
            {
                MonoBehaviour mb = list[i];
                if (mb == null) continue;
                MethodInfo m = FindTakeDamage(mb.GetType());
                if (m == null) continue;

                Type pt = m.GetParameters()[0].ParameterType;
                object arg = pt == typeof(int) ? (object)damage : (object)(float)damage;
                m.Invoke(mb, new object[] { arg });
                return true;
            }
            return false;
        }

        static MethodInfo FindTakeDamage(Type t)
        {
            MethodInfo found;
            if (methodCache.TryGetValue(t, out found)) return found;

            found = null;
            MethodInfo[] methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name != "TakeDamage") continue;
                ParameterInfo[] ps = methods[i].GetParameters();
                if (ps.Length == 1 && (ps[0].ParameterType == typeof(int) || ps[0].ParameterType == typeof(float)))
                {
                    found = methods[i];
                    break;
                }
            }
            methodCache[t] = found;
            return found;
        }

        void SpawnHit()
        {
            AttackFx.Spawn(hitFrames, transform.position, 22f, hitScale, 0f, 21, true);
            if (hitClip != null) AudioSource.PlayClipAtPoint(hitClip, transform.position);
        }

        void Kill()
        {
            dead = true;
            Destroy(gameObject);
        }
    }
}
