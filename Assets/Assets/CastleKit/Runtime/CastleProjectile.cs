using System;
using System.Collections.Generic;
using UnityEngine;

namespace CastleKit
{
    /// <summary>
    /// 魔導士・魔王の弾。Time.deltaTime で動くので、スロー中はゆっくり進みます。
    /// 当たり判定は距離で計算(スローでも安定)。当たると CastleHit 経由で TakeDamage(1)。
    /// スロー中にギリギリをかすめると「ニアミス」演出と Grazed イベントが出ます。
    /// </summary>
    public class CastleProjectile : MonoBehaviour
    {
        public static readonly List<CastleProjectile> All = new List<CastleProjectile>();

        // ニアミス(グレイズ)した位置。SE再生やスローゲージ回復などに使えます
        public static event Action<Vector2> Grazed;

        public Vector2 velocity;
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

        public static CastleProjectile Spawn(Sprite sprite, Vector2 pos, Vector2 vel, float scale,
                                             float killBelowY, float killMinX, float killMaxX, string playerTag)
        {
            GameObject go = new GameObject("CastleProjectile");
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;

            SpriteRenderer s = go.AddComponent<SpriteRenderer>();
            s.sprite = sprite;
            s.sortingOrder = 8;

            CastleProjectile b = go.AddComponent<CastleProjectile>();
            b.velocity = vel;
            b.hitRadius = 0.28f * scale; // 見た目より少し小さい判定(ギリギリで当たらない気持ちよさ)
            b.killBelowY = killBelowY;
            b.killMinX = killMinX;
            b.killMaxX = killMaxX;
            b.playerTag = playerTag;
            return b;
        }

        public static void RaiseGraze(Vector2 pos)
        {
            if (Grazed != null) Grazed(pos);
            CastleFx.Spawn(CastleFx.RingSprite, pos, new Color(0.5f, 1f, 1f, 1f), 0.3f, 1.6f, 0.35f);
        }

        public static void ClearAll()
        {
            List<CastleProjectile> copy = new List<CastleProjectile>(All);
            for (int i = 0; i < copy.Count; i++)
            {
                if (copy[i] != null) Destroy(copy[i].gameObject);
            }
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Update()
        {
            float dt = Time.deltaTime;
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
            Collider2D pc = CastleHit.GetPlayerCollider(playerTag);
            if (pc == null) return;

            Vector2 pos = transform.position;
            Vector2 closest = pc.ClosestPoint(pos);
            float d = Vector2.Distance(closest, pos);

            if (d <= hitRadius)
            {
                if (CastleHit.TryDamage(1)) Destroy(gameObject);
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
