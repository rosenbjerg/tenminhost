        var title = $('#nfileName');
        var template = '<div id="template" class="file-row"> <div> <p class="name" data-dz-name></p> <strong class="error text-danger" data-dz-errormessage></strong> </div> <div> <p class="size" data-dz-size></p> <div class="progress progress-striped active" role="progressbar" aria-valuemin="0" aria-valuemax="100" aria-valuenow="0"> <div class="progress-bar progress-bar-success" style="width:0%;" data-dz-uploadprogress></div> </div> </div> <div> <button class="btn btn-primary start"> <span>Start</span> </button> <button data-dz-remove class="btn btn-warning cancel"> <span>Cancel</span> </button> </div></div>';

        if (id != ''){
            var icon = $('#icon');
            icon.removeClass("fa-cloud-upload").addClass("fa-cloud-download");
            icon.click(function () {
                window.location.replace("/" + id + '/download');
            });
            title.text(id);
            title.click(function () {
                window.location.replace("/" + id + '/download');
            });
        }
        else {
            if (!canUpload){
                title.text('Please try again in 5 - 10 minutes');
            }
            else {
                title.text('Drag and drop the file');
                var dropzone = new Dropzone(document.body, { // Make the whole body a dropzone
                    url: "http://10min.host/upload", // Set the url
                    maxFiles: 1,
                    maxFileSize: 500,
                    previewTemplate: template,
                    autoQueue: false, // Make sure the files aren't queued until manually added
                    previewsContainer: "#previewDiv", // Define the container to display the previews
                    clickable: "#icon" // Define the element that should be used as click trigger to select files.
                });

                dropzone.on("success", function(file, responseText) {
                    window.location.replace("/" + responseText);
                });

                dropzone.on("maxfilesexceeded", function(file) {
                    dropzone.removeAllFiles();
                    dropzone.addFile(file);
                });

                dropzone.on("addedfile", function(file) {
                    file.previewElement.querySelector(".start").onclick = function() { dropzone.enqueueFile(file); };
                });

                dropzone.on("totaluploadprogress", function(progress) {
                    var bar = document.querySelector(".progress-bar");
                    if (bar) bar.style.width = progress + "%";
                });

                dropzone.on("sending", function(file) {
                    document.querySelector(".progress").style.opacity = "1";
                    file.previewElement.querySelector(".start").setAttribute("disabled", "disabled");
                });

                dropzone.on("queuecomplete", function(progress) {
                    document.querySelector(".progress").style.opacity = "0";
                });
            }
        }