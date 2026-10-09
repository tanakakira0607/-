using UnityEngine;
using UnityEngine.Events;

namespace StageKit
{
    /// <summary>
    /// 触れるとランタンが灯るだけの飾りギミック。
    /// 復活地点の保存などは onActivated に自作の処理を登録してください。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        public Sprite offSprite;
        public Sprite onSprite;
        public string playerTag = "Player";
        public UnityEvent onActivated;

        SpriteRenderer sr;
        bool active;

        void Awake()
        {
            sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null && offSprite != null) sr.sprite = offSprite;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (active || !other.CompareTag(playerTag)) return;
            active = true;
            if (sr != null && onSprite != null) sr.sprite = onSprite;
            if (onActivated != null) onActivated.Invoke();
        }
    }
}
