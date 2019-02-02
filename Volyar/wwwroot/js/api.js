
function getStatus(done, fail) {
    $.get(
        "../external/api/conversion/status"
    ).done(done).fail(fail);
}

function getTestStatus(done, fail) {
    $.get(
        "../external/api/conversion/teststatus"
    ).done(done).fail(fail);
}

function getComplete(done, fail) {
    $.get(
        "../external/api/conversion/complete"
    ).done(done).fail(fail);
}

function cancelItem(item, done, fail) {
    $.post(
        "../external/api/conversion/cancel?name=" + encodeURIComponent(item)
    ).done(done).fail(fail);
}

function cancelAll(done, fail) {
    $.post(
        "../external/api/conversion/cancel"
    ).done(done).fail(fail);
}

function pauseQueue(done, fail) {
    $.post(
        "../external/api/conversion/pause"
    ).done(done).fail(fail);
}

function resumeQueue(done, fail) {
    $.post(
        "../external/api/conversion/resume"
    ).done(done).fail(fail);
}

function postFullscan(done, fail) {
    $.post(
        "../internal/api/scan/fullscan"
    ).done(done).fail(fail);
}