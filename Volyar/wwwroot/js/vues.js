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

var pendingDeletionsComponent = {
    computed: {
        pendingDelete() {
            return this.$store.state.pendingDelete;
        }
    },
    components: {

    },
    methods: {

    },
    template: '#pending-deletions-template'
};

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
            { path: '/pendingDeletions', component: pendingDeletionsComponent }
        ]
    })
}).$mount('#app');