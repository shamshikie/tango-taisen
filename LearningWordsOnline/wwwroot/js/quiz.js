"use strict";

export class Timer {
    constructor(timeLimit, onTick, onComplete, intervalTime) {
        this.timeLimit = timeLimit; // 制限時間 (ミリ秒)
        this.onTick = onTick; // 更新ごとに呼び出すコールバック
        this.intervalTime = intervalTime; //Intervalの時間 (ms)
        this.onComplete = onComplete; // 完了時に呼び出すコールバック
        this.startTime = null; // 開始時刻
        this.timerId = null; // カウントダウンのタイマーID
    }

    // タイマーを開始
    start() {
        this.startTime = Date.now();
        this.timerId = setInterval(() => this.update(), this.intervalTime); // 100msごとに更新
    }

    // タイマーを停止
    stop() {
        clearInterval(this.timerId);
        this.timerId = null;
    }

    // 残り時間の計算とコールバック呼び出し
    update() {
        const elapsedTime = Date.now() - this.startTime;
        const remainingTime = this.timeLimit - elapsedTime;

        if (remainingTime <= 0) {
            this.onTick(0, 0); // 更新コールバックを実行
            this.onComplete(); // 完了コールバックを実行
            this.stop();
        } else {
            const progress = Math.min((remainingTime / this.timeLimit) * 100, 100);
            this.onTick(progress, remainingTime); // 更新コールバックを実行
        }
    }
}

export function returnToHome() {
    // 現在のページのパスを取得
    const currentPath = window.location.pathname;
    // パスを切り詰めてディレクトリ部分を取得
    const directoryPath = currentPath.substring(0, currentPath.lastIndexOf('/') + 1);
    // ディレクトリ部分に"Index"を追加して遷移
    window.location.href = directoryPath + "Index";
}

export function colorAnswerButtons(buttons, correctAnswer) {
    buttons.forEach(button => {
        if (button.innerText === correctAnswer) {
            //正解
            if (button.classList.contains('btn-brand')) {
                button.classList.remove('btn-brand');
                button.classList.add('btn-success');
            }
            else { //未回答
                button.classList.remove('btn-outline-brand');
                button.classList.add('btn-outline-success');
            }
        } else if (button.classList.contains('btn-brand')) {
            // 不正解である場合
            button.classList.remove('btn-brand');
            button.classList.add('btn-danger');
        }
        else { //選択していないボタン
            button.classList.remove('btn-outline-brand');
            button.classList.add('btn-secondary');
        }
    });
}
export function colorSelectedOption(button, option) {
    button.classList.remove('btn-outline-brand');
    button.classList.add("btn-brand");
}
export function displayQuestion(question) {
    document.getElementById("question").innerText = question;
}

export function displayOptions(questionOrOptions, timeLimit, isOffline) {
    const optionsDiv = document.getElementById("options");
    optionsDiv.innerHTML = ''; // 前回の選択肢をクリア

    // タイマーインスタンスの作成
    const timer = new Timer(
        timeLimit,
        (progress, remainingTime) => updateTimerUI(progress, remainingTime),
        () => handleTimerComplete(isOffline, questionOrOptions),
        100
    );
    timer.start();

    // 選択肢を生成
    const options = isOffline ? questionOrOptions.options : questionOrOptions;
    options.forEach(option => {
        const button = document.createElement("button");
        button.innerText = option;
        button.style.display = 'block';
        button.classList.add("answer-button", "btn", "btn-outline-brand");
        button.onclick = () => {
            timer.stop(); // タイマー停止
            colorSelectedOption(button);
            disableOptions(document.querySelectorAll(".answer-button"));

            if (isOffline) {
                // オフライン用：回答を保存
                const optionButtons = document.querySelectorAll('.answer-button');
                colorAnswerButtons(optionButtons, questionOrOptions.correctAnswer);
                questionOrOptions.selectedAnswer = option;
                questionOrOptions.isCorrect = option === questionOrOptions.correctAnswer;
            } else {
                // オンライン用：回答を送信
                sendAnswer(option, timeLimit - (Date.now() - timer.startTime));
            }
        };
        optionsDiv.appendChild(button);
    });
}

function handleTimerComplete(isOffline, questionOrOptions) {
    const optionButtons = document.querySelectorAll('.answer-button');
    disableOptions(optionButtons); // 選択肢を無効化

    if (!isOffline) {
        // オンライン用：未回答を送信
        sendAnswer('', 0);
    }
    else {
        colorAnswerButtons(optionButtons, questionOrOptions.correctAnswer);
    }
    // オフラインでは特に何もしない
}

//function displayOptions(options, timeLimit) {
//    const optionsDiv = document.getElementById("options");
//    optionsDiv.innerHTML = ''; // 前回の選択肢をクリア

//    const timer = new Quiz.Timer(
//        timeLimit,
//        (progress, remainingTime) => updateTimerUI(progress, remainingTime),
//        () => handleTimerComplete(), 100);
//    timer.start();

//    options.forEach(option => {
//        const button = document.createElement("button");
//        button.innerText = option;
//        button.style.display = 'block';
//        button.classList.add("answer-button", "btn", "btn-outline-primary");
//        button.onclick = () => {
//            timer.stop();
//            Quiz.colorSelectedOption(button);
//            Quiz.disableOptions(document.querySelectorAll(".answer-button"));
//            //オンライン用
//            sendAnswer(option, timeLimit - (Date.now() - timer.startTime));
//        }
//        optionsDiv.appendChild(button);
//    });
//}

export function updateTimerUI(progress, remainingTime) {
    const timerBar = document.getElementById("timer-bar");
    //const timerText = document.getElementById("timer-text");

    timerBar.style.width = `${progress}%`; // ゲージを更新
    //timerText.innerText = `Time Left: ${Math.max(0, Math.ceil(remainingTime / 1000))}s`; // 秒単位で表示
}

//function handleTimerComplete() {
//    const optionButtons = document.querySelectorAll('.answer-button');
//    Quiz.disableOptions(optionButtons); // 選択肢を無効化
//    sendAnswer('', 0);
//}

function sendAnswer(selectedAnswer, remainingTime) {
    // サーバーに選択した回答を送信
    console.log("submitting the answer:" + selectedAnswer);
    connection.invoke("SubmitAnswer", roomId, selectedAnswer, remainingTime)
        .catch(err => console.error(err.toString()));
}
export function disableOptions(buttons) {
    // ボタンを無効化する
    buttons.forEach(button => button.disabled = true);
}

export function updateQuestionCounter(questionCounter, current, max) {
    questionCounter.textContent = `${current}/${max}`;
}

///ここまでクイズロジック--------------------------
function initializeWaitingUI(playerCount) {
    document.getElementById("playerCount").innerText = playerCount
    console.log("Initialized waiting state.");
    switchToWaitingPhase();
}

function initializeGameUI() {
    const playersArea = document.querySelector("#players-area");
    playersArea.innerHTML = ''; // 既存の内容をクリア
    players.forEach(player => {
        const playerDiv = document.createElement("div");
        playerDiv.classList.add("player");
        playerDiv.id = `player-${player.id}`;

        // アイコン
        const iconDiv = document.createElement("div");
        iconDiv.classList.add("player-icon", "rounded-circle", "bg-primary", "text-white", "d-flex", "justify-content-center", "align-items-center");
        iconDiv.innerHTML = `<span>${player.initials}</span>`; // 初期を表示
        playerDiv.appendChild(iconDiv);

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
    switchToGamePhase();
    displayQuestion("GET READY?");
    document.getElementById("options").innerHTML = '';
}

function initializeResultUI() {
    const resultsArea = document.querySelector("#results-area");
    resultsArea.innerHTML = ''; // 既存の内容をクリア
    players.sort((a, b) => a.position - b.position);
    players.forEach(player => {
        const resultItem = document.createElement("div");
        resultItem.classList.add("result-item");
        // 順位
        const rankDiv = document.createElement("span");
        rankDiv.classList.add("rank");
        rankDiv.innerText = player.position;
        resultItem.appendChild(rankDiv);

        // アイコン
        const iconDiv = document.createElement("div");
        iconDiv.classList.add("player-icon");
        iconDiv.innerText = player.initials; // 初期
        resultItem.appendChild(iconDiv);

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

    switchToResultPhase();
}
export function switchToGamePhase() {
    switchPhase(document.querySelectorAll('.phase'),
        document.getElementById("game-phase"));
}

export function switchToWaitingPhase() {
    switchPhase(document.querySelectorAll('.phase'),
        document.getElementById("waiting-phase"));
}

export function switchToResultPhase() {
    switchPhase(document.querySelectorAll('.phase'),
        document.getElementById("result-phase"));
}

export function switchToRankCalcPhase() {
    switchPhase(document.querySelectorAll('.phase'),
        document.getElementById("rankCalc-phase"));
}
function switchPhase(hiddens, visible) {
    hiddens.forEach(phase => phase.hidden = true);
    visible.hidden = false;
}