# LearningWordsOnline

�u�I�����C���P��ΐ�Q�[���v�̃��C��Web�A�v���P�[�V����

## �O�����

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [SQL Server Express](https://www.microsoft.com/ja-jp/sql-server/sql-server-downloads)
- [EF Core CLI](https://learn.microsoft.com/ef/core/cli/dotnet)  
    ```bash
    dotnet tool install --global dotnet-ef
    ```

## �Z�b�g�A�b�v�菇

### 1. ���|�W�g���̃N���[��
```bash
git clone https://github.com/shamshikie/tango-taisen.git
cd tango-taisen/LearningWordsOnline
```
### 2. �f�[�^�x�[�X�̏���
[LearningWordsOnline/appsettings.json](appsettings.json)��`DefaultConnection`�����ɍ��킹�Đݒ肵�Ă��������B

�� SQL Server Express �̏ꍇ:
```json
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=LearningWordsOnline;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
```

### 3. �}�C�O���[�V�����̓K�p
```bash
dotnet ef database update --context ApplicationDbContext
dotnet ef database update --context LearningWordsOnlineDbContext
```
### 4. seed�t�@�C���̎��s
�����f�[�^��o�^���܂��B`localhost\SQLEXPRESS`�͂����g�̊��ɍ��킹�ĕύX���Ă��������B
```bash
sqlcmd -S localhost\SQLEXPRESS -d LearningWordsOnline -i Data/seed.sql
```

### 5. �A�v���P�[�V�����̋N��
```bash
dotnet run
```

### �⑫
- ApplicationDbContext �͔F�؁E���[�U�[�Ǘ��p
- LearningWordsOnlineDbContext �̓A�v���{�̂̃f�[�^�p
- �����f�[�^�ɂ͒��w�����x���̒P�ꂪ���S�ɓo�^����Ă��܂�

# appsettings.json�̐���

## DefaultConnection
- �f�[�^�x�[�X�ڑ�������

## AppSettings
�A�v���S�̂̓���Ɋւ���ݒ�
- `LastLoginUpdateIntervalMinutes` : LastLoginedAt��DB�ւ̏������ݔ��莞�ԁi���j
- `ActiveMinutesThreshold` : �I�����C�����莞�ԁi���j

## TrainingSettings
�g���[�j���O���[�h�̐ݒ�
- `MaxQuestionCount` : �ő�o�萔

## CommonMatchSettings
�g���[�j���O�A�����N�}�b�`�A���[���}�b�`�̋��ʐݒ�
- `Timer` : �N�C�Y�̐������ԁi�~���b�j
- `OptionCount` : �I�����̐�

## RoomMatchSettings
���[���}�b�`�i�����l�ŕ���������đΐ킷�郂�[�h�j�̐ݒ�
- `MaxQuestionCount` : �ő�o�萔
- `MaxPlayerCount` : �ő�l���i1�����ɎQ���ł���v���C���[���j
- `Points` : �N�C�Y���|�C���g�̔z���i��: `[20, 10, 5, 2]`�B1�ʁ`4�ʂ̓��_�j

## RankedMatchSettings
�����N�}�b�`�i���[�g�ϓ�����̑ΐ탂�[�h�j�̐ݒ�
- `QuestionCount` : �o�萔
- `MaxPlayerCount` : �ő�l��
- `Points` : �N�C�Y���|�C���g�z���i��: `[20, 10, 5, 2]`�j
- `RankPoints` : ���U���g���̃����N�|�C���g�z���i�l�����Ƃɔz��Ŏw��B��: `{ "2": [20, -20], "3": [20, 0, -20], "4": [20, 10, -10, -20] }`�j