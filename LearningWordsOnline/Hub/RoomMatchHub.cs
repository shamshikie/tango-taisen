using Microsoft.AspNetCore.Identity;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using LearningWordsOnline.Models;
using LearningWordsOnline.Data;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using static System.Net.Mime.MediaTypeNames;
using System.Collections.Generic;
using LearningWordsOnline.GameLogic;
using LearningWordsOnline.Services;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Numerics;


namespace LearningWordsOnline.Hubs
{
    public class RoomMatchHub : Hub
    {
        private readonly LearningWordsOnlineDbContext _appContext;
        private readonly IQuizService _quizService;

        public RoomMatchHub(LearningWordsOnlineDbContext appContext, IConfiguration configuration, IQuizService quizService)
        {
            _appContext = appContext;
            _quizService = quizService;
        }

        /// <summary>
        /// クライアントがRoom/Joinに飛んだときにSignalRで最初に呼び出されるメソッド
        /// これを呼び出したクライアントが現在プレイ中なのか、完全に新規なのかを判断する
        /// 1. 別タブで待機中なのにそのページを複製した場合→古いタブを無効化して現在のタブで待機させる
        /// 2. 別タブでプレイ中なのにそのページを複製した場合、またはプレイ画面を閉じてしまいゲーム終了までに再度アクセスしてきた場合→古いタブを無効化し、新しいタブでプレイ中の部屋に復帰させる
        /// 3. 上記に当てはまらず、完全新規クライアントの場合
        ///     a. ゲーム進行中または満員の場合、入室不可
        ///     b. 待機中の場合、入室
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        public async Task CheckUserState(string roomId)
        {
            var aspNetUserId = GetAspNetUserId();
            var connectionId = Context.ConnectionId;
            var room = RoomManager.GetRoom(roomId);

            // 部屋が存在しない
            if (room is null)
            {
                await Clients.Caller.SendAsync("RoomNotFound");
                return;
            }

            //部屋にいる人がまた入ってきた場合 (別タブとか)
            var player = room.Players.FirstOrDefault(p => p.Id == aspNetUserId);
            if (player is not null)
            {
                var oldConnectionId = player.ConnectionId;
                // 接続IDを新しいものに更新
                player.ConnectionId = connectionId;
                // SignalRグループに追加
                await Groups.AddToGroupAsync(connectionId, room.Id);

                //古いページへの送信はawait不要
                // 古いページで何かしらのエラーが出ていた場合、ここの処理がストップするため
                _ = Clients.Client(oldConnectionId).SendAsync("DisableTab", "新しいタブが引き継ぎました。");
                if (room.IsInProgress) //進行中
                {
                    const int rejoinDelay = 500;
                    // NOTE: 遅延理由(Delayは必須)
                    // DisableTabによって古いページからOnDisconnectedAsyncが呼ばれる。その中のif (room.IsInProgress)の処理中に、IsInGame = false;とする部分がある (ただ単にページを閉じた時も呼ばれる)。IsInGameが確実にfalseになってから次のplayer.InInGameをtrueとしたいため遅延をいれる。そうしないとこちらが先にIsInGame = true からの OnDisconnectedAsyncの方で IsInGame = falseとなると、ゲームに参加しているのにIsInGame = falseという状況ができてしまうので、結果発表の時に部屋から削除されてしまう
                    //
                    await Task.Delay(rejoinDelay);
                    player.IsInGame = true;
                    //room.Settings.Timer * 3はタイムアウト時間の設定、この時間待機してもクライアントが何も受け取らなかった場合AskQuestionがクライアントから呼ばれる
                    await Clients.Caller.SendAsync("ReturnToGame", room.HostId, PlayerDto.GetPlayersForClient(room.Players), room.Questions.Count, room.QuestionSentCount, room.Settings.Timer * 3);
                }
                else //待機中
                {
                    await Clients.Caller.SendAsync("PlayerJoined", room.HostId, room.Players);
                    if (room.HostId == aspNetUserId)
                        await Clients.Caller.SendAsync("ShowStartButton");
                }

                return;
            }

            // 新規且つ部屋が満員
            if (room.Players.Count >= room.Settings.MaxPlayerCount)
            {
                await Clients.Caller.SendAsync("RoomNotFound");
                return;
            }

            //進行中に新規が入室
            if (room.IsInProgress)
            {
                await Clients.Caller.SendAsync("RoomIsInProgress");
                return;
            }

            // 待機中の完全新規
            // 部屋にまだ誰もいない場合、入ってきた人をホストとする
            if (room.Players.Count == 0)
                room.HostId = aspNetUserId;

            AppUser appUser = await _appContext.AppUsers.Include(a => a.Profile).ThenInclude(p => p.Icon)
                .FirstAsync(a => a.AspNetUserId == aspNetUserId);
            room.Players.Add(new Player() { Id = aspNetUserId, ConnectionId = Context.ConnectionId, Name = appUser.Profile.DisplayName, IsInGame = false, IconUrl = appUser.Profile?.Icon?.Url });
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            await Clients.Caller.SendAsync("PlayerJoined", room.HostId, PlayerDto.GetPlayersForClient(room.Players));
            await Clients.OthersInGroup(roomId).SendAsync("UpdateWaitingList", room.HostId, PlayerDto.GetPlayersForClient(room.Players));
            // ホストにのみStartButtonを表示
            if (room.HostId == aspNetUserId)
                await Clients.Caller.SendAsync("ShowStartButton");
        }

        /// <summary>
        /// クライアントがクイズ開始ボタンを押すと呼ばれる。クイズはここで生成され、クイズ開始の準備ができたことを部屋内のクライアントに一斉送信
        /// </summary>
        /// <param name="roomId">ルームID</param>
        /// <returns></returns>
        public async Task StartQuiz(string roomId)
        {
            var room = RoomManager.GetRoom(roomId);
            if (room is null)
                return;

            //初期化→ルームマッチは同部屋で連続対戦ができる仕様のため
            room.IsInProgress = true;
            room.QuestionSentCount = 0;
            room.Questions.Clear();
            room.Players.ToList().ForEach(p => p.IsInGame = true);
            room.Players.ToList().ForEach(p => p.Points = 0);
            // ゲーム開始通知
            var questions = await _quizService
                .GenerateRandomQuestions(room.Settings.QuestionCount, room.Language);
            //サンプルクイズ
            //var questions = new List<Question> 
            //        {
            //                 new Question
            //                 {
            //                     Text = "temperature",
            //                     Options = ["気温", "嵐", "天ぷら", "一時的"],
            //                     CorrectAnswer = "気温"
            //                 },
            //                 new Question
            //                 {
            //                     Text = "気温",
            //                     Options = ["temperature", "tempest", "tempura", "temporary"],
            //                     CorrectAnswer = "temperature"
            //                 },
            //        };
            // 問題をセット
            questions.ToList().ForEach(question => room.Questions.Add(question));
            // プレイヤー情報、問題数数を部屋内のクライアントに一斉送信 
            await Clients.Group(room.Id).SendAsync("StartingQuiz", PlayerDto.GetPlayersForClient(room.Players), room.Questions.Count);
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
            var room = RoomManager.GetRoom(roomId);
            //部屋が存在しない OR プレイヤが部屋に存在しない場合 
            if (room is null || room.Players.All(p => p.Id != GetAspNetUserId()))
            {
                throw new HubException("部屋が存在しないか、部屋にプレイヤーが存在しません。");
                //return;
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
                // 結果を送信
                await SendResults(room);
                //プレイ中に切断し、戻ってこなかったプレイヤーを部屋から削除
                await ResetPlayers(room);
                return;
            }

            // ここでSignalRを用いて部屋内のクライアントに問題を一斉送信する
            var question = room.Questions[room.QuestionSentCount - 1];
            await Clients.Group(roomId).SendAsync("ReceiveQuestion", room.QuestionSentCount, question.Text, question.Options, room.Settings.Timer);
        }

        /// <summary>
        /// 順位結果をクライアントに送信
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        private async Task SendResults(Room room)
        {
            PointManager.DeterminePositions(room.Players);
            var playerPositions = room.Players.Select(p => new { p.Id, p.Points, p.Position });
            await Clients.Group(room.Id).SendAsync("ReceiveResults", playerPositions);
            room.IsInProgress = false;
        }

        /// <summary>
        /// プレイ中に切断し、戻ってこなかったプレイヤーを部屋から削除し、クライアントに更新されたプレイヤー情報を送信
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        private async Task ResetPlayers(Room room)
        {
            // 削除対象のプレイヤーを一時的に保存するリスト
            var playersToRemove = room.Players
                .Where(player => !player.IsInGame)
                .ToList();

            // プレイヤーを削除
            foreach (var player in playersToRemove)
            {
                room.Players.Remove(player);
                if (player.Id == room.HostId && room.Players.Any())
                {
                    // 新しいホストを先頭のプレイヤーに設定
                    room.HostId = room.Players.First().Id;
                }
            }

            // 部屋に誰もいなくなった場合
            if (!room.Players.Any())
            {
                RemoveRoom(room.Id);
                return;
            }

            // 1ゲーム終了後、部屋内の更新されたプレイヤー情報を部屋内のクライアントに一斉送信
            await Clients.Group(room.Id).SendAsync("UpdateWaitingList", room.HostId, PlayerDto.GetPlayersForClient(room.Players));
            var hostPlayer = room.Players.First(p => p.Id == room.HostId);
            // ホストにのみStartButtonを表示
            await Clients.Client(hostPlayer.ConnectionId).SendAsync("ShowStartButton");
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
            var room = RoomManager.GetRoom(roomId);
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
        private async Task HandleAnswerEvaluation(Room room, Question question)
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
        /// クライアントから回答が提出されたとき呼び出される
        /// 呼び出したクライアントの回答と回答残り時間を保存
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="selectedAnswer">クライアントが選んだ答え</param>
        /// <param name="remainingTime">回答残り時間</param>
        /// <exception cref="HubException">部屋が存在しません。</exception>
        public async Task SubmitAnswer(string roomId, string selectedAnswer, int remainingTime)
        {
            var room = RoomManager.GetRoom(roomId);
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
            // あるクライアントが回答したことを部屋内の他のクライアントにも通知
            await Clients.Group(roomId).SendAsync("SomeoneAnswered", aspNetUserId);
        }

        /// <summary>
        /// 部屋内のクライアントに獲得後のポイントを通知
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        private async Task NotifyPoints(Room room)
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
            var connectionId = Context.ConnectionId;
            const int removeRoomDelay = 1000;
            var room = RoomManager.GetRoomByPlayerId(aspNetUserId);
            // 部屋が存在しない→切断
            if (room is null)
            {
                await base.OnDisconnectedAsync(exception);
                return;
            }

            // 部屋のクイズが進行中に切断が呼び出された場合
            // NOTE: クイズ進行中であれば、プレイヤー削除の処理をいれない
            // ただし、クイズ進行中であっても部屋のクライアント全員が切断した場合は部屋を削除する
            if (room.IsInProgress)
            {
                var playerDisconnected = room.Players.FirstOrDefault(p => p.Id == aspNetUserId);
                if (playerDisconnected is not null)
                {
                    // 部屋に存在するがゲーム中ではない
                    playerDisconnected.IsInGame = false;
                    // 遅延を挟む （1人プレイ中のプレイヤーがリロードしたとき、部屋が即削除されるのを防ぐため)
                    // NOTE: 遅延中の間にリロードで再度部屋に入れた場合は、CheckUserStateでIsInGameがTrueとなるため次の部屋削除は呼ばれない
                    await Task.Delay(removeRoomDelay);

                    // 部屋に誰もいなくなった場合、部屋を削除
                    if (room.Players.All(p => !p.IsInGame))
                        RemoveRoom(room.Id);
                }
                await base.OnDisconnectedAsync(exception);
                return;
            }

            // 待機中に切断が呼び出された場合
            // 単に部屋から抜ける
            var playerWaiting = room.Players.FirstOrDefault(p => p.Id == aspNetUserId);
            if (playerWaiting is not null)
            {
                // SignalRグループから削除
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, room.Id);

                // 別タブ(古いタブ)からのconnection.stopだった場合
                if (playerWaiting.ConnectionId != connectionId)
                {
                    await base.OnDisconnectedAsync(exception);
                    return;
                }

                // 部屋からユーザーを削除
                room.Players.Remove(playerWaiting);

                // 抜けたのがホストだった場合、他の人にホストを譲る
                if (room.HostId == aspNetUserId && room.Players.Any())
                {
                    room.HostId = room.Players.First().Id; // 新しいホストを先頭のプレイヤーに設定
                    //クイズ開始ボタンを表示
                    await Clients.Client(room.Players.First().ConnectionId).SendAsync("ShowStartButton");
                }

                // 他のプレイヤーに通知
                await Clients.Group(room.Id).SendAsync("UpdateWaitingList", room.HostId, room.Players);

                //誰もいなくなった場合、部屋を削除
                // 遅延を挟む （全員の一斉リロードまたは、1人待機中の部屋でリロードが発生したとき、部屋が即削除されるのを防ぐため)
                // この遅延の間に新しい複製タブでCheckUserStateが呼ばれて再入室する
                await Task.Delay(removeRoomDelay);
                if (!room.Players.Any())
                    RemoveRoom(room.Id);
            }
            // ベースの処理を呼び出す
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// 部屋の削除処理
        /// </summary>
        /// <param name="roomId"></param>
        private void RemoveRoom(string roomId)
        {
            RoomManager.RemoveRoom(roomId);

            // 部屋招待があった場合の処理
            var roomInvitations = _appContext.RoomInvitations.Where(ri => ri.RoomId == roomId);
            if (!roomInvitations.Any())
            {
                return;
            }

            roomInvitations.ToList().ForEach(ri => ri.DismissedAt = DateTime.UtcNow);

            _appContext.RoomInvitations.UpdateRange(roomInvitations);
            _appContext.SaveChanges();
        }

        /// <summary>
        /// 呼び出してきたクライアントのAspNetUserIdを取得
        /// </summary>
        /// <returns></returns>
        /// <exception cref="HubException"></exception>
        private string GetAspNetUserId()
        {
            return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new HubException("ユーザー名の取得に失敗しました。再度ログインしてください。"); ;
        }
    }
}

