#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// メニュー Tools > Ghost > Create Ghost Prefab で
/// ・スプライトのインポート設定（Point / 非圧縮）
/// ・各モーションのフレーム割り当て
/// ・Prefab生成（Assets/GhostEnemy/Ghost.prefab）
/// を一括で行います。
/// </summary>
public static class GhostPrefabCreator
{
    const string SpriteFolder = "Assets/GhostEnemy/Sprites";
    const string PrefabPath = "Assets/GhostEnemy/Ghost.prefab";

    [MenuItem("Tools/Ghost/Create Ghost Prefab")]
    public static void Create()
    {
        // 1) インポート設定
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D ghost_", new[] { SpriteFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 160;           // 1セル = 1ユニット
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;   // 足元を基準に
            ti.SetTextureSettings(settings);
            ti.SaveAndReimport();
        }

        // 2) Prefab作成
        var go = new GameObject("Ghost");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;
        var enemy = go.AddComponent<GhostEnemy>();

        enemy.idleFrames   = Load("idle");
        enemy.moveFrames   = Load("move");
        enemy.alertFrames  = Load("alert");
        enemy.attackFrames = Load("attack");
        enemy.damageFrames = Load("damage");
        enemy.vanishFrames = Load("vanish");
        sr.sprite = enemy.idleFrames.FirstOrDefault();

        // 当たり判定（Trigger）。プレイヤー側の攻撃判定用
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.35f;
        col.offset = new Vector2(0f, 0.45f);

        PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);
        AssetDatabase.SaveAssets();
        Debug.Log("Ghost.prefab を作成しました: " + PrefabPath);
    }

    static Sprite[] Load(string motion)
    {
        return AssetDatabase.FindAssets("t:Sprite ghost_" + motion + "_", new[] { SpriteFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(p => p)                        // _0, _1, _2 ...の順
            .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
            .ToArray();
    }
}
#endif
