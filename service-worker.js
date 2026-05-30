/* Manifest version: AG1GhjgW */
importScripts("./service-worker-assets.js");

const cacheNamespace = "atlas-of-you";
const cachePrefix = `${cacheNamespace}-v0.6.0`;
const assetCacheName = `${cachePrefix}-assets-${self.assetsManifest.version}`;
const dataCacheName = `${cachePrefix}-data`;
const cacheNames = [assetCacheName, dataCacheName];

const offlineAssets = self.assetsManifest.assets
  .filter((asset) => shouldPrecache(asset.url))
  .map((asset) => new URL(asset.url, self.location).href);

const offlineAssetSet = new Set(offlineAssets);
const indexUrl = new URL("index.html", self.location).href;

self.addEventListener("install", (event) => {
  self.skipWaiting();
  event.waitUntil(
    caches.open(assetCacheName)
      .then((cache) => cache.addAll(offlineAssets))
  );
});

self.addEventListener("activate", (event) => {
  event.waitUntil(
    caches.keys()
      .then((keys) => Promise.all(
        keys
          .filter((key) => key.startsWith(cacheNamespace) && !cacheNames.includes(key))
          .map((key) => caches.delete(key))
      ))
      .then(() => self.clients.claim())
  );
});

self.addEventListener("fetch", (event) => {
  const request = event.request;

  if (request.method !== "GET") {
    return;
  }

  const url = new URL(request.url);
  if (url.origin !== self.location.origin) {
    return;
  }

  if (request.mode === "navigate") {
    event.respondWith(networkFirst(request, assetCacheName, indexUrl));
    return;
  }

  if (isDataRequest(url)) {
    event.respondWith(networkFirst(request, dataCacheName));
    return;
  }

  if (offlineAssetSet.has(url.href)) {
    event.respondWith(cacheFirst(request, assetCacheName));
  }
});

function shouldPrecache(url) {
  if (!url || url.startsWith("data/")) {
    return false;
  }

  if (url.includes("service-worker") || url.endsWith(".pdb") || url.endsWith(".br") || url.endsWith(".gz")) {
    return false;
  }

  return true;
}

function isDataRequest(url) {
  return url.pathname.includes("/data/") && url.pathname.endsWith(".json");
}

async function cacheFirst(request, cacheName) {
  const cache = await caches.open(cacheName);
  const cached = await cache.match(request);
  if (cached) {
    return cached;
  }

  const response = await fetch(request);
  if (response.ok) {
    await cache.put(request, response.clone());
  }

  return response;
}

async function networkFirst(request, cacheName, fallbackUrl) {
  const cache = await caches.open(cacheName);

  try {
    const response = await fetch(request);
    if (response.ok) {
      await cache.put(request, response.clone());
    }

    return response;
  } catch {
    const cached = await cache.match(request);
    if (cached) {
      return cached;
    }

    if (fallbackUrl) {
      const fallback = await cache.match(fallbackUrl);
      if (fallback) {
        return fallback;
      }
    }

    throw new Error(`No cached response for ${request.url}`);
  }
}
