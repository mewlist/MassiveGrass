# MassiveGrass

Terrain の地形に沿って大量の草を生やします。

Unity2019.3 以降に対応

## インストール

PackageManageer に Github の リポジトリ uri を指定してください。

### セットアップ

- 任意の GameObject に MassiveGrass コンポーネントを追加します

### プロファイルの作成

- 右クリックメニューから MassiveGrass を選択して MassiveGrassProfile を作成しパラメータを調整します
- TerrainLayers : 草をはやしたい TerrainLayers を指定します。 Terrain Layer で塗られた場所に草を生やします。
- Scale : 草一つ分の Quad メッシュのサイズ
- Radius : 視点からの最大生成距離
- Grid Size : Terrain をグリッドに分割する際の 1 グリッド四方あたりの辺の長さ
- Slant : 草をランダムに寝そべらせる強さ
- Amount Per Block : 1グリッドあたりに生成する Quad の数
- Material : マテリアル
- Layer : 生成する Unity レイヤ
- Alpha Map Threshold : Alphamap の濃さがこの閾値を超えた場所に草を生成
- Cast Shadows : 影の ON / OFF
- BuilderType
  - Quad : 正方形メッシュを生成します
  - FromMesh : 指定した Mesh を複製して生成します

### サンプルシーン

サンプルシーンを以下で公開しています。
https://github.com/mewlist/MassiveGrassExample

