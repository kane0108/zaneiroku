// ===============================
// Blazor WebAssembly SW (GitHub Pages + PWA 完全対応版)
// ===============================

const CACHE_VERSION = "v1.0.1";
const CACHE_NAME = `blazor-cache-${CACHE_VERSION}`;

const OFFLINE_ASSETS = [
    "./",
    "index.html",
    "manifest.json",
    "favicon.png"
];

// === Install: 新 SW を即時アクティブ化 ===
self.addEventListener("install", (event) => {
    console.log("[SW] Install");

    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => cache.addAll(OFFLINE_ASSETS))
    );

    // ★ 新しい SW を待機無しで即時アクティブ化
    self.skipWaiting();
});

// === Activate: 古いキャッシュを確実に除去 & 即乗り換え ===
self.addEventListener("activate", (event) => {
    console.log("[SW] Activate");

    event.waitUntil(
        (async () => {
            const keys = await caches.keys();
            await Promise.all(
                keys.map(key => {
                    if (key !== CACHE_NAME) return caches.delete(key);
                })
            );
            // ★ クライアントを即このSWに切り替える
            await self.clients.claim();
        })()
    );
});

// === Fetch: DLL/wasm など Blazor 本体は network-first ===
//      → これが黒画面を防ぐ最重要ポイント
self.addEventListener("fetch", (event) => {
    const req = event.request;

    // ★ Blazor の DLL/wasm（_framework）は network-first が正解
    if (req.url.includes("_framework")) {
        event.respondWith(
            (async () => {
                try {
                    const network = await fetch(req);
                    const cache = await caches.open(CACHE_NAME);
                    cache.put(req, network.clone());
                    return network;
                } catch {
                    return caches.match(req);
                }
            })()
        );
        return;
    }

    // === 通常ファイル: ネットがダメならキャッシュから ===
    event.respondWith(
        fetch(req).catch(() =>
            caches.match(req).then(res => res || caches.match("index.html"))
        )
    );
});
