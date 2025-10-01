# 仕様書 - WordImporter

## このアプリの目的
- LearningWordsOnlineで扱う単語をデータベースに登録
- CSV形式の単語一覧を一括でデータベースに登録

## 使い方
1. appsettings.jsonに接続文字列/ファイル名/言語を設定
  - languageCode → ISO639-1 (アルファベット2文字)
    - 例. 英語 → en
  - fileName → 単語一覧CSVファイルのファイル名
  - connectionString → DBの接続文字列
1. [単語],[意味],[品詞]が並んだCSVファイルを用意
    - 例. achievement,達成,名詞(改行)analyze,分析する,動詞
1. CSVファイルをWordフォルダに配置
1. 本アプリを実行 → DBに登録完了