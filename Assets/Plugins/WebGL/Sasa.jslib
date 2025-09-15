mergeInto(LibraryManager.library, (() => {
    let actx = null;
    let ptrbase = 0xff;
    const newPtr = () => ptrbase++;

    const audioManagers = {};
    const audioClips = {};
    const audioSfxs = {};
    const audioMusics = {};

    return {
        initialize: function () {
            actx = new AudioContext();
        },

        create_audio_manager: function () {
            if (!actx) this.initialize();
            
            const ptr = newPtr();
            audioManagers[ptr] = {
                sfxs: [],
                musics: [],
            };
            return ptr;
        },

        load_audio_clip_from_memory: function (dataPtr, dataLen) {
            if (!actx) this.initialize();
            
            const data = HEAPU8.slice(dataPtr, dataPtr + dataLen);
            const ptr = newPtr();
            const clip = new Audio();
            clip.src = URL.createObjectURL(new Blob([data]));
            audioClips[ptr] = clip;

            (async () => {
                const new_clip = await actx.decodeAudioData(data);
                audioClips[ptr] = new_clip;
            })();

            return ptr;
        },

        create_sfx: function (manager_ptr, clip_ptr) {
            if (!actx) this.initialize();
            
            const clip = audioClips[clip_ptr];
            const manager = audioManagers[manager_ptr];

            if (!clip) return 0;
            if (!manager) return 0;

            const ptr = newPtr();

            const sfx = {
                clip_ptr: clip_ptr,
                self_ptr: ptr
            };

            manager.sfxs.push(sfx);
            audioSfxs[ptr] = sfx;
        },

        create_music: function (manager_ptr, clip_ptr) {
            if (!actx) this.initialize();
            
            const clip = audioClips[clip_ptr];
            const manager = audioManagers[manager_ptr];

            if (!clip) return 0;
            if (!manager) return 0;

            const ptr = newPtr();

            const music = {
                clip_ptr: clip_ptr,
                self_ptr: ptr,
                instance: null,
                saved_position: 0
            };

            manager.musics.push(music);
            audioMusics[ptr] = music;
        },

        play_sfx: function (sfx_ptr, volume) {
            if (!actx) this.initialize();
            
            const sfx = audioSfxs[sfx_ptr];
            if (!sfx) return;
            const clip = audioClips[sfx.clip_ptr];

            if (clip instanceof Audio) {
                const temp = clip.cloneNode(true);
                temp.volume = volume;
                temp.play();
                temp.onended = () => {
                    temp.remove();
                }
            } else {
                const source = actx.createBufferSource();
                const gain_node = actx.createGain();
                gain_node.gain.value = volume;
                source.buffer = clip;
                source.connect(gain_node);
                gain_node.connect(actx.destination);
                source.start();
            }
        },

        play_music: function (music_ptr, volume) {
            if (!actx) this.initialize();
            
            const music = audioMusics[music_ptr];
            if (!music) return;
            const clip = audioClips[music.clip_ptr];

            if (clip instanceof Audio) {
                const temp = clip.cloneNode(true);
                temp.volume = volume;
                temp.play();
                music.instance = temp;
            } else {
                const source = actx.createBufferSource();
                const gain_node = actx.createGain();
                gain_node.gain.value = volume;
                source.buffer = clip;
                source.connect(gain_node);
                gain_node.connect(actx.destination);
                source.start();
                source.gain_node = gain_node;
                source.start_time = actx.currentTime;
                music.instance = source;
            }
        },

        pause_music: function (music_ptr) {
            if (!actx) this.initialize();
            
            const music = audioMusics[music_ptr];
            if (!music) return;
            const ins = music.instance;
            
            if (ins instanceof Audio) {
                ins.pause();
            } else {
                ins.stop();
                music.saved_position = ins.currentTime;
            }
        },

        is_music_paused: function (music_ptr) {
            if (!actx) this.initialize();
            
            const music = audioMusics[music_ptr];
            if (!music) return false;
            const ins = music.instance;

            if (ins instanceof Audio) {
                return ins.paused;
            } else {
                return ins.playbackState === 2;
            }
        },

        seek_music: function (music_ptr, time) {

        },

        set_music_volume: function (music_ptr, volume) {
            if (!actx) this.initialize();
            
            const music = audioMusics[music_ptr];
            if (!music) return;
            const ins = music.instance;

            if (ins instanceof Audio) {
                ins.volume = volume;
            } else {
                ins.gain_node.gain.value = volume;
            }
        },

        get_music_position: function (music_ptr) {
            if (!actx) this.initialize();
            
            const music = audioMusics[music_ptr];
            if (!music) return 0;
            const ins = music.instance;

            if (ins instanceof Audio) {
                return ins.currentTime;
            } else {
                return actx.currentTime - ins.start_time;
            }
        },

        get_audio_clip_duration: function (clip_ptr) {
            if (!actx) this.initialize();
            
            const clip = audioClips[clip_ptr];
            if (!clip) return 0;

            return clip.duration;
        },

        destroy_manager: function (manager_ptr) {
            
        },

        destroy_clip: function (clip_ptr) {
            
        },

        destroy_sfx: function (sfx_ptr) {
            
        },

        destroy_music: function (music_ptr) {
            
        },

        recover_if_needed: function () {
            
        }
    };
})());
