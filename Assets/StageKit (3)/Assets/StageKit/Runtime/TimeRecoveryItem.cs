using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace StageKit
{
    /// <summary>
    /// ふわふわ浮かぶ回復アイテム。取ると消えて onCollected が呼ばれるだけ。
    /// 実際の回復処理は onCollected に自作の関数を登録してください(recoverSeconds は値の置き場)。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TimeRecoveryItem : MonoBehaviour
    {
        public float recoverSeconds = 5f;
        public string playerTag = "Player";
        public float bobHeight = 0.15f;
        public float bobSpeed = 2f;
        public bool respawn = false;
        public float respawnTime = 10f;
        public UnityEvent onCollected;

        Vector3 basePos;
        bool taken;
        Collider2D col;
        SpriteRenderer[] renderers;

        void Awake()
        {
            basePos = transform.position;
            col = GetComponent<Collider2D>();
            renderers = GetComponentsInChildren<SpriteRenderer>();
        }

        void Update()
        {
            if (taken) return;
            transform.position = basePos + Vector3.up * Mathf.Sin(Time.unscaledTime * bobSpeed) * bobHeight;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (taken || !other.CompareTag(playerTag)) return;
            taken = true;
            if (onCollected != null) onCollected.Invoke();
            SetVisible(false);
            if (respawn) StartCoroutine(Restore());
        }

        IEnumerator Restore()
        {
            yield return new WaitForSecondsRealtime(respawnTime);
            taken = false;
            SetVisible(true);
        }

        void SetVisible(bool v)
        {
            col.enabled = v;
            for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = v;
        }
    }
}
