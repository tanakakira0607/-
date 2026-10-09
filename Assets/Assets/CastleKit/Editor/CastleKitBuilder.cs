// Tools > CastleKit > 魔王城ステージを生成
// 同梱のドット絵を取り込み、魔王城ステージ・モブの魔物・魔王をシーンに配置します。
// StageKit / BossKit とは完全に独立しています(同時に入れても衝突しません)。

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CastleKit;

public static class CastleKitBuilder
{
    // 画像フォルダ。このスクリプトの場所から自動で探します(置き場所がずれていても動く)
    static string TexDir = "Assets/CastleKit/Textures";
    const string RootName = "Stage_DemonCastle";
    const string GroundTag = "Ground";

    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
    static readonly List<string> missing = new List<string>();

    // ------------------------------------------------------------------
    // メニュー
    // ------------------------------------------------------------------
    [MenuItem("Tools/CastleKit/魔王城ステージを生成")]
    public static void CreateStage()
    {
        GameObject old = GameObject.Find(RootName);
        if (old != null)
        {
            if (!EditorUtility.DisplayDialog("確認", "既存の魔王城ステージを削除して作り直しますか？", "作り直す", "キャンセル")) return;
            Undo.DestroyObjectImmediate(old);
        }

        if (!PrepareTextures()) return;
        EnsureTag(GroundTag);
        Build();
    }

    [MenuItem("Tools/CastleKit/テクスチャ設定を修復")]
    public static void RepairTextures()
    {
        if (PrepareTextures()) Debug.Log("テクスチャの設定を確認しました。");
    }

    // ------------------------------------------------------------------
    // テクスチャの取り込み設定(ドット絵向け)
    // ------------------------------------------------------------------
    static string FindTexDir()
    {
        string[] guids = AssetDatabase.FindAssets("CastleKitBuilder t:MonoScript");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!path.EndsWith("/CastleKitBuilder.cs")) continue;
            string editorDir = Path.GetDirectoryName(path).Replace('\\', '/');
            string rootDir = Path.GetDirectoryName(editorDir).Replace('\\', '/');
            return rootDir + "/Textures";
        }
        return "Assets/CastleKit/Textures";
    }

    static bool PrepareTextures()
    {
        cache.Clear();
        missing.Clear();
        TexDir = FindTexDir();

        if (!Directory.Exists(TexDir))
        {
            Debug.LogError("フォルダが見つかりません: " + TexDir + "\nzipの中の CastleKit フォルダ(Textures を含む)ごと、プロジェクトの Assets 直下に置いてください。");
            return false;
        }

        string[] files = Directory.GetFiles(TexDir, "*.png");
        for (int i = 0; i < files.Length; i++)
        {
            string path = files[i].Replace('\\', '/');
            string name = Path.GetFileNameWithoutExtension(path);
            SetupTexture(path, name);
        }
        AssetDatabase.Refresh();

        // 必要な画像が揃っているか確認
        string[] required = { "floor_top", "floor_fill", "platform_stone", "bridge_crack", "platform_phantom",
                              "lava_0", "lava_1", "lava_2", "spikes", "vent", "carpet", "wall_bg",
                              "pillar", "torch_0", "torch_1", "torch_2", "banner", "throne", "gate_arch",
                              "chain", "axe", "flame_0", "flame_1", "flame_2", "orb_fire", "fx_ring", "pixel_white",
                              "imp_move_0", "bat_fly_0", "mage_idle_0", "lord_idle_0" };
        for (int i = 0; i < required.Length; i++)
        {
            if (S(required[i]) == null) missing.Add(required[i]);
        }
        if (missing.Count > 0)
        {
            Debug.LogError("読み込めない画像があります: " + string.Join(", ", missing.ToArray()) + "\n" + TexDir + " に画像が揃っているか確認してください。");
            return false;
        }
        return true;
    }

    static int Ppu(string name)
    {
        if (name == "pixel_white") return 4;
        if (name.StartsWith("floor_") || name.StartsWith("platform_") || name.StartsWith("bridge_")
            || name.StartsWith("lava_") || name.StartsWith("flame_")
            || name == "spikes" || name == "vent" || name == "carpet" || name == "wall_bg") return 32;
        return 16;
    }

    static SpriteAlignment Pivot(string name)
    {
        if (name.StartsWith("imp_") || name.StartsWith("mage_") || name.StartsWith("flame_")
            || name == "pillar" || name == "throne" || name == "gate_arch") return SpriteAlignment.BottomCenter;
        if (name == "banner") return SpriteAlignment.TopCenter;
        return SpriteAlignment.Center;
    }

    static void SetupTexture(string path, string name)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;

        int ppu = Ppu(name);
        SpriteAlignment align = Pivot(name);

        TextureImporterSettings cur = new TextureImporterSettings();
        imp.ReadTextureSettings(cur);
        bool need = imp.textureType != TextureImporterType.Sprite
                    || imp.spritePixelsPerUnit != ppu
                    || imp.filterMode != FilterMode.Point
                    || cur.spriteMeshType != SpriteMeshType.FullRect
                    || cur.spriteAlignment != (int)align;
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
        s.spriteAlignment = (int)align;
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

    static Sprite S(string name)
    {
        Sprite s;
        if (cache.TryGetValue(name, out s)) return s;
        s = AssetDatabase.LoadAssetAtPath<Sprite>(TexDir + "/" + name + ".png");
        cache[name] = s;
        return s;
    }

    static Sprite[] Seq(string prefix, int n)
    {
        Sprite[] a = new Sprite[n];
        for (int i = 0; i < n; i++) a[i] = S(prefix + "_" + i);
        return a;
    }

    static void EnsureTag(string tag)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return;
        SerializedObject tm = new SerializedObject(assets[0]);
        SerializedProperty tags = tm.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
        }
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tm.ApplyModifiedProperties();
    }

    // ------------------------------------------------------------------
    // ステージ構築(地面の上面が y=0。全長 約185ユニット)
    // ------------------------------------------------------------------
    static void Build()
    {
        GameObject rootGo = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(rootGo, "Create Demon Castle");
        Transform root = rootGo.transform;

        Transform bg = Child(root, "Background");
        Transform decor = Child(root, "Decor");
        Transform ground = Child(root, "Ground");
        Transform gimmicks = Child(root, "Gimmicks");
        Transform mobs = Child(root, "Mobs");
        Transform points = Child(root, "Points");

        // ---- 背景の城壁 ----
        for (int row = 0; row < 2; row++)
            for (int col = 0; col < 24; col++)
                Sprite1(bg, "wall", S("wall_bg"), new Vector3(col * 8f, row * 8f, 0f), -20);

        // ---- 地面 ----
        MakeGround(ground, "Ground_A1", 0f, 34f, 0f, 3f, S("floor_top"), S("floor_fill"));      // 城門
        MakeGround(ground, "Ground_A2", 34f, 74f, 0f, 3f, S("floor_top"), S("floor_fill"));     // 大広間
        MakeGround(ground, "Ground_A4_A5", 104f, 184f, 0f, 3f, S("floor_top"), S("floor_fill")); // 処刑回廊〜魔王の間

        // 溶岩の回廊: 溶岩の底(立ち上がれる浅さ) + 溶岩
        MakeGround(ground, "LavaBed", 74f, 104f, -2f, 2f, S("floor_fill"), S("floor_fill"));
        MakeLava(gimmicks, 74f, 104f);

        // 壊れる橋
        MakeBridge(gimmicks, "Bridge_1", 76f, 80f);
        MakeBridge(gimmicks, "Bridge_2", 83f, 87f);
        MakeBridge(gimmicks, "Bridge_3", 90f, 94f);
        MakeBridge(gimmicks, "Bridge_4", 97f, 101f);

        // 足場(魔導士の台・バルコニー)
        MakeLedge(ground, "Ledge_Mage_A2", 64f, 70f, 3f);
        MakeLedge(ground, "Ledge_A4_1", 115f, 118f, 5f);   // 振り子の鎖と重ならない高いバルコニー
        MakeLedge(ground, "Ledge_A4_2", 133.5f, 136.5f, 5f);

        // スロー中だけ現れる幽玄の足場(溶岩の上の別ルート)
        MakePhantom(gimmicks, "Phantom_1", 80f, 83f, 3f);
        MakePhantom(gimmicks, "Phantom_2", 91f, 94f, 3.5f);

        // 見えない壁
        MakeWall(ground, "Wall_Left", -0.5f);
        MakeWall(ground, "Wall_Right", 184.5f);

        // ---- 罠 ----
        MakeFireJet(gimmicks, "FireJet_1", 40f, 0f);
        MakeFireJet(gimmicks, "FireJet_2", 48f, 1.1f);
        MakeFireJet(gimmicks, "FireJet_3", 56f, 2.2f);

        MakePendulum(gimmicks, "Pendulum_1", 112f, 7.5f, 6.5f, 0f);
        MakePendulum(gimmicks, "Pendulum_2", 121f, 7.5f, 6.5f, 0.33f);
        MakePendulum(gimmicks, "Pendulum_3", 130f, 7.5f, 6.5f, 0.66f);

        MakeSpikes(gimmicks, "Spikes_1", 114f, 116f);
        MakeSpikes(gimmicks, "Spikes_2", 126f, 128f);

        // ---- モブの魔物 ----
        MakeMob(mobs, "Imp_1", MobKind.Imp, new Vector2(14f, 0f), 5f);
        MakeMob(mobs, "Imp_2", MobKind.Imp, new Vector2(27f, 0f), 4f);
        MakeMob(mobs, "Imp_3", MobKind.Imp, new Vector2(109f, 0f), 3f);
        MakeMob(mobs, "Imp_4", MobKind.Imp, new Vector2(135f, 0f), 3f);

        MakeMob(mobs, "Bat_1", MobKind.Bat, new Vector2(44f, 6f), 0f);
        MakeMob(mobs, "Bat_2", MobKind.Bat, new Vector2(52f, 5.5f), 0f);
        MakeMob(mobs, "Bat_3", MobKind.Bat, new Vector2(62f, 6f), 0f);
        MakeMob(mobs, "Bat_4", MobKind.Bat, new Vector2(82f, 4.5f), 0f);
        MakeMob(mobs, "Bat_5", MobKind.Bat, new Vector2(92f, 5.5f), 0f);
        MakeMob(mobs, "Bat_6", MobKind.Bat, new Vector2(100f, 4.5f), 0f);
        MakeMob(mobs, "Bat_7", MobKind.Bat, new Vector2(114f, 6.5f), 0f);
        MakeMob(mobs, "Bat_8", MobKind.Bat, new Vector2(127f, 6.5f), 0f);

        MakeMob(mobs, "Mage_1", MobKind.Mage, new Vector2(67f, 3f), 0f);
        MakeMob(mobs, "Mage_2", MobKind.Mage, new Vector2(116.5f, 5f), 0f);
        MakeMob(mobs, "Mage_3", MobKind.Mage, new Vector2(135f, 5f), 0f);

        // ---- 魔王 ----
        MakeLord(root, "DemonLord", new Vector2(164f, 4.5f));

        // ---- 飾り ----
        float[] pillars = { 38f, 46f, 54f, 62f, 70f, 138f, 148f, 156f, 172f, 180f };
        for (int i = 0; i < pillars.Length; i++)
            Sprite1(decor, "Pillar_" + i, S("pillar"), new Vector3(pillars[i], 0f, 0f), -8);

        float[] torches = { 6f, 18f, 30f, 42f, 50f, 58f, 66f, 106f, 142f, 146f, 152f, 176f, 182f };
        for (int i = 0; i < torches.Length; i++) MakeTorch(decor, "Torch_" + i, torches[i], 3.2f);

        float[] banners = { 10f, 24f, 42f, 58f, 110f, 134f, 152f, 176f };
        for (int i = 0; i < banners.Length; i++)
            Sprite1(decor, "Banner_" + i, S("banner"), new Vector3(banners[i], 9.5f, 0f), -7);

        GameObject gate1 = Sprite1(decor, "GateArch_Entrance", S("gate_arch"), new Vector3(3f, 0f, 0f), -8);
        gate1.transform.localScale = Vector3.one * 1.2f;
        GameObject gate2 = Sprite1(decor, "GateArch_Boss", S("gate_arch"), new Vector3(146f, 0f, 0f), -8);
        gate2.transform.localScale = Vector3.one * 1.2f;

        GameObject throne = Sprite1(decor, "Throne", S("throne"), new Vector3(172f, 0f, 0f), -9);
        throne.transform.localScale = Vector3.one * 1.5f;

        // 魔王の間の赤い絨毯
        TileFill(decor, "Carpet", S("carpet"), 40f, 0.25f, 0.25f, new Vector3(164f, 0.125f, 0f), 4);

        // ---- プレイヤー開始位置の目印 ----
        GameObject start = new GameObject("PlayerStart");
        start.transform.SetParent(points, false);
        start.transform.position = new Vector3(2f, 1f, 0f);
        SetIcon(start, 3);

        Selection.activeGameObject = rootGo;
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("魔王城ステージを生成しました。プレイヤーに Player タグ、体力(PlayerHealth)のオブジェクトに Slider タグが付いているか確認してください。");
    }

    // ------------------------------------------------------------------
    // 配置ヘルパー(parent は全て Transform)
    // ------------------------------------------------------------------
    static Transform Child(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static GameObject Sprite1(Transform parent, string name, Sprite sprite, Vector3 pos, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return go;
    }

    // 1タイルずつ並べる(Tiledモードを使わず、環境差で崩れない)
    static void TileFill(Transform parent, string name, Sprite sprite, float width, float height, float tileH, Vector3 center, int order)
    {
        GameObject holder = new GameObject(name);
        holder.transform.SetParent(parent, false);
        holder.transform.position = center;

        int cols = Mathf.Max(1, Mathf.RoundToInt(width));
        int rows = Mathf.Max(1, Mathf.RoundToInt(height / tileH));
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                GameObject t = new GameObject("tile");
                t.transform.SetParent(holder.transform, false);
                t.transform.localPosition = new Vector3(-width * 0.5f + c + 0.5f, height * 0.5f - (r + 0.5f) * tileH, 0f);
                SpriteRenderer sr = t.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = order;
            }
        }
    }

    static GameObject MakeGround(Transform parent, string name, float x0, float x1, float yTop, float thick, Sprite top, Sprite fill)
    {
        float w = x1 - x0;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3((x0 + x1) * 0.5f, yTop - thick * 0.5f, 0f);
        go.tag = GroundTag;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(w, thick);

        TileFill(go.transform, "Top", top, w, 1f, 1f, go.transform.position + new Vector3(0f, thick * 0.5f - 0.5f, 0f), 3);
        if (thick > 1.01f)
        {
            float fillH = thick - 1f;
            TileFill(go.transform, "Fill", fill, w, fillH, 1f, go.transform.position + new Vector3(0f, thick * 0.5f - 1f - fillH * 0.5f, 0f), 3);
        }
        return go;
    }

    static GameObject MakeThin(Transform parent, string name, Sprite sprite, float x0, float x1, float yTop)
    {
        float w = x1 - x0;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3((x0 + x1) * 0.5f, yTop - 0.25f, 0f);
        go.tag = GroundTag;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(w, 0.5f);
        TileFill(go.transform, "Visual", sprite, w, 0.5f, 0.5f, go.transform.position, 3);
        return go;
    }

    static void MakeLedge(Transform parent, string name, float x0, float x1, float yTop)
    {
        MakeThin(parent, name, S("platform_stone"), x0, x1, yTop);
    }

    static void MakeBridge(Transform parent, string name, float x0, float x1)
    {
        GameObject go = MakeThin(parent, name, S("bridge_crack"), x0, x1, 0f);
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        go.AddComponent<CrumblePlatform>();
    }

    static void MakePhantom(Transform parent, string name, float x0, float x1, float yTop)
    {
        GameObject go = MakeThin(parent, name, S("platform_phantom"), x0, x1, yTop);
        go.AddComponent<PhantomPlatform>();
    }

    static void MakeWall(Transform parent, string name, float x)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, 15f, 0f);
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 40f);
    }

    static void MakeLava(Transform parent, float x0, float x1)
    {
        float w = x1 - x0;
        GameObject holder = new GameObject("Lava");
        holder.transform.SetParent(parent, false);
        holder.transform.position = Vector3.zero;

        // 溶岩の面(上面 y=-1、深さ1ユニット)。3コマを一斉に切り替えて波打たせる
        TileFill(holder.transform, "Surface", S("lava_0"), w, 1f, 1f, new Vector3((x0 + x1) * 0.5f, -1.5f, 0f), 2);
        SpriteCycler cyc = holder.AddComponent<SpriteCycler>();
        cyc.frames = Seq("lava", 3);
        cyc.fps = 3f;
        cyc.randomPhase = false;

        // 触れたらダメージ
        GameObject hz = new GameObject("LavaHazard");
        hz.transform.SetParent(holder.transform, false);
        hz.transform.position = new Vector3((x0 + x1) * 0.5f, -1.5f, 0f);
        Hazard h = hz.AddComponent<Hazard>();
        h.size = new Vector2(w, 1.2f);
        h.active = true;
    }

    static void MakeFireJet(Transform parent, string name, float x, float delay)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, 0f, 0f);

        // 噴出口
        GameObject ventGo = Sprite1(go.transform, "Vent", S("vent"), new Vector3(x, 0.125f, 0f), 5);
        SpriteRenderer ventSr = ventGo.GetComponent<SpriteRenderer>();

        // 炎(足元が基準)
        GameObject flameGo = Sprite1(go.transform, "Flame", S("flame_0"), new Vector3(x, 0.2f, 0f), 6);
        SpriteRenderer flameSr = flameGo.GetComponent<SpriteRenderer>();

        Hazard h = go.AddComponent<Hazard>();
        float height = 3.2f;
        h.size = new Vector2(0.7f, height);
        h.offset = new Vector2(0f, 0.2f + height * 0.5f);
        h.active = false;

        FireJet jet = go.AddComponent<FireJet>();
        jet.height = height;
        jet.width = 0.9f;
        jet.startDelay = delay;
        jet.vent = ventSr;
        jet.flame = flameSr;
        jet.flameFrames = Seq("flame", 3);
    }

    static void MakePendulum(Transform parent, string name, float x, float pivotY, float length, float phase)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, pivotY, 0f);

        Pendulum p = go.AddComponent<Pendulum>();
        p.amplitude = 55f;
        p.period = 3.6f;
        p.phase = phase;

        // 鎖(0.5×1 ユニットの輪を下へ並べる)
        for (float y = 0.5f; y < length - 1f; y += 0.9f)
        {
            GameObject link = new GameObject("Chain");
            link.transform.SetParent(go.transform, false);
            link.transform.localPosition = new Vector3(0f, -y, 0f);
            SpriteRenderer lsr = link.AddComponent<SpriteRenderer>();
            lsr.sprite = S("chain");
            lsr.sortingOrder = 5;
        }

        // 刃
        GameObject blade = new GameObject("Blade");
        blade.transform.SetParent(go.transform, false);
        blade.transform.localPosition = new Vector3(0f, -length, 0f);
        SpriteRenderer bsr = blade.AddComponent<SpriteRenderer>();
        bsr.sprite = S("axe");
        bsr.sortingOrder = 6;

        Hazard h = blade.AddComponent<Hazard>();
        h.radius = 0.9f;
        h.offset = new Vector2(0f, -0.2f);
        h.active = true;
    }

    static void MakeSpikes(Transform parent, string name, float x0, float x1)
    {
        float w = x1 - x0;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3((x0 + x1) * 0.5f, 0.25f, 0f);

        TileFill(go.transform, "Visual", S("spikes"), w, 0.5f, 0.5f, go.transform.position, 5);

        Hazard h = go.AddComponent<Hazard>();
        h.size = new Vector2(w, 0.4f);
        h.offset = new Vector2(0f, 0.05f);
        h.active = true;
    }

    static void MakeTorch(Transform parent, string name, float x, float y)
    {
        GameObject go = Sprite1(parent, name, S("torch_0"), new Vector3(x, y, 0f), -7);
        SpriteCycler cyc = go.AddComponent<SpriteCycler>();
        cyc.frames = Seq("torch", 3);
        cyc.fps = 8f;
        cyc.randomPhase = true;
    }

    // ------------------------------------------------------------------
    // モブの魔物
    // ------------------------------------------------------------------
    static SpriteClip Clip(string name, string prefix, int n, float fps, bool loop)
    {
        SpriteClip c = new SpriteClip();
        c.name = name;
        c.frames = Seq(prefix, n);
        c.fps = fps;
        c.loop = loop;
        return c;
    }

    static void MakeMob(Transform parent, string name, MobKind kind, Vector2 pos, float range)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 1.5f;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 7;

        FrameAnimator fa = go.AddComponent<FrameAnimator>();
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;

        CastleMob mob = go.AddComponent<CastleMob>();
        mob.kind = kind;
        mob.fireSprite = S("orb_fire");
        mob.ringSprite = S("fx_ring");

        switch (kind)
        {
            case MobKind.Imp:
                sr.sprite = S("imp_move_0");
                fa.clips.Add(Clip("move", "imp_move", 4, 8f, true));
                fa.clips.Add(Clip("windup", "imp_windup", 2, 6f, true));
                fa.clips.Add(Clip("lunge", "imp_lunge", 1, 8f, false));
                fa.clips.Add(Clip("death", "imp_death", 3, 7f, false));
                col.radius = 0.5f;
                col.offset = new Vector2(0f, 0.5f);
                mob.hp = 2;
                mob.patrolRange = range;
                mob.moveSpeed = 1.8f;
                mob.detectRange = 4f;
                mob.contactRadius = 0.85f;
                mob.centerOffset = new Vector2(0f, 0.5f);
                break;

            case MobKind.Bat:
                sr.sprite = S("bat_fly_0");
                fa.clips.Add(Clip("fly", "bat_fly", 4, 10f, true));
                fa.clips.Add(Clip("windup", "bat_windup", 2, 12f, true));
                fa.clips.Add(Clip("dive", "bat_dive", 1, 8f, false));
                fa.clips.Add(Clip("death", "bat_death", 3, 7f, false));
                col.radius = 0.5f;
                mob.hp = 1;
                mob.detectRange = 8f;
                mob.minY = 0.8f;
                mob.contactRadius = 0.7f;
                mob.centerOffset = Vector2.zero;
                break;

            default:
                sr.sprite = S("mage_idle_0");
                fa.clips.Add(Clip("idle", "mage_idle", 4, 5f, true));
                fa.clips.Add(Clip("cast", "mage_cast", 3, 5f, false));
                fa.clips.Add(Clip("death", "mage_death", 3, 7f, false));
                col.radius = 0.7f;
                col.offset = new Vector2(0f, 0.75f);
                mob.hp = 3;
                mob.detectRange = 13f;
                mob.contactRadius = 0.7f;
                mob.centerOffset = new Vector2(0f, 0.75f);
                mob.muzzleOffset = new Vector2(0.35f, 1.25f);
                break;
        }
    }

    // ------------------------------------------------------------------
    // 魔王
    // ------------------------------------------------------------------
    static void MakeLord(Transform parent, string name, Vector2 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 1.3f;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = S("lord_idle_0");
        sr.sortingOrder = 7;

        // プレイヤーの攻撃を当てる用のトリガー(接触ダメージは突進中のみスクリプトで判定)
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 1.3f;

        FrameAnimator fa = go.AddComponent<FrameAnimator>();
        fa.clips.Add(Clip("idle", "lord_idle", 4, 6f, true));
        fa.clips.Add(Clip("idlerage", "lord_idlerage", 4, 8f, true));
        fa.clips.Add(Clip("charge", "lord_charge", 4, 14f, true));
        fa.clips.Add(Clip("attack", "lord_attack", 3, 14f, false));
        fa.clips.Add(Clip("dash", "lord_dash", 2, 16f, true));
        fa.clips.Add(Clip("tired", "lord_tired", 3, 5f, true));
        fa.clips.Add(Clip("hurt", "lord_hurt", 2, 12f, false));
        fa.clips.Add(Clip("death", "lord_death", 5, 8f, false));

        DemonLord lord = go.AddComponent<DemonLord>();
        lord.fireSprite = S("orb_fire");
        lord.warnSprite = S("pixel_white");
        lord.ringSprite = S("fx_ring");
        lord.pillarFrames = Seq("flame", 3);
        lord.groundY = 0f;
    }

    // 0:灰 1:青 2:水 3:緑 4:黄 5:橙 6:赤 7:紫
    static void SetIcon(GameObject go, int index)
    {
        Texture2D tex = EditorGUIUtility.IconContent("sv_label_" + index).image as Texture2D;
        if (tex != null) EditorGUIUtility.SetIconForObject(go, tex);
    }
}
