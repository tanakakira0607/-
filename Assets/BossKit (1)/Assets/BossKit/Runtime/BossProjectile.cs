using System;
using System.Collections.Generic;
using UnityEngine;

namespace BossKit
{
    /// <summary>
    /// ボスの弾。Time.deltaTime で動くので、スロー中はゆっくり進みます。
    /// 当たり判定は距離で計算(スローでも安定)。当たると BossHit 経由で TakeDamage(1)。
    /// スロー中にギリギリをかすめると「ニアミス」演出と Grazed イベントが出ます。
    /// </summary>
    public class BossProjectile : MonoBehaviour
    {
        public static readonly List<BossProjectile> All = new List<BossProjectile>();

        // ニアミス(グレイズ)した位置。SE再生やスローゲージ回復などに使えます
        public static event Action<Vector2> Grazed;

        public Vector2 velocity;
        public Vector2 acceleration;
        public float hitRadius = 0.28f;
        public float grazeMargin = 0.8f;
        public float lifeTime = 8f;
        public float killBelowY = -50f;
        public float killMinX = -1e6f;
        public float killMaxX = 1e6f;
        public string playerTag = "Player";

        float age;
        bool grazed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic()
        {
            All.Clear();
            Grazed = null;
        }

        public static void RaiseGraze(Vector2 pos)
        {
            if (Grazed != null) Grazed(pos);
            BossFx.Spawn(BossFx.RingSprite, pos, new Color(0.5f, 1f, 1f, 1f), 0.3f, 1.6f, 0.35f);
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Update()
        {
            float dt = Time.deltaTime;
            velocity += acceleration * dt;
            transform.position += (Vector3)(velocity * dt);

            age += dt;
            Vector3 p = transform.position;
            if (age > lifeTime || p.y < killBelowY || p.x < killMinX || p.x > killMaxX)
            {
                Destroy(gameObject);
                return;
            }

            CheckPlayer();
        }

        void CheckPlayer()
        {
            Collider2D pc = BossHit.GetPlayerCollider(playerTag);
            if (pc == null) return;

            Vector2 pos = transform.position;
            Vector2 closest = pc.ClosestPoint(pos);
            float d = Vector2.Distance(closest, pos);

            if (d <= hitRadius)
            {
                if (BossHit.TryDamage(pc, 1)) Destroy(gameObject);
                return;
            }

            // スロー中だけ、ギリギリを抜けたらニアミス演出
            if (!grazed && d <= hitRadius + grazeMargin && Time.timeScale < 0.5f)
            {
                grazed = true;
                RaiseGraze(closest);
            }
        }
    }
}
