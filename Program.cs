function readFolderProperties_onClick() {
    WL.login({
        scope: "wl.skydrive"
    }).then(
        function (response) {
            WL.api({
                path: "folder.a6b2a7e8f2515e5e.A6B2A7E8F2515E5E!164",
                method: "GET"
            }).then(
                function (response) {
                    document.getElementById("infoArea").innerText = 
                        "Folder properties: name = " + response.name + ", ID = " + response.id;                
                }, 
                function (responseFailed) {
                    document.getElementById("infoArea").innerText = 
                        "Error reading folder properties: " + responseFailed.error.message;
                }
            );
        }, 
        function (responseFailed) {
            document.getElementById("infoArea").innerText = 
                "Error signing in: " + responseFailed.error_description;
        }
    );
}

