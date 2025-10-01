"use strict";

import * as Quiz from './quiz.js';

const userId = document.getElementById('user-data').dataset.userId;
const roomId = document.getElementById('user-data').dataset.roomId;

let players;
let hostId;
let questionReceivedCount = 0;
let questionCount;
let timeoutId;

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/roomMatchHub")
    .build();

startConnection();

// 非同期関数を定義して、接続処理を実行
async function startConnection() {
    try {
        await connection.start();
        await connection.invoke("CheckUserState", roomId);
    } catch (err) {
        // 複製タブによるものだった場合、遷移しない
        if (err.message.includes("Invocation canceled due to the underlying connection being closed")) {
            console.log("タブが複製されました");
            return;
        }
        // 予期しないエラーならアラートを表示してリダイレクト
        console.error(err);
        alert("予期せぬエラーが発生しました。(1)");
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

document.addEventListener('DOMContentLoaded', () => {
    const copyButton = document.getElementById('copy-room-id');
    const copyIcon = document.getElementById('copy-icon');
    const copyText = document.getElementById('copy-text');

    copyButton.addEventListener('click', () => {
        // Room IDをクリップボードにコピー
        navigator.clipboard.writeText(roomId).then(() => {
            // アイコンとテキストを変更
            copyIcon.textContent = "check"; // Material Symbolsのチェックマーク
            copyText.textContent = "Copied!";

            // 2秒後に元に戻す
            setTimeout(() => {
                copyIcon.textContent = "content_copy";
                copyText.textContent = "Copy Room ID";
            }, 2000);
        }).catch(err => {
            console.error("Failed to copy Room ID: ", err);
        });
    });

    // URLコピー処理
    document.getElementById('copy-url-btn').addEventListener('click', function () {
        // data-url属性からURLを取得
        const urlToCopy = this.getAttribute('data-url');
        navigator.clipboard.writeText(urlToCopy)
            .then(() => {
                //alert('URL copied to clipboard!');
            })
            .catch(err => {
                console.error('Failed to copy URL: ', err);
            });
    });

    // 部屋IDコピー処理
    document.getElementById('copy-room-id-btn').addEventListener('click', function () {
        // data-room-id属性から部屋IDを取得
        const roomIdToCopy = this.getAttribute('data-room-id');
        navigator.clipboard.writeText(roomIdToCopy)
            .then(() => {
                //alert('Room ID copied to clipboard!');
            })
            .catch(err => {
                console.error('Failed to copy Room ID: ', err);
            });
    });

    document.getElementById("goWaiting").addEventListener("click", () => {
        document.getElementById("goWaiting").disabled = true;
        goNextMatch();
    });

    animateDots();
});

connection.on("RoomNotFound", () => {
    alert("部屋が既に満員、または存在しません");
    Quiz.returnToHome();
});

connection.on("ReturnToGame", function (hostId_arg, players_arg, questionCount_arg, questionSentCount, timer) {
    hostId = hostId_arg;
    players = players_arg;
    questionCount = questionCount_arg;
    questionReceivedCount = questionSentCount;
    console.log(`Returning to the game..., questionReceivedCount = ${questionReceivedCount}`);
    // UIをゲーム画面に切り替え
    initializeGameUI(players_arg);
    document.querySelector("#question").innerHTML = `
                <span>Rejoining...</span>
                <span class="spinner-border mt-1" style="display:inline-block; font-size:1rem;"></span>`;
    //無限Rejoin対策
    timeoutId = setTimeout(() => {
        console.log("Time is out. Asking question...");
        // 引数がquestionReceivedCountではなくquestionSentCountなのは、questionReceivedCountはsetTimeoutの途中で更新されるから
        connection.invoke("AskQuestion", roomId, questionSentCount, true)
            .catch(err => {
                console.error("Failed to invoke CheckUserState: ", err)
                alert("予期せぬエラーが発生しました。(2)");
                window.location.href = "Index";
            });
    }, timer);
});

connection.on("DisableTab", function (message) {
    if (timeoutId)
        clearTimeout(timeoutId);

    connection.stop(); // SignalR接続を切断
    //alert(message);
    document.body.innerHTML = `<div style="text-align:center; margin-top:20px;">
                    <h1>${message}</h1>
                    <p>このタブは現在非アクティブです。</p>
                </div>`;
});

connection.on("RoomIsInProgress", () => {
    alert("ゲームが進行中なので、入室できません。");
    Quiz.returnToHome();
});

connection.on("PlayerJoined", initializeWaitingUI);

connection.on("UpdateWaitingList", (hostId_arg, players_arg) => {
    updateWaitingPlayerList(hostId_arg, players_arg);
    //document.getElementById("playerCount").innerText = updatedPlayers.length;
});

connection.on("ShowStartButton", () => {
    const startButton = document.getElementById("start");
    const quitButton = document.getElementById("quitButton");
    const statusMessage = document.getElementById("status-message");

    if (statusMessage) {
        statusMessage.innerText = "Waiting for You to Start";
    }
    console.log(startButton);
    // ホストにのみstartボタン表示
    startButton.hidden = false;
    startButton.onclick = () => {
        connection.invoke("StartQuiz", roomId)
            .then(() => console.log('Start Button Pressed'))
            .catch((err) => {
                console.error('Error starting quiz: ', err);
                alert("予期せぬエラーが発生しました。(3)");
                window.location.href = "Index";
            });
        startButton.hidden = true;
        quitButton.hidden = true;
    };
});

function askQuestion() {
    console.log("asking next question Count: " + questionReceivedCount);
    connection.invoke("AskQuestion", roomId, questionReceivedCount, false)
        .then(() => console.log('Asked a question'))
        .catch(err => console.error(err));
}

function askCorrectAnswer() {
    console.log("asking the correct answer");
    connection.invoke("AskCorrectAnswer", roomId, questionReceivedCount)
        .catch((err) => {
            console.error('Error starting quiz: ', err);
            alert("予期せぬエラーが発生しました。(4)");
            window.location.href = "Index";
        });
}

function initializeWaitingUI(hostId_arg, players_arg) {
    console.log(players_arg);
    updateWaitingPlayerList(hostId_arg, players_arg);
    //document.getElementById("playerCount").innerText = players.length;
    console.log("Initialized waiting state.");
    Quiz.switchToWaitingPhase();
}

function updateWaitingPlayerList(hostId_arg, players_arg) {
    if (!players_arg.some(player => player.id === userId)) {
        console.error('待機リストに自分が含まれていません。');
        alert("予期せぬエラーが発生しました。(5)");
        window.location.href = "Index"; // Index にリダイレクト
    }

    players = players_arg;
    hostId = hostId_arg;
    const playerCards = document.querySelectorAll('.player-card');
    playerCards.forEach((playerCard, index) => {
        playerCard.innerHTML = "";
        //const playerName = playerCard.querySelector('.player-name');
        if (players[index]) {
            const playerIcon = getPlayerIcon(players[index].iconUrl, index);
            playerIcon.classList.add("waiting-player-icon");
            playerCard.appendChild(playerIcon);

            if (players[index].id === hostId) {
                const hostIcon = playerCard.querySelector(`.waiting-player-icon`);
                if (hostIcon) {
                    // 枠線の色をホスト用に設定
                    hostIcon.style.border = "1px solid #ff5733";
                    hostIcon.style.borderRadius = "50%"; // 円形の枠線にする（念のため）
                }
            }

            const nameDiv = document.createElement("div");
            nameDiv.classList.add("player-name");
            playerCard.appendChild(nameDiv);

            nameDiv.textContent = players[index].name;
            if (players[index].id === userId) {
                nameDiv.classList.add('text-brand');
            }
            else
                nameDiv.classList.remove('text-brand');
        }
        else {
            playerCard.innerHTML = `<span class="spinner-border mt-1" role="status">`;
        }
    });
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

function initializeGameUI(players_arg) {
    const playersArea = document.querySelector("#players-area");
    playersArea.innerHTML = ''; // 既存の内容をクリア
    console.log("Initializing GameUi");
    players_arg.forEach((player, index) => {
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
            // playerName += ' (You)';
            nameDiv.classList.add('text-brand');
        }
        playerDiv.appendChild(nameDiv);

        const pointsDiv = document.createElement("div");
        pointsDiv.classList.add("player-points");
        pointsDiv.innerText = `0 pts`;
        playerDiv.appendChild(pointsDiv);

        // プレイヤー情報を追加
        playersArea.appendChild(playerDiv);
    });
    Quiz.updateQuestionCounter(document.getElementById('question-counter'), questionReceivedCount, questionCount);
    Quiz.displayQuestion("GET READY?");
    document.getElementById("options").innerHTML = '';
    Quiz.switchToGamePhase();
}

function initializeResultUI() {
    const resultsArea = document.querySelector("#results-area");
    resultsArea.innerHTML = ''; // 既存の内容をクリア
    players.sort((a, b) => a.position - b.position);
    players.forEach((player, index) => {
        if (!document.getElementById(`player-${player.id}`)) {
            //対戦中にいなかったプレイヤーは表示しない　（対戦中の途中参加プレイやは非表示)
            return;
        }
        const resultItem = document.createElement("div");
        resultItem.classList.add("result-item");
        // 順位
        const rankDiv = document.createElement("span");
        rankDiv.classList.add("rank");
        rankDiv.innerText = player.position;
        resultItem.appendChild(rankDiv);

        // アイコン
        const playerIcon = getPlayerIcon(player.iconUrl, index);
        playerIcon.classList.add("player-icon");
        resultItem.appendChild(playerIcon);

        // 名前
        const nameDiv = document.createElement("span");
        nameDiv.classList.add("player-name");
        let playerName = player.name;
        nameDiv.innerText = playerName;
        if (player.id === userId) {
            // playerName += ' (You)';
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
    document.getElementById("goWaiting").disabled = false;
    // 生存確認のため3秒待つ
    /*setTimeout(() => { document.getElementById("goWaiting").disabled = false; }, 3000);*/

}

function goNextMatch() {
    const statusMessage = document.getElementById("status-message");
    if (userId !== hostId) {
        statusMessage.innerText = "Waiting for Host to Start";
        //ホストの場合ShowStartButton参照
    }
    document.getElementById("quitButton").hidden = false;
    document.getElementById("share-btn").hidden = false;

    initializeWaitingUI(hostId, players);
}

connection.on("StartingQuiz", (players_arg, questionCount_arg) => {
    if (timeoutId)
        clearTimeout(timeoutId);

    // 結果画面のままのひと
    if (document.getElementById('waiting-phase').hidden === true) {
        Quiz.switchToWaitingPhase();
    }
    players = players_arg;
    questionCount = questionCount_arg;
    questionReceivedCount = 0;
    console.log("Quiz is starting...");

    document.getElementById("status-message").innerText = "The game will start soon";
    document.getElementById("quitButton").hidden = true;
    document.getElementById("share-btn").hidden = true;

    setTimeout(initializeGameUI, 2000, players_arg);
    setTimeout(askQuestion, 3000);
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

connection.on("ReceiveResults", (results) => {
    if (timeoutId)
        clearTimeout(timeoutId);

    console.log("Receiving results...", results);
    document.querySelector("#timer-container").hidden = true;
    document.querySelector("#timer-bar").hidden = true;
    document.getElementById("question").innerText = "Quiz has ended!";
    results.forEach(result => {
        const player = players.find(player => player.id === result.id);
        if (player) {
            player.points = result.points; // プレイヤーのポイントを更新
            player.position = result.position; // プレイヤーの順位を決定
        }
    });
    questionReceivedCount = 0;

    // 試合中に入室したユーザーには結果表示しない
    if (document.getElementById('waiting-phase').hidden === false) {
        document.getElementById("status-message").textContent = "Waiting for Host to Start";
        return;
    }
    initializeResultUI();
});

connection.on("ReceivePoints", (playerPoints, timer) => {
    if (timeoutId)
        clearTimeout(timeoutId);

    playerPoints.forEach(({ id, points }) => {
        // `players` 配列内の該当プレイヤーを更新
        const player = players.find(player => player.id === id);
        const playerDiv = document.getElementById(`player-${id}`);
        // console.log(player);
        if (player && playerDiv) {
            //colorPlayerIcon(playerDiv.querySelector(".player-icon"), points, player.points);
            player.points = points; // プレイヤーのポイントを更新
            const pointsDiv = playerDiv.querySelector(".player-points");
            if (pointsDiv) {
                pointsDiv.innerText = `${points} pts`; // ポイントを更新
            }
        }
    });
    setTimeout(() => {
        askQuestion();
    }, timer)
});

function decolorPlayerIcon(imgs) {
    if (updatedPoints > oldPoints)
        img.classList.add('with-border', 'correct-answer');
    else if (updatedPoints === oldPoints)
        img.classList.add('with-border', 'incorrect-answer');
    else
        img.classList.add('with-border', 'no-answer');
}

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
    if (timerBar) {
        timerBar.style.width = `${progress}%`; // ゲージを更新
    }
    //timerText.innerText = `Time Left: ${Math.max(0, Math.ceil(remainingTime / 1000))}s`; // 秒単位で表示
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

connection.on("RevealAnswer", (questionSentCount, correctAnswer) => {
    console.log('Correct Answer Received: ' + correctAnswer);
    questionReceivedCount = questionSentCount;
    const optionButtons = document.querySelectorAll('.answer-button');
    Quiz.colorAnswerButtons(optionButtons, correctAnswer);
    const playerIcons = document.querySelectorAll('.player-icon');
    playerIcons.forEach(playerIcon => {
        if (playerIcon.classList.contains('with-border'))
            playerIcon.classList.remove('with-border');
    })
});

function animateDots() {
    const dots = document.querySelector('.loading-dots');
    let dotCount = 0;
    if (dots) {
        // 1秒ごとにドットを増減させる
        setInterval(() => {
            dotCount = (dotCount + 1) % 4; // 0, 1, 2, 3 をループ
            dots.textContent = '.'.repeat(dotCount); // ドットの数を更新
        }, 500); // 500msごとに更新
    }
}
