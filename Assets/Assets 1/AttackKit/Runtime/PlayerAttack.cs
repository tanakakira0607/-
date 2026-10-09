using System;
using UnityEngine;
using UnityEngine.Events;

namespace AttackKit
{
    /// <summary>
    /// プレイヤーの攻撃(ショット + チャージショット)。プレイヤーにアタッチします。
    ///
    ///  - 攻撃キー(既定 Z / マウス左)を押す      → 通常弾を即発射
    ///  - 押しっぱなしで溜める(光球が育つ)       → 離すとチャージ弾(大きくて貫通・高威力)
    ///
    /// 【制限時間の消費】攻撃すると Fired(cost) が呼ばれます。自作の時間管理につないでください:
    ///   AttackKit.PlayerAttack.Fired += cost => slowTime = Mathf.Max(0f, slowTime - cost);
    ///   AttackKit.PlayerAttack.CanAfford = cost => slowTime >= cost;   // 足りないと撃てない(任意)
    ///
    /// 発射間隔・溜め時間は実時間なので、スロー中でも操作感が変わりません。
    /// </summary>
    public class PlayerAttack : MonoBehaviour
    {
        // 攻撃で消費する時間(秒)を受け取る
        public static event Action<float> Fired;
        // 撃てるか(時間が足りるか)を返す関数。既定は常に撃てる
        public static Func<float, bool> CanAfford = cost => true;

        [Header("入力")]
        public KeyCode attackKey = KeyCode.Z;
        public bool useMouseLeft = true;

        [Header("通常弾")]
        public GameObject shotPrefab;
        public float fireInterval = 0.22f;
        public float normalCost = 0.5f;      // 消費する時間(秒)

        [Header("チャージ弾")]
        public GameObject chargedPrefab;
        public float chargeDelay = 0.3f;     // 押してから溜め始めるまで
        public float chargeTime = 1.0f;      // 溜め始めてから満タンまで
        public float chargedCost = 1.5f;

        [Header("位置と向き")]
        public Vector2 muzzleOffset = new Vector2(0.7f, 0.1f);
        public bool followFlipX = true;      // プレイヤーのスプライトが左向き(flipX)なら左へ撃つ

        [Header("見た目(自動で入ります)")]
        public Sprite[] muzzleFrames;
        public Sprite[] chargeFrames;
        public Sprite[] chargeFullFrames;

        [Header("音(任意)")]
        public AudioClip fireClip;
        public AudioClip chargedClip;
        public AudioClip chargeFullClip;

        [Header("イベント")]
        public UnityEvent onFire;
        public UnityEvent onChargedFire;
        public UnityEvent onChargeFull;

        public bool IsCharging { get { return holding && holdTime >= chargeDelay; } }
        public float ChargeRatio { get { return Mathf.Clamp01((holdTime - chargeDelay) / Mathf.Max(0.01f, chargeTime)); } }

        SpriteRenderer playerSr;
        GameObject chargeGo;
        SpriteRenderer chargeSr;
        float nextFire;
        float holdTime;
        bool holding, fullNotified;

        void Awake()
        {
            playerSr = GetComponent<SpriteRenderer>();
            if (playerSr == null) playerSr = GetComponentInChildren<SpriteRenderer>();
            BuildChargeFx();
        }

        void OnDestroy()
        {
            if (chargeGo != null) Destroy(chargeGo);
        }

        void Update()
        {
            bool down = Input.GetKeyDown(attackKey) || (useMouseLeft && Input.GetMouseButtonDown(0));
            bool held = Input.GetKey(attackKey) || (useMouseLeft && Input.GetMouseButton(0));
            bool up = Input.GetKeyUp(attackKey) || (useMouseLeft && Input.GetMouseButtonUp(0));

            if (down)
            {
                FireNormal();
                holding = true;
                holdTime = 0f;
                fullNotified = false;
            }

            if (holding && held)
            {
                holdTime += Time.unscaledDeltaTime;
                UpdateChargeFx();

                if (!fullNotified && holdTime >= chargeDelay + chargeTime)
                {
                    fullNotified = true;
                    if (onChargeFull != null) onChargeFull.Invoke();
                    Play(chargeFullClip);
                }
            }

            if (holding && !held)
            {
                if (holdTime >= chargeDelay + chargeTime) FireCharged();
                holding = false;
                holdTime = 0f;
                HideChargeFx();
            }
        }

        // ------------------------------------------------------------
        // 発射
        // ------------------------------------------------------------
        void FireNormal()
        {
            if (shotPrefab == null || Time.unscaledTime < nextFire) return;
            if (!CanAfford(normalCost)) return;

            nextFire = Time.unscaledTime + fireInterval;
            Spawn(shotPrefab, normalCost, 1f);
            if (onFire != null) onFire.Invoke();
            Play(fireClip);
        }

        void FireCharged()
        {
            if (chargedPrefab == null) return;
            if (!CanAfford(chargedCost)) return;

            nextFire = Time.unscaledTime + fireInterval;
            Spawn(chargedPrefab, chargedCost, 1.7f);
            if (onChargedFire != null) onChargedFire.Invoke();
            Play(chargedClip);
        }

        void Spawn(GameObject prefab, float cost, float flashScale)
        {
            float sign = FacingSign();
            Vector2 pos = MuzzlePosition(sign);

            GameObject go = Instantiate(prefab, pos, Quaternion.identity);
            PlayerShot shot = go.GetComponent<PlayerShot>();
            if (shot != null) shot.Launch(new Vector2(sign, 0f));

            AttackFx.Spawn(muzzleFrames, pos, 24f, flashScale, 0f, 19, true);
            if (Fired != null) Fired(cost);
        }

        float FacingSign()
        {
            float sign = Mathf.Sign(transform.lossyScale.x);
            if (followFlipX && playerSr != null && playerSr.flipX) sign = -sign;
            return sign;
        }

        Vector2 MuzzlePosition(float sign)
        {
            return (Vector2)transform.position + new Vector2(muzzleOffset.x * sign, muzzleOffset.y);
        }

        // ------------------------------------------------------------
        // 溜め中の光球
        // ------------------------------------------------------------
        void BuildChargeFx()
        {
            chargeGo = new GameObject("ChargeFx");
            chargeSr = chargeGo.AddComponent<SpriteRenderer>();
            chargeSr.sortingOrder = 15;
            chargeGo.SetActive(false);
        }

        void UpdateChargeFx()
        {
            if (chargeGo == null || chargeFrames == null || chargeFrames.Length == 0) return;

            if (holdTime < chargeDelay)
            {
                if (chargeGo.activeSelf) chargeGo.SetActive(false);
                return;
            }
            if (!chargeGo.activeSelf) chargeGo.SetActive(true);

            chargeGo.transform.position = MuzzlePosition(FacingSign());

            float ratio = ChargeRatio;
            if (ratio < 1f)
            {
                int idx = Mathf.Min(chargeFrames.Length - 1, Mathf.FloorToInt(ratio * chargeFrames.Length));
                chargeSr.sprite = chargeFrames[idx];
                chargeGo.transform.localScale = Vector3.one;
            }
            else if (chargeFullFrames != null && chargeFullFrames.Length > 0)
            {
                // 満タン: 光球が点滅して脈打つ
                chargeSr.sprite = chargeFullFrames[Mathf.FloorToInt(Time.unscaledTime * 14f) % chargeFullFrames.Length];
                chargeGo.transform.localScale = Vector3.one * (1f + 0.12f * Mathf.Sin(Time.unscaledTime * 30f));
            }
        }

        void HideChargeFx()
        {
            if (chargeGo != null) chargeGo.SetActive(false);
        }

        void Play(AudioClip clip)
        {
            if (clip != null) AudioSource.PlayClipAtPoint(clip, transform.position);
        }
    }
}
