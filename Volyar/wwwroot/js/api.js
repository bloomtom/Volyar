
function getStatus(done, fail) {
    $.get(
        "/external/api/conversion/status"
    ).done(done).fail(fail);
}

function getTestStatus(done, fail) {
    $.get(
        "/external/api/conversion/teststatus"
    ).done(done).fail(fail);
}

function postFullscan(done, fail) {
    $.post(
        "/internal/api/scan/fullscan"
    ).done(done).fail(fail);
}