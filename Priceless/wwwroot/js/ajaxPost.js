let input = document.querySelector("#file");

input.onchange = function (event) {
    var fileList = input.files;
    console.log(fileList.length);
    var _url = window.location.origin + '/Home/SaveFile';

    let image = document.querySelector("#input");

    var formData = new FormData();
    formData.append('file', fileList[0]);

    $.ajax({
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
        image.value = JSON.parse(response).file.url;
    });
}