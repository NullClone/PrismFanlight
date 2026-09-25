<h1 align="center">
  Prism Fanlight
</h1>

<p align="center">
    <img src="https://img.shields.io/github/v/release/NullClone/PrismFanlight" alt="Latest Release"></a>
  <a href="https://github.com/NullClone/PrismFanlight/blob/main/LICENSE.md">
    <img src="https://img.shields.io/badge/License-MIT-brightgreen.svg" alt="License MIT"></a>
</p>

<p align="center">
  <a href="#概要">概要</a> •
  <a href="#機能">機能</a> •
  <a href="#インストール">インストール</a> •
  <a href="#はじめかた">はじめかた</a> •
  <a href="#動作環境">動作環境</a> •
  <a href="#ライセンス">ライセンス</a>
</p>


<p align="center">
  <a href="README.md">English</a> | 日本語
</p>

## 概要

Prism Fanlightは、バーチャルライブで大規模な観客によるペンライト演出を実現するためのシステムです。

低負荷で数万人規模の観客を描画することができ、Timelineから色や明るさなどを細かく制御できます。

## 機能

- **Timeline 制御**
  - モーション、色、明るさ、BPM を専用トラックで制御
  - ブレンド機能によるクリップ間のスムーズな遷移
  - シーク、逆再生、スクラブ、ループに対応
- **ライティング・カラー演出**
  - パレット配色やグラデーションによる会場全体の色制御
  - ウェーブ、パルス、ランダムな明滅など、空間全体を走る多彩な発光パターン
  - 楽曲テンポに連動したビート同期演出
- **観客モーション**
  - 基本的なペンライト動作プリセットを同梱
  - 自然にモーションを切り替えれるブレンド機能
- **観客席 レイアウト編集**
  - ブロック・列・座席単位での自由な会場設計
  - グリッド生成やミラー配置などの編集支援機能
  - ランタイム実行用データへの事前ベイク機構
- **GPU 描画と最適化**
  - 数万人規模の観客の姿勢・発光計算および描画を GPU で一括処理
  - 視錐台カリングと距離に応じた観客の LOD

## インストール

1. パッケージマネージャーを開きます `Window > Package Manager`
2. 左上の`+`ボタンから`Add package from git URL...`を選択します。

<p align="center">
  <img width="50%" src="https://github.com/user-attachments/assets/ed1fc738-0412-40e8-aa84-b32b643c31cb">
</p>

3. 以下のURLを入力します。
   ```bash
   https://github.com/NullClone/PrismFanlight.git
   ```

> [!NOTE]
> また、[ここから](https://github.com/NullClone/PrismFanlight/releases/latest)`.unitypackage`をUnity上にドロップすることでインストールも可能です。

## はじめかた

1. `GameObject > Light > Prism Fanlight`からシーン上に配置します。
2. Inspectorで既定の演出を設定します。Timelineのクリップがない時はこの設定で動きます。
3. Layoutアセットをダブルクリックしてレイアウトエディターを開き、会場を編集してBakeします。
4. TimelineにPrism Fanlightのトラックを追加し、クリップを配置します。

## 動作環境

- Unity 6.3 – 6.5
- URP または HDRP
- Compute Shaderに対応したプラットフォーム

## ライセンス

本ツールは **MITライセンス** のもとで公開されています（詳細は `LICENSE` ファイルをご確認ください）。

商用・非商用問わず自由にご利用いただけます。
必須ではありませんが、本ツールを気に入っていただけましたら、以下の2点についてご協力とご配慮をいただけますと幸いです。

### 1. クレジット表記
制作物のスタッフロール、または同梱のドキュメント等に、制作者名とリポジトリのURLを明記していただけると励みになります。

`Tools developed by NullClone (github.com/NullClone/PrismFanlight)`

### 2. 法人・大規模チームでのご利用について
法人または大規模なプロジェクトで本ツールをご利用の際は、メールやSNS等でご一報いただけますと大変嬉しいです。
また、ご報告いただいたプロジェクトにつきましては、私のポートフォリオとして掲載・ご紹介させていただけますと幸いです。
