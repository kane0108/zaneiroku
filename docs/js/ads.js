// =======================================
// ads.js  (IMA SDK / Rewarded Test Ad)
// =======================================

window.ads = window.ads || {};

(function () {

    let initialized = false;
    let sdkReady = false;

    // -------------------------------
    // IMA SDK ロード
    // -------------------------------
    function loadImaSdk() {
        return new Promise(resolve => {
            if (window.google && google.ima) {
                resolve();
                return;
            }

            const s = document.createElement("script");
            s.src = "https://imasdk.googleapis.com/js/sdkloader/ima3.js";
            s.async = true;
            s.onload = () => {
                console.log("[ads] IMA SDK loaded");
                resolve();
            };
            s.onerror = () => {
                console.error("[ads] IMA SDK load failed");
                resolve(); // 落とさない
            };
            document.head.appendChild(s);
        });
    }

    // -------------------------------
    // 初期化（起動時に1回）
    // -------------------------------
    window.ads.initRewarded = async function () {
        if (initialized) return;
        initialized = true;

        await loadImaSdk();

        if (window.google && google.ima) {
            sdkReady = true;
            console.log("[ads] IMA SDK ready");
        } else {
            console.warn("[ads] IMA SDK not available");
        }
    };

    // -------------------------------
    // リワード広告表示
    // -------------------------------
    window.ads.showRewarded = function () {
        return new Promise(resolve => {

            console.log("[ads] showRewarded");

            if (!window.google || !google.ima) {
                resolve("failed");
                return;
            }

            // ---- DOM ----
            const container = document.createElement("div");
            container.style.position = "fixed";
            container.style.inset = "0";
            container.style.background = "black";
            container.style.zIndex = "999999";
            document.body.appendChild(container);

            const video = document.createElement("video");
            video.style.width = "100%";
            video.style.height = "100%";
            video.muted = true;
            video.autoplay = true;
            video.playsInline = true;
            video.setAttribute("webkit-playsinline", "true");
            video.load();                    // ★重要
            container.appendChild(video);

            // ---- IMA ----
            const adDisplayContainer =
                new google.ima.AdDisplayContainer(container, video);

            // ★ user gesture 直結で必須
            adDisplayContainer.initialize();

            const adsLoader =
                new google.ima.AdsLoader(adDisplayContainer);

            adsLoader.addEventListener(
                google.ima.AdErrorEvent.Type.AD_ERROR,
                e => {
                    console.error("[ads] IMA error", e.getError());
                    cleanup("failed");
                }
            );

            adsLoader.addEventListener(
                google.ima.AdsManagerLoadedEvent.Type.ADS_MANAGER_LOADED,
                e => {
                    const adsManager = e.getAdsManager(video);

                    adsManager.addEventListener(
                        google.ima.AdEvent.Type.COMPLETE,
                        () => cleanup("completed")
                    );
                    adsManager.addEventListener(
                        google.ima.AdEvent.Type.SKIPPED,
                        () => cleanup("skipped")
                    );
                    adsManager.addEventListener(
                        google.ima.AdErrorEvent.Type.AD_ERROR,
                        () => cleanup("failed")
                    );

                    adsManager.init(
                        container.offsetWidth,
                        container.offsetHeight,
                        google.ima.ViewMode.FULLSCREEN
                    );
                    adsManager.start();
                }
            );

            // ---- AdsRequest（★ここが致命的に足りてなかった）----
            const request = new google.ima.AdsRequest();
            request.adTagUrl =
                "https://pubads.g.doubleclick.net/gampad/ads?" +
                "env=vp&output=vast&unviewed_position_start=1&" +
                "correlator=" + Date.now();

            request.setAdWillAutoPlay(true);   // ★必須
            request.setAdWillPlayMuted(true); // ★必須

            adsLoader.requestAds(request);

            function cleanup(result) {
                container.remove();
                console.log("[ads] finished:", result);
                resolve(result);
            }
        });
    };

    // Android ネイティブ広告を呼ぶ
    window.showRewardedAd = function () {
        if (window.Android && window.Android.showRewarded) {
            window.Android.showRewarded();
        } else {
            console.warn("Android bridge not found");
            // Web/PWA ではここは呼ばれない
        }
    };

    // Android → Web → Blazor
    window.onRewardedResult = function (result) {
        if (window.DotNet) {
            DotNet.invokeMethodAsync(
                "BlazorApp",   // ★ あなたの Assembly 名
                "OnRewardedResult",
                result
            );
        }
    };

})();
