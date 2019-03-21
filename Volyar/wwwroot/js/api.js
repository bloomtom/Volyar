
function getStatus(done, fail) {
    get("../external/api/conversion/status", done, fail);
}

function getTestStatus(done, fail) {
    get("../external/api/conversion/teststatus", done, fail);
}

function getComplete(done, fail) {
    get("../external/api/conversion/complete", done, fail);
}

function getPendingDelete(done, fail) {
    get("../external/api/delete/pending", done, fail);
}

function cancelItem(item, done, fail) {
    post("../external/api/conversion/cancel?name=" + encodeURIComponent(item), done, fail);
}

function cancelAll(done, fail) {
    post("../external/api/conversion/cancel", done, fail);
}

function pauseQueue(done, fail) {
    post("../external/api/conversion/pause", done, fail);
}

function resumeQueue(done, fail) {
    post("../external/api/conversion/resume", done, fail);
}

function postFullscan(done, fail) {
    post("../internal/api/scan/fullscan", done, fail);
}

function confirmDelete(items, done, fail) {
    call('POST', "../external/api/delete/confirm", done, fail, items);
}

function get(url, done, fail) {
    call('GET', url, done, fail);
}

function post(url, done, fail) {
    call('POST', url, done, fail);
}

function call(type, url, done = null, fail = null, data = null) {
    var request = new XMLHttpRequest();

    request.onload = function () {
        if (request.status >= 200 && request.status < 400) {
            if (done) {
                done(request);
            }
        } else {
            if (fail) {
                fail(request);
            }
        }
    };

    request.onerror = function () {
        if (fail) {
            fail();
        }
    };

    request.open(type, url, true);

    if (data !== null) {
        request.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded; charset=UTF-8');
        request.send(data);
    }
    else {
        request.send();
    }
}