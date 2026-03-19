# yuyu Inspector

MITライセンスで公開されている [Tri Inspector](https://github.com/codewriter-packages/Tri-Inspector) をカスタマイズするためにフォークしたリポジトリです。

## インストール方法

### Package Manager 経由でのインストール

1. Package Manager を開く
2. 左上の＋ボタンをクリック
3. 「Add package from git URL...」をクリック
4. テキストフィールドに `https://github.com/yuyu0127/yuyu-Inspector.git` と入力
5. 「Add」ボタンをクリック

### manifest.json 直接編集でのインストール

Packages/manifest.json に `"com.yuyu.yuyuinspector": "https://github.com/yuyu0127/yuyu-Inspector.git"` の記述を追加

```manifest.json
{
  ...
  "dependencies": {
    ...
    "com.yuyu.yuyuinspector": "https://github.com/yuyu0127/yuyu-Inspector.git",
    ...
  },
  ...
}
```

## Tri Inspector からの変更点

### インスペクタ全般

#### WideMode の有効化

`EditorGUIUtility.wideMode` を常に有効化し、フィールドの横幅を広く使えるようにしています。

#### ラベル幅のドラッグ調整

プロパティのラベルとフィールドの境界線をドラッグすることで、ラベル幅を動的に調整できます。設定値は `EditorSessionState` に保存され、セッション中は維持されます。

#### 折りたたみの縦線表示

Foldout や SerializeReference を展開した際、折りたたみ矢印の下に縦線を表示し、ネスト構造を視覚的にわかりやすくしています。

#### Alt キーによる再帰展開

Foldout の矢印を Alt キーを押しながらクリックすると、子要素をすべて再帰的に展開・折りたたみできます。

#### TriEditorWindow

`TriEditorWindow` 抽象クラスを追加し、Inspector と同様の描画機能を EditorWindow 上で利用できるようにしています。

### リスト / テーブル

#### ListDrawerSettings によるテーブル描画

`ListDrawerSettings` に `Table` プロパティを追加し、リスト要素をテーブル形式で描画できるようにしています。列幅はヘッダーの境界線をドラッグして調整可能です。

```csharp
[ListDrawerSettings(Table = true)]
public List<MyItem> items;
```

#### TSV との相互変換

テーブルモード時にヘッダーに「コピー」「貼付」ボタンを表示し、テーブルデータと TSV テキストの相互変換を行えます。`ITriTsvConverter` インターフェースを実装して変換ロジックをカスタマイズできます。

#### 要素数の直接編集

リストヘッダーに要素数を変更するための IntField を表示し、Add/Remove ボタンを使わずに要素数を直接変更できます。

#### ToString() によるリスト要素ラベルの自動表示

リスト要素の型が `ToString()` をオーバーライドしている場合、その戻り値をリスト要素のラベルとして自動的に使用します（Unity / System 名前空間の型は除外）。

```csharp
[Serializable]
public struct MyStruct
{
    public string name;
    public int value;
    public override string ToString() => $"name={name}, value={value}";
}
```

#### カスタム要素ラベルメソッド

`ListDrawerSettings` に `ElementLabelMethod` プロパティを追加し、メソッド名を指定してリスト要素のラベルを動的に生成できます。

```csharp
[ListDrawerSettings(ElementLabelMethod = nameof(GetLabel))]
public List<MyItem> items;

private string GetLabel(int index) => $"Item {index}";
```

#### 交互の背景色

`ListDrawerSettings` に `AlternatingRowBackgrounds` プロパティを追加し、リスト要素に交互の背景色を設定できます。

```csharp
[ListDrawerSettings(AlternatingRowBackgrounds = true)]
public List<string> items;
```

### Dropdown

#### 条件付きドロップダウン表示

`DropdownAttribute` に `Condition` パラメータを追加し、条件を満たす場合のみドロップダウンを表示できるようにしています。条件が偽の場合は通常のフィールドとして描画されます。

```csharp
[Dropdown(nameof(GetValues), nameof(ShouldShowDropdown))]
public int value;
```

### SerializeReference

#### 描画範囲の調整

SerializeReference のフィールドの描画位置・高さを微調整し、レイアウトを改善しています。

#### 型選択時の名前空間非表示

SerializeReference の型選択ドロップダウンで名前空間を表示せず、型名のみを表示するようにしています。

#### 型名表示のスペース挿入

型名の表示に `ObjectNames.NicifyVariableName()` を使用し、キャメルケースの型名にスペースを自動挿入しています（例: `MyClassName` → `My Class Name`）。

#### DisplayName 属性の対応

`System.ComponentModel.DisplayNameAttribute` が付与されている型は、型名の代わりに指定した表示名を使用します。

#### Description 属性の対応

`System.ComponentModel.DescriptionAttribute` が付与されている型は、型名の後ろに説明文を括弧付きで表示します。

### パッケージ情報

- パッケージ名: `com.yuyu.yuyuinspector`
- ベースバージョン: Tri Inspector 1.15.1
- バージョニング: `{upstream}-yuyu.{N}` 形式（例: `1.15.1-yuyu.1`）
- Localization パッケージへの依存を削除済み
