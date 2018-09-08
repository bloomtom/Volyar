var qualityComponent = {
    props: {
        quality: {}
    },
    template: '#quality-template'
};

var queueComponent = {
    props: {
        items: {}
    },
    methods: {
        timeago: function (x) {
            return moment(x).fromNow();
        },
        progressInt: function (x) {
            return Math.round(x * 100);
        }
    },
    components: {
        'quality-component': qualityComponent
    },
    template: '#queue-template'
};

var mainVue = new Vue({
    el: '#vuebox',
    data: {
        waiting: [],
        inProgress: []
    },
    components: {
        'wait-queue-component': queueComponent,
        'progress-queue-component': queueComponent
    }
});