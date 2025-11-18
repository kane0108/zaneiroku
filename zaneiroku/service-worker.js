// ============================================
//  Fuuma Zaneiroku - Unified Dev/Release SW
//  スマホデバッグ最優先・即時更新・完全オフライン
// ============================================

const CACHE_NAME = "fz-cache-v1";

// ------------------------------------------------------
// ★ インストール時：即時適用（skipWaiting）
// ------------------------------------------------------
self.addEventListener("install", event => {
    console.log("[SW] Installing...");

    self.skipWaiting();

    // 必要最低限のアセット（初回ロードに絶対必要なもの）
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => {
            return cache.addAll([
                '/',
                '/index.html',
                '/manifest.json',
                '/icon-192.png',
                '/icon-512.png',
                '/css/app.css',
                '/_framework/blazor.webassembly.js',
                '/_framework/dotnet.wasm',
                // DLLや画像は動的にフェッチされるため追加不要（後述）
            ]);
        })
    );
});

// ------------------------------------------------------
// ★ Activate：即座に全クライアントへ反映
// ------------------------------------------------------
self.addEventListener("activate", event => {
    console.log("[SW] Activated");
    event.waitUntil(clients.claim());
});

// ------------------------------------------------------
// ★ Fetch：基本は最新のネットを優先 → 失敗したらキャッシュ
//
// これにより：
//   - PCデバッグ停止 → ネット失敗 → キャッシュから動作（ゲーム継続）
//   - 開発中→スマホに即時更新（skipWaiting）
//   - .pdb fetch error が出ない（キャッシュの古さ問題が消滅）
// ------------------------------------------------------
self.addEventListener("fetch", event => {
    const req = event.request;

    event.respondWith(
        fetch(req)
            .then(response => {
                // 成功したリソースはキャッシュ更新
                let clone = response.clone();
                caches.open(CACHE_NAME).then(cache => cache.put(req, clone));
                return response;
            })
            .catch(() => {
                // ネットに失敗 → キャッシュから取得（オフライン動作）
                return caches.match(req);
            })
    );
});

// ------------------------------------------------------
// ★ Message Handler：手動更新用（必要なら）
// ------------------------------------------------------
self.addEventListener("message", event => {
    if (event.data === "SKIP_WAITING") {
        self.skipWaiting();
    }
});
