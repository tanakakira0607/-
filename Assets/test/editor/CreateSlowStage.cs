// 配置場所: Assets/Editor/CreateSlowStage.cs
// 使い方: Unityメニュー Tools > Stage > 月夜の墓地ステージを生成
//
// 座標の基準: 地面の上面が y=0、プレイヤー開始位置が x=0 付近。1ユニット=1m換算。
// 敵(幽霊)は「印」だけ配置します。自作の幽霊Prefabに置き換えるときは
// "Enemies" 以下の各マーカーの位置に Prefab を置いて、マーカーを削除してください。

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class CreateSlowStage
{
    const float GroundThickness = 3f;

    static Sprite boxSprite;
    static Sprite circleSprite;

    [MenuItem("Tools/Stage/月夜の墓地ステージを生成")]
    public static void Create()
    {
        var old = GameObject.Find("Stage_MoonlitGraveyard");
        if (old != null)
        {
            if (!EditorUtility.DisplayDialog("確認", "既存のステージを削除して作り直しますか？", "作り直す", "キャンセル")) return;
            Undo.DestroyObjectImmediate(old);
        }

        boxSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        var root = new GameObject("Stage_MoonlitGraveyard");
        Undo.RegisterCreatedObjectUndo(root, "Create Stage");

        var ground = Child(root.transform, "Ground");
        var hazards = Child(root.transform, "Hazards");
        var enemies = Child(root.transform, "Enemies");
        var items = Child(root.transform, "Items");
        var points = Child(root.transform, "Points");
        var areas = Child(root.transform, "AreaLabels");

        Color groundCol = new Color(0.35f, 0.27f, 0.18f);
        Color collapseCol = new Color(0.85f, 0.5f, 0.2f);
        Color slowCol = new Color(0.4f, 0.7f, 0.9f);

        // ---------------- 地面 ----------------
        // エリア1: 墓地の入口 (x 0-40)  小さな穴 x24-26.5
        Block(ground, "Ground_A1_a", 0f, 24f, 0f, groundCol);
        Block(ground, "Ground_A1_b", 26.5f, 70f, 0f, groundCol); // エリア1後半+エリア2

        // エリア3: 崩れる足場 (x 70-98)  ※足場が崩れる処理は自作スクリプトを付けてください
        Block(ground, "CollapsePlatform_1", 73f, 77f, 0f, collapseCol, 0.8f);
        Block(ground, "CollapsePlatform_2", 80f, 83.5f, 0f, collapseCol, 0.8f);
        Block(ground, "CollapsePlatform_3", 86.5f, 90f, 0f, collapseCol, 0.8f);
        Block(ground, "CollapsePlatform_4", 93f, 96f, 0f, collapseCol, 0.8f);

        // エリア4: 時間を稼ぐ区間 (x 98-130)
        Block(ground, "Ground_A4", 98f, 130f, 0f, groundCol);
        Block(ground, "HighPlatform_A4", 116f, 122f, 3.5f, groundCol, 0.8f);
        Block(ground, "SlowOnlyPlatform_A4", 106f, 110f, 4f, slowCol, 0.5f); // スロー中だけ実体化させる想定

        // エリア5: ボス部屋 (x 132-162)  手前に小さな穴 x130-132
        Block(ground, "Ground_A5", 132f, 162f, 0f, groundCol);

        // 壁
        Block(ground, "Wall_Left", -1f, 0f, 30f, groundCol, 40f);
        Block(ground, "Wall_Right", 162f, 163f, 30f, groundCol, 40f);

        // 落下判定
        var death = Child(hazards.transform, "DeathZone");
        death.transform.position = new Vector3(81f, -10f, 0f);
        var dz = death.AddComponent<BoxCollider2D>();
        dz.isTrigger = true;
        dz.size = new Vector2(200f, 4f);

        // ---------------- プレイヤー ----------------
        Marker(points, "PlayerStart", new Vector2(2f, 1f), 3, new Color(0.3f, 1f, 0.4f, 0.8f));

        // ---------------- チェックポイント ----------------
        Marker(points, "Checkpoint_1", new Vector2(40f, 1f), 3, new Color(0.3f, 1f, 0.4f, 0.6f));
        Marker(points, "Checkpoint_2", new Vector2(99f, 1f), 3, new Color(0.3f, 1f, 0.4f, 0.6f));
        Marker(points, "Checkpoint_3", new Vector2(133f, 1f), 3, new Color(0.3f, 1f, 0.4f, 0.6f));
        Marker(points, "Goal", new Vector2(159f, 1.5f), 4, new Color(1f, 0.9f, 0.2f, 0.9f));

        // ---------------- 幽霊の印 (赤) ----------------
        Color ghost = new Color(1f, 0.2f, 0.2f, 0.6f);
        // エリア1: 1体だけ
        Ghost(enemies, "Ghost_A1_01", 14f, 2f, ghost, "ゆっくり漂う(チュートリアル)");
        // エリア2: 規則的に上下しながら横切る。スローで隙間を抜ける
        Ghost(enemies, "Ghost_A2_01", 46f, 2f, ghost, "上下ゆれ");
        Ghost(enemies, "Ghost_A2_02", 52f, 3.5f, ghost, "上下ゆれ(逆位相)");
        Ghost(enemies, "Ghost_A2_03", 58f, 2f, ghost, "上下ゆれ");
        Ghost(enemies, "Ghost_A2_04", 64f, 3.5f, ghost, "上下ゆれ(逆位相)");
        // エリア3: 崩れる足場に追加のプレッシャー
        Ghost(enemies, "Ghost_A3_01", 84.5f, 3f, ghost, "足場上空を巡回");
        Ghost(enemies, "Ghost_A3_02", 91f, 2.5f, ghost, "足場上空を巡回");
        // エリア4: 巡回(倒すか避けるか)
        Ghost(enemies, "Ghost_A4_Patrol_01", 104f, 1.5f, ghost, "巡回 x:101-109");
        Ghost(enemies, "Ghost_A4_Patrol_02", 114f, 1.5f, ghost, "巡回 x:111-119");
        Ghost(enemies, "Ghost_A4_Patrol_03", 124f, 1.5f, ghost, "巡回 x:121-128");
        // エリア5: ボス
        Marker(enemies, "Boss_Spawn", new Vector2(150f, 4f), 6, new Color(0.7f, 0.1f, 0.9f, 0.7f), 3f);

        // ---------------- アイテム(時間回復) ----------------
        Color item = new Color(0.4f, 0.9f, 1f, 0.8f);
        Marker(items, "TimeRecovery_01", new Vector2(108f, 5.2f), 2, item, 0.6f); // スロー足場の上
        Marker(items, "TimeRecovery_02", new Vector2(119f, 5f), 2, item, 0.6f);   // 高台
        Marker(items, "TimeRecovery_03", new Vector2(127f, 1f), 2, item, 0.6f);   // 巡回ゴーストの奥

        // ---------------- エリア見出し ----------------
        Label(areas, "Area1_墓地の入口", 20f);
        Label(areas, "Area2_ふわふわゴースト地帯", 55f);
        Label(areas, "Area3_崩れる足場", 85f);
        Label(areas, "Area4_時間を稼ぐ区間", 114f);
        Label(areas, "Area5_月下のボス戦", 147f);

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("月夜の墓地ステージを生成しました。");
    }

    // ---------- helpers ----------
    static GameObject Child(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    // 上面が yTop、x範囲 [xMin,xMax] のブロックを作る
    static void Block(GameObject parent, string name, float xMin, float xMax, float yTop, Color color, float thickness = GroundThickness)
    {
        float w = xMax - xMin;
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = new Vector3((xMin + xMax) * 0.5f, yTop - thickness * 0.5f, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = boxSprite;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(w, thickness);
        sr.color = color;
        sr.sortingOrder = 1;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(w, thickness);
    }

    static void Ghost(GameObject parent, string name, float x, float y, Color color, string note)
    {
        var go = Marker(parent, name + "  [" + note + "]", new Vector2(x, y), 6, color, 1f);
    }

    static GameObject Marker(GameObject parent, string name, Vector2 pos, int iconIndex, Color color, float size = 1f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * size;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = circleSprite;
        sr.color = color;
        sr.sortingOrder = 5;

        SetIcon(go, iconIndex);
        return go;
    }

    static void Label(GameObject parent, string name, float x)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = new Vector3(x, 8f, 0f);
        SetIcon(go, 1);
    }

    // Scene上でラベル付きアイコンとして見えるようにする (0:灰 1:青 2:水 3:緑 4:黄 5:橙 6:赤 7:紫)
    static void SetIcon(GameObject go, int index)
    {
        var tex = EditorGUIUtility.IconContent("sv_label_" + index).image as Texture2D;
        if (tex != null) EditorGUIUtility.SetIconForObject(go, tex);
    }
}
