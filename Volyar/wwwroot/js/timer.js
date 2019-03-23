
const baseTimerPeriod = 2000;
const backgroundThrottle = 10;
const throttleWaitMultiplier = 2; // Number of milliseconds extra to wait per item retrieved from the api.
const throttleWaitFilledDivider = 2;
var runTimer = true;
var throttleWait = 0;
var iterationThrottle = 1;

var timerRunning = false;
var timerCounter = 0; // Free running clock of timer executions.

var statusTimerEnabled = true;
var pendingDeleteTimerEnabled = true;

function invalidateTimerEnables() {
    statusTimerEnabled = false;
    pendingDeleteTimerEnabled = false;
}

function enableStatusTimer() {
    invalidateTimerEnables();
    statusTimerEnabled = true;
}

function enablePendingDeleteTimer() {
    invalidateTimerEnables();
    pendingDeleteTimerEnabled = true;
}

var testMode = false;

function sortQueueItems(a, b) {
    return a.CreateTime > b.CreateTime;
}

function sortPendingDeletions(a, b) {
    return a.mediaId > b.mediaId;
}

function updateStatus() {
    var statusFunc = testMode ? getTestStatus : getStatus;

    statusFunc(function (data) {
        let result = JSON.parse(data.responseText);
        throttleWait += (result.queued.length + result.processing.length) * throttleWaitMultiplier;

        store.commit('setWaiting', result.queued.sort(sortQueueItems));
        store.commit('setProgress', result.processing.sort(sortQueueItems));
        console.log('Updated progress');
    }, null);

    getComplete(function (data) {
        let result = JSON.parse(data.responseText);
        throttleWait += result.length * throttleWaitMultiplier;

        for (var i = 0; i < result.length; i++) {
            result[i].Complete = true;
        }

        store.commit('setComplete', result.sort(sortQueueItems));
    }, null);
}

function updatePendingDelete() {
    getPendingDelete(function (data) {
        let result = JSON.parse(data.responseText);
        throttleWait += result.length * throttleWaitMultiplier;

        store.commit('setPendingDelete', result.sort(sortPendingDeletions));
        console.log('Updated pending delete');
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
            if (runTimer) {
                if (statusTimerEnabled && timerCounter % iterationThrottle === 0) {
                    updateStatus();
                }
                if (pendingDeleteTimerEnabled && timerCounter % (iterationThrottle * 10) === 0) {
                    updatePendingDelete();
                }
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

function enableTimerConversionStatus() {
    iterationThrottle = backgroundThrottle;
}

function enableTimerPendingDeletions() {
    iterationThrottle = backgroundThrottle;
}


window.addEventListener("focus", function () {
    iterationThrottle = 1;
    throttleWait = 0;
    startTimer(true, true);
}, false);

window.addEventListener("blur", function () {
    iterationThrottle = backgroundThrottle;
}, false);