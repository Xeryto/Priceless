function editorFunc(onlyRead, jsonData) {
    if (!onlyRead) {
        let input = document.querySelector("#input");
        let saveBtn = document.querySelector("#saveBtn");
    }


    const editor = new EditorJS({
        /**
         * Id of Element that should contain the Editor
         */
        holder: 'editorjs',

        /**
         * Available Tools list.
         * Pass Tool's class or Settings object for each Tool you want to use
         */
        tools: {
            attaches: {
                class: AttachesTool,
                config: {
                    uploader: {
                        /**
                        * Upload file to the server and return an uploaded image data
                        * param {File} file - file selected from the device or pasted by drag-n-drop
                        * return {Promise.<{success, file: {url}}>}
                        */
                        uploadByFile(file) {
                            var formData = new FormData();
                            formData.append('file', file); // myFile is the input type="file" control

                            var _url = window.location.origin + '/Home/SaveFile';
                            var resOut = null;

                            return $.ajax({
                                async: false,
                                url: _url,
                                type: 'POST',
                                data: formData,
                                processData: false,  /** tell jQuery not to process the data*/
                                contentType: false,  // tell jQuery not to set contentType
                                success: function (result) {
                                },
                                error: function (jqXHR) {
                                },
                                complete: function (jqXHR, status) {
                                }
                            }).then((response) => {
                                return {
                                    success: 1,
                                    file: {
                                        url: JSON.parse(response).file.url,
                                        name: JSON.parse(response).file.name,
                                        title: JSON.parse(response).file.name
                                    }
                                }
                            });
                        }
                    }
                }
            }
        },
        onChange: function () {
            if (!onlyRead) {
                editor.save().then((savedData) => {
                    input.value = JSON.stringify(savedData);
                });
            }
        },
        data: jsonData
    });
}

saveBtn.addEventListener("click", function () {
    editor.save().then((savedData) => {
        input.value = JSON.stringify(savedData);
    });
});