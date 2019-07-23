
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
            data: this.$store.state.mediaManager,
            css: {
                table: {
                    tableClass: 'table table-striped table-bordered table-hovered',
                    loadingClass: 'loading',
                    ascendingIcon: 'glyphicon glyphicon-chevron-up',
                    descendingIcon: 'glyphicon glyphicon-chevron-down',
                    handleIcon: 'glyphicon glyphicon-menu-hamburger',
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
                        last: '',
                    },
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
                },
                multiSorting: {
                    Series: [
                        {
                            column: 'seasonNumber',
                            matchDir: false
                        },
                        {
                            column: 'episodeNumber',
                            matchDir: false
                        },
                        {
                            column: 'version',
                            matchDir: false
                        }
                    ],
                    seasonNumber: [
                        {
                            column: 'episodeNumber',
                            matchDir: false
                        },
                        {
                            column: 'version',
                            matchDir: false
                        }
                    ]
                },
                filterable: ['mediaId', 'seriesName', 'name']
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

var testData = [
    {
        "createDate": "2019-05-11T16:14:43.5330692+00:00",
        "duration": "00:00:00",
        "indexHash": "1757F7A1FF1A58A8302A28703296E920",
        "indexName": "0B3E87F3_Demon.Slayer-.Kimetsu.no.Yaiba.S01E06.Swordsman.Accompanying.a.Demon.mpd",
        "libraryName": "Series",
        "mediaId": 1406,
        "name": "Swordsman Accompanying a Demon Swordsman Accompanying a Demon",
        "seasonNumber": 1,
        "episodeNumber": 7,
        "version": 0,
        "absoluteEpisodeNumber": 6,
        "seriesName": "Demon Slayer: Kimetsu no Yaiba",
        "imdbId": "tt9335498",
        "tvdbId": "348545",
        "tmdbId": null,
        "tvmazeId": "41469",
        "sourceHash": "0B3E87F35CA78785C744F973DE03F692",
        "sourceModified": "2019-05-11T16:14:01.8961385+00:00",
        "sourcePath": "/mnt/media/downloads/series/Demon Slayer- Kimetsu no Yaiba/S1/Demon.Slayer-.Kimetsu.no.Yaiba.S01E06.Swordsman.Accompanying.a.Demon.mkv",
        "metadata": "{\"encoder\":\"no_variable_data\",\"creation_time\":\"1970-01-01T00:00:00.000000Z\"}"
    },
    {
        "createDate": "2019-05-12T12:25:53.034617+00:00",
        "duration": "00:23:40.1280000",
        "indexHash": "2EC3E28DA789BFBE247D62543B15827A",
        "indexName": "E55053B1_Demon.Slayer-.Kimetsu.no.Yaiba.S01E06.Swordsman.Accompanying.a.Demon.mpd",
        "libraryName": "Series",
        "mediaId": 1407,
        "name": "Swordsman Accompanying a Demon",
        "seasonNumber": 1,
        "episodeNumber": 6,
        "version": 1,
        "absoluteEpisodeNumber": 6,
        "seriesName": "Demon Slayer: Kimetsu no Yaiba",
        "imdbId": "tt9335498",
        "tvdbId": "348545",
        "tmdbId": null,
        "tvmazeId": "41469",
        "sourceHash": "E55053B12BCC21132548D51EFF312FDC",
        "sourceModified": "2019-05-12T12:23:23.9368644+00:00",
        "sourcePath": "/mnt/media/downloads/series/Demon Slayer- Kimetsu no Yaiba/S1/Demon.Slayer-.Kimetsu.no.Yaiba.S01E06.Swordsman.Accompanying.a.Demon.mp4",
        "metadata": "{\"major_brand\":\"isom\",\"minor_version\":\"512\",\"compatible_brands\":\"isomiso2avc1mp41\",\"title\":\"Demon Slayer Kimetsu No Yaiba E06\",\"encoder\":\"Lavf57.56.101\"}"
    },
    {
        "createDate": "2019-05-12T16:22:31.6618582+00:00",
        "duration": "00:00:00",
        "indexHash": "53F02C9CFD15D61D5971B339486E8F54",
        "indexName": "D2967085_Fairy.Gone.S01E06.Episode.6.mpd",
        "libraryName": "Series",
        "mediaId": 1408,
        "name": "Episode 6",
        "seasonNumber": 1,
        "episodeNumber": 6,
        "version": 0,
        "absoluteEpisodeNumber": 6,
        "seriesName": "Fairy Gone",
        "imdbId": "tt9828600",
        "tvdbId": "359645",
        "tmdbId": null,
        "tvmazeId": "0",
        "sourceHash": "D296708508D9C17B966C5E7C9CC39968",
        "sourceModified": "2019-05-12T16:20:26.9437477+00:00",
        "sourcePath": "/mnt/media/downloads/series/Fairy gone/S1/Fairy.Gone.S01E06.Episode.6.mkv",
        "metadata": "{\"encoder\":\"libebml v1.3.7 + libmatroska v1.5.0\",\"creation_time\":\"2019-05-12T16:12:57.000000Z\"}"
    },
    {
        "createDate": "2019-05-13T02:27:27.5696628+00:00",
        "duration": "01:20:06.8020000",
        "indexHash": "EDB145149C8A2A16BBA4DCEAF279AFF3",
        "indexName": "FB81BA56_Game.of.Thrones.S08E05.TBA.mpd",
        "libraryName": "Series",
        "mediaId": 1409,
        "name": "TBA",
        "seasonNumber": 8,
        "episodeNumber": 5,
        "version": 0,
        "absoluteEpisodeNumber": 72,
        "seriesName": "Game of Thrones",
        "imdbId": "tt0944947",
        "tvdbId": "121361",
        "tmdbId": null,
        "tvmazeId": "82",
        "sourceHash": "FB81BA56C58BBEA2DA64AA49D54180E5",
        "sourceModified": "2019-05-13T01:53:29.4624074+00:00",
        "sourcePath": "/mnt/media/downloads/series/Game of Thrones/Game.of.Thrones.S08E05.TBA.mp4",
        "metadata": "{\"major_brand\":\"isom\",\"minor_version\":\"512\",\"compatible_brands\":\"isomiso2avc1mp41\",\"encoder\":\"Lavf57.56.101\"}"
    },
    {
        "createDate": "2019-05-13T14:18:13.085359+00:00",
        "duration": "00:00:00",
        "indexHash": "4613E43D4F93335D1791811F5999C635",
        "indexName": "39FB38BD_Dororo.(2019).S01E18.Episode.18.mpd",
        "libraryName": "Series",
        "mediaId": 1410,
        "name": "Episode 18",
        "seasonNumber": 1,
        "episodeNumber": 18,
        "version": 0,
        "absoluteEpisodeNumber": 18,
        "seriesName": "Dororo (2019)",
        "imdbId": "tt9458304",
        "tvdbId": "354167",
        "tmdbId": null,
        "tvmazeId": "40005",
        "sourceHash": "39FB38BD7506680265A529E750404F6E",
        "sourceModified": "2019-05-13T14:15:40.0846593+00:00",
        "sourcePath": "/mnt/media/downloads/series/Dororo (2019)/S1/Dororo.(2019).S01E18.Episode.18.mkv",
        "metadata": "{\"encoder\":\"no_variable_data\",\"creation_time\":\"1970-01-01T00:00:00.000000Z\"}"
    },
    {
        "createDate": "2019-05-14T17:42:32.7500088+00:00",
        "duration": "00:00:00",
        "indexHash": "077E1D3628A67AED15CE1940787EC9F0",
        "indexName": "A46A705E_One-Punch.Man.S02E06.The.Uprising.of.the.Monsters.mpd",
        "libraryName": "Series",
        "mediaId": 1411,
        "name": "The Uprising of the Monsters",
        "seasonNumber": 2,
        "episodeNumber": 6,
        "version": 0,
        "absoluteEpisodeNumber": 18,
        "seriesName": "One-Punch Man",
        "imdbId": "tt4508902",
        "tvdbId": "293088",
        "tmdbId": null,
        "tvmazeId": "4201",
        "sourceHash": "A46A705E8A1C0E51870E1093CE15B460",
        "sourceModified": "2019-05-14T17:40:55.6034449+00:00",
        "sourcePath": "/mnt/media/downloads/series/One-Punch Man/S2/One-Punch.Man.S02E06.The.Uprising.of.the.Monsters.mkv",
        "metadata": "{\"encoder\":\"no_variable_data\",\"creation_time\":\"1970-01-01T00:00:00.000000Z\"}"
    },
    {
        "createDate": "2019-05-12T12:25:53.034617+00:00",
        "duration": "00:23:40.1280000",
        "indexHash": "2EC3E28DA789BFBE247D62543B15827A",
        "indexName": "E55053B1_Demon.Slayer-.Kimetsu.no.Yaiba.S01E06.Swordsman.Accompanying.a.Demon.mpd",
        "libraryName": "Series",
        "mediaId": 1407,
        "name": "Swordsman Accompanying a Demon",
        "seasonNumber": 1,
        "episodeNumber": 6,
        "version": 1,
        "absoluteEpisodeNumber": 6,
        "seriesName": "Demon Slayer: Kimetsu no Yaiba",
        "imdbId": "tt9335498",
        "tvdbId": "348545",
        "tmdbId": null,
        "tvmazeId": "41469",
        "sourceHash": "E55053B12BCC21132548D51EFF312FDC",
        "sourceModified": "2019-05-12T12:23:23.9368644+00:00",
        "sourcePath": "/mnt/media/downloads/series/Demon Slayer- Kimetsu no Yaiba/S1/Demon.Slayer-.Kimetsu.no.Yaiba.S01E06.Swordsman.Accompanying.a.Demon.mp4",
        "metadata": "{\"major_brand\":\"isom\",\"minor_version\":\"512\",\"compatible_brands\":\"isomiso2avc1mp41\",\"title\":\"Demon Slayer Kimetsu No Yaiba E06\",\"encoder\":\"Lavf57.56.101\"}"
    },
    {
        "createDate": "2019-05-12T16:22:31.6618582+00:00",
        "duration": "00:00:00",
        "indexHash": "53F02C9CFD15D61D5971B339486E8F54",
        "indexName": "D2967085_Fairy.Gone.S01E06.Episode.6.mpd",
        "libraryName": "Series",
        "mediaId": 1408,
        "name": "Episode 6",
        "seasonNumber": 1,
        "episodeNumber": 6,
        "version": 0,
        "absoluteEpisodeNumber": 6,
        "seriesName": "Fairy Gone",
        "imdbId": "tt9828600",
        "tvdbId": "359645",
        "tmdbId": null,
        "tvmazeId": "0",
        "sourceHash": "D296708508D9C17B966C5E7C9CC39968",
        "sourceModified": "2019-05-12T16:20:26.9437477+00:00",
        "sourcePath": "/mnt/media/downloads/series/Fairy gone/S1/Fairy.Gone.S01E06.Episode.6.mkv",
        "metadata": "{\"encoder\":\"libebml v1.3.7 + libmatroska v1.5.0\",\"creation_time\":\"2019-05-12T16:12:57.000000Z\"}"
    },
    {
        "createDate": "2019-05-13T02:27:27.5696628+00:00",
        "duration": "01:20:06.8020000",
        "indexHash": "EDB145149C8A2A16BBA4DCEAF279AFF3",
        "indexName": "FB81BA56_Game.of.Thrones.S08E05.TBA.mpd",
        "libraryName": "Series",
        "mediaId": 1409,
        "name": "TBA",
        "seasonNumber": 8,
        "episodeNumber": 5,
        "version": 0,
        "absoluteEpisodeNumber": 72,
        "seriesName": "Game of Thrones",
        "imdbId": "tt0944947",
        "tvdbId": "121361",
        "tmdbId": null,
        "tvmazeId": "82",
        "sourceHash": "FB81BA56C58BBEA2DA64AA49D54180E5",
        "sourceModified": "2019-05-13T01:53:29.4624074+00:00",
        "sourcePath": "/mnt/media/downloads/series/Game of Thrones/Game.of.Thrones.S08E05.TBA.mp4",
        "metadata": "{\"major_brand\":\"isom\",\"minor_version\":\"512\",\"compatible_brands\":\"isomiso2avc1mp41\",\"encoder\":\"Lavf57.56.101\"}"
    },
    {
        "createDate": "2019-05-13T14:18:13.085359+00:00",
        "duration": "00:00:00",
        "indexHash": "4613E43D4F93335D1791811F5999C635",
        "indexName": "39FB38BD_Dororo.(2019).S01E18.Episode.18.mpd",
        "libraryName": "Series",
        "mediaId": 1410,
        "name": "Episode 18",
        "seasonNumber": 1,
        "episodeNumber": 18,
        "version": 0,
        "absoluteEpisodeNumber": 18,
        "seriesName": "Dororo (2019)",
        "imdbId": "tt9458304",
        "tvdbId": "354167",
        "tmdbId": null,
        "tvmazeId": "40005",
        "sourceHash": "39FB38BD7506680265A529E750404F6E",
        "sourceModified": "2019-05-13T14:15:40.0846593+00:00",
        "sourcePath": "/mnt/media/downloads/series/Dororo (2019)/S1/Dororo.(2019).S01E18.Episode.18.mkv",
        "metadata": "{\"encoder\":\"no_variable_data\",\"creation_time\":\"1970-01-01T00:00:00.000000Z\"}"
    },
    {
        "createDate": "2019-05-14T17:42:32.7500088+00:00",
        "duration": "00:00:00",
        "indexHash": "077E1D3628A67AED15CE1940787EC9F0",
        "indexName": "A46A705E_One-Punch.Man.S02E06.The.Uprising.of.the.Monsters.mpd",
        "libraryName": "Series",
        "mediaId": 1411,
        "name": "The Uprising of the Monsters",
        "seasonNumber": 2,
        "episodeNumber": 6,
        "version": 0,
        "absoluteEpisodeNumber": 18,
        "seriesName": "One-Punch Man",
        "imdbId": "tt4508902",
        "tvdbId": "293088",
        "tmdbId": null,
        "tvmazeId": "4201",
        "sourceHash": "A46A705E8A1C0E51870E1093CE15B460",
        "sourceModified": "2019-05-14T17:40:55.6034449+00:00",
        "sourcePath": "/mnt/media/downloads/series/One-Punch Man/S2/One-Punch.Man.S02E06.The.Uprising.of.the.Monsters.mkv",
        "metadata": "{\"encoder\":\"no_variable_data\",\"creation_time\":\"1970-01-01T00:00:00.000000Z\"}"
    },
    {
        "createDate": "2019-05-15T14:51:08.0672349+00:00",
        "duration": "00:00:00",
        "indexHash": "D8BEA8C6D4E63D452D3D968BA86A3511",
        "indexName": "8CE0CAC0_The.Rising.of.the.Shield.Hero.S01E19.The.Four.Cardinal.Heroes.mpd",
        "libraryName": "Series",
        "mediaId": 1412,
        "name": "The Four Cardinal Heroes",
        "seasonNumber": 1,
        "episodeNumber": 19,
        "version": 0,
        "absoluteEpisodeNumber": 19,
        "seriesName": "The Rising of the Shield Hero",
        "imdbId": "tt9529546",
        "tvdbId": "353712",
        "tmdbId": null,
        "tvmazeId": "40140",
        "sourceHash": "8CE0CAC0E35C09E29A46BB817B19505A",
        "sourceModified": "2019-05-15T14:50:15.1526477+00:00",
        "sourcePath": "/mnt/media/downloads/series/The Rising of the Shield Hero/S1/The.Rising.of.the.Shield.Hero.S01E19.The.Four.Cardinal.Heroes.mkv",
        "metadata": "{\"encoder\":\"no_variable_data\",\"creation_time\":\"1970-01-01T00:00:00.000000Z\"}"
    },
    {
        "createDate": "2019-05-16T20:05:46.9817475+00:00",
        "duration": "00:23:40.1700000",
        "indexHash": "0AD1891E0CC23D972495F325642DA0DC",
        "indexName": "FE8F2E9A_The.Rising.of.the.Shield.Hero.S01E19.The.Four.Cardinal.Heroes.mpd",
        "libraryName": "Series",
        "mediaId": 1413,
        "name": "The Four Cardinal Heroes",
        "seasonNumber": 1,
        "episodeNumber": 19,
        "version": 0,
        "absoluteEpisodeNumber": 19,
        "seriesName": "The Rising of the Shield Hero",
        "imdbId": "tt9529546",
        "tvdbId": "353712",
        "tmdbId": null,
        "tvmazeId": "40140",
        "sourceHash": "FE8F2E9A43856454137025AB1E80AA5E",
        "sourceModified": "2019-05-16T20:03:20.171008+00:00",
        "sourcePath": "/mnt/media/downloads/series/The Rising of the Shield Hero/S1/The.Rising.of.the.Shield.Hero.S01E19.The.Four.Cardinal.Heroes.mp4",
        "metadata": "{\"major_brand\":\"isom\",\"minor_version\":\"512\",\"compatible_brands\":\"isomiso2avc1mp41\",\"title\":\"The Rising Of The Shield Hero E19\",\"encoder\":\"Lavf57.56.101\"}"
    }
];

Vue.use(VueTables.ClientTable,
    {
        options:
        {
            perPage: 25
        },
        useVuex: true,
        theme: 'bootstrap4'
    });

const store = new Vuex.Store({
    state: {
        waiting: [],
        progress: [],
        complete: [],
        pendingDelete: [],
        mediaManager: testData
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