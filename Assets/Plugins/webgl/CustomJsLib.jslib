mergeInto(LibraryManager.library, {
  // Outbound: Unity -> iframe host, as { type, data } via window.parent.postMessage.
  SendPostMessage: function (messagePtr) {
    var message = UTF8ToString(messagePtr);
    if (typeof window !== "undefined" && window.parent && typeof window.parent.postMessage === "function") {
      window.parent.postMessage({ type: message, data: {} }, "*");
    }
  },

  // Self-contained resize bridge: the Unity page listens to its own viewport and pushes
  // "width,height" into Unity (OC.SwitchDisplay) — no dependency on the iframe host.
  RegisterResizeListener: function (gameObjectNamePtr, methodNamePtr) {
    var gameObjectName = UTF8ToString(gameObjectNamePtr);
    var methodName = UTF8ToString(methodNamePtr);

    function sendDimensionsToUnity() {
      try {
        // visualViewport is the accurate visible area on iOS; fall back to innerWidth/Height.
        var vv = window.visualViewport;
        var w = Math.round(vv ? vv.width : window.innerWidth);
        var h = Math.round(vv ? vv.height : window.innerHeight);
        var dimensions = w + ',' + h;
        if (typeof SendMessage === 'function') {
          SendMessage(gameObjectName, methodName, dimensions);
        } else if (typeof unityInstance !== 'undefined' && unityInstance && unityInstance.SendMessage) {
          unityInstance.SendMessage(gameObjectName, methodName, dimensions);
        }
      } catch (err) {
        console.error('[JS] resize send failed:', err);
      }
    }

    // No debounce here — OrientationChange.SwitchDisplay coalesces via StopCoroutine + waitForRotation,
    // so send on every event and let C# settle it. Remove any prior listener before re-adding.
    if (window._unityResizeCallback) {
      window.removeEventListener('resize', window._unityResizeCallback);
      window.removeEventListener('orientationchange', window._unityResizeCallback);
      if (window.visualViewport) window.visualViewport.removeEventListener('resize', window._unityResizeCallback);
    }
    window._unityResizeCallback = sendDimensionsToUnity;
    window.addEventListener('resize', window._unityResizeCallback);
    window.addEventListener('orientationchange', window._unityResizeCallback);
    if (window.visualViewport) window.visualViewport.addEventListener('resize', window._unityResizeCallback);

    sendDimensionsToUnity();   // initial sync
  },

  // Inbound auth: host posts { type:"TokenReceived", data:{cookie,socketURL,nameSpace} } -> Unity.
  RegisterTokenListener: function (gameObjectNamePtr, methodNamePtr) {
    var gameObjectName = UTF8ToString(gameObjectNamePtr);
    var methodName = UTF8ToString(methodNamePtr);

    if (window._unityTokenCallback) {
      window.removeEventListener('message', window._unityTokenCallback);
    }
    window._unityTokenCallback = function (event) {
      if (!event.data || event.data.type !== 'TokenReceived') return;
      var json = JSON.stringify(event.data.data);
      if (typeof SendMessage === 'function') {
        SendMessage(gameObjectName, methodName, json);
      } else if (typeof unityInstance !== 'undefined' && unityInstance && unityInstance.SendMessage) {
        unityInstance.SendMessage(gameObjectName, methodName, json);
      }
    };
    window.addEventListener('message', window._unityTokenCallback);
  }
});
