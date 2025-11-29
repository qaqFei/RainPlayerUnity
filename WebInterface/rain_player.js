'use strict';

(() => {
    let webglBuildDir = null;

    class RainPlayerImpl {
        constructor(options) {
            this.iframe = document.createElement('iframe');
            this.iframe.style.filter = 'opacity(0.001)';
            this.iframe.style.position = 'absolute';
            this.iframe.src = `${webglBuildDir}/index.html?${this.#makeRainPlyerParams(options)}`;
            if (!options.container) throw new Error('RainPlayer: container is required');
            options.container.appendChild(this.iframe);

            this._onload_pm = new Promise((res, rej) => {
                this.iframe.addEventListener('load', () => res(this.iframe.contentWindow));
                this.iframe.addEventListener('error', () => rej(new Error('RainPlayer: iframe load error')));
            }).then(wind => new Promise((res, rej) => {
                wind.addEventListener('rain_player_chart_player_loaded', () => {
                    this.iframe.style.filter = 'none';
                    this.iframe.style.position = '';
                    this.iframe.style.border = 'none';
                    res();
                });
                wind.addEventListener('rain_player_chart_player_load_failed', () => rej(new Error('RainPlayer: chart player load failed')));
            }));
        }

        #makeRainPlyerParams(options) {
            if (!options.chartUrl) {
                throw new Error('RainPlayer: chartUrl is required');
            }

            const params = new URLSearchParams();
            params.append('disableSavables', true);
            params.append('startImmediately', true);
            params.append('chartUrl', options.chartUrl);
            if (options.flowSpeed !== void 0) params.append('flowSpeed', options.flowSpeed);
            if (options.noteSize !== void 0) params.append('noteSize', options.noteSize);
            if (options.offset !== void 0) params.append('offset', options.offset);
            if (options.speed !== void 0) params.append('speed', options.speed);
            if (options.musicVol !== void 0) params.append('musicVol', options.musicVol);
            if (options.hitsoundVol !== void 0) params.append('hitsoundVol', options.hitsoundVol);
            if (options.autoPlay !== void 0) params.append('autoPlay', options.autoPlay);
            if (options.debug !== void 0) params.append('debug', options.debug);
            if (options.chordHL !== void 0) params.append('chordHL', options.chordHL);
            if (options.elIndicator !== void 0) params.append('elIndicator', options.elIndicator);
            if (options.showTouchPoint !== void 0) params.append('showTouchPoint', options.showTouchPoint);
            if (options.oklchColorInterplate !== void 0) params.append('oklchColorInterplate', options.oklchColorInterplate);
            if (options.comboText !== void 0) params.append('comboText', options.comboText);

            return params.toString();
        }

        waitLoaded() {
            return this._onload_pm;
        }
    }

    window.RainPlayer = {
        configure: (config) => {
            if (!config.webglBuildDir) {
                throw new Error('RainPlayer: webglBuildDir is required');
            }

            webglBuildDir = config.webglBuildDir;
        },

        instantiate: (options) => {
            if (!webglBuildDir) {
                throw new Error('RainPlayer: configure() must be called before instantiate()');
            }

            return new RainPlayerImpl(options);
        },
    };
})();
