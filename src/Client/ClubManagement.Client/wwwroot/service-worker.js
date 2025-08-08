const CACHE_VERSION = '1.1.55';
const STATIC_CACHE_NAME = `clubmanagement-static-v${CACHE_VERSION}`;
const DYNAMIC_CACHE_NAME = `clubmanagement-dynamic-v${CACHE_VERSION}`;
const OFFLINE_PAGE = '/offline.html';

// Static assets to cache immediately
const STATIC_ASSETS = [
    '/',
    '/offline.html',
    '/css/app.css',
    '/css/bootstrap/bootstrap.min.css',
    '/manifest.json',
    '/favicon.png',
    '/icon-192.png',
    '/icon-512.png',
    '/_framework/blazor.webassembly.js'
];

// URLs that should never be cached (always fetch from network)
const NEVER_CACHE_URLS = [
    '/api/',
    '/logout',
    '/signin-oidc',
    '/signout-oidc'
];

// NuGet package assets - use stale-while-revalidate with short TTL
const NUGET_PACKAGE_PATTERNS = [
    '/_content/'
];

// Cache TTL for different resource types (in milliseconds)
const CACHE_TTL = {
    STATIC_ASSETS: 24 * 60 * 60 * 1000,    // 24 hours
    NUGET_PACKAGES: 2 * 60 * 60 * 1000,    // 2 hours - shorter for library updates
    DYNAMIC_CONTENT: 30 * 60 * 1000        // 30 minutes
};

// Maximum number of items in dynamic cache
const MAX_DYNAMIC_CACHE_SIZE = 50;

// Install event - cache static assets
self.addEventListener('install', event => {
    console.log('Service Worker: Installing...');
    event.waitUntil(
        caches.open(STATIC_CACHE_NAME)
            .then(cache => {
                console.log('Service Worker: Caching static assets');
                return cache.addAll(STATIC_ASSETS);
            })
            .catch(err => console.error('Service Worker: Failed to cache static assets', err))
    );
    // Skip waiting to activate immediately
    self.skipWaiting();
});

// Activate event - clean up old caches
self.addEventListener('activate', event => {
    console.log('Service Worker: Activating...');
    event.waitUntil(
        Promise.all([
            // Clean up old caches
            caches.keys().then(cacheNames => {
                return Promise.all(
                    cacheNames.map(cacheName => {
                        if (cacheName !== STATIC_CACHE_NAME && cacheName !== DYNAMIC_CACHE_NAME) {
                            console.log('Service Worker: Deleting old cache:', cacheName);
                            return caches.delete(cacheName);
                        }
                    })
                );
            }),
            // Claim all clients
            self.clients.claim()
        ])
    );
});

// Fetch event - implement caching strategies
self.addEventListener('fetch', event => {
    const { request } = event;
    const url = new URL(request.url);

    // Skip non-GET requests
    if (request.method !== 'GET') {
        return;
    }

    // Skip URLs that should never be cached
    if (NEVER_CACHE_URLS.some(path => url.pathname.startsWith(path))) {
        return;
    }

    // Handle different types of requests
    if (isStaticAsset(request)) {
        event.respondWith(cacheFirst(request));
    } else if (isAPIRequest(request)) {
        event.respondWith(networkFirst(request));
    } else if (isNuGetPackageRequest(request)) {
        event.respondWith(staleWhileRevalidateWithTTL(request, CACHE_TTL.NUGET_PACKAGES));
    } else if (isBlazorFrameworkRequest(request)) {
        event.respondWith(cacheFirst(request));
    } else {
        event.respondWith(staleWhileRevalidate(request));
    }
});

// Cache strategies
async function cacheFirst(request) {
    try {
        const cachedResponse = await caches.match(request);
        if (cachedResponse) {
            return cachedResponse;
        }
        
        const networkResponse = await fetch(request);
        if (networkResponse.ok) {
            const cache = await caches.open(STATIC_CACHE_NAME);
            await cache.put(request, networkResponse.clone());
        }
        return networkResponse;
    } catch (error) {
        console.error('Service Worker: Cache first failed:', error);
        return getOfflineFallback(request);
    }
}

async function networkFirst(request) {
    try {
        const networkResponse = await fetch(request);
        if (networkResponse.ok) {
            const cache = await caches.open(DYNAMIC_CACHE_NAME);
            await cache.put(request, networkResponse.clone());
            limitCacheSize(DYNAMIC_CACHE_NAME, MAX_DYNAMIC_CACHE_SIZE);
        }
        return networkResponse;
    } catch (error) {
        console.log('Service Worker: Network first fallback to cache');
        const cachedResponse = await caches.match(request);
        if (cachedResponse) {
            return cachedResponse;
        }
        return getOfflineFallback(request);
    }
}

async function staleWhileRevalidate(request) {
    try {
        const cachedResponse = await caches.match(request);
        const networkPromise = fetch(request).then(async networkResponse => {
            if (networkResponse.ok) {
                const cache = await caches.open(DYNAMIC_CACHE_NAME);
                await cache.put(request, networkResponse.clone());
                limitCacheSize(DYNAMIC_CACHE_NAME, MAX_DYNAMIC_CACHE_SIZE);
            }
            return networkResponse;
        });

        return cachedResponse || networkPromise;
    } catch (error) {
        console.error('Service Worker: Stale while revalidate failed:', error);
        return getOfflineFallback(request);
    }
}

async function staleWhileRevalidateWithTTL(request, ttl) {
    try {
        const cache = await caches.open(DYNAMIC_CACHE_NAME);
        const cachedResponse = await cache.match(request);
        
        // Check if cached response is still fresh
        if (cachedResponse) {
            const cachedDate = new Date(cachedResponse.headers.get('sw-cached-date') || 0);
            const now = new Date();
            const age = now.getTime() - cachedDate.getTime();
            
            if (age < ttl) {
                // Cache is fresh, but still try to update in background
                fetch(request).then(async networkResponse => {
                    if (networkResponse.ok) {
                        const responseWithDate = new Response(networkResponse.body, {
                            status: networkResponse.status,
                            statusText: networkResponse.statusText,
                            headers: {
                                ...Object.fromEntries(networkResponse.headers.entries()),
                                'sw-cached-date': new Date().toISOString()
                            }
                        });
                        await cache.put(request, responseWithDate);
                        limitCacheSize(DYNAMIC_CACHE_NAME, MAX_DYNAMIC_CACHE_SIZE);
                    }
                }).catch(() => {}); // Ignore background update errors
                
                return cachedResponse;
            }
        }
        
        // Cache is stale or doesn't exist, fetch from network
        const networkResponse = await fetch(request);
        if (networkResponse.ok) {
            const responseWithDate = new Response(networkResponse.body, {
                status: networkResponse.status,
                statusText: networkResponse.statusText,
                headers: {
                    ...Object.fromEntries(networkResponse.headers.entries()),
                    'sw-cached-date': new Date().toISOString()
                }
            });
            await cache.put(request, responseWithDate.clone());
            limitCacheSize(DYNAMIC_CACHE_NAME, MAX_DYNAMIC_CACHE_SIZE);
            return responseWithDate;
        }
        
        // Network failed, return stale cache if available
        return cachedResponse || getOfflineFallback(request);
        
    } catch (error) {
        console.error('Service Worker: TTL stale while revalidate failed:', error);
        return getOfflineFallback(request);
    }
}

// Helper functions
function isStaticAsset(request) {
    const url = new URL(request.url);
    return url.pathname.includes('.css') || 
           url.pathname.includes('.js') || 
           url.pathname.includes('.png') || 
           url.pathname.includes('.jpg') || 
           url.pathname.includes('.svg') ||
           url.pathname.includes('.ico') ||
           url.pathname.includes('.woff') ||
           url.pathname.includes('.woff2') ||
           url.pathname.includes('.ttf');
}

function isAPIRequest(request) {
    return request.url.includes('/api/');
}

function isNuGetPackageRequest(request) {
    const url = new URL(request.url);
    return NUGET_PACKAGE_PATTERNS.some(pattern => url.pathname.startsWith(pattern));
}

function isBlazorFrameworkRequest(request) {
    const url = new URL(request.url);
    return url.pathname.startsWith('/_framework/') ||
           url.pathname.endsWith('.wasm') ||
           url.pathname.endsWith('.dll') ||
           url.pathname.endsWith('.json') ||
           url.pathname.endsWith('.dat');
}


async function getOfflineFallback(request) {
    const url = new URL(request.url);
    
    // Return offline page for navigation requests
    if (request.mode === 'navigate' || 
        (request.method === 'GET' && request.headers.get('accept').includes('text/html'))) {
        return caches.match(OFFLINE_PAGE);
    }
    
    // Return a generic offline response for other requests
    return new Response('Offline', {
        status: 503,
        statusText: 'Service Unavailable',
        headers: { 'Content-Type': 'text/plain' }
    });
}

async function limitCacheSize(cacheName, maxSize) {
    const cache = await caches.open(cacheName);
    const keys = await cache.keys();
    
    if (keys.length > maxSize) {
        // Delete oldest entries
        const entriesToDelete = keys.slice(0, keys.length - maxSize);
        await Promise.all(entriesToDelete.map(key => cache.delete(key)));
    }
}

// Background sync for failed requests (if supported)
self.addEventListener('sync', event => {
    if (event.tag === 'background-sync') {
        event.waitUntil(
            // Handle queued requests when back online
            console.log('Service Worker: Background sync triggered')
        );
    }
});

// Push notifications (if supported)
self.addEventListener('push', event => {
    if (event.data) {
        const data = event.data.json();
        event.waitUntil(
            self.registration.showNotification(data.title, {
                body: data.body,
                icon: '/icon-192.png',
                badge: '/icon-192.png',
                data: data.url
            })
        );
    }
});

// Notification click handling
self.addEventListener('notificationclick', event => {
    event.notification.close();
    
    if (event.notification.data) {
        event.waitUntil(
            clients.openWindow(event.notification.data)
        );
    }
});

// Message handling for communication with main thread
self.addEventListener('message', event => {
    if (event.data && event.data.type === 'SKIP_WAITING') {
        self.skipWaiting();
    }
    
    if (event.data && event.data.type === 'GET_VERSION') {
        event.ports[0].postMessage({
            type: 'VERSION',
            version: CACHE_VERSION
        });
    }
});

console.log('Service Worker: Loaded version', CACHE_VERSION);