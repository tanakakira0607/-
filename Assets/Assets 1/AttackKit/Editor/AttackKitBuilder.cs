// Tools > AttackKit > プレイヤーの攻撃を作成
// 同梱のドット絵を取り込み、弾のプレハブを作って、プレイヤーに攻撃(PlayerAttack)を付けます。
// StageKit / BossKit / CastleKit とは完全に独立しています。

using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using AttackKit;

public static class AttackKitBuilder
{
    static string rootDir = "Assets/AttackKit";
    static string TexDir { get { return rootDir + "/Textures"; } }
    static string PrefabDir { get { return rootDir + "/Prefabs"; } }

    // ------------------------------------------------------------------
    // メニュー
    // ------------------------------------------------------------------
    [MenuItem("Tools/AttackKit/プレイヤーの攻撃を作成")]
    public static void CreateAttack()
    {
        FindRoot();
        if (!PrepareTextures()) return;

        // 付ける先: シーン内で選択中のオブジェクト。なければ Player タグのオブジェクト
        GameObject target = null;
        GameObject sel = Selection.activeGameObject;
        if (sel != null && sel.scene.IsValid()) target = sel;
        if (target == null) target = GameObject.FindWithTag("Player");
        if (target == null)
        {
            Debug.LogError("プレイヤーが見つかりません。プレイヤーを Hierarchy で選択するか、Player タグを付けてからもう一度実行してください。");
            return;
        }

        Directory.CreateDirectory(PrefabDir);
        AssetDatabase.Refresh();

        GameObject shot = MakeShotPrefab("PlayerShot", "shot", 20f, 1, 0, 0.2f, new Vector2(0.22f, 0f), 1f);
        GameObject charged = MakeShotPrefab("PlayerShotCharged", "shotcharged", 24f, 3, 3, 0.45f, new Vector2(0.53f, 0f), 1.8f);

        PlayerAttack atk = target.GetComponent<PlayerAttack>();
        if (atk == null) atk = Undo.AddComponent<PlayerAttack>(target);
        else Undo.RecordObject(atk, "Setup PlayerAttack");

        atk.shotPrefab = shot;
        atk.chargedPrefab = charged;
        atk.muzzleFrames = Seq("muzzle", 3);
        atk.chargeFrames = Seq("charge", 4);
        atk.chargeFullFrames = Seq("chargefull", 2);

        EditorUtility.SetDirty(target);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Selection.activeGameObject = target;
        Debug.Log("プレイヤーの攻撃を設定しました(" + target.name + ")。Zキー/マウス左で発射、押しっぱなしでチャージ弾。");
    }

    [MenuItem("Tools/AttackKit/テクスチャ設定を修復")]
    public static void RepairTextures()
    {
        FindRoot();
        if (PrepareTextures()) Debug.Log("テクスチャの設定を確認しました。");
    }

    // ------------------------------------------------------------------
    // 弾のプレハブ
    // ------------------------------------------------------------------
    static GameObject MakeShotPrefab(string name, string framePrefix, float speed, int damage, int pierce,
                                     float radius, Vector2 colliderOffset, float hitScale)
    {
        GameObject go = new GameObject(name);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        Sprite[] frames = Seq(framePrefix, 4);
        sr.sprite = frames[0];
        sr.sortingOrder = 12;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = radius;
        col.offset = colliderOffset;

        PlayerShot ps = go.AddComponent<PlayerShot>();
        ps.speed = speed;
        ps.damage = damage;
        ps.pierce = pierce;
        ps.frames = frames;
        ps.hitFrames = Seq("hit", 4);
        ps.hitScale = hitScale;

        string path = PrefabDir + "/" + name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        UnityEngine.Object.DestroyImmediate(go);
        return prefab;
    }

    // ------------------------------------------------------------------
    // テクスチャの取り込み(ドット絵向け)
    // ------------------------------------------------------------------
    static void FindRoot()
    {
        string[] guids = AssetDatabase.FindAssets("AttackKitBuilder t:MonoScript");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!path.EndsWith("/AttackKitBuilder.cs")) continue;
            string editorDir = Path.GetDirectoryName(path).Replace('\\', '/');
            rootDir = Path.GetDirectoryName(editorDir).Replace('\\', '/');
            return;
        }
        rootDir = "Assets/AttackKit";
    }

    static bool PrepareTextures()
    {
        if (!Directory.Exists(TexDir))
        {
            Debug.LogError("フォルダが見つかりません: " + TexDir + "\nzipの中の AttackKit フォルダ(Textures を含む)ごと、プロジェクトの Assets 直下に置いてください。");
            return false;
        }

        string[] files = Directory.GetFiles(TexDir, "*.png");
        for (int i = 0; i < files.Length; i++)
        {
            SetupTexture(files[i].Replace('\\', '/'));
        }
        AssetDatabase.Refresh();

        string[] required = { "shot_0", "shot_3", "shotcharged_0", "shotcharged_3", "muzzle_0", "muzzle_2",
                              "hit_0", "hit_3", "charge_0", "charge_3", "chargefull_0", "chargefull_1" };
        for (int i = 0; i < required.Length; i++)
        {
            if (S(required[i]) == null)
            {
                Debug.LogError("読み込めない画像があります: " + required[i] + "\n" + TexDir + " に画像が揃っているか確認してください。");
                return false;
            }
        }
        return true;
    }

    static void SetupTexture(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;

        const int ppu = 16;
        TextureImporterSettings cur = new TextureImporterSettings();
        imp.ReadTextureSettings(cur);
        bool need = imp.textureType != TextureImporterType.Sprite
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

    static Sprite S(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(TexDir + "/" + name + ".png");
    }

    static Sprite[] Seq(string prefix, int n)
    {
        Sprite[] a = new Sprite[n];
        for (int i = 0; i < n; i++) a[i] = S(prefix + "_" + i);
        return a;
    }
}
