using UnityEngine;

namespace CastleKit
{
    /// <summary>
    /// 天井から揺れる大斧。子オブジェクト(鎖と刃)ごと左右に振れる。
    /// ゲーム内時間で動くので、スロー中は刃もゆっくり。タイミングを見計らって通り抜ける罠です。
    /// </summary>
    public class Pendulum : MonoBehaviour
    {
        public float amplitude = 55f;   // 振れ幅(度)
        public float period = 3.6f;     // 1往復の秒数
        public float phase = 0f;        // 0〜1 で開始位置をずらす

        float t;

        void Awake()
        {
            Apply();
        }

        void Update()
        {
            t += Time.deltaTime;
            Apply();
        }

        void Apply()
        {
            float angle = amplitude * Mathf.Sin(2f * Mathf.PI * (t / Mathf.Max(0.1f, period) + phase));
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
