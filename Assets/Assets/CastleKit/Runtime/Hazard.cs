using UnityEngine;

namespace CastleKit
{
    /// <summary>
    /// 触れるとダメージを受ける範囲(トゲ・溶岩・炎・振り子の刃)。
    /// コライダーは使わず、毎フレーム距離で判定するので、スロー中でも安定します。
    /// radius が 0 より大きければ円、0 なら四角(size)で判定します。
    /// </summary>
    public class Hazard : MonoBehaviour
    {
        public bool active = true;
        public int damage = 1;
        public string playerTag = "Player";
        public Vector2 size = Vector2.one;
        public Vector2 offset = Vector2.zero;
        public float radius = 0f;

        void Update()
        {
            if (!active) return;

            Vector2 center = transform.TransformPoint(offset);
            Vector3 ls = transform.lossyScale;
            bool hit;
            if (radius > 0f)
            {
                float s = Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y));
                hit = CastleHit.Touches(center, radius * s, playerTag);
            }
            else
            {
                Vector2 half = new Vector2(size.x * Mathf.Abs(ls.x), size.y * Mathf.Abs(ls.y)) * 0.5f;
                hit = CastleHit.Overlaps(center, half, playerTag);
            }

            if (hit) CastleHit.TryDamage(damage);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
            Vector3 c = transform.TransformPoint(offset);
            Vector3 ls = transform.lossyScale;
            if (radius > 0f)
                Gizmos.DrawWireSphere(c, radius * Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y)));
            else
                Gizmos.DrawWireCube(c, new Vector3(size.x * Mathf.Abs(ls.x), size.y * Mathf.Abs(ls.y), 0.1f));
        }
    }
}
