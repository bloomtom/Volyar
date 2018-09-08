
function getStatus(done, fail) {
    $.get(
        "/voly/external/api/conversion/status"
    ).done(done).fail(fail);
}

function getTestStatus(done, fail) {
    $.get(
        "/voly/external/api/conversion/teststatus"
    ).done(done).fail(fail);
}

function postFullscan(done, fail) {
    $.post(
        "/voly/internal/api/scan/fullscan"
    ).done(done).fail(fail);
}