"use strict";

import * as Quiz from './quiz.js';

const questionData = JSON.parse(document.getElementById('questions-container').dataset.questions);
const timeLimitData = document.getElementById('settings-data').dataset.timer;
/*let countdown;*/

class Question {
    constructor(question) {
        this.word = question.text;
        this.correctAnswer = question.correctAnswer;
        this.options = question.options.map(option => ({
            text: option,
            isSelected: false,
        }));
        this.definitionId = question.definitionId;
        //this.options = question.options;
        //this.isCorrect = false;
        //this.selectedAnswer = "";
    }
}

document.addEventListener('DOMContentLoaded', () => {
    // Questionオブジェクトのリストを生成
    const questionsList = questionData.map(data => new Question(data));
    const timeLimit = timeLimitData;

    Quiz.updateQuestionCounter(document.getElementById('question-counter'), 0, questionsList.length);
    initializeGameUI();
    setTimeout(run, 1500, questionsList, timeLimit);

});


function run(questions, timeLimit) {
    document.querySelector("#timer-container").hidden = false;
    document.querySelector("#timer-bar").hidden = false;

    let currentIndex = 0; // 現在の質問のインデックス

    async function goNextQuestion() {
        if (currentIndex < questions.length) {
            const currentQuestion = questions[currentIndex];
            Quiz.updateQuestionCounter(document.getElementById('question-counter'), currentIndex + 1, questions.length);
            Quiz.displayQuestion(currentQuestion.word);
            displayOptions(currentQuestion, timeLimit, () => {
                currentIndex++;
                goNextQuestion(); // 次の質問に進む
            });
        } else {
            const results = generateResults(questions);
            await sendResults(results.map(result => ({
                definitionId: result.definitionId,
                isCorrect: result.isCorrect
            })));
            initializeResultUI(results); // 全ての質問が終わったら結果表示
        }
    }
    goNextQuestion();
}

async function sendResults(results) {
    try {
        const response = await fetch('/api/submitTrainingResults', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(results),
        });
        const responseBody = await response.text();

        if (!response.ok) {
            console.error('Error from server:', responseBody);
            throw new Error(`Failed to send results: ${responseBody}`);
            //window.location.href = '/error-page'; 特定のエラーページ
        }
        console.log('Results successfully sent to server:', responseBody);
    } catch (error) {
        console.error('Error sending results:', error);
    }
}

function displayOptions(question, timeLimit, onTimerStop) {
    const optionsDiv = document.getElementById("options");

    optionsDiv.innerHTML = ''; // 前回の選択肢をクリア
    const timer = new Quiz.Timer(
        timeLimit,
        (progress, remainingTime) => Quiz.updateTimerUI(progress, remainingTime),
        () => {
            const optionButtons = document.querySelectorAll('.answer-button');
            Quiz.disableOptions(optionButtons); // 選択肢を無効化
            Quiz.colorAnswerButtons(optionButtons, question.correctAnswer);
            setTimeout(onTimerStop, timeLimit / 4);
        }, 100);
    timer.start();

    question.options.forEach(option => {
        const button = document.createElement("button");
        button.innerText = option.text;
        button.style.display = 'block';
        button.classList.add("answer-button", "btn", "btn-outline-brand");
        button.onclick = () => {
            const optionButtons = document.querySelectorAll('.answer-button');
            timer.stop();
            Quiz.colorSelectedOption(button);
            Quiz.disableOptions(optionButtons); // 選択肢を無効化
            Quiz.colorAnswerButtons(optionButtons, question.correctAnswer);
            //オフライン用
            option.isSelected = true;
            setTimeout(onTimerStop, timeLimit / 4);
        }
        optionsDiv.appendChild(button);
    });
}

function initializeGameUI() {
    console.log("Initializing GameUi");
    Quiz.displayQuestion("GET READY?");
    document.getElementById("options").innerHTML = '';
    Quiz.switchToGamePhase();
}

function generateResults(questions) {
    return questions.map(question => {
        const selectedAnswer = question.options.find(option => option.isSelected) || null;
        return {
            word: question.word,
            correctAnswer: question.correctAnswer,
            selectedAnswer: selectedAnswer ? selectedAnswer.text : null,
            isCorrect: selectedAnswer ? selectedAnswer.text === question.correctAnswer : false,
            definitionId: question.definitionId
        };
    });
}

function initializeResultUI(results) {
    const resultsArea = document.querySelector("#results-area");
    /* resultsArea.innerHTML = ''; // 既存の内容をクリア*/
    const resultsBody = document.getElementById("results-body");
    results.forEach((result, index) => {
        const row = document.createElement("tr");

        //<td>${result.isCorrect ? "◯" : "☓"}</td>
        // 項目ごとのセルを作成
        row.innerHTML = `
            <td>${index + 1}</td>
            <td>${result.word}</td>
            <td>${result.correctAnswer}</td>
            <td>${result.selectedAnswer ? result.selectedAnswer : 'N/A'}</td>
        `;
        if (result.isCorrect) {
            row.classList.add('table-success');
        }
        else {
            row.classList.add('table-danger');
        }
        resultsBody.appendChild(row);
    });


    Quiz.switchToResultPhase();
}
