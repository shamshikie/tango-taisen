# LearningWordsOnline

「オンライン単語対戦ゲーム」のメインWebアプリケーション

## 前提条件

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [SQL Server Express](https://www.microsoft.com/ja-jp/sql-server/sql-server-downloads)
- [EF Core CLI](https://learn.microsoft.com/ef/core/cli/dotnet)  
    ```bash
    dotnet tool install --global dotnet-ef
    ```

## セットアップ手順

### 1. リポジトリのクローン
```bash
git clone https://github.com/shamshikie/tango-taisen.git
cd tango-taisen/LearningWordsOnline
```
### 2. データベースの準備
[LearningWordsOnline/appsettings.json](appsettings.json)の`DefaultConnection`を環境に合わせて設定してください。

例 SQL Server Express の場合:
```json
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=LearningWordsOnline;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
```

### 3. マイグレーションの適用
```bash
dotnet ef database update --context ApplicationDbContext
dotnet ef database update --context LearningWordsOnlineDbContext
```
### 4. seedファイルの実行
初期データを登録します。`localhost\SQLEXPRESS`はご自身の環境に合わせて変更してください。
```bash
sqlcmd -S localhost\SQLEXPRESS -d LearningWordsOnline -i Data/seed.sql
```

### 5. アプリケーションの起動
```bash
dotnet run
```

### 補足
- ApplicationDbContext は認証・ユーザー管理用
- LearningWordsOnlineDbContext はアプリ本体のデータ用
- 初期データには中学生レベルの単語が中心に登録されています

# appsettings.jsonの説明

## DefaultConnection
- データベース接続文字列

## AppSettings
アプリ全体の動作に関する設定
- `LastLoginUpdateIntervalMinutes` : LastLoginedAtのDBへの書き込み判定時間（分）
- `ActiveMinutesThreshold` : オンライン判定時間（分）

## TrainingSettings
トレーニングモードの設定
- `MaxQuestionCount` : 最大出題数

## CommonMatchSettings
トレーニング、ランクマッチ、ルームマッチの共通設定
- `Timer` : クイズの制限時間（ミリ秒）
- `OptionCount` : 選択肢の数

## RoomMatchSettings
ルームマッチ（複数人で部屋を作って対戦するモード）の設定
- `MaxQuestionCount` : 最大出題数
- `MaxPlayerCount` : 最大人数（1部屋に参加できるプレイヤー数）
- `Points` : クイズ内ポイントの配分（例: `[20, 10, 5, 2]`。1位～4位の得点）

## RankedMatchSettings
ランクマッチ（レート変動ありの対戦モード）の設定
- `QuestionCount` : 出題数
- `MaxPlayerCount` : 最大人数
- `Points` : クイズ内ポイント配分（例: `[20, 10, 5, 2]`）
- `RankPoints` : リザルト時のランクポイント配分（人数ごとに配列で指定。例: `{ "2": [20, -20], "3": [20, 0, -20], "4": [20, 10, -10, -20] }`）