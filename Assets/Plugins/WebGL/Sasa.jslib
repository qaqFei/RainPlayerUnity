mergeInto(LibraryManager.library, (() => {
    return {
        webgl_sasa_initialize: function () {
            if (window.sasa_data) return;

            window.sasa_data = {
                actx: new AudioContext(),
                audioManagers: {},
                audioClips: {},
                audioSfxs: {},
                audioMusics: {},
                ptrbase: 0xff,
                newPtr: function () {
                    return this.ptrbase++;
                },

                init_play_music: function (music, clip) {
                    const onended = (callback) => {
                        if (music.ended) return;
                        music.instance = null;
                        music.ended = true;
                        callback();
                    };
                    
                    if (clip instanceof Audio) {
                        const temp = clip.cloneNode(true);
                        temp.volume = music.current_volume;
                        temp.playbackRate = music.playback_rate;
                        if (music.paused) temp.currentTime = music.saved_position;
                        temp.play();
                        music.instance = temp;
                        temp.onended = () => onended(() => temp.remove());
                    } else {
                        const source = window.sasa_data.actx.createBufferSource();
                        const gain_node = window.sasa_data.actx.createGain();
                        source.playbackRate.value = music.playback_rate;
                        gain_node.gain.value = music.current_volume;
                        source.buffer = clip;
                        source.connect(gain_node);
                        gain_node.connect(window.sasa_data.actx.destination);
                        const start_position = music.paused ? music.saved_position : 0;
                        source.start(0, start_position);
                        source.start_time = window.sasa_data.actx.currentTime - start_position;
                        source.gain_node = gain_node;
                        music.instance = source;
                        source.onended = () => onended(() => source.disconnect());
                    }

                    music.paused = false;
                    music.ended = false;
                }
            };
        },

        create_audio_manager: function () {
            const ptr = window.sasa_data.newPtr();
            window.sasa_data.audioManagers[ptr] = {
                sfxs: [],
                musics: [],
            };
            return ptr;
        },

        load_audio_clip_from_memory: function (dataPtr, dataLen) {
            const data = HEAPU8.slice(dataPtr, dataPtr + dataLen);
            const ptr = window.sasa_data.newPtr();
            const clip = new Audio();
            clip.src = URL.createObjectURL(new Blob([data]));
            window.sasa_data.audioClips[ptr] = clip;

            (async () => {
                const new_clip = await window.sasa_data.actx.decodeAudioData(data.buffer);
                window.sasa_data.audioClips[ptr] = new_clip;
            })();

            return ptr;
        },

        create_sfx: function (manager_ptr, clip_ptr) {
            const clip = window.sasa_data.audioClips[clip_ptr];
            const manager = window.sasa_data.audioManagers[manager_ptr];

            if (!clip) return 0;
            if (!manager) return 0;

            const ptr = window.sasa_data.newPtr();

            const sfx = {
                clip_ptr: clip_ptr,
                self_ptr: ptr
            };

            manager.sfxs.push(sfx);
            window.sasa_data.audioSfxs[ptr] = sfx;

            return ptr;
        },

        create_music: function (manager_ptr, clip_ptr, playback_rate) {
            const clip = window.sasa_data.audioClips[clip_ptr];
            const manager = window.sasa_data.audioManagers[manager_ptr];

            if (!clip) return 0;
            if (!manager) return 0;

            const ptr = window.sasa_data.newPtr();

            const music = {
                clip_ptr: clip_ptr,
                self_ptr: ptr,
                instance: null,
                saved_position: 0,
                paused: false,
                ended: false,
                duration: clip.duration,
                current_volume: 1.0,
                playback_rate: playback_rate
            };

            manager.musics.push(music);
            window.sasa_data.audioMusics[ptr] = music;

            return ptr;
        },

        play_sfx: function (sfx_ptr, volume) {
            const sfx = window.sasa_data.audioSfxs[sfx_ptr];
            if (!sfx) return;
            const clip = window.sasa_data.audioClips[sfx.clip_ptr];

            if (clip instanceof Audio) {
                const temp = clip.cloneNode(true);
                temp.volume = volume;
                temp.play();
                temp.onended = () => {
                    temp.remove();
                };
            } else {
                const source = window.sasa_data.actx.createBufferSource();
                const gain_node = window.sasa_data.actx.createGain();
                gain_node.gain.value = volume;
                source.buffer = clip;
                source.connect(gain_node);
                gain_node.connect(window.sasa_data.actx.destination);
                source.start();
                source.onended = () => {
                    source.disconnect();
                };
            }
        },

        play_music: function (music_ptr, volume) {
            const music = window.sasa_data.audioMusics[music_ptr];
            if (!music) return;
            const clip = window.sasa_data.audioClips[music.clip_ptr];

            if (music.instance && !music.paused) return;

            music.current_volume = volume;
            window.sasa_data.init_play_music(music, clip);
        },

        pause_music: function (music_ptr) {
            const music = window.sasa_data.audioMusics[music_ptr];
            if (!music) return;
            const ins = music.instance;

            if (ins === null) return;
            
            if (ins instanceof Audio) {
                ins.pause();
                music.saved_position = ins.currentTime;
            } else {
                ins.stop();
                music.saved_position = window.sasa_data.actx.currentTime - ins.start_time;
            }

            ins.onended();
            music.instance = null;
            music.paused = true;
        },

        is_music_paused: function (music_ptr) {
            const music = window.sasa_data.audioMusics[music_ptr];
            if (!music) return false;

            return music.paused;
        },

        seek_music: function (music_ptr, time) {
            const music = window.sasa_data.audioMusics[music_ptr];
            if (!music) return;
            const ins = music.instance;

            if (ins === null) {
                music.saved_position = time;
                return;
            }

            if (ins instanceof Audio) {
                ins.currentTime = time;
            } else {
                ins.onended();
                music.paused = true;
                music.saved_position = time;
                const clip = window.sasa_data.audioClips[music.clip_ptr];
                window.sasa_data.init_play_music(music, clip);
            }
        },

        set_music_volume: function (music_ptr, volume) {
            const music = window.sasa_data.audioMusics[music_ptr];
            if (!music) return;
            const ins = music.instance;
            if (ins === null) return;

            if (ins instanceof Audio) {
                ins.volume = volume;
            } else {
                ins.gain_node.gain.value = volume;
            }
        },

        get_music_position: function (music_ptr) {
            const music = window.sasa_data.audioMusics[music_ptr];
            if (!music) return 0;
            const ins = music.instance;

            if (music.paused) return music.saved_position;
            if (ins === null && music.ended) return music.duration;

            if (ins instanceof Audio) {
                return ins.currentTime;
            } else {
                return window.sasa_data.actx.currentTime - ins.start_time;
            }
        },

        get_audio_clip_duration: function (clip_ptr) {
            const clip = window.sasa_data.audioClips[clip_ptr];
            if (!clip) return 0;

            return clip.duration;
        },

        destroy_manager: function (manager_ptr) {
            delete window.sasa_data.audioManagers[manager_ptr];
        },

        destroy_clip: function (clip_ptr) {
            delete window.sasa_data.audioClips[clip_ptr];
        },

        destroy_sfx: function (sfx_ptr) {
            delete window.sasa_data.audioSfxs[sfx_ptr];
        },

        destroy_music: function (music_ptr) {
            delete window.sasa_data.audioMusics[music_ptr];
        },

        recover_if_needed: function () {
            
        }
    };
})());
