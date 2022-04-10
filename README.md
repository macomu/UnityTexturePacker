# 言語
- [日本語](#日本語)
- [English](#English)

# 日本語
## UnityTexturePackerとは
UnityTexturePackerは複数のテクスチャを1枚のアトラスにまとめ、Spriteで使えるようにするUnity用のツールです。

## 導入方法
Assets/ma_comuフォルダをダウンロードしてUnityのプロジェクトフォルダにおくか、
ReleasesページからUnityパッケージをダウンロードしてインポートしてください。

## 使い方
「Window」->「ma_comu」->「TexturePacker」でウィンドウを表示してください。

### 新規作成する場合
1. アトラスに含めたいテクスチャをプロジェクトビューで選択します  
1個ずつではなく複数選択してください
2. リストにテクスチャ名が表示されていることを確認します  
これがスプライト名になります
3. 「Select」ボタンを押して保存場所を指定します(**Assets**フォルダ内に作成してください)
4. 「Create」ボタンを押してください

### 既存のテクスチャを編集する場合
1. 削除したいスプライト名横の「Stay」を押します
2. 追加、もしくは修正したいテクスチャをプロジェクトビューで選択します(1個ずつではなく複数選択してください)
3. スプライト名横の表示が下記になっていることを確認します  
「D」 -> 削除するスプライト  
「M」 -> 修正するスプライト  
「Stay」 -> そのまま使うスプライト
4. 「Update」ボタンを押してください

# English
## What's this?
This is a Texture Packer for Unity to create an atlas.

## Installation
Dowonload "Assets/ma_comu" and put in your project.
Or Download Unity Package and import your project.

## How to use
Click "Window" -> "ma_comu" -> "TexturePacker"

### Create new atlas
1. Select textures in Project view
2. Check the list which displaying selected textures
3. Click "Select" to select save directory
4. Click "Create"

### Modify already exists atlas
1. If you want to remove a sprite in the atlas, Click "Stay" which is left of sprite name
2. Select textures in Project view to add/modify sprites
3. Check the status which is left of sprite name  
「D」 -> Will remove
「M」 -> Will modify (overwrite)
「Stay」 -> Keep sprite
4. Click "Update"

