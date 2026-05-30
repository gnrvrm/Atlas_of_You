(function () {
  let deferredInstallPrompt = null;
  let nextSubscriberId = 1;
  const subscribers = new Map();

  function getStatus() {
    return {
      canInstall: Boolean(deferredInstallPrompt),
      isStandalone: isStandalone(),
      serviceWorkerSupported: "serviceWorker" in navigator,
      serviceWorkerController: Boolean(navigator.serviceWorker?.controller),
      online: navigator.onLine !== false,
      shareSupported: Boolean(navigator.share),
    };
  }

  function isStandalone() {
    return window.matchMedia("(display-mode: standalone)").matches ||
      window.navigator.standalone === true;
  }

  function notifySubscribers() {
    const status = getStatus();
    for (const dotNetRef of subscribers.values()) {
      dotNetRef.invokeMethodAsync("UpdatePwaStatus", status).catch(() => {
        // The component may have been disposed during navigation.
      });
    }
  }

  async function tryInstall() {
    if (isStandalone()) {
      return "standalone";
    }

    if (!deferredInstallPrompt) {
      return "unavailable";
    }

    const prompt = deferredInstallPrompt;
    deferredInstallPrompt = null;
    prompt.prompt();
    const choice = await prompt.userChoice.catch(() => null);
    notifySubscribers();
    return choice?.outcome || "unknown";
  }

  async function share(payload) {
    const title = payload?.title || "Atlas of You";
    const url = payload?.url || window.location.href.split("#")[0];
    const text = payload?.text || "";
    const shareData = { title, text, url };

    if (navigator.share) {
      await navigator.share(shareData);
      return "shared";
    }

    if (navigator.clipboard?.writeText) {
      await navigator.clipboard.writeText(text ? `${text}\n\n${url}` : url);
      return "copied";
    }

    return "unavailable";
  }

  function subscribe(dotNetRef) {
    const id = nextSubscriberId++;
    subscribers.set(id, dotNetRef);
    dotNetRef.invokeMethodAsync("UpdatePwaStatus", getStatus()).catch(() => {
      subscribers.delete(id);
    });
    return id;
  }

  function unsubscribe(id) {
    subscribers.delete(id);
  }

  window.addEventListener("beforeinstallprompt", (event) => {
    event.preventDefault();
    deferredInstallPrompt = event;
    notifySubscribers();
  });

  window.addEventListener("appinstalled", () => {
    deferredInstallPrompt = null;
    notifySubscribers();
  });

  window.addEventListener("online", notifySubscribers);
  window.addEventListener("offline", notifySubscribers);

  if ("serviceWorker" in navigator) {
    navigator.serviceWorker.addEventListener("controllerchange", notifySubscribers);
    window.addEventListener("load", () => {
      navigator.serviceWorker.register("service-worker.js", { updateViaCache: "none" })
        .then((registration) => {
          registration.update().catch(() => {
            // A failed update check should not block the app shell.
          });
          notifySubscribers();
        })
        .catch(() => notifySubscribers());
    });
  }

  window.atlasPwa = {
    getStatus,
    share,
    subscribe,
    tryInstall,
    unsubscribe,
  };
})();
