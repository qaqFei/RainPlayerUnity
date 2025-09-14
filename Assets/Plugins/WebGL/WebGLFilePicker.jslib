mergeInto(LibraryManager.library, {
    PickZipFile: function (gameObjectName, methodName) {
        let input = document.getElementById("_hiddenFileInput");
        if  (!input)  {
            input = document.createElement("input");
            input.type = "file";
            input.id = "_hiddenFileInput";
            input.style.display = "none";
            input.accept = ".zip";
            document.body.appendChild(input);
        }

        input.onchange = null;

        input.onchange = function () {
            if (input.files.length > 0) {
                const file = input.files[0];
                const url = URL.createObjectURL(file);
                SendMessage(Pointer_stringify(gameObjectName), Pointer_stringify(methodName), url);
            }
        };

        input.click();
    },

    FreeBlobURL: function (url) {
        URL.revokeObjectURL(Pointer_stringify(url));
    }
});
