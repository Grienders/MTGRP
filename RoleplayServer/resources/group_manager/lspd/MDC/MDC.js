﻿var mdcBrowser = null;

API.onResourceStart.connect(function () {
    
});

//From server
API.onServerEventTrigger.connect(function (eventName, args) {
 
    switch (eventName) {
        case "showMDC":
            var res = API.getScreenResolution();
            mdcBrowser = API.createCefBrowser(res.Width, res.Height);
        
            API.waitUntilCefBrowserInit(mdcBrowser);
            API.setCefBrowserPosition(mdcBrowser, 0, 0);
            API.loadPageCefBrowser(mdcBrowser, "group_manager/lspd/MDC/MDC.html");
            API.setCefDrawState(true);
            API.setCanOpenChat(false);
            API.showCursor(true);

            API.sleep(500);
            API.triggerServerEvent("requestMdcInformation");
            break;

        case "add911":

            //number, time, info
            mdcBrowser.call("html_add911", args[0], args[1], args[2], args[3]);
            break;

        case "addBolo":

            //boloId, officer, time, priority, info
            API.sendChatMessage("Calling html_addBolo with: " + args[0] + " " + args[1] + " " + args[2] + " " + args[3] + " " + args[4]);
            mdcBrowser.call("html_addBolo", args[0], args[1], args[2], args[3], args[4]);
            break;

        case "remove911":

            break;
    }
});

//From HTML 

function client_updateMdcAnnoucement(text) {
    API.triggerServerEvent("server_updateMdcAnnouncement", text);
}

function client_removeBolo(boloId) {
    API.triggerServerEvent("server_removeBolo", boloId);
}

function client_sendBoloToServer(info, priority) {
    API.triggerServerEvent("server_createBolo", info, priority);
}

function client_mdc_close() {
    API.destroyCefBrowser(mdcBrowser);
    API.showCursor(false);
    API.setCefDrawState(false);
    API.setCanOpenChat(true);
    API.triggerServerEvent("server_mdc_close");
}
