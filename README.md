# Prism Fanlight

Unityのライブ用の観客ペンライトツールです。

## 主な機能

- GPUインスタンシングによる大量のペンライト描画
- BPM 機能
- 便利なプリセット機能
- 座席ごとの反応遅延、ジッター
- 色設定（単色、ランダム、レインボー、ウェーブ）
- 座席プレビュー

## 対応環境

- Unity 6000.3 以降
- Compute Shaderが使用できる環境

## 基本的な使い方

シーン上のゲームオブジェクトに`PrismFanlight` コンポーネントを追加してください。

## 注意点

- 大量のペンライトを描画する場合にパフォーマンスが著しく低下する可能性があります。
- Compute Shaderがアタッチされていない場合は、スクリプトファイルのインスペクターから再度設定してください。
- 現在のカリングにはブロック単位の球カリングを使用しています。
- Debugの項目の更新タイミングは遅いです。
- 席が多い場合はSceneViewが重くなることがあります。Debugからプレビューを無効にすることが出来ます。

## 設定

### Rendering

描画に必要なリソースと更新頻度を設定します。

- `Mesh`: 描画するペンライトメッシュ
- `Material`: Indirect 描画用マテリアル
- `Rendering Layer`: 描画レイヤー
- `Animation / Color`: モーションとカラーの GPU 更新頻度
- `Visibility`: カリングと可視インスタンス生成の GPU 更新頻度
- `Enable Culling`: GPU カリングの有効/無効
- `Culling Camera`: カリングに使うカメラ

### Layout

観客席の矩形ブロックレイアウトを設定します。

- `Block Count`: ブロック数
- `Aisle Width`: ブロック間の通路幅
- `Seats Per Block`: 1 ブロック内の座席数
- `Seat Pitch`: 座席間隔

### Tempo

BPM 同期モーション用の曲時間を設定します。

- `Enable`: BPM 同期を有効化
- `BPM`: 曲の BPM
- `Beats Per Bar`: 1 小節あたりの拍数
- `Clock Source`: 曲時間の取得元
- `Audio Source`: `AudioSourceTime` 使用時の AudioSource
- `Manual Time`: `ManualTime` 使用時の手動曲時間
- `Offset Seconds`: 曲時間のオフセット
- `Latency Compensation Seconds`: レイテンシ補正

### Motion

ペンライトの振り方を設定します。

主な設定グループ:

- `Timing`: 基本周波数、ランダム位相、ノイズ、反応遅延、テンポ揺らぎ
- `BPM Sync`: BPM 同期量、何拍で 1 振りか、拍位相、ダウンビート強調、座席/ブロック遅延
- `Swing Shape`: 腕の長さ、角度、スナップ、ホールド、フリック、戻りバイアス
- `Direction / Axis`: 振り軸、前後/上下成分、軸のばらつき
- `Variation`: 座席位置、高さ、腕長のばらつき
- `Humanization`: 熱量、休憩、控えめな動き

`Beat Sync Amount` が 0 の場合は従来の Hz ベースの動き、1 の場合は BPM の拍に同期した動きになります。

### Rest

休憩を再現し、動きの少ない観客を混ぜられます。

- `Rest Amount`: 休憩候補になる観客の割合
- `Rest Intensity`: 休憩中の動きの強さ
- `Rest Cycle Duration`: 休憩サイクルの長さ
- `Rest Duration`: 1 回の休憩時間
- `Rest Fade Duration`: 休憩への入り/戻りのフェード時間
- `Rest Phase Randomness`: 座席ごとの休憩開始タイミングのばらつき
- `Small Motion Ratio`: 常に控えめに振る観客の割合

`Rest Cycle Duration` または `Rest Duration` が 0 の場合、休憩候補は常に `Rest Intensity` の動きになります。両方を設定すると、休憩候補が周期的に休憩し、フェードしながら復帰します。

### Color

ペンライトの色と色エフェクトを設定します。

対応モード:

- `Solid`
- `RandomHue`
- `Rainbow`
- `Wave`
- `RadialWave`
- `BlockGradient`

## ライセンス

MIT License
