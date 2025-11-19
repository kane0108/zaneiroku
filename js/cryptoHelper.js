
window.fzCrypto = {
    async encrypt(plainText, key) {
        const enc = new TextEncoder();
        const iv = crypto.getRandomValues(new Uint8Array(12)); // GCM用 12byte IV
        const algo = { name: "AES-GCM", iv };
        const keyBytes = enc.encode(key.padEnd(32, "0")).slice(0, 32);
        const cryptoKey = await crypto.subtle.importKey("raw", keyBytes, algo, false, ["encrypt"]);
        const cipher = await crypto.subtle.encrypt(algo, cryptoKey, enc.encode(plainText));
        const result = new Uint8Array(iv.length + cipher.byteLength);
        result.set(iv, 0);
        result.set(new Uint8Array(cipher), iv.length);
        return btoa(String.fromCharCode(...result));
    },
    async decrypt(base64, key) {
        const data = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
        const iv = data.slice(0, 12);
        const cipher = data.slice(12);
        const algo = { name: "AES-GCM", iv };
        const enc = new TextEncoder();
        const keyBytes = enc.encode(key.padEnd(32, "0")).slice(0, 32);
        const cryptoKey = await crypto.subtle.importKey("raw", keyBytes, algo, false, ["decrypt"]);
        const plain = await crypto.subtle.decrypt(algo, cryptoKey, cipher);
        return new TextDecoder().decode(plain);
    }
};
