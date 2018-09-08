
var timerPeriod = 2500;
var timerRunning = false;
var timerCounter = 0; // Free running clock of timer executions.

function updateStatus() {
    getStatus(function (data) {
        let result = JSON.parse(data);
        mainVue.waiting = result.queued;
        mainVue.inProgress = result.processing;
    }, null);
}

function startTimer(force) {
    // Only allow one running timer.
    if (timerRunning && !force) { return; }
    timerRunning = true;

    setTimeout(function () {
        timerCounter++;

        try {
            updateStatus();
        }
        finally {
            startTimer(true);
        }
    }, timerPeriod);
}

startTimer();

try {
    updateStatus();
} finally {
    //
}