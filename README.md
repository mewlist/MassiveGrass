# MassiveGrass

![Sample Image](https://github.com/mewlist/MassiveGrass/blob/master/MassiveGrassImage.png?raw=true)

Terrain の地形に沿って大量の草を生やします。

Unity2018.4 以降に対応

## 使い方

### セットアップ

- 任意の GameObject に MassiveGrass コンポーネントを追加します
- Target Terrain に草をはやしたい Terrain を指定します
- Bake ボタンを押して，Terrain の AlphaMap (塗り分け情報) を取得します

### プロファイルの作成

- 右クリックメニューから MassiveGrass を選択して MassiveGrassProfile を作成しパラメータを調整します
- Paint Texture Index : 草をはやしたい AlphaMap のインデックスを指定します
- Scale : 草一つ分の Quad メッシュのサイズ
- Radius : 視点からの最大生成距離
- Grid Size : Terrain をグリッドに分割する際の 1 グリッド四方あたりの辺の長さ
- Slant : 草をランダムに寝そべらせる強さ
- Amount Per Block : 1グリッドあたりに生成する Quad の数
- Material : マテリアル
- Layer : 生成する Unity レイヤ
- Alpha Map Threshold : Alphamap の濃さがこの閾値を超えた場所に草を生成
- Cast Shadows : 影の ON / OFF

### サンプルシーン

- MassiveGrass/Sample/SampleScene.unity で動作を確認できます
- WASD キーで移動，マウスのドラッグで視線の移動が出来ます

