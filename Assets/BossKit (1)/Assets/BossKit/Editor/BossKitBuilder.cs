// Tools > BossKit > ボスを生成
// アニメーション付きドット絵(全27コマ)を自動生成し、ボスをシーンに配置します。
// ステージ用のツールとは完全に独立しています。

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BossKit;

public static class BossKitBuilder
{
    const string GenDir = "Assets/BossKit/Generated";
    const string FrameDir = GenDir + "/Frames";
    const string BossName = "BossKit_Boss";

    // ------------------------------------------------------------------
    // メニュー
    // ------------------------------------------------------------------
    [MenuItem("Tools/BossKit/ボスを生成")]
    public static void CreateBoss()
    {
        GenerateAll(false);

        Sprite[] idle, idleRage, charge, attack, dash, tired, hurt, death;
        Sprite bullet, ring, pixel;
        if (!LoadAll(out idle, out idleRage, out charge, out attack, out dash, out tired, out hurt, out death,
                     out bullet, out ring, out pixel))
        {
            Debug.LogError("ドット絵の準備に失敗したため、ボス生成を中止しました。上のエラーを確認してください。");
            return;
        }

        GameObject old = GameObject.Find(BossName);
        if (old != null)
        {
            if (!EditorUtility.DisplayDialog("確認", "既存のボスを削除して作り直しますか？", "作り直す", "キャンセル")) return;
            Undo.DestroyObjectImmediate(old);
        }

        // 配置位置: Boss_Spawn があればそこ、なければシーンビューの中心
        Vector2 pos = new Vector2(0f, 4f);
        GameObject spawn = GameObject.Find("Boss_Spawn");
        if (spawn != null) pos = spawn.transform.position;
        else if (SceneView.lastActiveSceneView != null) pos = (Vector2)SceneView.lastActiveSceneView.pivot;

        // 地面の高さを下向きに探す(見つからなければ 4 ユニット下)
        float groundY = pos.y - 4f;
        Physics2D.SyncTransforms();
        RaycastHit2D hit = Physics2D.Raycast(pos, Vector2.down, 40f);
        if (hit.collider != null) groundY = hit.point.y;

        GameObject go = new GameObject(BossName);
        Undo.RegisterCreatedObjectUndo(go, "Create Boss");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 1.5f;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = idle[0];
        sr.sortingOrder = 7;

        // プレイヤーの攻撃を当てる用のトリガー(接触ダメージはダッシュ中のみスクリプトで判定)
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.8f;

        BossAnimator anim = go.AddComponent<BossAnimator>();
        anim.idle = idle;
        anim.idleRage = idleRage;
        anim.charge = charge;
        anim.attack = attack;
        anim.dash = dash;
        anim.tired = tired;
        anim.hurt = hurt;
        anim.death = death;

        BossController ctl = go.AddComponent<BossController>();
        ctl.bulletSprite = bullet;
        ctl.warnSprite = pixel;
        ctl.ringSprite = ring;
        ctl.groundY = groundY;

        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("ボスを生成しました(地面の高さ y=" + groundY.ToString("0.0") + ")。プレイヤーが近づくと戦闘が始まります。");
    }

    [MenuItem("Tools/BossKit/ドット絵を再生成(上書き)")]
    public static void RegenerateSprites()
    {
        if (!EditorUtility.DisplayDialog("確認", "生成済みのドット絵を上書きします。差し替えた画像も消えます。よろしいですか？", "上書き", "キャンセル")) return;
        GenerateAll(true);
        AssetDatabase.Refresh();
    }

    // ------------------------------------------------------------------
    // 生成と読み込み
    // ------------------------------------------------------------------
    class FrameDef
    {
        public string name;
        public Pose pose;
        public FrameDef(string n, Pose p) { name = n; pose = p; }
    }

    static List<FrameDef> Frames()
    {
        List<FrameDef> f = new List<FrameDef>();

        // 待機(ふわふわ + まばたき)
        f.Add(new FrameDef("idle_0", new Pose { bob = 0, shift = 0, eyes = 0, mouth = 0 }));
        f.Add(new FrameDef("idle_1", new Pose { bob = 1, shift = 2, eyes = 0, mouth = 0 }));
        f.Add(new FrameDef("idle_2", new Pose { bob = 1, shift = 4, eyes = 5, mouth = 0 }));
        f.Add(new FrameDef("idle_3", new Pose { bob = 0, shift = 6, eyes = 0, mouth = 0 }));

        // 怒り待機(角が燃え、赤みがかる)
        f.Add(new FrameDef("idlerage_0", new Pose { bob = 0, shift = 0, eyes = 4, mouth = 0, palette = 2, horn = 2, aura = 1, seed = 1 }));
        f.Add(new FrameDef("idlerage_1", new Pose { bob = 1, shift = 2, eyes = 4, mouth = 1, palette = 2, horn = 2, aura = 1, seed = 2 }));
        f.Add(new FrameDef("idlerage_2", new Pose { bob = 1, shift = 4, eyes = 4, mouth = 0, palette = 2, horn = 2, aura = 1, seed = 3 }));
        f.Add(new FrameDef("idlerage_3", new Pose { bob = 0, shift = 6, eyes = 4, mouth = 1, palette = 2, horn = 2, aura = 1, seed = 4 }));

        // 溜め(予兆): 縮んで腕を上げ、目が白熱する
        f.Add(new FrameDef("charge_0", new Pose { bob = -1, sx = 1.06f, sy = 0.92f, arm = 1, eyes = 1, mouth = 3, aura = 1, shift = 0, seed = 5 }));
        f.Add(new FrameDef("charge_1", new Pose { bob = -1, sx = 1.10f, sy = 0.88f, arm = 2, eyes = 1, mouth = 3, aura = 2, shift = 2, seed = 6 }));
        f.Add(new FrameDef("charge_2", new Pose { bob = 0, sx = 1.04f, sy = 0.95f, arm = 2, eyes = 1, mouth = 3, aura = 3, shift = 4, seed = 7 }));
        f.Add(new FrameDef("charge_3", new Pose { bob = 1, sx = 0.96f, sy = 1.08f, arm = 2, eyes = 1, mouth = 1, aura = 3, shift = 6, seed = 8 }));

        // 攻撃(発射): 伸び上がって口を開け、反動で戻る
        f.Add(new FrameDef("attack_0", new Pose { bob = 1, sx = 0.92f, sy = 1.10f, arm = 2, eyes = 1, mouth = 1, aura = 2, seed = 9 }));
        f.Add(new FrameDef("attack_1", new Pose { bob = 0, arm = 2, eyes = 4, mouth = 1, aura = 1, shift = 3, seed = 10 }));
        f.Add(new FrameDef("attack_2", new Pose { bob = 0, sx = 1.05f, sy = 0.94f, arm = 1, eyes = 4, mouth = 1, shift = 5, seed = 11 }));

        // ダッシュ: 横に伸びる
        f.Add(new FrameDef("dash_0", new Pose { bob = 0, sx = 1.15f, sy = 0.85f, arm = 0, eyes = 4, mouth = 1, palette = 2, horn = 2, shift = 0, seed = 12 }));
        f.Add(new FrameDef("dash_1", new Pose { bob = 0, sx = 1.15f, sy = 0.85f, arm = 0, eyes = 4, mouth = 1, palette = 2, horn = 2, shift = 4, seed = 13 }));

        // ぐったり(反撃チャンス): 青白く、目が半開き、舌が出る
        f.Add(new FrameDef("tired_0", new Pose { bob = -1, sy = 0.95f, eyes = 6, mouth = 2, palette = 1, shift = 0 }));
        f.Add(new FrameDef("tired_1", new Pose { bob = 0, eyes = 6, mouth = 2, palette = 1, shift = 2 }));
        f.Add(new FrameDef("tired_2", new Pose { bob = -1, sy = 0.95f, eyes = 5, mouth = 2, palette = 1, shift = 4 }));

        // 被弾: 白くフラッシュ → X目
        f.Add(new FrameDef("hurt_0", new Pose { bob = 0, sx = 1.1f, sy = 0.9f, arm = 2, eyes = 3, mouth = 1, palette = 3 }));
        f.Add(new FrameDef("hurt_1", new Pose { bob = 1, sx = 0.94f, sy = 1.06f, arm = 2, eyes = 3, mouth = 1, shift = 3 }));

        // 撃破: 少しずつ粒になって消える
        f.Add(new FrameDef("death_0", new Pose { eyes = 3, mouth = 1, arm = 2, dissolve = 0.15f, seed = 20 }));
        f.Add(new FrameDef("death_1", new Pose { eyes = 3, mouth = 1, arm = 2, dissolve = 0.35f, shift = 2, seed = 21 }));
        f.Add(new FrameDef("death_2", new Pose { eyes = 3, mouth = 1, arm = 2, dissolve = 0.55f, shift = 4, seed = 22 }));
        f.Add(new FrameDef("death_3", new Pose { eyes = 3, mouth = 1, arm = 2, dissolve = 0.75f, shift = 6, seed = 23 }));
        f.Add(new FrameDef("death_4", new Pose { eyes = 3, mouth = 1, arm = 2, dissolve = 0.92f, shift = 1, seed = 24 }));
        return f;
    }

    static string FramePath(string name) { return FrameDir + "/boss_" + name + ".png"; }

    static void GenerateAll(bool force)
    {
        try
        {
            Directory.CreateDirectory(FrameDir);
            AssetDatabase.Refresh();

            List<FrameDef> frames = Frames();
            for (int i = 0; i < frames.Count; i++)
            {
                Pose pose = frames[i].pose;
                Ensure(FramePath(frames[i].name), 32, 32, 16, p => DrawGhost(p, pose), force);
            }
            Ensure(GenDir + "/boss_bullet.png", 12, 12, 16, PaintBullet, force);
            Ensure(GenDir + "/fx_ring.png", 16, 16, 16, PaintRing, force);
            Ensure(GenDir + "/pixel_white.png", 4, 4, 4, PaintWhite, force);
            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            Debug.LogError("ドット絵の生成中にエラー: " + e);
        }
    }

    static Sprite[] Group(string prefix, int n)
    {
        Sprite[] a = new Sprite[n];
        for (int i = 0; i < n; i++)
            a[i] = AssetDatabase.LoadAssetAtPath<Sprite>(FramePath(prefix + "_" + i));
        return a;
    }

    static bool AllLoaded(Sprite[] a, string label)
    {
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] == null)
            {
                Debug.LogError("スプライトを読み込めません: " + label + "_" + i);
                return false;
            }
        }
        return true;
    }

    static bool LoadAll(out Sprite[] idle, out Sprite[] idleRage, out Sprite[] charge, out Sprite[] attack,
                        out Sprite[] dash, out Sprite[] tired, out Sprite[] hurt, out Sprite[] death,
                        out Sprite bullet, out Sprite ring, out Sprite pixel)
    {
        idle = Group("idle", 4);
        idleRage = Group("idlerage", 4);
        charge = Group("charge", 4);
        attack = Group("attack", 3);
        dash = Group("dash", 2);
        tired = Group("tired", 3);
        hurt = Group("hurt", 2);
        death = Group("death", 5);
        bullet = AssetDatabase.LoadAssetAtPath<Sprite>(GenDir + "/boss_bullet.png");
        ring = AssetDatabase.LoadAssetAtPath<Sprite>(GenDir + "/fx_ring.png");
        pixel = AssetDatabase.LoadAssetAtPath<Sprite>(GenDir + "/pixel_white.png");

        bool ok = AllLoaded(idle, "idle") & AllLoaded(idleRage, "idlerage") & AllLoaded(charge, "charge")
                  & AllLoaded(attack, "attack") & AllLoaded(dash, "dash") & AllLoaded(tired, "tired")
                  & AllLoaded(hurt, "hurt") & AllLoaded(death, "death");
        if (bullet == null) { Debug.LogError("スプライトを読み込めません: boss_bullet"); ok = false; }
        if (ring == null) { Debug.LogError("スプライトを読み込めません: fx_ring"); ok = false; }
        if (pixel == null) { Debug.LogError("スプライトを読み込めません: pixel_white"); ok = false; }
        return ok;
    }

    // 画像を保存して、ドット絵向けのSprite設定にする
    static void Ensure(string path, int w, int h, int ppu, Action<Pix> paint, bool force)
    {
        if (force || !File.Exists(path))
        {
            Pix p = new Pix(w, h);
            paint(p);
            p.Save(path);
        }

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null)
        {
            Debug.LogError("テクスチャの取り込みに失敗: " + path);
            return;
        }

        TextureImporterSettings cur = new TextureImporterSettings();
        imp.ReadTextureSettings(cur);
        bool need = force
                    || imp.textureType != TextureImporterType.Sprite
                    || imp.spritePixelsPerUnit != ppu
                    || imp.filterMode != FilterMode.Point
                    || cur.spriteMeshType != SpriteMeshType.FullRect;
        if (!need) return;

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.wrapMode = TextureWrapMode.Clamp;

        TextureImporterSettings s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect;
        s.spriteAlignment = (int)SpriteAlignment.Center;
        imp.SetTextureSettings(s);

        SerializedObject so = new SerializedObject(imp);
        SerializedProperty mesh = so.FindProperty("m_SpriteMeshType");
        if (mesh != null)
        {
            mesh.intValue = 0; // Full Rect
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        imp.SaveAndReimport();
    }

    // ------------------------------------------------------------------
    // ドット絵の描画
    // ------------------------------------------------------------------
    class Pix
    {
        public int w, h;
        public Color32[] d;
        public Pix(int w, int h) { this.w = w; this.h = h; d = new Color32[w * h]; }

        public void Set(int x, int y, Color32 c)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            d[y * w + x] = c;
        }

        public Color32 Get(int x, int y)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return new Color32(0, 0, 0, 0);
            return d[y * w + x];
        }

        public void Save(string path)
        {
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.SetPixels32(d);
            t.Apply();
            File.WriteAllBytes(path, t.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(t);
        }
    }

    // 1コマぶんの「ポーズ」
    class Pose
    {
        public float bob;                 // 上下のゆれ(ピクセル)
        public float sx = 1f, sy = 1f;    // 横・縦の伸び縮み
        public float dissolve;            // 粒になって消える度合い 0〜1
        public int shift;                 // 裾のゆらぎ位相
        public int arm;                   // 腕 0:下 1:中 2:上
        public int eyes;                  // 0:通常 1:白熱 3:X 4:怒り 5:まばたき 6:半目
        public int mouth;                 // 0:閉じ 1:開き 2:舌 3:小さなo
        public int palette;               // 0:通常 1:ぐったり 2:怒り 3:被弾
        public int horn;                  // 2:角が燃える
        public int aura;                  // 周りの光 0〜3
        public int seed;
    }

    // outline, light, base, shade, aura, horn
    static readonly int[][] Pal =
    {
        new[] { 0x2a2540, 0xf2eeff, 0xd4ccf0, 0xa99ad8, 0xd070ff, 0x4a3a7a },
        new[] { 0x1f2a50, 0xdce8ff, 0xb4c8f0, 0x8aa0d8, 0x70b0ff, 0x3a4a7a },
        new[] { 0x401830, 0xffe8f0, 0xf0c0d0, 0xc8809c, 0xff8030, 0xff7a40 },
        new[] { 0x601020, 0xffffff, 0xffffff, 0xffd0d0, 0xffffff, 0xff9090 },
    };

    static Color32 C(int hex, byte a = 255)
    {
        return new Color32((byte)((hex >> 16) & 255), (byte)((hex >> 8) & 255), (byte)(hex & 255), a);
    }

    static int H(int x, int y, int seed)
    {
        unchecked
        {
            int n = x * 374761393 + y * 668265263 + seed * 1442695041;
            n = (n ^ (n >> 13)) * 1274126177;
            n = n ^ (n >> 16);
            return n & 0x7fffffff;
        }
    }

    // 左右対称に打つ(side 0 = 左、1 = 右へ反転)
    static void Put2(Pix p, int side, int x, int y, Color32 c)
    {
        p.Set(side == 0 ? x : 31 - x, y, c);
    }

    static void Rect2(Pix p, int side, int x0, int y0, int x1, int y1, Color32 c)
    {
        for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
                Put2(p, side, x, y, c);
    }

    static void DrawGhost(Pix p, Pose o)
    {
        int[] pal = Pal[o.palette];
        Color32 cOut = C(pal[0]), cLight = C(pal[1]), cBase = C(pal[2]), cShade = C(pal[3]);
        Color32 dark = C(0x2a2540);

        const float cx = 15.5f;
        float hy = 15f + o.bob;
        int hyI = Mathf.RoundToInt(hy);
        float rx = 12f * o.sx, ry = 12f * o.sy;

        // ---- 手(体より先に描いて、体で上書きする) ----
        int handY = hyI - 8 + o.arm * 5;
        for (int xx = 0; xx < 4; xx++)
        {
            for (int yy = 0; yy < 3; yy++)
            {
                Color32 c = (xx == 0 || yy == 0 || yy == 2) ? cOut : cBase;
                p.Set(xx, handY + yy, c);
                p.Set(31 - xx, handY + yy, c);
            }
        }

        // ---- 体のかたち ----
        bool[,] m = new bool[32, 32];
        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 32; y++)
            {
                float dx = x - cx;
                bool inside;
                if (y >= hyI)
                {
                    float ex = dx / rx, ey = (y - hy) / ry;
                    inside = ex * ex + ey * ey <= 1f;
                }
                else
                {
                    int bottom = ((((x + o.shift) / 4) % 2) == 0 ? 2 : 6) + Mathf.RoundToInt(o.bob); // ひらひらの裾
                    inside = Mathf.Abs(dx) <= rx && y >= bottom;
                }
                m[x, y] = inside;
            }
        }

        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 32; y++)
            {
                if (!m[x, y]) continue;
                bool edge = x == 0 || y == 0 || x == 31 || y == 31
                            || !m[x - 1, y] || !m[x + 1, y] || !m[x, y - 1] || !m[x, y + 1];
                Color32 c;
                if (edge) c = cOut;
                else if (x >= 22) c = cShade;
                else if (y >= hyI + 8 && x <= 14) c = cLight;
                else c = cBase;
                p.Set(x, y, c);
            }
        }

        // ---- 角 ----
        int baseY = Mathf.RoundToInt(hy + ry) - 1;
        int hx = Mathf.RoundToInt(cx - rx * 0.62f);
        int[,] hr = { { 0, 0 }, { 1, 0 }, { 0, 1 }, { 1, 1 }, { 0, 2 }, { -1, 3 } };
        Color32 hornC = C(pal[5]);
        for (int i = 0; i < hr.GetLength(0); i++)
        {
            p.Set(hx + hr[i, 0], baseY + hr[i, 1], hornC);
            p.Set(31 - (hx + hr[i, 0]), baseY + hr[i, 1], hornC);
        }
        if (o.horn >= 2)
        {
            int f = o.seed % 3;
            for (int side = 0; side < 2; side++)
            {
                Put2(p, side, hx - 1, baseY + 4, C(0xffd060));
                Put2(p, side, hx - 1 + (f == 1 ? -1 : (f == 2 ? 1 : 0)), baseY + 5, C(0xff8030));
            }
        }

        // ---- 目 ----
        int ey0 = hyI;
        for (int side = 0; side < 2; side++)
        {
            switch (o.eyes)
            {
                case 0: // 通常(赤く光る)
                    Rect2(p, side, 9, ey0 - 1, 13, ey0 + 4, C(0xff9fb0));
                    Rect2(p, side, 10, ey0, 12, ey0 + 3, C(0xff3050));
                    break;
                case 1: // 白熱
                    Rect2(p, side, 8, ey0 - 2, 13, ey0 + 5, C(0xff9fb0));
                    Rect2(p, side, 9, ey0 - 1, 12, ey0 + 4, C(0xff3050));
                    Rect2(p, side, 10, ey0, 11, ey0 + 3, C(0xffffff));
                    break;
                case 3: // X目
                    Put2(p, side, 10, ey0, C(0x401020));
                    Put2(p, side, 12, ey0, C(0x401020));
                    Put2(p, side, 11, ey0 + 1, C(0x401020));
                    Put2(p, side, 10, ey0 + 2, C(0x401020));
                    Put2(p, side, 12, ey0 + 2, C(0x401020));
                    break;
                case 4: // 怒り(つり目 + 眉)
                    Rect2(p, side, 9, ey0 - 1, 13, ey0 + 3, C(0xffb060));
                    Rect2(p, side, 10, ey0, 12, ey0 + 2, C(0xff4010));
                    Put2(p, side, 9, ey0 + 4, dark);
                    Put2(p, side, 10, ey0 + 4, dark);
                    Put2(p, side, 11, ey0 + 3, dark);
                    Put2(p, side, 12, ey0 + 3, dark);
                    Put2(p, side, 13, ey0 + 2, dark);
                    break;
                case 5: // まばたき
                    Rect2(p, side, 10, ey0 + 1, 12, ey0 + 1, C(0x401020));
                    break;
                case 6: // 半目
                    Rect2(p, side, 10, ey0 + 2, 12, ey0 + 2, dark);
                    Rect2(p, side, 10, ey0 + 1, 12, ey0 + 1, C(0xff3050));
                    Put2(p, side, 11, ey0, C(0xff3050));
                    break;
            }
        }

        // ---- 口 ----
        switch (o.mouth)
        {
            case 0: // 閉じ(ギザギザ)
                for (int x = 11; x <= 20; x++) p.Set(x, hyI - 5, dark);
                for (int x = 11; x <= 20; x += 2) p.Set(x, hyI - 6, dark);
                break;
            case 1: // 開き(牙と舌)
                for (int y = hyI - 9; y <= hyI - 4; y++)
                {
                    int x0 = 12, x1 = 19;
                    if (y == hyI - 9 || y == hyI - 4) { x0 = 13; x1 = 18; }
                    for (int x = x0; x <= x1; x++) p.Set(x, y, dark);
                }
                p.Set(13, hyI - 4, C(0xffffff));
                p.Set(15, hyI - 4, C(0xffffff));
                p.Set(17, hyI - 4, C(0xffffff));
                for (int x = 14; x <= 17; x++)
                {
                    p.Set(x, hyI - 9, C(0xff6080));
                    p.Set(x, hyI - 8, C(0xff6080));
                }
                break;
            case 2: // 舌が出る
                for (int x = 11; x <= 20; x++) p.Set(x, hyI - 5, dark);
                for (int y = hyI - 8; y <= hyI - 6; y++)
                {
                    p.Set(15, y, C(0xff6080));
                    p.Set(16, y, C(0xff6080));
                }
                p.Set(15, hyI - 7, C(0xffa0b8));
                break;
            case 3: // 小さなo
                for (int x = 14; x <= 17; x++)
                    for (int y = hyI - 8; y <= hyI - 5; y++)
                        p.Set(x, y, dark);
                p.Set(14, hyI - 8, cBase);
                p.Set(17, hyI - 8, cBase);
                p.Set(14, hyI - 5, cBase);
                p.Set(17, hyI - 5, cBase);
                break;
        }

        // ---- 周りの光 ----
        if (o.aura > 0)
        {
            Color32 ac = C(pal[4], 150);
            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    if (p.Get(x, y).a != 0) continue;
                    float dx = x - cx, dy = y - hy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d >= 13f && d <= 15.9f && H(x, y, o.seed + 11) % 100 < o.aura * 14) p.Set(x, y, ac);
                }
            }
        }

        // ---- 粒になって消える ----
        if (o.dissolve > 0f)
        {
            for (int x = 0; x < 32; x++)
                for (int y = 0; y < 32; y++)
                    if (p.Get(x, y).a != 0 && H(x, y, o.seed + 23) % 100 < o.dissolve * 100f)
                        p.Set(x, y, new Color32(0, 0, 0, 0));
        }
    }

    static void PaintWhite(Pix p)
    {
        for (int x = 0; x < p.w; x++)
            for (int y = 0; y < p.h; y++)
                p.Set(x, y, C(0xffffff));
    }

    static void PaintRing(Pix p)
    {
        for (int x = 0; x < 16; x++)
            for (int y = 0; y < 16; y++)
            {
                float dx = x - 7.5f, dy = y - 7.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d >= 6.0f && d <= 7.6f) p.Set(x, y, C(0xffffff));
            }
    }

    static void PaintBullet(Pix p)
    {
        for (int x = 0; x < 12; x++)
        {
            for (int y = 0; y < 12; y++)
            {
                float dx = x - 5.5f, dy = y - 5.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= 2.2f) p.Set(x, y, C(0xffffff));
                else if (d <= 3.8f) p.Set(x, y, C(0xe0c4ff));
                else if (d <= 4.8f) p.Set(x, y, C(0x9a5cff));
                else if (d <= 5.8f) p.Set(x, y, C(0x9a5cff, 140));
            }
        }
    }
}
