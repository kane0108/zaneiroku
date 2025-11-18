// ===============================
// Blazor WebAssembly SW (GitHub Pages対応版)
// ===============================

const CACHE_VERSION = "v1.0.0";
const CACHE_NAME = `blazor-cache-${CACHE_VERSION}`;

// Blazor の基本ファイル（GitHub Pages は相対パス）
const OFFLINE_ASSETS = [
    "./",
    "index.html",
    "manifest.json",
    "favicon.png",
];

// インストール（キャッシュ）
self.addEventListener("install", (event) => {
    console.log("[SW] Install");
    event.waitUntil(
        caches.open(CACHE_NAME).then((cache) => {
            return cache.addAll(OFFLINE_ASSETS);
        })
    );
    self.skipWaiting();
});

// アクティベート（古いキャッシュ削除）
self.addEventListener("activate", (event) => {
    console.log("[SW] Activate");
    event.waitUntil(
        caches.keys().then((keys) =>
            Promise.all(keys.map((key) => {
                if (key !== CACHE_NAME) {
                    return caches.delete(key);
                }
            }))
        )
    );
});

// フェッチ（GitHub Pages 対応）
self.addEventListener("fetch", (event) => {
    const req = event.request;

    // Blazor の _framework ファイルはキャッシュ優先
    if (req.url.includes("_framework")) {
        event.respondWith(
            caches.open(CACHE_NAME).then(async (cache) => {
                const cached = await cache.match(req);
                if (cached) return cached;

                try {
                    const fresh = await fetch(req);
                    cache.put(req, fresh.clone());
                    return fresh;
                } catch {
                    return cached; // 最終 fallback
                }
            })
        );
        return;
    }

    // 通常ファイル（オフライン fallback）
    event.respondWith(
        fetch(req).catch(() => caches.match(req).then((res) => res ?? caches.match("index.html")))
    );
});
