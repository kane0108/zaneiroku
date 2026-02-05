window.ads = window.ads || {};

(function () {

    let initialized = false;
    let adDisplayContainer;
    let adsLoader;
    let adsManager;

    function loadImaSdk() {
        return new Promise(resolve => {
            if (window.google && google.ima) {
                resolve();
                return;
            }

            const s = document.createElement("script");
            s.src = "https://imasdk.googleapis.com/js/sdkloader/ima3.js";
            s.onload = resolve;
            document.head.appendChild(s);
        });
    }

    window.ads.initRewarded = async function () {
        if (initialized) return;
        initialized = true;

        await loadImaSdk();
        console.log("[ads] IMA SDK ready");
    };

    window.ads.showRewarded = function () {
        return new Promise(resolve => {

            // 広告用DOM
            const container = document.createElement("div");
            container.style.position = "fixed";
            container.style.inset = "0";
            container.style.zIndex = "999999";
            container.style.background = "black";
            document.body.appendChild(container);

            const video = document.createElement("video");
            video.style.width = "100%";
            video.style.height = "100%";
            container.appendChild(video);

            adDisplayContainer =
                new google.ima.AdDisplayContainer(container, video);

            adsLoader = new google.ima.AdsLoader(adDisplayContainer);

            adsLoader.addEventListener(
                google.ima.AdsManagerLoadedEvent.Type.ADS_MANAGER_LOADED,
                e => {
                    adsManager = e.getAdsManager(video);

                    adsManager.addEventListener(
                        google.ima.AdEvent.Type.COMPLETE,
                        () => finish("completed")
                    );
                    adsManager.addEventListener(
                        google.ima.AdEvent.Type.SKIPPED,
                        () => finish("skipped")
                    );

                    adsManager.init(
                        container.offsetWidth,
                        container.offsetHeight,
                        google.ima.ViewMode.FULLSCREEN
                    );
                    adsManager.start();
                }
            );

            // ★ Google公式テスト広告タグ
            const request = new google.ima.AdsRequest();
            request.adTagUrl =
                "https://pubads.g.doubleclick.net/gampad/ads?" +
                "sz=640x480&iu=/6355419/Travel/Europe/France/Paris&" +
                "ciu_szs=300x250&impl=s&gdfp_req=1&env=vp&" +
                "output=vast&unviewed_position_start=1&" +
                "cust_params=deployment%3Ddevsite&" +
                "correlator=" + Date.now();

            adDisplayContainer.initialize();
            adsLoader.requestAds(request);

            function finish(result) {
                container.remove();
                resolve(result);
            }
        });
    };

})();
