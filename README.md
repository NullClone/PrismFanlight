# Prism Fanlight

`Prism Fanlight`は、大規模な観客とペンライト演出をTimeline上で制御できるツールです。

## 主な機能

<p align="left">
  <img width="60%" src="https://github.com/user-attachments/assets/cceb3440-762a-459a-b176-35b94c7fe74a">
</p>

- **Timelineによる演出** — モーション・カラーなどのパラメーターを細かく制御できます
- **リアルなモーション** — リアルかつ様々な動きに対応することができます
- **完全な再現性** — 時間に対して完全に動きを再現することができます
- **GPU駆動の描画** — GPUを使用して描画しているため、大量の観客でも軽量です
- **レイアウトシステム** — 観客・ペンライトを大規模な座席レイアウト上で調整できます
- **カリング機能** — 必要な観客・ペンライトだけを描画対象として判定します

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
   
4. インストール完了後、`Create Other/Prism Fanlight`からシーン上に配置してください。

> [!NOTE]
> また、[ここから](https://github.com/NullClone/PrismFanlight/releases/latest)`.unitypackage`をUnity上にドロップすることでインストールも可能です。

## 注意点

- 大量のペンライトを描画する場合にパフォーマンスが著しく低下する可能性があります。
- Compute Shaderが使えない環境では動作しません。

## 動作確認

- Unity 6.3
- URP / ~~HDRP~~

## Todo
- [x] Timeline サポート
- [ ] HDRP サポート
- [ ] ~~Built-in サポート~~
- [x] カリング 機能
- [x] 複数持ち 機能
- [x] 観客の描画 機能
- [x] 再現性を完全に担保
- [x] ベイク機能の実装
- [ ] レイアウト機能の強化
- [ ] 色のモードを追加 (Rainbow)
- [ ] 専用のポストプロセッシング
- [ ] サンプルを追加

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
