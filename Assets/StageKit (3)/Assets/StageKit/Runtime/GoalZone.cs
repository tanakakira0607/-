using UnityEngine;
using UnityEngine.Events;

namespace StageKit
{
    /// <summary>
    /// ゴールの門。触れると onReached が呼ばれるだけ。処理は自作側で登録してください。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class GoalZone : MonoBehaviour
    {
        public string playerTag = "Player";
        public UnityEvent onReached;

        bool done;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (done || !other.CompareTag(playerTag)) return;
            done = true;
            if (onReached != null) onReached.Invoke();
        }
    }
}
