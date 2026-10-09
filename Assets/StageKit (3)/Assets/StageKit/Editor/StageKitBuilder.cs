// Tools > Stage > 月夜の墓地ステージを生成
// テクスチャ(ドット絵)を自動生成し、ギミック付きのステージをシーンに配置します。
// 地面の上面が y=0。プレイヤー(Playerタグ)は自作のものを使ってください。

using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using StageKit;

public static class StageKitBuilder
{
    const string GenDir = "Assets/StageKit/Generated";
    const string GroundTag = "Ground";

    static Sprite sprTop, sprDirt, sprCollapse, sprSlow, sprTomb, sprGrass,
                  sprOrb, sprLanternOff, sprLanternOn, sprGate, sprMarker;

    // ------------------------------------------------------------------
    // メニュー
    // ------------------------------------------------------------------
    [MenuItem("Tools/Stage/月夜の墓地ステージを生成")]
    public static void CreateStage()
    {
        GameObject old = GameObject.Find("Stage_MoonlitGraveyard");
        if (old != null)
        {
            if (!EditorUtility.DisplayDialog("確認", "既存のステージを削除して作り直しますか？", "作り直す", "キャンセル")) return;
            Undo.DestroyObjectImmediate(old);
        }

        EnsureTag(GroundTag);
        GenerateTextures(false);
        if (!LoadSprites())
        {
            Debug.LogError("テクスチャの準備に失敗したため、ステージ生成を中止しました。上のエラーを確認してください。");
            return;
        }
        Build();
    }

    [MenuItem("Tools/Stage/テクスチャを再生成(上書き)")]
    public static void RegenerateTextures()
    {
        if (!EditorUtility.DisplayDialog("確認", "生成済みテクスチャを上書きします。差し替えた画像も消えます。よろしいですか？", "上書き", "キャンセル")) return;
        GenerateTextures(true);
        AssetDatabase.Refresh();
    }


    [MenuItem("Tools/Stage/既存ステージの地面・足場にGroundタグを付ける")]
    public static void TagExistingStage()
    {
        GameObject root = GameObject.Find("Stage_MoonlitGraveyard");
        if (root == null) { Debug.LogWarning("Stage_MoonlitGraveyard が見つかりません。"); return; }

        EnsureTag(GroundTag);
        int count = 0;
        BoxCollider2D[] cols = root.GetComponentsInChildren<BoxCollider2D>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            BoxCollider2D c = cols[i];
            if (c.isTrigger) continue;
            if (c.name.StartsWith("Wall")) continue;
            Undo.RecordObject(c.gameObject, "Set Ground Tag");
            c.gameObject.tag = GroundTag;
            EditorUtility.SetDirty(c.gameObject);
            count++;
        }
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Groundタグを " + count + " 個のオブジェクトに付けました。");
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
    // ステージ構築
    // ------------------------------------------------------------------
    static void Build()
    {
        GameObject rootGo = new GameObject("Stage_MoonlitGraveyard");
        Undo.RegisterCreatedObjectUndo(rootGo, "Create Stage");
        Transform root = rootGo.transform;

        Transform decor = Child(root, "Decor");
        Transform ground = Child(root, "Ground");
        Transform gimmicks = Child(root, "Gimmicks");
        Transform enemies = Child(root, "Enemies");
        Transform items = Child(root, "Items");
        Transform points = Child(root, "Points");
        Transform areas = Child(root, "AreaLabels");

        // ---- 地面 ----
        // エリア1〜2
        MakeGround(ground, "Ground_A1_a", 0f, 24f, 0f, 3f);
        MakeGround(ground, "Ground_A1_b", 27f, 70f, 0f, 3f);
        // エリア4
        MakeGround(ground, "Ground_A4", 98f, 130f, 0f, 3f);
        MakeGround(ground, "HighPlatform_A4", 116f, 122f, 3.5f, 1.5f);
        // エリア5
        MakeGround(ground, "Ground_A5", 132f, 162f, 0f, 3f);

        // 見えない壁
        MakeWall(ground, "Wall_Left", -0.5f);
        MakeWall(ground, "Wall_Right", 162.5f);

        // ---- ギミック ----
        // エリア3: 崩れる足場
        MakeCollapse(gimmicks, "CollapsePlatform_1", 73f, 77f);
        MakeCollapse(gimmicks, "CollapsePlatform_2", 80f, 84f);
        MakeCollapse(gimmicks, "CollapsePlatform_3", 87f, 91f);
        MakeCollapse(gimmicks, "CollapsePlatform_4", 94f, 97f);
        // エリア4: スロー中だけ現れる足場
        MakeSlowPlatform(gimmicks, "SlowOnlyPlatform_A4", 106f, 110f, 4f);

        // 落下エリア(穴の下の目印。判定処理は自作側で付けてください)
        GameObject death = new GameObject("DeathZone");
        death.transform.SetParent(gimmicks, false);
        death.transform.position = new Vector3(81f, -10f, 0f);
        BoxCollider2D dz = death.AddComponent<BoxCollider2D>();
        dz.isTrigger = true;
        dz.size = new Vector2(220f, 4f);

        // チェックポイントとゴール
        MakeCheckpoint(points, "Checkpoint_1", 40f);
        MakeCheckpoint(points, "Checkpoint_2", 99f);
        MakeCheckpoint(points, "Checkpoint_3", 133f);
        MakeGoal(points, "Goal", 159f);
        Marker(points, "PlayerStart", new Vector2(2f, 1f), 3, new Color(0.3f, 1f, 0.4f, 0.8f), 0.8f);

        // ---- 時間回復アイテム ----
        MakeOrb(items, "TimeRecovery_01", 108f, 5.4f, 5f);   // スロー足場の上
        MakeOrb(items, "TimeRecovery_02", 119f, 4.9f, 5f);   // 高台
        MakeOrb(items, "TimeRecovery_03", 127f, 1.3f, 5f);   // 巡回ゴーストの奥

        // ---- 敵(幽霊)の印 ----
        Ghost(enemies, "Ghost_A1_01", 14f, 2f, "ゆっくり漂う(チュートリアル)", 3f, 0.3f);
        Ghost(enemies, "Ghost_A2_01", 46f, 2f, "上下ゆれ", 0f, 1f);
        Ghost(enemies, "Ghost_A2_02", 52f, 3.5f, "上下ゆれ(逆位相)", 0f, 1f);
        Ghost(enemies, "Ghost_A2_03", 58f, 2f, "上下ゆれ", 0f, 1f);
        Ghost(enemies, "Ghost_A2_04", 64f, 3.5f, "上下ゆれ(逆位相)", 0f, 1f);
        Ghost(enemies, "Ghost_A3_01", 85.5f, 3f, "足場の上空を巡回", 2f, 0.5f);
        Ghost(enemies, "Ghost_A3_02", 92.5f, 2.5f, "足場の上空を巡回", 2f, 0.5f);
        Ghost(enemies, "Ghost_A4_Patrol_01", 105f, 1.5f, "巡回(倒すか避けるか)", 4f, 0f);
        Ghost(enemies, "Ghost_A4_Patrol_02", 115f, 1.5f, "巡回(倒すか避けるか)", 4f, 0f);
        Ghost(enemies, "Ghost_A4_Patrol_03", 124.5f, 1.5f, "巡回(倒すか避けるか)", 3.5f, 0f);

        GameObject boss = Marker(enemies, "Boss_Spawn", new Vector2(150f, 4f), 6, new Color(0.7f, 0.1f, 0.9f, 0.7f), 3f);
        EnemyMarker bm = boss.AddComponent<EnemyMarker>();
        bm.behaviour = "ボス: 周期的に無敵 / 弾幕はスローで避ける";
        bm.patrolLeft = 8f;
        bm.patrolRight = 8f;

        // ---- 飾り(墓石・枯れ草) ----
        float[] tombs = { 8f, 30f, 48f, 66f, 102f, 126f, 140f };
        for (int i = 0; i < tombs.Length; i++) MakeDecor(decor, "Tombstone_" + i, sprTomb, tombs[i], 1);
        float[] grasses = { 5f, 18f, 34f, 44f, 60f, 104f, 112f, 128f, 136f, 150f };
        for (int i = 0; i < grasses.Length; i++) MakeDecor(decor, "DeadGrass_" + i, sprGrass, grasses[i], 0);

        // ---- エリア見出し ----
        Label(areas, "Area1_墓地の入口", 20f);
        Label(areas, "Area2_ふわふわゴースト地帯", 55f);
        Label(areas, "Area3_崩れる足場", 85f);
        Label(areas, "Area4_時間を稼ぐ区間", 114f);
        Label(areas, "Area5_月下のボス戦", 147f);

        Selection.activeGameObject = rootGo;
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("月夜の墓地ステージを生成しました。ギミックはプレイヤーの Player タグに反応します。");
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

    // Tiledモードを使わず、1タイルずつ並べる(環境差で崩れない)
    static void TileFill(Transform parent, string name, Sprite sprite, float width, float height, float tileH, Vector3 localCenter, int order)
    {
        GameObject holder = new GameObject(name);
        holder.transform.SetParent(parent, false);
        holder.transform.localPosition = localCenter;

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

    static GameObject MakeGround(Transform parent, string name, float x0, float x1, float yTop, float thick)
    {
        float w = x1 - x0;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3((x0 + x1) * 0.5f, yTop - thick * 0.5f, 0f);
        go.tag = GroundTag;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(w, thick);

        TileFill(go.transform, "Top", sprTop, w, 1f, 1f, new Vector3(0f, thick * 0.5f - 0.5f, 0f), 3);
        if (thick > 1.01f)
        {
            float dirtH = thick - 1f;
            TileFill(go.transform, "Dirt", sprDirt, w, dirtH, 1f, new Vector3(0f, thick * 0.5f - 1f - dirtH * 0.5f, 0f), 3);
        }
        return go;
    }

    static void MakeWall(Transform parent, string name, float x)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, 15f, 0f);
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 40f);
    }

    static GameObject MakeThinPlatform(Transform parent, string name, Sprite s, float x0, float x1, float yTop)
    {
        float w = x1 - x0;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3((x0 + x1) * 0.5f, yTop - 0.25f, 0f);
        go.tag = GroundTag;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(w, 0.5f);
        TileFill(go.transform, "Visual", s, w, 0.5f, 0.5f, Vector3.zero, 3);
        return go;
    }

    static void MakeCollapse(Transform parent, string name, float x0, float x1)
    {
        GameObject go = MakeThinPlatform(parent, name, sprCollapse, x0, x1, 0f);
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        go.AddComponent<CollapsePlatform>();
    }

    static void MakeSlowPlatform(Transform parent, string name, float x0, float x1, float yTop)
    {
        GameObject go = MakeThinPlatform(parent, name, sprSlow, x0, x1, yTop);
        go.AddComponent<SlowOnlyPlatform>();
    }

    static void MakeCheckpoint(Transform parent, string name, float x)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, 0f, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprLanternOff;
        sr.sortingOrder = 4;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.offset = new Vector2(0f, 1.5f);
        col.size = new Vector2(2.5f, 3f);

        Checkpoint cp = go.AddComponent<Checkpoint>();
        cp.offSprite = sprLanternOff;
        cp.onSprite = sprLanternOn;
        SetIcon(go, 3);
    }

    static void MakeGoal(Transform parent, string name, float x)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, 0f, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprGate;
        sr.sortingOrder = 4;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.offset = new Vector2(0f, 1.5f);
        col.size = new Vector2(1.5f, 2.5f);

        go.AddComponent<GoalZone>();
        SetIcon(go, 4);
    }

    static void MakeOrb(Transform parent, string name, float x, float y, float seconds)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprOrb;
        sr.sortingOrder = 5;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.6f;

        TimeRecoveryItem item = go.AddComponent<TimeRecoveryItem>();
        item.recoverSeconds = seconds;
        SetIcon(go, 2);
    }

    static void MakeDecor(Transform parent, string name, Sprite s, float x, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, -0.1f, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = order;
    }

    static void Ghost(Transform parent, string name, float x, float y, string behaviour, float patrolHalf, float bob)
    {
        GameObject go = Marker(parent, name, new Vector2(x, y), 6, new Color(1f, 0.2f, 0.2f, 0.6f), 0.8f);
        EnemyMarker em = go.AddComponent<EnemyMarker>();
        em.behaviour = behaviour;
        em.patrolLeft = patrolHalf;
        em.patrolRight = patrolHalf;
        em.bobAmplitude = bob;
    }

    static GameObject Marker(Transform parent, string name, Vector2 pos, int iconIndex, Color color, float size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * size;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprMarker;
        sr.color = color;
        sr.sortingOrder = 10;

        SetIcon(go, iconIndex);
        return go;
    }

    static void Label(Transform parent, string name, float x)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, 8f, 0f);
        SetIcon(go, 1);
    }

    // 0:灰 1:青 2:水 3:緑 4:黄 5:橙 6:赤 7:紫
    static void SetIcon(GameObject go, int index)
    {
        Texture2D tex = EditorGUIUtility.IconContent("sv_label_" + index).image as Texture2D;
        if (tex != null) EditorGUIUtility.SetIconForObject(go, tex);
    }

    // ------------------------------------------------------------------
    // テクスチャ生成(ドット絵をコードで描く)
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

    static Color32 Dirt(int x, int y)
    {
        int n = H(x, y, 3) % 100;
        if (n < 12) return C(0x52371f);
        if (n > 93) return C(0x8a6a48);
        return C(0x6b4a2e);
    }

    static void GenerateTextures(bool force)
    {
        try { GenerateTexturesInner(force); }
        catch (Exception e) { Debug.LogError("テクスチャ生成中にエラー: " + e); }
    }

    static void GenerateTexturesInner(bool force)
    {
        Directory.CreateDirectory(GenDir);
        AssetDatabase.Refresh();
        //      名前              幅  高  PPU  足元基準 タイル  描画関数
        Tex("ground_top",       32, 32, 32, false, true,  PaintGroundTop, force);
        Tex("ground_dirt",      32, 32, 32, false, true,  PaintGroundDirt, force);
        Tex("platform_collapse", 32, 16, 32, false, true,  PaintCollapse, force);
        Tex("platform_slow",    32, 16, 32, false, true,  PaintSlow, force);
        Tex("tombstone",        16, 20, 16, true,  false, PaintTomb, force);
        Tex("dead_grass",       16, 16, 16, true,  false, PaintGrass, force);
        Tex("time_orb",         16, 16, 16, false, false, PaintOrb, force);
        Tex("lantern_off",      16, 32, 16, true,  false, p => PaintLantern(p, false), force);
        Tex("lantern_on",       16, 32, 16, true,  false, p => PaintLantern(p, true), force);
        Tex("marker_circle",    16, 16, 16, false, false, PaintCircle, force);
        Tex("goal_gate",        32, 48, 16, true,  false, PaintGate, force);
        AssetDatabase.Refresh();
    }

    static void Tex(string name, int w, int h, int ppu, bool bottomPivot, bool tiled, Action<Pix> paint, bool force)
    {
        string path = GenDir + "/" + name + ".png";

        // 画像がなければ(または上書き指定なら)描いて保存
        if (force || !File.Exists(path))
        {
            Pix p = new Pix(w, h);
            paint(p);
            p.Save(path);
        }

        // 取り込み。失敗していたらここで分かるようにする
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null)
        {
            Debug.LogError("テクスチャの取り込みに失敗: " + path);
            return;
        }

        // 設定が違っていれば(前回の失敗・Mesh Type違いなど)設定し直す
        TextureImporterSettings cur = new TextureImporterSettings();
        imp.ReadTextureSettings(cur);
        bool needSetup = force
                         || imp.textureType != TextureImporterType.Sprite
                         || imp.spritePixelsPerUnit != ppu
                         || imp.filterMode != FilterMode.Point
                         || cur.spriteMeshType != SpriteMeshType.FullRect;
        if (!needSetup) return;

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.wrapMode = tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

        TextureImporterSettings s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect;
        s.spriteAlignment = (int)(bottomPivot ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
        imp.SetTextureSettings(s);

        // 念のため、シリアライズ値にも直接書き込む(0 = Full Rect)
        SerializedObject so = new SerializedObject(imp);
        SerializedProperty mesh = so.FindProperty("m_SpriteMeshType");
        if (mesh != null)
        {
            mesh.intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        imp.SaveAndReimport();
    }

    static Sprite Load(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(GenDir + "/" + name + ".png");
    }

    static bool LoadSprites()
    {
        sprTop = Load("ground_top");
        sprDirt = Load("ground_dirt");
        sprCollapse = Load("platform_collapse");
        sprSlow = Load("platform_slow");
        sprTomb = Load("tombstone");
        sprGrass = Load("dead_grass");
        sprOrb = Load("time_orb");
        sprLanternOff = Load("lantern_off");
        sprLanternOn = Load("lantern_on");
        sprGate = Load("goal_gate");
        sprMarker = Load("marker_circle");

        string[] names = { "ground_top", "ground_dirt", "platform_collapse", "platform_slow", "tombstone",
                           "dead_grass", "time_orb", "lantern_off", "lantern_on", "goal_gate", "marker_circle" };
        Sprite[] all = { sprTop, sprDirt, sprCollapse, sprSlow, sprTomb, sprGrass, sprOrb, sprLanternOff, sprLanternOn, sprGate, sprMarker };
        bool ok = true;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null)
            {
                Debug.LogError("スプライトを読み込めません: " + GenDir + "/" + names[i] + ".png");
                ok = false;
            }
        }
        return ok;
    }

    // ---- 各テクスチャの描画 ----
    static void PaintGroundTop(Pix p)
    {
        for (int x = 0; x < 32; x++)
        {
            int gh = 7 + H(x, 0, 1) % 3;
            for (int y = 0; y < 32; y++)
            {
                if (y >= 32 - gh)
                {
                    if (y == 31 && H(x, 1, 2) % 3 == 0) continue; // 草の先端をギザギザに
                    Color32 c;
                    if (y == 32 - gh) c = C(0x4d5a10);
                    else if (y >= 30) c = C(0x8a9a1e);
                    else c = C(0x6f7f17);
                    p.Set(x, y, c);
                }
                else
                {
                    p.Set(x, y, Dirt(x, y));
                }
            }
        }
    }

    static void PaintGroundDirt(Pix p)
    {
        for (int x = 0; x < 32; x++)
            for (int y = 0; y < 32; y++)
                p.Set(x, y, Dirt(x, y));

        int[,] st = { { 8, 22, 5, 4 }, { 23, 12, 6, 4 }, { 10, 5, 4, 3 } };
        for (int i = 0; i < st.GetLength(0); i++)
        {
            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    float dx = (x - st[i, 0]) / (float)st[i, 2];
                    float dy = (y - st[i, 1]) / (float)st[i, 3];
                    float d = dx * dx + dy * dy;
                    if (d <= 0.6f) p.Set(x, y, (dx < 0f && dy > 0f) ? C(0xa5835c) : C(0x8f6f4b));
                    else if (d <= 1f) p.Set(x, y, C(0x4a3320));
                }
            }
        }
    }

    static void PaintCollapse(Pix p)
    {
        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 16; y++)
            {
                Color32 c = C(0x6e5a4a);
                if (H(x, y, 5) % 100 < 10) c = C(0x5d4b3d);
                if (y == 15) c = C(0x8c7560);
                if (y <= 1) c = C(0x3e3026);
                if (x % 16 == 0) c = C(0x3e3026);
                p.Set(x, y, c);
            }
        }
        int[,] crack = { { 20, 14 }, { 20, 13 }, { 21, 12 }, { 21, 11 }, { 22, 10 }, { 21, 9 }, { 21, 8 }, { 20, 7 }, { 19, 6 }, { 19, 5 }, { 18, 4 }, { 18, 3 } };
        for (int i = 0; i < crack.GetLength(0); i++) p.Set(crack[i, 0], crack[i, 1], C(0x2a2018));
    }

    static void PaintSlow(Pix p)
    {
        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 16; y++)
            {
                if (y == 0 || y == 15) p.Set(x, y, C(0x9be8ff, 230));
                else p.Set(x, y, ((x + y) % 2 == 0) ? C(0x5fc8f5, 150) : C(0x5fc8f5, 110));
            }
        }
    }

    static void PaintTomb(Pix p)
    {
        bool[,] m = new bool[16, 20];
        for (int x = 0; x < 16; x++)
        {
            for (int y = 0; y < 20; y++)
            {
                bool inside;
                if (y <= 2) inside = x >= 1 && x <= 14;
                else if (y <= 13) inside = x >= 3 && x <= 12;
                else
                {
                    float dx = (x - 7.5f) / 4.5f;
                    float dy = (y - 13f) / 6f;
                    inside = dx * dx + dy * dy <= 1f;
                }
                m[x, y] = inside;
            }
        }
        for (int x = 0; x < 16; x++)
        {
            for (int y = 0; y < 20; y++)
            {
                if (!m[x, y]) continue;
                bool edge = x == 0 || y == 0 || x == 15 || y == 19
                            || !m[x - 1, y] || !m[x + 1, y] || !m[x, y - 1] || !m[x, y + 1];
                Color32 c;
                if (edge) c = C(0x3c3e44);
                else if (y <= 2) c = C(0x6b6d74);
                else if (x >= 11) c = C(0x777a82);
                else if (x == 4) c = C(0x9fa1a8);
                else c = C(0x8d8f96);
                p.Set(x, y, c);
            }
        }
        // 十字の彫り
        for (int y = 7; y <= 12; y++) { p.Set(7, y, C(0x55575d)); p.Set(8, y, C(0x55575d)); }
        for (int x = 5; x <= 10; x++) p.Set(x, 10, C(0x55575d));
    }

    static void PaintGrass(Pix p)
    {
        // x, 高さ, 傾き
        int[,] blades = { { 3, 9, -2 }, { 5, 12, 1 }, { 7, 7, 0 }, { 9, 13, 2 }, { 11, 8, -1 }, { 13, 10, 2 } };
        for (int b = 0; b < blades.GetLength(0); b++)
        {
            int x0 = blades[b, 0], h = blades[b, 1], lean = blades[b, 2];
            for (int i = 0; i < h; i++)
            {
                int x = x0 + Mathf.RoundToInt(lean * i / (float)h);
                p.Set(x, i, i > h - 3 ? C(0x9a9a2c) : C(0x6b6b1a));
            }
        }
    }

    static void PaintOrb(Pix p)
    {
        for (int x = 0; x < 16; x++)
        {
            for (int y = 0; y < 16; y++)
            {
                float dx = x - 7.5f, dy = y - 7.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= 2.8f) p.Set(x, y, C(0xf4ffff));
                else if (d <= 4.5f) p.Set(x, y, C(0x7be9ff));
                else if (d <= 7.2f) p.Set(x, y, C(0x7be9ff, (byte)Mathf.Lerp(110f, 0f, (d - 4.5f) / 2.7f)));
            }
        }
    }

    static void PaintLantern(Pix p, bool on)
    {
        if (on)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    float dx = x - 9f, dy = y - 19.5f;
                    if (Mathf.Sqrt(dx * dx + dy * dy) <= 6.5f) p.Set(x, y, C(0xffd36a, 70));
                }
            }
        }
        Color32 wood = C(0x3a2a1c);
        // 支柱と台座
        for (int y = 0; y <= 27; y++) { p.Set(4, y, wood); p.Set(5, y, wood); }
        for (int x = 2; x <= 7; x++) { p.Set(x, 0, wood); p.Set(x, 1, wood); }
        // 腕
        for (int x = 4; x <= 9; x++) { p.Set(x, 27, wood); p.Set(x, 26, wood); }
        // 吊り下げ部
        for (int y = 23; y <= 25; y++) p.Set(9, y, wood);
        // ランタン本体
        for (int x = 7; x <= 11; x++)
        {
            for (int y = 17; y <= 22; y++)
            {
                bool border = x == 7 || x == 11 || y == 17 || y == 22;
                if (border) p.Set(x, y, C(0x2e2218));
                else p.Set(x, y, on ? C(0xffd36a) : C(0x4a4036));
            }
        }
    }

    static void PaintCircle(Pix p)
    {
        for (int x = 0; x < 16; x++)
            for (int y = 0; y < 16; y++)
            {
                float dx = x - 7.5f, dy = y - 7.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= 7.5f) p.Set(x, y, d >= 6.2f ? C(0xffffff) : C(0xffffff, 150));
            }
    }

    static void PaintGate(Pix p)
    {
        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 48; y++)
            {
                if (y < 4) { p.Set(x, y, C(0x4a4650)); continue; }
                bool outer, inner;
                if (y < 33)
                {
                    outer = x >= 2 && x <= 29;
                    inner = x >= 8 && x <= 23;
                }
                else
                {
                    float dx = x - 15.5f, dy = y - 33f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    outer = d <= 13.5f;
                    inner = d <= 7.5f;
                }
                if (inner)
                {
                    float t = (y - 4) / 43f;
                    p.Set(x, y, Color32.Lerp(C(0x8f9bd8, 200), C(0xe0e6ff, 200), t));
                }
                else if (outer)
                {
                    p.Set(x, y, H(x, y, 7) % 100 < 20 ? C(0x5c5862) : C(0x6e6a74));
                }
            }
        }
    }
}
