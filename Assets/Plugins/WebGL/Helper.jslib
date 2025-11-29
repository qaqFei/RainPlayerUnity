mergeInto(LibraryManager.library, {
    WebGLHelper_Initialize: function() {
        if (window.WebGLHelper_Data) return;

        window.WebGLHelper_Data = {
            _strings_ptr: 0xff,
            _strings: new Map(),

            getString: (ptr, len) => {
                const uint8Arr = HEAPU8.slice(ptr, ptr + len);
                const decoder = new TextDecoder();
                return decoder.decode(uint8Arr);
            },
            makeString: function (str) {
                const encoder = new TextEncoder();
                const uint8Arr = encoder.encode(str);
                const id = this._strings_ptr++;
                this._strings.set(id, uint8Arr);
                return id;
            },
        }
    },
    
    WebGLHelper_SetCanvasFull: function() {
        const unityContainer = document.getElementById("unity-container");
        const unityCanvas = document.getElementById("unity-canvas");

        unityContainer.style.width = unityContainer.style.height = "100%";
        unityCanvas.style.position = "absolute";
        unityCanvas.style.top = "0";
        unityCanvas.style.left = "0";
        unityCanvas.style.width = "100%";
        unityCanvas.style.height = "100%";
        unityCanvas.style.zIndex = "9999";
        unityCanvas.style.display = "block";
    },

    WebGLHelper_GetStringSize: function (stringId) {
        if (!window.WebGLHelper_Data._strings.has(stringId)) return 0;

        const uint8Arr = window.WebGLHelper_Data._strings.get(stringId);
        return uint8Arr.length;
    },

    WebGLHelper_WriteStringIntoBuffer: function (stringId, bufferPtr) {
        if (!window.WebGLHelper_Data._strings.has(stringId)) return;

        const uint8Arr = window.WebGLHelper_Data._strings.get(stringId);
        HEAPU8.set(uint8Arr, bufferPtr);
    },

    WebGLHelper_ReleaseString: function (stringId) {
        if (!window.WebGLHelper_Data._strings.has(stringId)) return;

        window.WebGLHelper_Data._strings.delete(stringId);
    },

    WebGLHelper_GetUrlParam: function (keyPtr, keyLen) {
        const key = window.WebGLHelper_Data.getString(keyPtr, keyLen);
        const value = new URLSearchParams(window.location.search).get(key);
        if (!value) return 0;
        return window.WebGLHelper_Data.makeString(value);
    },

    WebGLHelper_FreeBlobURL: function (url) {
        // URL.revokeObjectURL(UTF16ToString(url));
    },

    WebGLHelper_ChartPlayerLoaded: function () {
        window.dispatchEvent(new Event("rain_player_chart_player_loaded"));
    },

    WebGLHelper_ChartPlayerLoadFailed: function () {
        window.dispatchEvent(new Event("rain_player_chart_player_load_failed"));
    },

    WebGLHelper_ChartPlayerStartedLoad: function () {
        window.dispatchEvent(new Event("rain_player_chart_player_started_load"));
    },

    WebGLHelper_BackToHub: function () {
        window.dispatchEvent(new Event("rain_player_back_to_hub"));
    }
});
