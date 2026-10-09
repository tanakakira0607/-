using UnityEngine;

namespace BossKit
{
    /// <summary>
    /// ボスの攻撃がプレイヤーに当たったときの処理を1か所にまとめたもの。
    ///
    /// PlayerHealth は "Slider" タグのオブジェクトから取得します:
    ///   playerHealth = GameObject.FindWithTag("Slider").GetComponent&lt;PlayerHealth&gt;();
    /// ダメージは playerHealth.TakeDamage(1) で入ります。
    /// ★ PlayerHealth のクラス名・名前空間が違う場合は、このファイルだけ直してください。
    /// </summary>
    public static class BossHit
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

        // 当たり判定に使うプレイヤーのコライダー
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
                    Debug.LogWarning("BossHit: タグ \"" + healthTag + "\" のオブジェクトに PlayerHealth が見つかりません。ボスのダメージが入りません。");
                }
            }
            return playerHealth;
        }

        // playerCollider は当たり判定側で使うため引数に残してあります(ダメージ自体には使いません)
        public static bool TryDamage(Collider2D playerCollider, int amount = 1)
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
