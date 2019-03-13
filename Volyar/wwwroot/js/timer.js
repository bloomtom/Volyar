﻿
const baseTimerPeriod = 2000;
const backgroundThrottle = 10;
const throttleWaitMultiplier = 2; // Number of milliseconds extra to wait per item retrieved from the api.
const throttleWaitFilledDivider = 2;
var throttleWait = 0;
var iterationThrottle = 1;

var timerRunning = false;
var timerCounter = 0; // Free running clock of timer executions.

var testMode = false;

function updateStatus() {
    var statusFunc = testMode ? getTestStatus : getStatus;

    statusFunc(function (data) {
        let result = JSON.parse(data);
        throttleWait += (result.queued.length + result.processing.length) * throttleWaitMultiplier;

        mainVue.waiting = result.queued;
        mainVue.inProgress = result.processing;
    }, null);

    getComplete(function (data) {
        let result = JSON.parse(data);
        throttleWait += result.length * throttleWaitMultiplier;

        for (var i = 0; i < result.length; i++) {
            result[i].Complete = true;
        }

        mainVue.complete = result;
    }, null);
}

function startTimer(force, single = false) {
    // Only allow one running timer.
    if (timerRunning && !force) { return; }
    timerRunning = true;
    throttleWait /= throttleWaitFilledDivider;

    setTimeout(function () {
        timerCounter++;

        try {
            if (timerCounter % iterationThrottle === 0) {
                updateStatus();
                console.log('Updated');
            }
        }
        finally {
            if (single === false) {
                startTimer(true);
            }
        }
        console.log('Waiting ' + (baseTimerPeriod + throttleWait + iterationThrottle));
    }, baseTimerPeriod + throttleWait);
}

startTimer();

try {
    updateStatus();
} finally {
    //
}

window.addEventListener("focus", function () {
    iterationThrottle = 1;
    throttleWait = 0;
    startTimer(true, true);
}, false);

window.addEventListener("blur", function () {
    iterationThrottle = backgroundThrottle;
}, false);