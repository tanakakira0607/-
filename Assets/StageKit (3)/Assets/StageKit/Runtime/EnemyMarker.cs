using UnityEngine;

namespace StageKit
{
    /// <summary>
    /// 敵(幽霊)の配置位置と動かし方のメモを持つ印。
    /// 自作の幽霊を置くときは、この位置・値を参考にしてください。
    /// 再生中は見た目を隠します。
    /// </summary>
    public class EnemyMarker : MonoBehaviour
    {
        [TextArea] public string behaviour;
        public float patrolLeft;
        public float patrolRight;
        public float bobAmplitude;
        public bool hideInPlay = true;

        void Awake()
        {
            if (!hideInPlay) return;
            foreach (SpriteRenderer r in GetComponentsInChildren<SpriteRenderer>()) r.enabled = false;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.9f);
            Vector3 p = transform.position;
            if (patrolLeft > 0f || patrolRight > 0f)
            {
                Vector3 a = p + Vector3.left * patrolLeft;
                Vector3 b = p + Vector3.right * patrolRight;
                Gizmos.DrawLine(a, b);
                Gizmos.DrawWireSphere(a, 0.15f);
                Gizmos.DrawWireSphere(b, 0.15f);
            }
            if (bobAmplitude > 0f)
            {
                Gizmos.DrawLine(p + Vector3.up * bobAmplitude, p + Vector3.down * bobAmplitude);
            }
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(behaviour))
                UnityEditor.Handles.Label(p + Vector3.up * 0.9f, behaviour);
#endif
        }
    }
}
