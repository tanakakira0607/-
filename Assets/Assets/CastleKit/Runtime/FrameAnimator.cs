using System;
using System.Collections.Generic;
using UnityEngine;

namespace CastleKit
{
    [Serializable]
    public class SpriteClip
    {
        public string name;
        public Sprite[] frames;
        public float fps = 8f;
        public bool loop = true;
    }

    /// <summary>
    /// ドット絵のコマ送りアニメーション(Animatorコントローラー不要)。
    /// 既定ではゲーム内時間で進むので、スロー中はアニメーションもゆっくりになります。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class FrameAnimator : MonoBehaviour
    {
        public List<SpriteClip> clips = new List<SpriteClip>();
        public bool useUnscaledTime = false;

        public string Current { get { return cur != null ? cur.name : ""; } }

        Dictionary<string, SpriteClip> map;
        SpriteRenderer sr;
        SpriteClip cur, next;
        float t;
        bool once;

        void Awake() { Build(); }

        void Build()
        {
            if (map != null) return;
            map = new Dictionary<string, SpriteClip>();
            for (int i = 0; i < clips.Count; i++)
            {
                SpriteClip c = clips[i];
                if (c != null && !string.IsNullOrEmpty(c.name)) map[c.name] = c;
            }
            sr = GetComponent<SpriteRenderer>();
        }

        SpriteClip Find(string n)
        {
            Build();
            SpriteClip c;
            if (n != null && map.TryGetValue(n, out c) && c.frames != null && c.frames.Length > 0) return c;
            return null;
        }

        // ループ設定に従って再生(同じクリップなら何もしない)
        public void Play(string n)
        {
            SpriteClip c = Find(n);
            if (c == null) return;
            if (cur == c && !once) return;
            cur = c;
            next = null;
            once = false;
            t = 0f;
            Apply();
        }

        // 1回だけ再生して、終わったら元(または thenName)に戻る(被弾など)
        public void PlayOnce(string n, string thenName = null)
        {
            SpriteClip c = Find(n);
            if (c == null) return;
            SpriteClip back = thenName != null ? Find(thenName) : (once ? next : cur);
            cur = c;
            next = back;
            once = true;
            t = 0f;
            Apply();
        }

        void Update()
        {
            if (cur == null) return;
            Build();

            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            int len = cur.frames.Length;
            int idx = Mathf.FloorToInt(t * cur.fps);

            if (cur.loop && !once)
            {
                idx %= len;
            }
            else if (idx >= len)
            {
                idx = len - 1;
                if (once)
                {
                    once = false;
                    if (next != null)
                    {
                        cur = next;
                        next = null;
                        t = 0f;
                        Apply();
                        return;
                    }
                }
            }

            if (sr.sprite != cur.frames[idx]) sr.sprite = cur.frames[idx];
        }

        void Apply()
        {
            if (cur != null && sr != null && cur.frames.Length > 0) sr.sprite = cur.frames[0];
        }
    }
}
