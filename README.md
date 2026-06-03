# Prism Fanlight

Unityのライブ用の観客ペンライトツール

## 主な機能

- GPUインスタンシングによる大量のペンライト描画
- プリセット機能
- 様々な色設定
- BPM機能
- カメラのカリング機能
- 座席ごとの反応遅延、ジッター
- 座席プレビュー機能

## 使用方法

1. パッケージマネージャーを開きます `Window > Package Manager`
2. 左上の`+`ボタンから`Add package from git URL...`を選択します。

<p align="center">
  <img width="50%" src="https://github.com/user-attachments/assets/ed1fc738-0412-40e8-aa84-b32b643c31cb">
</p>

3. 以下のURLを入力します。
   ```bash
   https://github.com/NullClone/PrismFanlight.git
   ```
   
4. シーン上のゲームオブジェクトに`PrismFanlight` コンポーネントを追加

## 対応環境

- Unity 6+ (6.3で機能を確認済み)
- URP (17+)
- Compute Shaderが使用可能な環境

## 注意点

- 大量のペンライトを描画する場合にパフォーマンスが著しく低下する可能性があります。
- 現在のカリングにはブロック単位の球カリングを使用しているため、精度があまり良くありません。
- ギズモが重い場合は、Debugからプレビューを無効にしてください。。

## Todo
- [ ] Timeline 連携
- [x] HDRP サポート
- [ ] Built-in サポート
- [ ] カリング機能の改善
- [ ] 複数持ち機能

## ライセンス

MIT License
