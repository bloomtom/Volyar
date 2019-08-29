
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
    }
}

var qualityComponent = {
    props: {
        quality: {}
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

var queueComponent = {
    props: {
        items: {}
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
        'wait-queue-component': queueComponent,
        'progress-queue-component': queueComponent,
        'complete-queue-component': queueComponent
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
            checked: false
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
                confirmDelete(confirmed, function () {
                    updatePendingDelete();
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
        name: 'mediaManager'
    },
    data: function () {
        return {
            columns: ['mediaId', 'seriesName', 'name', 'seasonNumber', 'episodeNumber', 'version', 'createDate'],
            data: [],
            css: {
                table: {
                    tableClass: 'table table-striped table-bordered table-hovered',
                    loadingClass: 'loading',
                    ascendingIcon: 'glyphicon glyphicon-chevron-up',
                    descendingIcon: 'glyphicon glyphicon-chevron-down',
                    handleIcon: 'glyphicon glyphicon-menu-hamburger'
                },
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
                preserveState: true,
                uniqueKey: 'mediaId',
                headings: {
                    mediaId: 'Media ID',
                    seriesName: 'Series',
                    name: 'Episode Title',
                    seasonNumber: 'Season',
                    episodeNumber: 'Episode Number',
                    version: 'Version',
                    createDate: 'Created'
                },
                columnsClasses: {
                    mediaId: 'text-center'
                }
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
        edit: function (item) {
            alert(item.mediaId);
        },
        timeago: function (x) {
            return moment(x).fromNow();
        }
    },
    template: '#media-manager-template'
};

Vue.use(VueTables.ServerTable,
    {
        options:
        {
            perPage: 100
        },
        useVuex: true,
        theme: 'bootstrap4'
    });

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

const mainVue = new Vue({
    data: {

    },
    store,
    router: new VueRouter({
        routes: [
            { path: '/conversionStatus', component: conversionStatusComponent, props: true },
            { path: '/pendingDeletions', component: pendingDeletionsComponent },
            { path: '/mediaManager', component: mediaManagerComponent }
        ]
    })
}).$mount('#app');