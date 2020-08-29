
function conversionStatusRouteNavigate() {
    if (typeof updateStatus === "function") {
        updateStatus();
        enableStatusTimer();
    }
}

function deletionRouteNavigate() {
    if (typeof updateStatus === "function") {
        updatePendingDelete();
        enablePendingDeleteTimer();
    }
}

function managerRouteNavigate() {
    if (typeof updateStatus === "function") {
        updateMediaManager();
        invalidateTimerEnables();
    }
}

function toast(title, body, variant = null) {
    mainVue.$bvToast.toast(body, {
        title: title,
        variant: variant,
        solid: true,
        append: true,
        autoHideDelay: 10000
    });
}

function apiFailToast(title, response) {
    var status = response.status;
    if (!status) { status = 'Unknown'; }
    var text = response.responseText;
    if (!text) { text = 'No message.'; }
    toast(title + ' (Status ' + status + ')', text, 'danger');
}

var qualityComponent = {
    props: {
        quality: {}
    },
    methods: {
        presetStr: function (x) {
            switch (x) {
                case 0: return 'None';
                case 1: return 'Ultrafast';
                case 2: return 'Superfast';
                case 3: return 'Veryfast';
                case 4: return 'Faster';
                case 5: return 'Fast';
                case 6: return 'Medium';
                case 7: return 'Slow';
                case 8: return 'Slower';
                case 9: return 'Veryslow';
                default: return null;
            }
        },
        profileStr: function (x) {
            switch (x) {
                case 0: return 'None';
                case 1: return 'Baseline';
                case 2: return 'Main';
                case 3: return 'High';
                default: return null;
            }
        },
    },
    template: '#quality-template'
};

var progressComponent = {
    props: {
        progress: {}
    },
    methods: {
        shortenDescription: function (str, len) {
            if (str.length <= len) return str;

            separator = ' ';

            var splitIndex = str.indexOf(separator, 0);
            if (splitIndex === -1) {
                return str.substr(0, len);
            }

            var start = str.substr(0, splitIndex);
            var end = str.substr(str.length - (len - splitIndex - 4));
            return start + ' ...' + end;
        },
        progressInt: function (x) {
            return Math.round(x * 100);
        }
    },
    template: '#progress-template'
};

var queueItemComponent = {
    props: {
        item: {}
    },
    methods: {
        timeago: function (x) {
            return moment(x).fromNow();
        },
        title: function (x) {
            if (x.Title !== null) { return x.Title; }
            return x.OutputBaseFilename;
        },
        tune: function (x) {
            switch (x) {
                case 1: return 'Film';
                case 2: return 'Grain';
                case 3: return 'Animation';
                default: return null;
            }
        },
        cancel: function (x) {
            cancelItem(x);
        }
    },
    components: {
        'quality-component': qualityComponent,
        'progress-component': progressComponent
    },
    template: '#queue-item-template'
};

var queueComponent = {
    props: {
        items: {}
    },
    components: {
        'queue-item-component': queueItemComponent
    },
    template: '#queue-template'
};

var completeComponent = {
    props: {
        items: {}
    },
    methods: {
        timeago: function (x) {
            return moment(x).fromNow();
        }
    },
    template: '#complete-template'
};

var conversionStatusComponent = {
    beforeRouteEnter(to, from, next) {
        conversionStatusRouteNavigate();
        next();
    },
    beforeRouteUpdate(to, from, next) {
        conversionStatusRouteNavigate();
        next();
    },
    computed: {
        waiting() {
            return this.$store.state.waiting;
        },
        progress() {
            return this.$store.state.progress;
        },
        complete() {
            return this.$store.state.complete;
        }
    },
    components: {
        'queue-component': queueComponent,
    },
    methods: {
        queueSize: function (x) {
            if (x.length === 0) { return '(none)'; }
            return '(' + x.length + ')';
        }
    },
    template: '#conversion-status-template'
};

var deletionItemComponent = {
    data: function () {
        return {
            checked: false,
            error: ''
        };
    },
    props: {
        item: {}
    },
    methods: {
        checkChanged() {
            this.$emit('checked', this.checked);
        },
        timeago: function (x) {
            return moment(x).fromNow();
        }
    },
    template: '#deletion-item-template'
};

var pendingDeletionsComponent = {
    data: function () {
        return {
            indeterminate: false,
            masterCheck: false
        };
    },
    beforeRouteEnter(to, from, next) {
        deletionRouteNavigate();
        next();
    },
    beforeRouteUpdate(to, from, next) {
        deletionRouteNavigate();
        next();
    },
    computed: {
        pendingDelete() {
            return this.$store.state.pendingDelete;
        }
    },
    components: {
        'deletion-item-component': deletionItemComponent
    },
    methods: {
        invalidateMasterCheck(checked) {
            var allChecked = true;
            var anyChecked = false;
            for (var i = 0; i < this.$children.length; i++) {
                allChecked &= this.$children[i].checked;
                anyChecked |= this.$children[i].checked;
            }
            this.indeterminate = anyChecked && !allChecked;
            this.masterCheck = allChecked;
        },
        masterCheckChanged() {
            this.indeterminate = false;
            for (var i = 0; i < this.$children.length; i++) {
                this.$children[i].checked = this.masterCheck;
            }
        },
        confirmDelete() {
            let confirmed = this.getChecked();
            if (confirmed.length > 0) {
                let localthis = this;
                confirmDelete(confirmed, function () {
                    updatePendingDelete();
                }, function (x) {
                    let errored = JSON.parse(x.response).FailedItems;
                    let lookup = {};
                    for (var i = 0; i < errored.length; i++) {
                        lookup[errored[i].MediaId + ':' + errored[i].Version] = errored[i].Reason;
                    }

                    for (var j = 0; j < localthis.$children.length; j++) {
                        var found = lookup[localthis.$children[j]._props.item.MediaId + ':' + localthis.$children[j]._props.item.Version];
                        if (found) {
                            localthis.$children[j].error = found;
                        }
                    }
                });
            }
            this.uncheckAll();
        },
        revertDelete() {
            let confirmed = this.getChecked();
            if (confirmed.length > 0) {
                revertDelete(confirmed, function () {
                    updatePendingDelete();
                });
            }
            this.uncheckAll();
        },
        uncheckAll() {
            this.masterCheck = false;
            this.masterCheckChanged();
        },
        getChecked() {
            let confirmed = [];
            for (var i = 0; i < this.$children.length; i++) {
                if (this.$children[i].checked) {
                    confirmed.push({ MediaId: this.$children[i]._props.item.MediaId, Version: this.$children[i]._props.item.Version });
                }
            }
            return confirmed;
        }
    },
    template: '#pending-deletions-template'
};

var mediaManagerComponent = {
    props: {
        name: String
    },
    data: function () {
        return {
            columns: ['mediaId', 'libraryName', 'seriesName', 'name', 'seasonNumber', 'episodeNumber', 'version', 'createDate'],
            data: [],
            css: {
                pagination: {
                    infoClass: 'pull-left',
                    wrapperClass: 'vuetable-pagination pull-right',
                    activeClass: 'btn-primary',
                    disabledClass: 'disabled',
                    pageClass: 'btn btn-border',
                    linkClass: 'btn btn-border',
                    icons: {
                        first: '',
                        prev: '',
                        next: '',
                        last: ''
                    }
                }
            },
            options:
            {
                theme: 'bootstrap4',
                pagination: { chunk: 20 },
                perPageValues: [10, 25, 50, 100, 200],
                useVuex: true,
                saveState: true,
                storage: 'session',
                uniqueKey: 'mediaId',
                headings: {
                    mediaId: 'Media ID',
                    libraryName: 'Library',
                    seriesName: 'Series',
                    name: 'Episode Title',
                    seasonNumber: 'Season',
                    episodeNumber: 'Episode Number',
                    version: 'Version',
                    createDate: 'Created'
                },
                columnsClasses: {
                    mediaId: 'text-center'
                },
                texts: {
                    count: "{from} to {to} of {count}|{count} records|One record",
                    first: 'First',
                    last: 'Last',
                    filter: "",
                    filterPlaceholder: "Search",
                    limit: "",
                    page: "Page:",
                    noResults: "No matching records",
                    filterBy: "Filter by {column}",
                    loading: 'Loading...',
                    defaultOption: 'Select {column}',
                    columns: 'Columns'
                },
                skin: 'table table-striped table-bordered table-hovered',
                sortIcon:
                {
                    base: 'fas chevron-margin',
                    up: 'fa-chevron-up',
                    down: 'fa-chevron-down',
                    is: ''
                },
            }
        };
    },
    beforeRouteEnter(to, from, next) {
        managerRouteNavigate();
        next();
    },
    beforeRouteUpdate(to, from, next) {
        managerRouteNavigate();
        next();
    },
    methods: {
        timeago: function (x) {
            return moment(x).fromNow();
        },
        persistModifications: function (x) {
            putItem(x, null, null);
        },
        singleDelete: function (x) {
            let delArr = [];
            delArr.push({ MediaId: x.mediaId, Version: -1 });
            scheduleDelete(delArr, function () {
                updatePendingDelete();
                this.$refs.mediaManager.getData();
            }, null);
        },
        bulkReconvert: function (x) {
            this.$bvToast.toast(`Scheduling reconversion in the UI is not yet implemented.`, {
                title: 'Not Implemented',
                autoHideDelay: 5000,
                appendToast: true
            });
        },
        bulkDelete: function (x) {
            scheduleDelete(this.$refs.mediaManager._data.data.map(function (x) {
                return { mediaId: x.mediaId, version: -1 };
            }), function () {
                updatePendingDelete();
                this.$refs.mediaManager.getData();
            });
        }
    },
    template: '#media-manager-template'
};

Vue.use(VueTables.ServerTable);

const store = new Vuex.Store({
    state: {
        waiting: [],
        progress: [],
        complete: [],
        pendingDelete: []
    },
    mutations: {
        setWaiting(store, x) {
            store.waiting = x;
        },
        setProgress(store, x) {
            store.progress = x;
        },
        setComplete(store, x) {
            store.complete = x;
        },
        setPendingDelete(store, x) {
            store.pendingDelete = x;
        }
    }
});

let cuid = 0;
Vue.mixin({
    beforeCreate() {
        this.cuid = cuid.toString();
        cuid++;
    },
})

const mainVue = new Vue({
    data: {

    },
    store,
    router: new VueRouter({
        routes: [
            {
                path: '/conversionStatus',
                component: conversionStatusComponent,
                props: true
            },
            {
                path: '/pendingDeletions',
                component: pendingDeletionsComponent
            },
            {
                path: '/mediaManager',
                component: mediaManagerComponent,
                props: { name: 'mediaManager' }
            }
        ]
    })
}).$mount('#app');