【導入】
1. この zip の Assets フォルダを、Unityプロジェクトの Assets に上書きコピー
2. 以前の CreateSlowStage.cs は削除してOK
3. メニュー Tools > Stage > 月夜の墓地ステージを生成
4. ギミックはタグ "Player" のオブジェクトに反応します

【含まれるギミック(ステージ専用・ゲームシステムなし)】
CollapsePlatform  : 乗ると崩れて復活する足場
SlowOnlyPlatform  : スロー中だけ実体化する足場
                    スロー判定を変える場合 → SlowOnlyPlatform.SlowCheck = () => player.IsSlow;
TimeRecoveryItem  : 浮かぶアイテム。取ると onCollected を呼ぶだけ
Checkpoint        : ランタンが灯るだけ。onActivated を呼ぶだけ
GoalZone          : 門。onReached を呼ぶだけ
EnemyMarker       : 敵の位置の印(巡回範囲などをSceneビューに表示)
