using Microsoft.AspNetCore.SignalR;
using LearningWordsOnline.Models;
using LearningWordsOnline.Data;
using System.Collections.Concurrent;
using System.Numerics;
using System;
using LearningWordsOnline.Services;
using LearningWordsOnline.GameLogic;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;


namespace LearningWordsOnline.Hubs
{
    public class RankedMatchHub : Hub
    {
        private readonly LearningWordsOnlineDbContext _appContext;
        //TODO: 将来的に言語、ランクポイントごとの待機部屋が必要
        private static ConcurrentQueue<Player> WaitingPlayers = new();
        private static readonly string waitingGroupId = "WaitingGroup";
        private readonly MatchSettings _settings;
        private readonly IReadOnlyList<int> _rankPoints;
        private readonly IQuizService _quizService;

        public RankedMatchHub(LearningWordsOnlineDbContext appContext, IConfiguration configuration, IQuizService quizService)
        {
            _appContext = appContext;
            _quizService = quizService;
            _settings = new MatchSettings()
            {
                MaxPlayerCount = configuration.GetValue<int>("RankedMatchSettings:MaxPlayerCount"),
                QuestionCount = configuration.GetValue<int>("RankedMatchSettings:QuestionCount"),
                OptionCount = configuration.GetValue<int>("CommonMatchSettings:OptionCount"),
                Timer = configuration.GetValue<int>("CommonMatchSettings:Timer"),
                Points = configuration.GetSection("RankedMatchSettings:Points").Get<IReadOnlyList<int>>() ?? throw new Exception("RankedMatchSettingsのPointsが設定されていません")
            };

            var rankPointsDict = configuration.GetSection("RankedMatchSettings:RankPoints").Get<Dictionary<int, List<int>>>()
                 ?? throw new Exception("RankedMatchSettingsのRankPointsが設定されていません");

            if (!rankPointsDict.TryGetValue(_settings.MaxPlayerCount, out var rankPoints))
            {
                throw new Exception($"RankedMatchSettingsのRankPointsに MaxPlayerCount {_settings.MaxPlayerCount} の設定がありません");
            }
            _rankPoints = rankPoints;
        }

        /// <summary>
        /// クライアントがRank/Playに飛んだときにSignalRで最初に呼び出されるメソッド
        /// これを呼び出したクライアントが現在プレイ中なのか、完全に新規なのかを判断する
        /// 1. 別タブで待機中なのにそのページを複製した場合→古いタブを無効化して現在のタブで待機させる
        /// 2.別タブで待機中なのにそのページを複製した、またはプレイ画面を閉じてしまいゲーム終了までにアクセスしてきた場合→古いタブを無効化し、新しいタブで部屋に復帰させる
        /// 3. 上記に当てはまらない完全新規クライアントの場合→待機部屋に遷移
        /// </summary>
        /// <param name="languageId"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public async Task CheckUserState(int languageId)
        {
            var language = await _appContext.Languages.FindAsync(languageId);
            if (language is null)
            {
                throw new NullReferenceException($"存在しない言語Idです。{languageId}");
            }
            var aspNetUserId = GetAspNetUserId();
            var connectionId = Context.ConnectionId;
            var room = RankRoomManager.GetRoomByPlayerId(aspNetUserId);
            var playerWaiting = WaitingPlayers.FirstOrDefault(p => p.Id == aspNetUserId);
            var playerPlaying = room?.Players.FirstOrDefault(p => p.Id == aspNetUserId);
            // 既にほかのタブで待機中の場合
            if (playerWaiting is not null)
            {
                var oldConnectionId = playerWaiting.ConnectionId;
                // 接続IDを新しいものに更新
                playerWaiting.ConnectionId = connectionId;
                // 古いタブを無効化
                _ = Clients.Client(oldConnectionId).SendAsync("DisableTab", "新しいタブが引き継ぎました。");
                // 新しいタブで待機
                await Groups.AddToGroupAsync(connectionId, waitingGroupId);
                await Clients.Caller.SendAsync("QueueJoined", WaitingPlayers.Count);
            }
            // 既に他のタブでプレイ中の場合
            else if (room is not null && playerPlaying is not null)
            {
                playerPlaying.IsInGame = true;
                var oldConnectionId = playerPlaying.ConnectionId;
                // 古いタブを無効化
                _ = Clients.Client(oldConnectionId).SendAsync("DisableTab", "新しいタブが引き継ぎました。");
                // コネクションIDを更新
                playerPlaying.ConnectionId = connectionId;
                // ゲームに復帰
                await Groups.AddToGroupAsync(connectionId, room.Id);
                await Clients.Caller.SendAsync("ReturnToGame", room.Id, PlayerDto.GetPlayersForClient(room.Players), room.Questions.Count, room.QuestionSentCount, room.Settings.Timer * 3);
            }
            else //完全に新規の場合
            {
                await JoinQueue(language);
            }
        }

        /// <summary>
        /// 新規プレイヤーは待機部屋に移動 & 既にその待機部屋にいる人たちに待機部屋にいる人数が増えたことを通知
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        private async Task JoinQueue(Language language)
        {
            string connectionId = Context.ConnectionId;
            string aspNetUserId = GetAspNetUserId();
            AppUser appUser = await _appContext.AppUsers.Include(a => a.Profile).ThenInclude(p => p.Icon)
                .FirstAsync(a => a.AspNetUserId == aspNetUserId);
            // 待機中のプレイヤーに追加
            WaitingPlayers.Enqueue(new Player() { Id = aspNetUserId, ConnectionId = connectionId, Name = appUser.Profile.DisplayName, IconUrl = appUser.Profile?.Icon?.Url });
            // SignalRのグループに追加
            await Groups.AddToGroupAsync(connectionId, waitingGroupId);
            // 呼びしたクライアントに待機部屋に移動させる
            await Clients.Caller.SendAsync("QueueJoined", WaitingPlayers.Count);
            // 待機部屋の人数が増えたことを他のクライアントに通知
            await Clients.OthersInGroup(waitingGroupId).SendAsync("UpdateWaitingList", WaitingPlayers.Count);
            await MatchPlayers(language);
        }

        private static readonly SemaphoreSlim _matchLock = new SemaphoreSlim(1, 1);
        /// <summary>
        /// 待機部屋にある一定人数が集まると対戦部屋に移動させる。
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        /// <exception cref="HubException">NullのPlayerが存在する</exception>
        private async Task MatchPlayers(Language language)
        {
            // このメソッドが同時並行して呼ばれることを禁止
            await _matchLock.WaitAsync();
            try
            {
                //設定した人数よりも待機部屋にいる人数が多くなったとき
                while (WaitingPlayers.Count >= _settings.MaxPlayerCount)
                {
                    // プレイヤーを設定された人数分、取り出す
                    var players = new List<Player>();
                    for (int i = 0; i < _settings.MaxPlayerCount; i++)
                    {
                        if (WaitingPlayers.TryDequeue(out Player? player))
                        {
                            if (player is null)
                            {
                                throw new HubException($"NullのPlayer{player}が存在しています。");
                            }
                            player.IsInGame = true;
                            players.Add(player);
                            //待機部屋から削除
                            await Groups.RemoveFromGroupAsync(player.ConnectionId, waitingGroupId);
                        }
                    }

                    if (players.Count == _settings.MaxPlayerCount)
                    {
                        var questions = await _quizService
                                .GenerateRandomQuestions(_settings.QuestionCount, language);
                        //サンプル質問
                        //    var questions = new List<Question>
                        //{
                        //         new Question
                        //         {
                        //             Text = "temperature",
                        //             Options = ["気温", "嵐", "天ぷら", "一時的"],
                        //             CorrectAnswer = "気温"
                        //         },
                        //         new Question
                        //         {
                        //             Text = "気温",
                        //             Options = ["temperature", "tempest", "tempura", "temporary"],
                        //             CorrectAnswer = "temperature"
                        //         },
                        //};
                        var room = RankRoomManager.CreateRoom(_settings, questions, language);

                        foreach (var player in players)
                        {
                            // 部屋にプレイヤーを登録
                            room.Players.Add(player);
                            await Groups.AddToGroupAsync(player.ConnectionId, room.Id);
                        }

                        //クライアントにプレイヤーにマッチしたことを通知
                        await Clients.Group(room.Id).SendAsync("Matched", room.Id, PlayerDto.GetPlayersForClient(room.Players), room.Questions.Count);
                    }
                }
            }
            finally
            {
                _matchLock.Release();
            }
        }

        /// <summary>
        /// クライアントから回答が提出されたとき呼び出される
        /// 呼び出したクライアントの回答と回答残り時間を保存
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="selectedAnswer">クライアントが選んだ答え</param>
        /// <param name="remainingTime">回答残り時間</param>
        /// <exception cref="HubException">部屋が存在しない</exception>
        public async Task SubmitAnswer(string roomId, string selectedAnswer, int remainingTime)
        {
            var room = RankRoomManager.GetRoom(roomId);
            string aspNetUserId = GetAspNetUserId();
            if (room is null)
            {
                throw new HubException($"部屋ID({roomId})の部屋が存在しません。");
            }
            var answers = room.Answers;
            //二重送信対策
            if (answers.Any(answer => answer.AspNetUserId == aspNetUserId))
            {
                Console.WriteLine($"Duplicate answer from {aspNetUserId} ignored.");
                return;
            }

            answers.Enqueue(new Answer
            {
                AspNetUserId = aspNetUserId,
                SelectedAnswer = selectedAnswer,
                RemainingTime = remainingTime
            });
            Console.WriteLine($"Answer from {aspNetUserId} added successfully.");
            await Clients.Group(roomId).SendAsync("SomeoneAnswered", aspNetUserId);
        }

        private static readonly SemaphoreSlim _askQuestionLock = new SemaphoreSlim(1, 1);
        /// <summary>
        /// クライアントが問題要求すると呼び出される。
        /// 部屋内のクライアントは一斉に問題要求をするため、最速で問題要求をしてきたクライアントを基準に部屋内のクライアントに問題を一斉送信する。
        /// 問題をすべて送信しきったあとにこれが呼び出された場合、結果を送信する
        /// </summary>
        /// <param name="roomId">ルームID</param>
        /// <param name="questionReceivedCount">クライアントが受け取ったクイズ数</param>
        /// <param name="timeOut">クライアントがある一定時間待っても問題が来なかったときにtrueとなる</param>
        /// <returns></returns>
        /// <exception cref="HubException">部屋が存在しない、またはプレイヤーがその部屋にいない場合の例外</exception>
        public async Task AskQuestion(string roomId, int questionReceivedCount, bool timeOut = false)
        {
            var room = RankRoomManager.GetRoom(roomId);
            //部屋が存在しない OR プレイヤが部屋に存在しない場合 
            if (room is null || !room.Players.Any(p => p.Id == GetAspNetUserId()))
            {
                throw new HubException("部屋が存在しないか、部屋にプレイヤーが存在しません。");
            }

            //クライアントの同時リクエストは一つずつ処理する
            await _askQuestionLock.WaitAsync();

            try
            {
                // ある問題に対して最速でこれを呼び出したクライアントの場合、サーバーが既に出した問題数 = クライアントが解いた問題数になる
                // ある問題に対してニ番目以降のリクエストである場合は、何もせずreturn
                // 通信状態が遅いクライアントがいない限り部屋内のクライアントたちが、その問題を受け取る前に同時にリクエストする想定
                // NOTE: 通信速度が遅く、ある問題を受け取ったあとにquestionReceivedCountが更新された状態でのリクエストはここを通ってしまう
                //       対策: 次のif文で前回の答え送っていないにも関わらず次の問題を要求してきたときはreturnする
                if (room.QuestionSentCount != questionReceivedCount)
                {
                    return;
                }

                // 既に一問以上送っている且つ前回の回答が送られていない場合
                if (room.QuestionSentCount >= 1 && !room.Questions[room.QuestionSentCount - 1].IsCorrectAnswerSent)
                {
                    // クライアントが同時リロードした場合、誰も問題要求がせず全員が無限rejoin画面状態のままになる対策
                    // ある一定の時間経過してもクライアントが何も受け取らなかった場合、TimeOutがTrueの状態で呼び出される
                    if (timeOut)
                    {
                        // 前回の問題の答え合わせ＆ポイント処理
                        var previousQuestion = room.Questions[room.QuestionSentCount - 1];
                        await HandleAnswerEvaluation(room, previousQuestion);

                        // ポイント通知をしてクライアント側から再度問題要求させる
                        await NotifyPoints(room);
                        return;
                    }
                    // タイムアウトしていない場合は何もせずreturn、つまり前回の答え送っていないにも関わらず次の問題を要求してきたときが当てはまる
                    return;
                }

                // 正常処理、または1問も送っていないときに無限rejoinを起こしている場合はここにたどり着く
                room.QuestionSentCount++;
            }
            finally
            {
                _askQuestionLock.Release();
            }
            //出題しきっていた場合
            if (room.Questions.Count < room.QuestionSentCount)
            {
                // 順位結果、ランクポイントの変動を通知する
                await FinalizeMatch(room);
                return;
            }

            // ここでSignalRを用いて部屋内のクライアントに問題を一斉送信する
            var question = room.Questions[room.QuestionSentCount - 1];
            await Clients.Group(roomId).SendAsync("ReceiveQuestion", room.QuestionSentCount, question.Text, question.Options, room.Settings.Timer);
        }

        private async Task FinalizeMatch(RankRoom room)
        {
            //順位決定
            PointManager.DeterminePositions(room.Players);
            // DBに対戦情報とランクポイント変動を記録
            await SaveBattle(room.Players, room.Language);
            // クライアントに結果発表
            await SendResults(room);
            //部屋削除
            RankRoomManager.RemoveRoom(room.Id);
        }

        /// <summary>
        /// 対戦結果とランクポイントの変動を記録
        /// </summary>
        /// <param name="players"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        private async Task SaveBattle(IEnumerable<Player> players, Language language)
        {
            var battle = new Battle
            {
                LanguageId = language.Id,
                BattledAt = DateTime.UtcNow,
            };

            // 対戦結果の記録
            _appContext.Battles.Add(battle);

            foreach (var player in players)
            {
                //現在のランクポイントを取得
                var appUser = await _appContext.AppUsers
                    .Include(a => a.Profile).FirstOrDefaultAsync(a => a.AspNetUserId == player.Id);
                if (appUser is null)
                    throw new NullReferenceException(nameof(appUser));

                //ランクポイントを更新
                appUser.Profile.RankPoint += _rankPoints[player.Position - 1];
                appUser.Profile.UpdatedAt = DateTime.UtcNow;
                _appContext.Update(appUser.Profile);
                player.RankPoints = appUser.Profile.RankPoint;

                //BattleUserの追加
                var battleAppUser = new BattleAppUser { Battle = battle, AppUserId = appUser.Id, Position = player.Position };
                _appContext.BattleAppUsers.Add(battleAppUser);
            }
            await _appContext.SaveChangesAsync();
        }

        /// <summary>
        /// 順位結果をクライアントに一斉送信 & 各クライアントに変動後のランクポイントを送信
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        private async Task SendResults(RankRoom room)
        {
            // 順位をクライアントに送信
            var playerPositions = room.Players.Select(p => new { p.Id, p.Points, p.Position });
            await Clients.Group(room.Id).SendAsync("ReceiveResults", playerPositions);

            // 各クライアントの変動後のランクポイントを送信
            foreach (var player in room.Players)
            {
                await Clients.Client(player.ConnectionId).SendAsync("ReceiveRankPoints", player.RankPoints, _rankPoints[player.Position - 1]);
            }
        }

        private static readonly SemaphoreSlim _askCorrectAnswerLock = new SemaphoreSlim(1, 1);
        /// <summary>
        /// クライアントが答えを要求したときに呼び出される
        /// 部屋内のクライアントは一斉に答えを要求をするため、最速で要求をしてきたクライアントを基準に部屋内のクライアントに答えを一斉送信する。
        /// </summary>
        /// <param name="roomId">ルームID</param>
        /// <param name="questionReceivedCount">クライアントが受け取った問題数</param>
        /// <returns></returns>
        /// <exception cref="HubException">部屋が存在しないか、部屋にプレイヤーが存在しない</exception>
        public async Task AskCorrectAnswer(string roomId, int questionReceivedCount)
        {
            var room = RankRoomManager.GetRoom(roomId);
            Question question;
            //部屋が存在しない OR プレイヤが部屋に存在しない場合 
            if (room is null || !room.Players.Any(p => p.Id == GetAspNetUserId()))
            {
                throw new HubException("部屋が存在しないか、部屋にプレイヤーが存在しません。");
            }

            //クライアントの同時リクエストは一つずつ処理する
            await _askCorrectAnswerLock.WaitAsync();

            try
            {
                // すでに答えが送られていた場合はreturn
                // 適切な答えの要求である場合、サーバーが既に出した問題数 = クライアントが解いた問題数になる、ならない場合return
                if (room.Questions[room.QuestionSentCount - 1].IsCorrectAnswerSent || room.QuestionSentCount != questionReceivedCount)
                {
                    return;
                }

                question = room.Questions[room.QuestionSentCount - 1];
                // 答え合わせ & 獲得ポイントの計算
                await HandleAnswerEvaluation(room, question);
            }
            finally
            {
                _askCorrectAnswerLock.Release();
            }

            // ここで部屋内のクライアントに答えを一斉送信
            await Clients.Group(roomId).SendAsync("RevealAnswer", room.QuestionSentCount, question.CorrectAnswer);

            // クライアントに参加者の総ポイントを通知
            await NotifyPoints(room);
        }

        /// <summary>
        /// 答え合わせ＆獲得ポイントを計算
        /// 各クライアントの辞書を更新
        /// </summary>
        /// <param name="room"></param>
        /// <param name="question"></param>
        /// <returns></returns>
        private async Task HandleAnswerEvaluation(RankRoom room, Question question)
        {
            //送信した問題を取得
            question.IsCorrectAnswerSent = true;
            // 答え合わせ
            room.Answers.ToList().ForEach(a => a.CheckAnswer(question.CorrectAnswer));
            // AppUserDefinition更新
            await UpdateAppUserDefinition(question, room.Answers);
            // 回答に基づき、クライアントのポイントを計算
            PointManager.DistributePoints(room.Players, room.Answers, room.Settings.Points);
            //コンソールに回答表示
            room.Answers.ToList().ForEach(Console.WriteLine);
            room.Players.ToList().ForEach(Console.WriteLine);
            // 回答リストをリセット
            room.Answers.Clear();
        }

        /// <summary>
        /// 各クライアントのMy単語帳 (AppUserDefinition)を更新
        /// </summary>
        /// <param name="question"></param>
        /// <param name="answers"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task UpdateAppUserDefinition(Question question, IEnumerable<Answer> answers)
        {
            foreach (Answer answer in answers)
            {
                //DefinitionIdが存在しないするか確認
                if (!_appContext.Definitions.Any(d => d.Id == question.DefinitionId))
                {
                    throw new Exception($"存在しないDefinitionId{question.DefinitionId}です。");
                }

                var appUser = await _appContext.AppUsers.Include(a => a.AppUserDefinitions)
                    .FirstOrDefaultAsync(a => a.AspNetUserId == answer.AspNetUserId);

                if (appUser is null)
                {
                    Console.WriteLine($"appUserが存在しません。");
                    continue;
                }

                var userDefinition = appUser.AppUserDefinitions
                    .FirstOrDefault(ud => ud.DefinitionId == question.DefinitionId);
                if (userDefinition is null) //新規単語
                {
                    var newUserDefinition = new AppUserDefinition
                    {
                        AppUserId = appUser.Id,
                        DefinitionId = question.DefinitionId,
                        Count = 1,
                        WrongCount = answer.AnswerStatus == Evaluation.Correct ? 0 : 1,
                        AnsweredAt = DateTime.UtcNow
                    };
                    appUser.AppUserDefinitions.Add(newUserDefinition);
                }
                else
                {
                    // 既存の場合は更新
                    userDefinition.Count++;
                    userDefinition.AnsweredAt = DateTime.UtcNow;
                    if (answer.AnswerStatus != Evaluation.Correct)
                        userDefinition.WrongCount++;
                }
            }
            await _appContext.SaveChangesAsync();
        }


        /// <summary>
        /// 部屋内のクライアントに獲得後のポイントを通知
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        private async Task NotifyPoints(RankRoom room)
        {
            var playerPoints = room.Players.Select(p => new { p.Id, p.Points });
            await Clients.Group(room.Id).SendAsync("ReceivePoints", playerPoints, room.Settings.Timer);
        }

        /// <summary>
        /// クライアントがSignalRの切断処理を行うと自動的に呼び出される
        /// 例. ページが閉じられた場合やconnection.stop()がクライアントで呼ばれるなど
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            string aspNetUserId = GetAspNetUserId();

            var room = RankRoomManager.GetRoomByPlayerId(aspNetUserId);
            var connectionId = Context.ConnectionId;

            //対戦部屋が存在するつまり、対戦が進行中である場合は、プレイヤー削除の処理をせずに切断
            if (room is not null)
            {
                const int removeRoomDelay = 1000;
                var playerDisconnected = room.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
                if (playerDisconnected is not null)
                {
                    // 部屋には存在するが、切断中であることを記録
                    playerDisconnected.IsInGame = false;
                }

                // ゲームから誰もいなくなった場合、部屋を削除
                // 遅延を挟む、最後の一人が更新した瞬間部屋が削除されるのを防ぐため、このDelay中にCheckUserStatusでIsInGameがTrueになる
                // NOTE: 通信状態が悪い場合はremoveRoomDelay[ms]以上かかると考えられるので、部屋が削除される
                await Task.Delay(removeRoomDelay);
                if (room.Players.All(p => !p.IsInGame))
                    RankRoomManager.RemoveRoom(room.Id);

                await base.OnDisconnectedAsync(exception);
                return;
            }

            var playerWaiting = WaitingPlayers.FirstOrDefault(p => p.Id == aspNetUserId);

            // 待機部屋でタブを閉じた場合 且つ 接続IDが一致している場合
            // NOTE:接続IDが一致しないときはどんなときか: 
            // 待機中に別のタブで待機ページに飛んだとき、新しいタブの方でplayerWaitingの接続IDを新しいものに更新され、古いタブの接続切断でここにたどり着く
            // このとき接続IDが更新されているため、一致しない
            if (playerWaiting is not null && playerWaiting.ConnectionId == connectionId)
            {
                // 切断したクライアントを削除
                WaitingPlayers = new ConcurrentQueue<Player>(WaitingPlayers.Where(player => player.Id != aspNetUserId));
                // 待機部屋の更新を全クライアントに通知
                await Clients.All.SendAsync("UpdateWaitingList", WaitingPlayers.Count);
            }

            // クイズ終了後の切断はここにたどり着く
            // 待機中に別のタブで待機ページに飛んだ場合結果、古いタブの接続切断でここにたどり着く
            //   理由: 古いタブの接続IDと新しいタブの接続IDが異なる、新しいタブの方でplayerWaitingの接続IDを新しいものに更新しているため
            // 古いタブの接続切断でこれが呼ばれた場合、待機リストからは削除しない
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// 呼び出してきたクライアントのAspNetUserIdを取得
        /// </summary>
        /// <returns></returns>
        /// <exception cref="HubException"></exception>
        private string GetAspNetUserId()
        {
            return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new HubException("ユーザー名の取得に失敗しました。再度ログインしてください。");
        }
    }

}
