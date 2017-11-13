
// $.get("/canupload", function (resp) {
//
// });
let colors = [ "#ffebee", "#fce4ec", "#f3e5f5", "#ede7f6", "#e8eaf6", "#e3f2fd", "#e1f5fe", "#e0f7fa", "#e0f2f1", "#e8f5e9", "#f1f8e9", "#f9fbe7", "#fffde7", "#fff8e1", "#fff3e0", "#fbe9e7", "#efebe9", "#fafafa", "#eceff1" ];
function hashCode(str) {
    let hash = 0;
    for (let i = 0; i < str.length; i++) {
        hash = str.charCodeAt(i) + ((hash << 5) - hash);
    }
    return Math.abs(hash);
}

let template = document.getElementById("preview-template").innerHTML;
let dropzone = new Dropzone("#dropzone", {
    url: "/upload",
    maxFileSize: 500,
    uploadMultiple: true,
    clickable: ".material-icons",
    previewsContainer: "#preview",
    previewTemplate: template,
    autoProcessQueue: false,
    parallelUploads: 10,
    maxFiles: 10


});

dropzone.on("addedfile", function(file) {
    // console.log(file);
    /* Maybe display some more file information on your page */
});
dropzone.on("maxfilesexceeded", function(file) {
    dropzone.removeFile(file);
    console.log("max files exceeded!");
});

// dropzone.on("sending", function(file) {
//     // $(".progress").style.opacity = "1";
// });
dropzone.on("addedfile", function(file) {
    let hashStr = file.name;
    let i = file.name.lastIndexOf('.');
    if (i > -1 && file.name.length > i + 2)
        hashStr = file.name.substr(i+1);
    let bgcColor = hashCode(hashStr) % colors.length;
    $(file.previewElement).css("background-color", colors[bgcColor]);
});
// dropzone.on("success", function(file, responseText) {
//     // window.location.replace("/" + responseText);
// });