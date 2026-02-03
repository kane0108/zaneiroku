console.log("gameAudio.js START");

window.GameAudio = {
    se: {},
    volume: 0.6,

    loadSe(name, path) {
        console.log("loadSe", name, path);
        const audio = new Audio(path);
        audio.preload = "auto";
        audio.volume = this.volume;
        this.se[name] = audio;
    },

    playSe(name) {
        const audio = this.se[name];
        if (!audio) return;
        audio.currentTime = 0;
        audio.play();
    },

    setVolume(v) {
        this.volume = v;
        for (const k in this.se) {
            this.se[k].volume = v;
        }
    }
};

// ★ これを必ず最後に
window.__gameAudioReady = true;
console.log("gameAudio.js READY");
