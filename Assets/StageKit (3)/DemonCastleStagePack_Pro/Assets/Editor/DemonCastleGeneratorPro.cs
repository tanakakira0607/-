#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class DemonCastleGeneratorPro : EditorWindow
{
    private static readonly string SAVE_PATH = "Assets/DemonCastlePackPro/";

    [MenuItem("Tools/【高品質版】魔王城ステージ一式を完全自動生成")]
    public static void GenerateAllPro()
    {
        if (!Directory.Exists(SAVE_PATH))
        {
            Directory.CreateDirectory(SAVE_PATH);
            Directory.CreateDirectory(SAVE_PATH + "Sprites");
            Directory.CreateDirectory(SAVE_PATH + "Prefabs");
        }

        // --- 1. 高精細ドット絵スプライトの自動生成 (16x16 / 32x32 / 64x64) ---

        // A. 床・壁タイル（深みのある暗紫色グラデーション＋レンガの目地・凹凸）
        Sprite brickSprite = CreateTexture("Tile_CastleBrick", 16, 16, (x, y) => {
            bool isOuterBorder = (x == 0 || y == 0 || x == 15 || y == 15);
            bool isBrickLine = (y == 8 || (y > 8 && x == 8) || (y < 8 && (x == 4 || x == 12)));
            
            Color basePurple = new Color(0.18f, 0.12f, 0.24f);
            Color darkShadow = new Color(0.08f, 0.05f, 0.12f);
            Color lightHighlight = new Color(0.28f, 0.20f, 0.36f);

            if (isOuterBorder || isBrickLine) return darkShadow;
            if (y == 14 || y == 7) return lightHighlight; // 上部のハイライト
            return basePurple;
        });

        // B. 背景壁（薄暗い石造り＋格子模様）
        Sprite bgWallSprite = CreateTexture("Tile_CastleBG", 16, 16, (x, y) => {
            bool isGrid = (x % 8 == 0 || y % 8 == 0);
            Color baseBG = new Color(0.09f, 0.06f, 0.12f);
            Color gridLine = new Color(0.05f, 0.03f, 0.07f);
            return isGrid ? gridLine : baseBG;
        });

        // C. 時間操作で動くギミック床（歯車風・時間時計デザイン 32x32）
        Sprite timePlatformSprite = CreateTexture("Gimmick_TimePlatform", 32, 16, (x, y) => {
            if (y >= 12) return new Color(0.35f, 0.25f, 0.45f); // 上部縁
            if (y <= 3) return new Color(0.15f, 0.10f, 0.20f);  // 下部影
            float centerDist = Mathf.Abs(x - 15.5f);
            if (centerDist < 4.0f && y >= 4 && y <= 11)
            {
                // 中央の時計文字盤風エフェクト（金＋シアン発光）
                if (centerDist < 1.5f) return new Color(0.2f, 0.9f, 1.0f); // 水色に光るコア
                return new Color(0.85f, 0.70f, 0.25f); // 金色
            }
            return new Color(0.25f, 0.18f, 0.32f);
        });

        // D. 時間の水晶トゲトラップ（赤青発光 16x16）
        Sprite trapSprite = CreateTexture("Trap_TimeSpike", 16, 16, (x, y) => {
            int centerX = 8;
            int heightLimit = 15 - Mathf.Abs(x - centerX) * 2;
            if (y <= heightLimit && y >= 0)
            {
                if (y > heightLimit - 3) return new Color(0.3f, 0.9f, 1.0f); // 鋭い水晶先端（シアン）
                if (x % 2 == 0) return new Color(0.85f, 0.15f, 0.3f); // 赤紫結晶
                return new Color(0.65f, 0.10f, 0.25f);
            }
            return Color.clear;
        });

        // E. 城の松明・照明（可動感のある炎 16x32）
        Sprite torchSprite = CreateTexture("Decor_Torch", 16, 32, (x, y) => {
            if (y < 12)
            {
                // 金属台座
                if (x >= 6 && x <= 9) return new Color(0.3f, 0.25f, 0.2f);
                return Color.clear;
            }
            // 炎のグラデーション
            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 22.0f));
            if (dist < 6.5f)
            {
                if (dist < 2.0f) return Color.white;
                if (dist < 4.0f) return new Color(1.0f, 0.8f, 0.2f); // 黄色
                return new Color(0.9f, 0.3f, 0.1f); // 赤橙
            }
            return Color.clear;
        });

        // F. 魔王の間への巨大ポータル門（32x48）
        Sprite doorSprite = CreateTexture("Decor_BossDoor", 32, 48, (x, y) => {
            float distFromTop = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 32f));
            if (x < 3 || x > 28 || y < 2 || (y > 32 && distFromTop > 13f))
            {
                return new Color(0.25f, 0.20f, 0.30f); // 豪華な石枠
            }
            // 内側の時空渦模様
            float ring = (x * 0.2f + y * 0.2f) % 1.0f;
            return Color.Lerp(new Color(0.1f, 0.05f, 0.3f), new Color(0.4f, 0.8f, 1.0f), ring);
        });

        // G. 魔物（ナイト型スライム 32x32）
        Sprite slimeSprite = CreateTexture("Enemy_KnightSlime", 32, 32, (x, y) => {
            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 13.5f));
            if (dist < 11.0f)
            {
                // 甲冑ヘルメット風のデザイン
                if (y >= 16 && y <= 22 && x >= 8 && x <= 23)
                {
                    if (y == 18 && (x >= 10 && x <= 21)) return Color.red; // 赤いバイザー発光
                    return new Color(0.4f, 0.4f, 0.5f); // 鉄兜
                }
                // ボディ（紫グラデーション）
                return new Color(0.5f, 0.15f, 0.6f);
            }
            return Color.clear;
        });

        // H. 魔王（Demon King 64x64 重厚ボス）
        Sprite bossSprite = CreateTexture("Boss_DemonKing", 64, 64, (x, y) => {
            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 28.0f));
            if (dist < 24.0f)
            {
                // 邪悪な目
                if ((x >= 22 && x <= 26 || x >= 36 && x <= 40) && (y >= 32 && y <= 35)) return Color.cyan; // 時空を司る青い眼光
                // 角（金色）
                if ((x < 16 && y > 40 && x + y > 48) || (x > 47 && y > 40 && y - x > -8)) return new Color(0.9f, 0.75f, 0.2f);
                // 漆黒のマントと王冠
                if (y < 18) return new Color(0.2f, 0.02f, 0.05f); // マント
                return new Color(0.15f, 0.08f, 0.22f); // 重厚な魔王アーマー
            }
            return Color.clear;
        });

        AssetDatabase.Refresh();

        // --- 2. 各種機能付きPrefabの生成 ---
        GameObject brickPrefab = CreateBlockPrefab(brickSprite);
        GameObject bgWallPrefab = CreateDecorationPrefab("Tile_CastleBGBlock", bgWallSprite, -2);
        GameObject timePlatformPrefab = CreateTimePlatformPrefab(timePlatformSprite);
        GameObject trapPrefab = CreateTrapPrefab(trapSprite);
        GameObject torchPrefab = CreateDecorationPrefab("Decor_Torch", torchSprite, 1);
        GameObject doorPrefab = CreateDecorationPrefab("Decor_BossDoor", doorSprite, 0);
        GameObject enemyPrefab = CreateEnemyPrefab(slimeSprite);
        GameObject bossPrefab = CreateBossPrefab(bossSprite);

        // --- 3. フルステージ（背景・地形・罠・敵・ボス部屋）の完全自動レイアウト作成 ---
        BuildFullStageLayout(brickPrefab, bgWallPrefab, timePlatformPrefab, trapPrefab, torchPrefab, doorPrefab, enemyPrefab, bossPrefab);

        AssetDatabase.Refresh();
        Debug.Log("【魔王城ステージ完全版】全てのテクスチャ、ギミックPrefab、フル地形レイアウトの生成が正常に完了しました！");
    }

    private static Sprite CreateTexture(string name, int width, int height, System.Func<int, int, Color> colorLogic)
    {
        Texture2D tex = new Texture2D(width, height);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                tex.SetPixel(x, y, colorLogic(x, y));
            }
        }
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        string path = SAVE_PATH + "Sprites/" + name + ".png";
        File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static GameObject CreateBlockPrefab(Sprite sprite)
    {
        GameObject go = new GameObject("Tile_CastleBrickBlock");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        go.AddComponent<BoxCollider2D>();
        string path = SAVE_PATH + "Prefabs/Tile_CastleBrickBlock.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);
        return prefab;
    }

    private static GameObject CreateTimePlatformPrefab(Sprite sprite)
    {
        GameObject go = new GameObject("Gimmick_TimePlatform");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 1;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2f, 1f);
        go.AddComponent<TimeMovingPlatform>(); // 移動床ギミック
        string path = SAVE_PATH + "Prefabs/Gimmick_TimePlatform.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);
        return prefab;
    }

    private static GameObject CreateDecorationPrefab(string name, Sprite sprite, int sortingOrder)
    {
        GameObject go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        string path = SAVE_PATH + "Prefabs/" + name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);
        return prefab;
    }

    private static GameObject CreateTrapPrefab(Sprite sprite)
    {
        GameObject go = new GameObject("Trap_TimeSpike");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 1;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        go.AddComponent<TimeSpikeTrapPro>();
        string path = SAVE_PATH + "Prefabs/Trap_TimeSpike.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);
        return prefab;
    }

    private static GameObject CreateEnemyPrefab(Sprite sprite)
    {
        GameObject go = new GameObject("Enemy_KnightSlime");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 2;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        go.AddComponent<BoxCollider2D>();
        var enemy = go.AddComponent<DemonEnemyPro>();
        enemy.maxHp = 50;
        enemy.attackDamage = 15;
        enemy.moveSpeed = 2.5f;

        string path = SAVE_PATH + "Prefabs/Enemy_KnightSlime.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);
        return prefab;
    }

    private static GameObject CreateBossPrefab(Sprite sprite)
    {
        GameObject go = new GameObject("Boss_DemonKing");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 3;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(3f, 3.5f);
        var boss = go.AddComponent<DemonKingBossPro>();
        boss.maxHp = 500;
        boss.attackDamage = 30;

        string path = SAVE_PATH + "Prefabs/Boss_DemonKing.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);
        return prefab;
    }

    private static void BuildFullStageLayout(GameObject block, GameObject bg, GameObject timePlatform, GameObject trap, GameObject torch, GameObject door, GameObject enemy, GameObject boss)
    {
        GameObject stageRoot = new GameObject("DemonCastleStage_Pro");

        GameObject blocksGroup = new GameObject("01_Blocks");
        blocksGroup.transform.SetParent(stageRoot.transform);

        GameObject bgGroup = new GameObject("00_Background");
        bgGroup.transform.SetParent(stageRoot.transform);

        GameObject gimmicksGroup = new GameObject("02_Gimmicks");
        gimmicksGroup.transform.SetParent(stageRoot.transform);

        GameObject enemiesGroup = new GameObject("03_Enemies");
        enemiesGroup.transform.SetParent(stageRoot.transform);

        // 60x14 マップデータの構築
        int width = 60;
        int height = 14;
        int[,] map = new int[height, width];

        // 構造定義: 1=床/壁, 2=トゲ, 3=松明, 4=ドア, 5=時間移動床, 8=ザコ敵, 9=魔王ボス
        for (int x = 0; x < width; x++) { map[0, x] = 1; map[13, x] = 1; }
        for (int y = 0; y < height; y++) { map[y, 0] = 1; map[y, width - 1] = 1; }

        // エリア1: 序盤（ステップ＋トゲトラップ）
        for (int x = 6; x <= 10; x++) map[3, x] = 1;
        for (int x = 12; x <= 16; x++) map[1, x] = 2; // 床一面のトゲ
        map[5, 14] = 5; // トゲの上に浮遊する「時間移動床」

        // エリア2: 中層（２段構造＋警備魔物）
        for (int x = 20; x <= 32; x++) map[5, x] = 1;
        map[6, 24] = 8;
        map[6, 29] = 8;
        map[1, 26] = 2;

        // エリア3: 魔王の間への大回廊
        for (int x = 36; x <= 38; x++) map[3, x] = 1;
        for (int x = 42; x <= 58; x++) map[1, x] = 1; // 魔王の間の床

        // 飾り・ボス配置
        map[2, 3] = 3; map[7, 22] = 3; map[7, 30] = 3; map[3, 44] = 3; map[3, 56] = 3;
        map[2, 57] = 4; // 扉
        map[2, 50] = 9; // 魔王（ボス）

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 pos = new Vector3(x, y, 0);

                // 背景タイルの敷き詰め
                GameObject bgObj = (GameObject)PrefabUtility.InstantiatePrefab(bg);
                bgObj.transform.position = pos;
                bgObj.transform.SetParent(bgGroup.transform);

                int tile = map[y, x];
                if (tile == 1)
                {
                    GameObject b = (GameObject)PrefabUtility.InstantiatePrefab(block);
                    b.transform.position = pos;
                    b.transform.SetParent(blocksGroup.transform);
                }
                else if (tile == 2)
                {
                    GameObject t = (GameObject)PrefabUtility.InstantiatePrefab(trap);
                    t.transform.position = pos;
                    t.transform.SetParent(gimmicksGroup.transform);
                }
                else if (tile == 3)
                {
                    GameObject tc = (GameObject)PrefabUtility.InstantiatePrefab(torch);
                    tc.transform.position = pos + new Vector3(0, 0.5f, 0);
                    tc.transform.SetParent(bgGroup.transform);
                }
                else if (tile == 4)
                {
                    GameObject d = (GameObject)PrefabUtility.InstantiatePrefab(door);
                    d.transform.position = pos + new Vector3(0.5f, 1.0f, 0);
                    d.transform.SetParent(stageRoot.transform);
                }
                else if (tile == 5)
                {
                    GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(timePlatform);
                    p.transform.position = pos;
                    p.transform.SetParent(gimmicksGroup.transform);
                }
                else if (tile == 8)
                {
                    GameObject e = (GameObject)PrefabUtility.InstantiatePrefab(enemy);
                    e.transform.position = pos + new Vector3(0, 0.5f, 0);
                    e.transform.SetParent(enemiesGroup.transform);
                }
                else if (tile == 9)
                {
                    GameObject b = (GameObject)PrefabUtility.InstantiatePrefab(boss);
                    b.transform.position = pos + new Vector3(0, 1.5f, 0);
                    b.transform.SetParent(enemiesGroup.transform);
                }
            }
        }

        PrefabUtility.SaveAsPrefabAsset(stageRoot, SAVE_PATH + "Prefabs/DemonCastleStage_FullLayout.prefab");
    }
}
#endif
