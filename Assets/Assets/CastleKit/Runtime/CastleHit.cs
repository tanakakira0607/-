using UnityEngine;

namespace CastleKit
{
    /// <summary>
    /// 魔王城の敵・罠がプレイヤーに当たったときの処理を1か所にまとめたもの。
    ///
    /// PlayerHealth は "Slider" タグのオブジェクトから取得します:
    ///   playerHealth = GameObject.FindWithTag("Slider").GetComponent&lt;PlayerHealth&gt;();
    /// ダメージは playerHealth.TakeDamage(1) で入ります。
    /// ★ PlayerHealth のクラス名・名前空間が違う場合は、このファイルだけ直してください。
    /// </summary>
    public static class CastleHit
    {
        // PlayerHealth が付いているオブジェクトのタグ
        public static string healthTag = "Player";

        // 連続ヒット防止(実時間の秒)。PlayerHealth側に無敵時間があるなら小さくしてOK
        public static float cooldown = 0.6f;

        static float nextAllowed;
        static Collider2D playerCol;
        static PlayerHealth playerHealth;
        static bool warned;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic()
        {
            nextAllowed = 0f;
            playerCol = null;
            playerHealth = null;
            warned = false;
        }

        // 当たり判定に使うプレイヤーのコライダー(Playerタグ)
        public static Collider2D GetPlayerCollider(string tag = "Player")
        {
            if (playerCol == null)
            {
                GameObject go = GameObject.FindGameObjectWithTag(tag);
                if (go != null)
                {
                    playerCol = go.GetComponent<Collider2D>();
                    if (playerCol == null) playerCol = go.GetComponentInChildren<Collider2D>();
                }
            }
            return playerCol;
        }

        // 円(中心・半径)とプレイヤーが触れているか
        public static bool Touches(Vector2 center, float radius, string tag = "Player")
        {
            Collider2D pc = GetPlayerCollider(tag);
            if (pc == null) return false;
            return Vector2.Distance(pc.ClosestPoint(center), center) <= radius;
        }

        // 四角(中心・半分のサイズ)とプレイヤーが重なっているか
        public static bool Overlaps(Vector2 center, Vector2 halfSize, string tag = "Player")
        {
            Collider2D pc = GetPlayerCollider(tag);
            if (pc == null) return false;
            Bounds b = pc.bounds;
            return Mathf.Abs(b.center.x - center.x) <= halfSize.x + b.extents.x
                && Mathf.Abs(b.center.y - center.y) <= halfSize.y + b.extents.y;
        }

        // Slider タグのオブジェクトから PlayerHealth を取得(見つかるまで毎回探す)
        static PlayerHealth GetPlayerHealth()
        {
            if (playerHealth == null)
            {
                GameObject go = GameObject.FindWithTag(healthTag);
                if (go != null) playerHealth = go.GetComponent<PlayerHealth>();

                if (playerHealth == null && !warned)
                {
                    warned = true;
                    Debug.LogWarning("CastleHit: タグ \"" + healthTag + "\" のオブジェクトに PlayerHealth が見つかりません。ダメージが入りません。");
                }
            }
            return playerHealth;
        }

        public static bool TryDamage(int amount = 1)
        {
            if (Time.unscaledTime < nextAllowed) return false;

            PlayerHealth health = GetPlayerHealth();
            if (health == null) return false;

            health.TakeDamage(1);
            nextAllowed = Time.unscaledTime + cooldown;
            return true;
        }
    }
}
