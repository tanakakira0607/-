【BossKit】ボス専用パッケージ(ステージ用ツールとは完全に独立)

■ 導入
1. この zip の Assets フォルダを、Unityプロジェクトの Assets に上書きコピー
2. メニュー Tools > BossKit > ボスを生成
   - Assets/BossKit/Generated にアニメーション付きドット絵(27コマ)ができます
   - シーンに BossKit_Boss が配置されます
     (配置位置: Boss_Spawn という名前のオブジェクトがあればそこ。なければSceneビューの中心)
3. プレイヤーに Player タグを付ける

■ ダメージ
BossKit/Runtime/BossHit.cs の中で、次の形でダメージが入ります。
  playerHealth = GameObject.FindWithTag("Slider").GetComponent<PlayerHealth>();
  playerHealth.TakeDamage(1);
(タグを変えるなら BossHit.healthTag を書き換え。クラス名が違う場合もこのファイルだけ直す)
※ 当たり判定そのものは Player タグのプレイヤーで行います。

■ プレイヤーの攻撃をボスに当てる
  other.GetComponent<BossKit.BossController>()?.TakeDamage(1);

■ ニアミス(スロー中のギリギリ回避)
  BossKit.BossProjectile.Grazed += pos => { /* SEやスローゲージ回復など */ };

■ イベント(インスペクターで登録可)
  BossController の onFightStart / onPhase2 / onDefeated

■ アニメーション
  待機 / 怒り待機 / 溜め(予兆) / 攻撃 / ダッシュ / ぐったり / 被弾 / 撃破
  BossAnimator の各コマ配列に自分の画像を入れれば差し替え可能。
  Tools > BossKit > ドット絵を再生成(上書き) で生成し直せます。
