// MainLayout JavaScript Module
let dotNetRef = null;
let resizeObserver = null;
let isInitialized = false;

export function initialize(dotNetReference) {
    console.log('MainLayout JS: Initializing');
    dotNetRef = dotNetReference;
    
    if (!isInitialized) {
        // Add window resize listener with debouncing
        let resizeTimeout;
        window.addEventListener('resize', () => {
            clearTimeout(resizeTimeout);
            resizeTimeout = setTimeout(handleResize, 150);
        });
        
        // Add keyboard listeners for accessibility
        document.addEventListener('keydown', handleKeyDown);
        
        isInitialized = true;
        console.log('MainLayout JS: Event listeners added');
    }
}

function handleResize() {
    if (!dotNetRef) return;
    
    try {
        const width = window.innerWidth;
        dotNetRef.invokeMethodAsync('HandleResize', width);
    } catch (error) {
        console.error('MainLayout JS: Error handling resize:', error);
    }
}

function handleKeyDown(event) {
    if (!dotNetRef) return;
    
    // Close sidebar on Escape key
    if (event.key === 'Escape') {
        try {
            dotNetRef.invokeMethodAsync('HandleEscapeKey');
        } catch (error) {
            console.error('MainLayout JS: Error handling escape key:', error);
        }
    }
}

export function getWindowWidth() {
    return window.innerWidth;
}

export function getViewportHeight() {
    return window.innerHeight;
}

export function isMobileDevice() {
    return window.innerWidth < 768;
}

export function isTabletDevice() {
    return window.innerWidth >= 768 && window.innerWidth < 1024;
}

export function isDesktopDevice() {
    return window.innerWidth >= 1024;
}

export function dispose() {
    console.log('MainLayout JS: Disposing');
    
    if (resizeObserver) {
        resizeObserver.disconnect();
        resizeObserver = null;
    }
    
    if (isInitialized) {
        window.removeEventListener('resize', handleResize);
        document.removeEventListener('keydown', handleKeyDown);
        isInitialized = false;
    }
    
    dotNetRef = null;
}