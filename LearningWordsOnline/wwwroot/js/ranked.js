"use strict";

import * as Quiz from './quiz.js';

const userId = document.getElementById('user-data').dataset.userId;
const languageId = parseInt(document.getElementById('language-data').dataset.languageId, 10);
let roomId;
let players;
let questionReceivedCount = 0;
let questionCount;
let timeoutId;

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/rankedMatchHub")
    .build();

startConnection();

// 非同期関数を定義して、接続処理を実行
async function startConnection() {
    try {
        await connection.start();
        await connection.invoke("CheckUserState", languageId);
    } catch (err) {
        // 複製タブによるものだった場合、遷移しない
        if (err.message.includes("Invocation canceled due to the underlying connection being closed")) {
            console.log("タブが複製されました");
            return;
        }

        // 予期しないエラーならアラートを表示してリダイレクト
        alert("予期せぬエラーが発生しました。(a)");
        window.location.href = "Index";
    }
}

connection.onclose(error => {
    if (error) {
        console.warn("SignalR 接続が切断されました:", error.message);
    } else {
        console.log("SignalR 接続が正常に閉じられました。");
    }
});

function animateDots() {
    const dots = document.querySelector('.loading-dots');
    let dotCount = 0;

    // 1秒ごとにドットを増減させる
    setInterval(() => {
        dotCount = (dotCount + 1) % 4; // 0, 1, 2, 3 をループ
        dots.textContent = '.'.repeat(dotCount); // ドットの数を更新
    }, 500); // 500msごとに更新
}

// ページがロードされたらアニメーションを開始
document.addEventListener('DOMContentLoaded', animateDots);

connection.on("QueueJoined", initializeWaitingUI);

connection.on("UpdateWaitingList", playerCount => {
    document.getElementById("playerCount").innerText = playerCount;
});

connection.on("Matched", (roomId_arg, players_arg, questionCount_arg) => {
    roomId = roomId_arg;
    players = players_arg;
    questionCount = questionCount_arg;
    document.querySelector(".spinner-border").hidden = false;
    document.getElementById("status").innerText = "MATCH FOUND!\n The match will start soon...";
    document.getElementById("quitButton").style.display = "none";
    console.log(`Matched! Room ID: ${roomId}`);
    setTimeout(initializeGameUI, 2000);
    setTimeout(askQuestion, 3000);
});

connection.on("DisableTab", message => {
    if (timeoutId)
        clearTimeout(timeoutId);
    connection.stop(); // SignalR接続を切断
    //alert(message);
    document.body.innerHTML = `<div style="text-align:center; margin-top:20px;">
                        <h1>${message}</h1>
                        <p>このタブは現在非アクティブです。</p>
                    </div>`;
});

function askQuestion() {
    console.log("asking next question Count: " + questionReceivedCount);
    connection.invoke("AskQuestion", roomId, questionReceivedCount, false)
        .catch((err) => {
            console.error('Error starting quiz: ', err);
            //alert("予期せぬエラーが発生しました。(b)");
            //window.location.href = "Index";
        });
    // connection.invoke("AskQuestion", roomId, questionReceivedCount);
}

function askCorrectAnswer() {
    console.log("asking the correct answer");
    connection.invoke("AskCorrectAnswer", roomId, questionReceivedCount)
        .catch((err) => {
            console.error('Error starting quiz: ', err);
            //alert("予期せぬエラーが発生しました。(c)");
            //window.location.href = "Index";
        });
}

// connection.on("StartingQuiz", (players_arg) => {
//     players = players_arg;
//     console.log("Quiz is starting...");
//     initializeGameUI();
// });

connection.on("ReturnToGame", function (roomId_arg, players_arg, questionCount_arg, questionSentCount, timer) {
    roomId = roomId_arg;
    players = players_arg;
    questionCount = questionCount_arg;
    questionReceivedCount = questionSentCount;
    console.log(`returning to the game..., questionReceivedCount = ${questionReceivedCount}`);
    // UIをゲーム画面に切り替え
    initializeGameUI();
    document.querySelector("#question").innerHTML = `
                <span>Rejoining...</span>
                <span class="spinner-border mt-1" role="status" style="display:inline-block; font-size:1rem;"></span>`;
    //無限Rejoin対策
    timeoutId = setTimeout(() => {
        console.log("Time is out. Asking question...");
        // 引数がquestionReceivedCountではなくquestionSentCountなのは、questionReceivedCountはsetTimeoutの途中で更新されるから
        connection.invoke("AskQuestion", roomId, questionSentCount, true)
            .catch(err => {
                console.error("Failed to invoke CheckUserState: ", err)
                alert("予期せぬエラーが発生しました。(d)"
                );
                window.location.href = "Index";
            });
    }, timer);
});
//ここからクイズロジック----------------
connection.on("ReceiveResults", (results) => {
    if (timeoutId)
        clearTimeout(timeoutId);
    // 質問を表示するロジック
    console.log("Receiving results...");
    document.getElementById("question").innerText = "Quiz has ended!";
    // connection.stop();
    results.forEach(result => {
        const player = players.find(player => player.id === result.id);
        if (player) {
            player.points = result.points; // プレイヤーのポイントを更新
            player.position = result.position; // プレイヤーの順位を決定
        }
    });
    initializeResultUI();
});

connection.on("ReceiveQuestion", (questionSentCount, question, options, timeLimit) => {
    if (timeoutId)
        clearTimeout(timeoutId);
    questionReceivedCount = questionSentCount;
    Quiz.updateQuestionCounter(document.getElementById('question-counter'), questionReceivedCount, questionCount);
    document.querySelector("#timer-container").hidden = false;
    document.querySelector("#timer-bar").hidden = false;
    Quiz.displayQuestion(question);
    //displayOptions(options, startTime, timer);
    displayOptions(options, timeLimit);
    console.log(`Receiving the question.question:${question}, options:${options}, questionSentCount:${questionSentCount}`);
    //answerTimer(timer, startTime);
    setTimeout(askCorrectAnswer, timeLimit * 2)
});

connection.on("SomeoneAnswered", playerId => {
    const playerDiv = document.getElementById(`player-${playerId}`);
    const playerIcon = playerDiv.querySelector('.player-icon');
    playerIcon.classList.add('with-border');
});

connection.on("ReceivePoints", (playerPoints, timer) => {
    if (timeoutId)
        clearTimeout(timeoutId);
    playerPoints.forEach(({ id, points }) => {
        // `players` 配列内の該当プレイヤーを更新
        const player = players.find(player => player.id === id);
        // console.log(player);
        if (player) {
            player.points = points; // プレイヤーのポイントを更新
        }

        // 該当プレイヤーのDOMを更新
        const playerDiv = document.getElementById(`player-${id}`);
        if (playerDiv) {
            const pointsDiv = playerDiv.querySelector(".player-points");
            if (pointsDiv) {
                pointsDiv.innerText = `${points} pts`; // ポイントを更新
            }
        }
    });
    setTimeout(askQuestion, timer);
});

connection.on("ReceiveRankPoints", (newRankPoints, delta) => {
    if (timeoutId)
        clearTimeout(timeoutId);

    connection.stop();
    const me = players.find(player => player.id === userId);
    me.rankPoints = newRankPoints;
    me.delta = delta;
    const goRankBtn = document.getElementById('goRankCalcBtn');
    goRankBtn.addEventListener('click', () => {
        initializeRankCalcUI();
    });
    goRankBtn.disabled = false;
});

connection.on("RevealAnswer", (questionSentCount, correctAnswer) => {
    console.log('RevealAnswer Called.' + correctAnswer);
    questionReceivedCount = questionSentCount;
    const optionButtons = document.querySelectorAll('.answer-button');
    Quiz.colorAnswerButtons(optionButtons, correctAnswer);

    const playerIcons = document.querySelectorAll('.player-icon');
    playerIcons.forEach(playerIcon => {
        if (playerIcon.classList.contains('with-border'))
            playerIcon.classList.remove('with-border');
    })
});

function displayOptions(options, timeLimit) {
    const optionsDiv = document.getElementById("options");
    optionsDiv.innerHTML = ''; // 前回の選択肢をクリア

    const timer = new Quiz.Timer(
        timeLimit,
        (progress, remainingTime) => updateTimerUI(progress, remainingTime),
        () => handleTimerComplete(), 100);
    timer.start();

    options.forEach(option => {
        const button = document.createElement("button");
        button.innerText = option;
        button.style.display = 'block';
        button.classList.add("answer-button", "btn", "btn-outline-brand");
        button.onclick = () => {
            //clearInterval(countdown);
            timer.stop();
            Quiz.colorSelectedOption(button);
            Quiz.disableOptions(document.querySelectorAll(".answer-button"));
            sendAnswer(option, timeLimit - (Date.now() - timer.startTime));
        }
        optionsDiv.appendChild(button);
    });
}

function updateTimerUI(progress, remainingTime) {
    const timerBar = document.getElementById("timer-bar");
    //const timerText = document.getElementById("timer-text");
    //timerText.innerText = `Time Left: ${Math.max(0, Math.ceil(remainingTime / 1000))}s`; // 秒単位で表示
    if (timerBar) {
        timerBar.style.width = `${progress}%`; // ゲージを更新
    }
}

function handleTimerComplete() {
    const optionButtons = document.querySelectorAll('.answer-button');
    Quiz.disableOptions(optionButtons); // 選択肢を無効化
    sendAnswer('', 0);
}

function sendAnswer(selectedAnswer, remainingTime) {
    // サーバーに選択した回答を送信
    console.log("submitting the answer:" + selectedAnswer);
    connection.invoke("SubmitAnswer", roomId, selectedAnswer, remainingTime)
        .catch(err => console.error(err.toString()));
}

///ここまでクイズロジック--------------------------
function initializeWaitingUI(playerCount) {
    document.getElementById("playerCount").innerText = playerCount
    console.log("Initialized waiting state.");
    Quiz.switchToWaitingPhase();
}

function getPlayerIcon(iconUrl, i) {
    // img要素を作成
    // "~/" をルート相対パスに変換
    if (iconUrl && iconUrl.startsWith("~/")) {
        iconUrl = iconUrl.replace("~/", "/");
    }
    const img = document.createElement("img");
    img.height = 32;
    img.width = 32;
    img.className = "rounded-circle";
    img.alt = `player-icon-${i}`;
    img.style.objectFit = "contain";
    img.src = iconUrl;

    // ロードエラー時にデフォルト画像に切り替え
    img.onerror = function () {
        this.src = "/images/icons/gear.png";
    };

    return img;
}

function initializeGameUI() {
    const playersArea = document.querySelector("#players-area");
    playersArea.innerHTML = ''; // 既存の内容をクリア
    players.forEach((player, index) => {
        const playerDiv = document.createElement("div");
        playerDiv.classList.add("player");
        playerDiv.id = `player-${player.id}`;

        // アイコン
        const plyaerIcon = getPlayerIcon(player.iconUrl, index);
        plyaerIcon.classList.add("player-icon");
        playerDiv.appendChild(plyaerIcon);

        // 名前とポイント
        const nameDiv = document.createElement("div");
        nameDiv.classList.add("player-name", "mt-2");
        let playerName = player.name;
        nameDiv.innerText = playerName;
        if (player.id === userId) {
            nameDiv.classList.add('text-brand');
        }
        playerDiv.appendChild(nameDiv);

        const pointsDiv = document.createElement("div");
        pointsDiv.classList.add("player-points");
        pointsDiv.innerText = `${player.points} pts`;
        playerDiv.appendChild(pointsDiv);

        // プレイヤー情報を追加
        playersArea.appendChild(playerDiv);
    });

    Quiz.updateQuestionCounter(document.getElementById('question-counter'), questionReceivedCount, questionCount);
    Quiz.switchToGamePhase();
    Quiz.displayQuestion("GET READY?");
    document.getElementById("options").innerHTML = '';
}

function initializeResultUI() {
    const resultsArea = document.querySelector("#results-area");
    resultsArea.innerHTML = ''; // 既存の内容をクリア
    players.sort((a, b) => a.position - b.position);
    players.forEach((player, index) => {
        const resultItem = document.createElement("div");
        resultItem.classList.add("result-item");
        // 順位
        const rankDiv = document.createElement("span");
        rankDiv.classList.add("rank");
        rankDiv.innerText = player.position;
        resultItem.appendChild(rankDiv);

        // アイコン
        const plyaerIcon = getPlayerIcon(player.iconUrl, index);
        plyaerIcon.classList.add("player-icon");
        resultItem.appendChild(plyaerIcon);

        // 名前
        const nameDiv = document.createElement("span");
        nameDiv.classList.add("player-name");
        let playerName = player.name;
        nameDiv.innerText = playerName;
        if (player.id === userId) {
            nameDiv.classList.add('text-brand');
        }
        resultItem.appendChild(nameDiv);

        // ポイント
        const pointsDiv = document.createElement("span");
        pointsDiv.classList.add("player-points");
        pointsDiv.innerText = `${player.points} pts`;
        resultItem.appendChild(pointsDiv);

        // 結果リストに追加
        resultsArea.appendChild(resultItem);
    });

    Quiz.switchToResultPhase();
}

async function initializeRankCalcUI() {
    const me = players.find(player => player.id === userId);
    const diff = document.querySelector('.rank-point-difference');
    const rankAlphabet = await fetchRankAlphabet(me.rankPoints);
    if (me.delta >= 0) {
        diff.textContent = '+' + me.delta;
        diff.parentElement.classList.add('text-success');
    }
    else {
        diff.textContent = me.delta;
        diff.parentElement.classList.add('text-danger');
    }

    //昇格まであとXptsのX→昇格は100ポイント刻み
    const promotionPoint = 100 - me.rankPoints % 100;
    if (rankAlphabet) {
        document.querySelector('.rank-alphabet').textContent = rankAlphabet;
    }

    if (rankAlphabet && rankAlphabet === "A+") {
        document.querySelector('.rank-point-promotion').textContent = 0;
    }
    else if (promotionPoint === 0) {
        document.querySelector('.rank-point-promotion').textContent = 100;
    }
    else {
        document.querySelector('.rank-point-promotion').textContent = promotionPoint;
    }

    document.querySelector('.rank-point-before').textContent = Math.max(0, me.rankPoints - me.delta);
    document.querySelector('.rank-point-total').textContent = me.rankPoints;
    Quiz.switchToRankCalcPhase();
}

async function fetchRankAlphabet(rankPoints) {
    if (!rankPoints) {
        console.error("rankPoints is null or undefined");
        return null;
    }
    try {
        const response = await fetch(`/api/GetRankAlphabet/${rankPoints}`);
        console.log(response);
        if (!response.ok) {
            throw new Error(`Failed to fetch: ${response.statusText}`);
        }

        const rankAlphabet = await response.text();
        return rankAlphabet;
    } catch (error) {
        console.error("Error fetching rank points:", error);
        return null;
    }
}

