【ひねくれ霊 Unity導入手順】
1. GhostEnemy フォルダごと Assets/ にドラッグ
2. メニュー Tools > Ghost > Create Ghost Prefab を実行
   -> スプライト設定とフレーム割り当て済みの Ghost.prefab ができます
3. Ghost.prefab をシーンに配置
4. プレイヤーに Tag "Player" を付ける（複数いれば一番近い相手を追尾）
5. 再生

・プレイヤー側に TakeDamage(int) メソッドがあれば攻撃時に呼ばれます
・倒すときは ghost.GetComponent<GhostEnemy>().TakeDamage(1);
・速度/索敵距離/HPはインスペクターで調整
