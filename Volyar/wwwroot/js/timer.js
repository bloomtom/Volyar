
const baseTimerPeriod = 2000;
const backgroundThrottle = 10;
const throttleWaitMultiplier = 2; // Number of milliseconds extra to wait per item retrieved from the api.
const throttleWaitFilledDivider = 2;
var throttleWait = 0;
var iterationThrottle = 1;

var timerRunning = false;
var timerCounter = 0; // Free running clock of timer executions.

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
    }, null);

    getComplete(function (data) {
        let result = JSON.parse(data.responseText);
        throttleWait += result.length * throttleWaitMultiplier;

        for (var i = 0; i < result.length; i++) {
            result[i].Complete = true;
        }

        store.commit('setComplete', result.sort(sortQueueItems));
    }, null);

    getPendingDelete(function (data) {
        let result = JSON.parse(data.responseText);
        throttleWait += result.length * throttleWaitMultiplier;

        store.commit('setPendingDelete', result.sort(sortPendingDeletions));
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