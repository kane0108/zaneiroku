
const imageCache = {};
const processedImageCache = {}; // ★ 透過色処理済み画像キャッシュ

const tintCanvas = document.createElement("canvas");
const tintCtx = tintCanvas.getContext("2d");

let lastFrameTime = performance.now();
let frameCount = 0;
let fps = 0;

// reportAlive がまだ無い復帰直後の衝突を避ける
function safeReportAlive() {
    if (typeof window.reportAlive === "function") {
        try {
            window.reportAlive();
        } catch (e) {
            console.warn("[game.js] reportAlive() error:", e);
        }
    } else {
        // 復帰直後や race condition 時はここに入る
        // これは正常（ロード順の一時的不整合）
        // モニター側が自動でリロードしてくれる
        // console.warn("[game.js] reportAlive not defined");
    }
}

window.initializeCanvas = function (canvasId) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) {
        console.warn("Canvas not found:", canvasId);
        return;
    }
    const ctx = canvas.getContext("2d");
    ctx.imageSmoothingEnabled = false;
};

window.drawSprite = async function (options) {
    const {
        canvasId, imagePath, x, y, width, height,
        sx, sy, sw, sh, mirror,
        transparentColor, threshold,
        opacity = 1.0,
        tint
    } = options;

    const canvas = document.getElementById(canvasId);
    const ctx = canvas.getContext("2d");

    // === 矩形デバッグ枠用 ===
    if (options.type === "rect") {
        ctx.save();
        ctx.globalAlpha = options.opacity ?? 1.0;
        ctx.lineWidth = options.lineWidth ?? 1;
        ctx.strokeStyle = options.strokeColor ?? "#FF0000";
        ctx.strokeRect(options.x, options.y, options.width, options.height);
        ctx.restore();
        return; // スプライト処理には進まない
    }

    // サイズ未確定なら描画スキップ
    if (width <= 0 || height <= 0 || sw <= 0 || sh <= 0) {
        return;
    }
    // ✅ 画像キャッシュ（透過色処理済みImageを返す）
    const img = await getProcessedImage(imagePath, transparentColor, threshold);

    // 画像がまだロードされていない/サイズ0ならスキップ
    if (!img || img.width === 0 || img.height === 0) {
        return;
    }

    ctx.save();
    ctx.globalAlpha = opacity;
    const doTint = (tint && tint !== "#FFFFFF");

    // === 鏡像描画 ===
    if (mirror) {
        ctx.translate(x + width / 2, y);
        ctx.scale(-1, 1);

        if (doTint) {
            tintCanvas.width = width;
            tintCanvas.height = height;
            tintCtx.clearRect(0, 0, width, height);

            tintCtx.drawImage(img, sx, sy, sw, sh, 0, 0, width, height);
            tintCtx.globalCompositeOperation = "multiply";
            tintCtx.fillStyle = tint;
            tintCtx.fillRect(0, 0, width, height);

            tintCtx.globalCompositeOperation = "destination-in";
            tintCtx.drawImage(img, sx, sy, sw, sh, 0, 0, width, height);

            ctx.drawImage(tintCanvas, -width / 2, 0, width, height);
        } else {
            ctx.drawImage(img, sx, sy, sw, sh, -width / 2, 0, width, height);
        }
    }
    // === 通常描画 ===
    else {
        if (doTint) {
            tintCanvas.width = width;
            tintCanvas.height = height;
            tintCtx.clearRect(0, 0, width, height);

            tintCtx.drawImage(img, sx, sy, sw, sh, 0, 0, width, height);
            tintCtx.globalCompositeOperation = "multiply";
            tintCtx.fillStyle = tint;
            tintCtx.fillRect(0, 0, width, height);

            tintCtx.globalCompositeOperation = "destination-in";
            tintCtx.drawImage(img, sx, sy, sw, sh, 0, 0, width, height);

            ctx.drawImage(tintCanvas, x, y, width, height);
        } else {
            ctx.drawImage(img, sx, sy, sw, sh, x, y, width, height);
        }
    }

    ctx.restore();
};


window.clearCanvas = function (canvasId) {
    const canvas = document.getElementById(canvasId);
    if (canvas && canvas.getContext) {
        const ctx = canvas.getContext('2d');
        ctx.clearRect(0, 0, canvas.width, canvas.height); // ← 元に戻す
    }
};


window.preventTouchDefaults = function (elementId) {
    const el = document.getElementById(elementId);
    if (!el) return;

    ["touchstart", "touchend", "touchmove"].forEach(eventType => {
        el.addEventListener(eventType, function (e) {
            if (e.cancelable) e.preventDefault();
        }, { passive: false });
    });
};

// 任意のイベントを強制キャンセル（Blazorから個別に使う場合）
window.cancelEvent = function (eventName) {
    document.addEventListener(eventName, function (e) {
        if (e.cancelable) e.preventDefault();
    }, { passive: false });
};

window.setupCanvasInput = function (canvasId, dotnetHelper) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    function getAdjustedCoords(event) {
        const rect = canvas.getBoundingClientRect();

        let clientX, clientY;
        if (event.touches && event.touches.length > 0) {
            clientX = event.touches[0].clientX;
            clientY = event.touches[0].clientY;
        } else {
            clientX = event.clientX;
            clientY = event.clientY;
        }

        const scaleX = canvas.width / rect.width;
        const scaleY = canvas.height / rect.height;

        const x = (clientX - rect.left) * scaleX;
        const y = (clientY - rect.top) * scaleY;

        return { x, y };
    }

    function handleInput(e) {
        const pos = getAdjustedCoords(e);
        dotnetHelper.invokeMethodAsync("OnCanvasClick", pos.x, pos.y);
        if (e.cancelable) e.preventDefault();
    }

    canvas.addEventListener("click", handleInput);
    canvas.addEventListener("touchstart", handleInput, { passive: false });
};

window.adjustCanvasSize = function (canvasId) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    const rect = canvas.getBoundingClientRect();

    // 論理サイズを見た目に合わせる
    canvas.width = rect.width;
    canvas.height = rect.height;

    // ctx.scale は行わない
};

window.getCanvasPointerPosition = function (canvasId, clientX, clientY) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return null;

    const rect = canvas.getBoundingClientRect();

    // CSSサイズ（表示）と属性サイズ（論理ピクセル）の比率
    const scaleX = canvas.width / rect.width;
    const scaleY = canvas.height / rect.height;

    return {
        x: (clientX - rect.left) * scaleX,
        y: (clientY - rect.top) * scaleY
    };
};

window.getCanvasRect = function (canvasId) {
    const el = document.getElementById(canvasId);
    if (el) {
        const rect = el.getBoundingClientRect();
        return {
            left: rect.left,
            top: rect.top,
            width: rect.width,
            height: rect.height
        };
    }
    return null;
};

window.drawText = function (options) {
    const canvas = document.getElementById(options.canvasId);
    const ctx = canvas.getContext("2d");

    ctx.save();

    ctx.font = options.font || "20px sans-serif";
    ctx.fillStyle = options.textColor || "#FFFFFF";
    ctx.textBaseline = "top";

    // ★ 右寄せ/中央寄せ対応
    ctx.textAlign = options.textAlign || "left";

    ctx.fillText(options.text, options.x, options.y);

    ctx.restore();
};

window.drawTextWithStretchedBackground = async function (params) {
    const canvas = document.getElementById(params.canvasId);
    const ctx = canvas.getContext("2d");
    // ★ 非同期で透過済み画像を取得
    const img = await getProcessedImage(params.imagePath, params.transparentColor, params.threshold);
    if (!ctx || !img) return null;

    if (img.width === 0 || img.height === 0) {
        return;
    }

    ctx.save();

    const opacity = (params.opacity !== undefined) ? params.opacity : 1.0;
    ctx.globalAlpha = opacity;

    ctx.font = params.font || "20px sans-serif";
    ctx.fillStyle = params.color || "#FFFFFF";

    const lines = params.text.split('\n');
    const fontSize = parseInt(params.font) || 20;
    const lineHeight = fontSize * 1.2;  // ← Blazor 側と完全一致させる
    const textHeight = lineHeight * lines.length;
    const textWidth = Math.max(...lines.map(line => ctx.measureText(line).width));

    const paddingX = params.paddingX || 0;
    const paddingY = params.paddingY || 0;

    const bgWidth = textWidth + paddingX * 2;
    const bgHeight = textHeight + paddingY * 2;

    const drawX = params.posX;
    const drawY = params.posY;

    // 背景描画
    ctx.drawImage(
        img,
        params.sx, params.sy, params.sw, params.sh,
        drawX, drawY,
        bgWidth, bgHeight
    );

    // テキスト描画（中央揃え）
    ctx.textAlign = 'center';
    ctx.textBaseline = 'top';

    const centerX = drawX + bgWidth / 2;
    const startY = drawY + paddingY;

    for (let i = 0; i < lines.length; i++) {
        ctx.fillText(lines[i], centerX, startY + i * lineHeight);
    }

    ctx.restore();

    // ★ 正確な描画サイズを返す
    return {
        width: bgWidth,
        height: bgHeight
    };
};

window.measureTextWidth = function (text, font) {
    const canvas = document.createElement("canvas");
    const ctx = canvas.getContext("2d");
    ctx.font = font;
    return ctx.measureText(text).width;
};

window.getImageSize = function (imagePath) {
    return new Promise((resolve, reject) => {
        const img = new Image();
        img.src = imagePath;
        img.onload = () => {
            resolve({ Width: img.width, Height: img.height });
        };
        img.onerror = reject;
    });
};

window.drawFadeRect = (params) => {
    const canvas = document.getElementById(params.canvasId);
    if (!canvas) return;
    const ctx = canvas.getContext("2d");
    ctx.save();
    ctx.globalAlpha = params.opacity;
    ctx.fillStyle = "black";
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    ctx.restore();
};

window.checkPixelHit = async function (params) {
    let img = imageCache[params.imagePath];
    if (!img) {
        img = new Image();
        img.src = params.imagePath;
        await new Promise((resolve) => { img.onload = resolve; });
        imageCache[params.imagePath] = img;
    }

    const offCanvas = document.createElement("canvas");
    offCanvas.width = params.sw;
    offCanvas.height = params.sh;
    const ctx = offCanvas.getContext("2d");
    ctx.drawImage(img, params.sx, params.sy, params.sw, params.sh, 0, 0, params.sw, params.sh);

    const imageData = ctx.getImageData(0, 0, params.sw, params.sh).data;
    const px = Math.floor(params.px / params.scaleX);
    const py = Math.floor(params.py / params.scaleY);

    if (px < 0 || py < 0 || px >= params.sw || py >= params.sh)
        return false;

    const idx = (py * params.sw + px) * 4;
    const r = imageData[idx];
    const g = imageData[idx + 1];
    const b = imageData[idx + 2];
    const a = imageData[idx + 3];

    // PNG透過
    if (a <= 10) return false;

    // 透過色指定
    if (params.transparentColor != null) {
        const rT = (params.transparentColor >> 16) & 0xFF;
        const gT = (params.transparentColor >> 8) & 0xFF;
        const bT = (params.transparentColor) & 0xFF;

        const diff = Math.abs(r - rT) + Math.abs(g - gT) + Math.abs(b - bT);
        if (diff <= (params.threshold ?? 0)) return false;
    }

    return true;
};

window.getProcessedImage = async function (path, transparentColor, threshold) {
    const key = transparentColor != null ? `${path}_tc_${transparentColor}_${threshold}` : path;

    // ★ 1. 透過処理済みキャッシュを最優先
    if (processedImageCache[key]) return processedImageCache[key];

    // ★ 2. 元画像キャッシュから探す（なければロード）
    let img = imageCache[path];
    if (!img) {
        img = new Image();
        img.src = path;
        await new Promise(r => img.onload = r);
        imageCache[path] = img;
    }

    // ★ 3. 透過色処理あり → 加工して processedImageCache に保存
    if (transparentColor != null) {
        const tmpCanvas = document.createElement("canvas");
        tmpCanvas.width = img.width;
        tmpCanvas.height = img.height;
        const tmpCtx = tmpCanvas.getContext("2d");
        tmpCtx.drawImage(img, 0, 0);

        const imageData = tmpCtx.getImageData(0, 0, img.width, img.height);
        const data = imageData.data;

        const argb = transparentColor >>> 0;
        const rT = (argb >> 16) & 0xFF;
        const gT = (argb >> 8) & 0xFF;
        const bT = argb & 0xFF;

        for (let i = 0; i < data.length; i += 4) {
            const r = data[i], g = data[i + 1], b = data[i + 2];
            const diff = Math.abs(r - rT) + Math.abs(g - gT) + Math.abs(b - bT);
            if (diff <= (threshold ?? 0)) {
                data[i + 3] = 0; // 透明化
            }
        }

        tmpCtx.putImageData(imageData, 0, 0);

        const processedImg = new Image();
        processedImg.src = tmpCanvas.toDataURL();
        await new Promise(r => processedImg.onload = r);

        processedImageCache[key] = processedImg;
        return processedImg;
    }

    // ★ 4. 透過色なし → 元画像をそのまま processedImageCache に登録
    processedImageCache[key] = img;
    return img;
};



window.updateFpsCounter = function () {
    const now = performance.now();
    frameCount++;

    if (now - lastFrameTime >= 1000) {
        fps = frameCount;
        frameCount = 0;
        lastFrameTime = now;

        const fpsElement = document.getElementById("fpsCounter");
        if (fpsElement) {
            fpsElement.innerText = `FPS: ${fps}`;
        }
    }

    // ★ 追加：ページ死活報告
    window.safeReportAlive();
};

window.drawBatch = async function (commands) {
    const canvas = document.getElementById("gameCanvas");
    const ctx = canvas.getContext("2d");
    const results = [];

    // zIndex順にソート
    commands.sort((a, b) => (a.zIndex || 0) - (b.zIndex || 0));

    for (const cmd of commands) {
        let result = null;
        const type = (cmd.type || cmd.Type || "").toLowerCase();

        if (type === "sprite") {
            await window.drawSprite(cmd);
        }
        else if (type === "textBg") {
            result = await drawTextWithStretchedBackground(cmd);
        }
        else if (type === "text") {
            ctx.save();
            ctx.globalAlpha = cmd.opacity ?? 1.0;
            ctx.font = cmd.font || "20px sans-serif";
            ctx.fillStyle = cmd.textColor || "#FFFFFF";
            ctx.textAlign = cmd.textAlign || "left";
            ctx.textBaseline = "top";
            ctx.fillText(cmd.text, cmd.x, cmd.y);
            ctx.restore();
        }
        else if (type === "fadeRect") {
            ctx.save();
            ctx.globalAlpha = cmd.opacity;
            ctx.fillStyle = "black";
            ctx.fillRect(0, 0, canvas.width, canvas.height);
            ctx.restore();
        }
        else if (type === "rect") {
            ctx.save();
            ctx.globalAlpha = cmd.opacity ?? 1.0;
            ctx.lineWidth = cmd.lineWidth ?? 1;
            ctx.strokeStyle = cmd.strokeColor ?? "#FF0000";
            ctx.strokeRect(cmd.x, cmd.y, cmd.width, cmd.height);
            ctx.restore();
        }
        else if (type === "polygon") {
            window.drawPolygon(cmd); // ← 共通関数を呼び出すようにする
        }
        else if (cmd.type === "fillRect") {
            ctx.globalAlpha = cmd.opacity;
            ctx.fillStyle = cmd.fillColor;
            ctx.fillRect(cmd.x, cmd.y, cmd.width, cmd.height);
            ctx.globalAlpha = 1.0;
            ctx.restore();
        }

        results.push(result);
    }

    return results;
};


// images の配列を受け取り、全てロードして解決
window.preloadImages = function (imagePaths) {
    // ★ Array.isArray で検査してから配列化
    const arr = Array.isArray(imagePaths) ? imagePaths : Array.from(imagePaths);

    return Promise.all(arr.map(path => {
        return new Promise((resolve, reject) => {
            const img = new Image();
            img.src = path;
            img.onload = () => {
                imageCache[path] = img;
                resolve(true);
            };
            img.onerror = (e) => {
                console.error("Failed to preload:", path, e);
                reject(`Failed to preload: ${path}`);
            };

        });
    }));
};


// 画像をプリロードしてサイズを返す
window.preloadImagesWithSize = async function (paths) {
    const results = {};
    for (const path of paths) {
        const img = new Image();
        await new Promise((resolve, reject) => {
            img.onload = () => {
                results[path] = { width: img.width, height: img.height };
                resolve();
            };
            img.onerror = reject;
            img.src = path;
        });
    }
    return results;
};

window.drawPolygon = function (cmd) {
    const { canvasId, points, color, opacity = 1.0 } = cmd;
    const canvas = document.getElementById(canvasId);
    const ctx = canvas.getContext("2d");
    if (!points || points.length < 3) return;

    ctx.save();
    ctx.globalAlpha = opacity;
    ctx.fillStyle = color;
    ctx.beginPath();
    ctx.moveTo(points[0].x, points[0].y);
    for (let i = 1; i < points.length; i++) {
        ctx.lineTo(points[i].x, points[i].y);
    }
    ctx.closePath();
    ctx.fill();
    ctx.restore();
};

