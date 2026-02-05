// =======================================
// ads.js  (PRODUCTION-READY TEMPLATE)
// Blazor WebAssembly / PWA / Rewarded Ad
// =======================================

// グローバル名前空間確保
window.ads = window.ads || {};

(function () {

    let initialized = false;
    let adUnitId = null;
    let sdkLoaded = false;

    // -------------------------------
    // SDK ロード（Google GPT / IMA 想定）
    // -------------------------------
    function loadAdSdk() {
        return new Promise(resolve => {
            if (sdkLoaded) {
                resolve();
                return;
            }

            // ▼ 実運用ではここを差し替える
            // Google GPT（Web広告の公式ルート）
            const script = document.createElement("script");
            script.src = "https://securepubads.g.doubleclick.net/tag/js/gpt.js";
            script.async = true;

            script.onload = () => {
                console.log("[ads] Ad SDK loaded");
                sdkLoaded = true;
                resolve();
            };

            script.onerror = () => {
                console.warn("[ads] Ad SDK failed to load");
                resolve(); // 失敗してもアプリは落とさない
            };

            document.head.appendChild(script);
        });
    }

    // -------------------------------
    // 初期化（アプリ起動時に1回）
    // -------------------------------
    window.ads.initRewarded = async function (_adUnitId) {
        if (initialized) {
            console.log("[ads] already initialized");
            return;
        }

        initialized = true;
        adUnitId = _adUnitId;

        console.log("[ads] initRewarded:", adUnitId);

        // SDKロード（DEBUGでも呼ばれてOK）
        await loadAdSdk();

        // SDKが無い場合でも安全に抜ける
        if (!window.googletag) {
            console.log("[ads] SDK not available (DEBUG or blocked)");
            return;
        }

        window.googletag = window.googletag || { cmd: [] };
        googletag.cmd.push(() => {
            console.log("[ads] SDK initialized");
        });
    };

    // -------------------------------
    // リワード広告表示
    // 戻り値:
    //   completed / skipped / failed
    // -------------------------------
    window.ads.showRewarded = function () {
        return new Promise(resolve => {

            console.log("[ads] showRewarded");

            // 初期化 or SDKなし → 失敗
            if (!initialized || !window.googletag) {
                console.warn("[ads] not ready");
                resolve("failed");
                return;
            }

            // =============================
            // ▼ ここから「Web用 擬似Rewarded」
            // （実広告SDKに差し替え可能）
            // =============================

            const overlay = document.createElement("div");
            overlay.style.position = "fixed";
            overlay.style.left = "0";
            overlay.style.top = "0";
            overlay.style.width = "100%";
            overlay.style.height = "100%";
            overlay.style.background = "rgba(0,0,0,0.85)";
            overlay.style.zIndex = "999999";
            document.body.appendChild(overlay);

            // 閉じる（スキップ）
            const closeBtn = document.createElement("button");
            closeBtn.textContent = "×";
            closeBtn.style.position = "absolute";
            closeBtn.style.right = "16px";
            closeBtn.style.top = "16px";
            closeBtn.style.fontSize = "24px";
            closeBtn.onclick = () => {
                cleanup();
                resolve("skipped");
            };
            overlay.appendChild(closeBtn);

            // 擬似「視聴完了タイマー」
            // 実広告では onRewarded イベントに置き換える
            const COMPLETE_TIME = 30000; // 30秒相当
            const timer = setTimeout(() => {
                cleanup();
                resolve("completed");
            }, COMPLETE_TIME);

            function cleanup() {
                clearTimeout(timer);
                overlay.remove();
            }

            console.log("[ads] rewarded started");
        });
    };

})();
