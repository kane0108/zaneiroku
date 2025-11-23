// ===============================
// Blazor WebAssembly SW (最終安定版)
// ===============================

const CACHE_VERSION = "v1.0.2";
const CACHE_NAME = `blazor-cache-${CACHE_VERSION}`;

const OFFLINE_ASSETS = [
    "./",
    "index.html",
    "manifest.json",
    "favicon.png"
];

// === Install ===
self.addEventListener("install", (event) => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => cache.addAll(OFFLINE_ASSETS))
    );
    self.skipWaiting();
});

// === Activate ===
self.addEventListener("activate", (event) => {
    event.waitUntil(
        (async () => {
            const keys = await caches.keys();
            await Promise.all(
                keys.map(key => {
                    if (key !== CACHE_NAME) return caches.delete(key);
                })
            );
            await self.clients.claim();
        })()
    );
});

// === Fetch ===
self.addEventListener("fetch", (event) => {
    const req = event.request;

    // ★ navigate（index.html）は必ずネット優先
    if (req.mode === "navigate") {
        event.respondWith(fetch(req).catch(() => caches.match("index.html")));
        return;
    }

    // ★ DLL/wasm は network-first
    if (req.url.includes("_framework")) {
        event.respondWith(
            (async () => {
                try {
                    const net = await fetch(req);
                    const cache = await caches.open(CACHE_NAME);
                    cache.put(req, net.clone());
                    return net;
                } catch {
                    return caches.match(req);
                }
            })()
        );
        return;
    }

    // ★ その他は通常（ネット優先 → キャッシュ）
    event.respondWith(
        fetch(req).catch(() =>
            caches.match(req).then(res => res || caches.match("index.html"))
        )
    );
});
