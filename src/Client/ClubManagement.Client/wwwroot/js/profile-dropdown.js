let dotNetRef = null;
let isInitialized = false;

export function initialize(dotNetReference) {
    console.log('ProfileDropdown JS: Initializing');
    dotNetRef = dotNetReference;
    
    if (!isInitialized) {
        // Add event listeners
        document.addEventListener('click', handleOutsideClick, true);
        document.addEventListener('keydown', handleKeyDown, true);
        isInitialized = true;
        console.log('ProfileDropdown JS: Event listeners added');
    }
}

function handleOutsideClick(event) {
    if (!dotNetRef) return;

    // Check if the click is on the profile dropdown or its children
    const profileDropdown = event.target.closest('.profile-dropdown');
    const mudPopover = event.target.closest('.mud-popover');
    
    // If click is outside both the profile dropdown and any mud popover, close it
    if (!profileDropdown && !mudPopover) {
        console.log('ProfileDropdown JS: Outside click detected, closing dropdown');
        try {
            dotNetRef.invokeMethodAsync('CloseDropdown');
        } catch (error) {
            console.error('ProfileDropdown JS: Error calling CloseDropdown:', error);
        }
    }
}

function handleKeyDown(event) {
    if (!dotNetRef) return;

    // Close dropdown on Escape key
    if (event.key === 'Escape') {
        console.log('ProfileDropdown JS: Escape key pressed, closing dropdown');
        event.preventDefault();
        try {
            dotNetRef.invokeMethodAsync('CloseDropdown');
        } catch (error) {
            console.error('ProfileDropdown JS: Error calling CloseDropdown:', error);
        }
    }
}

export function dispose() {
    console.log('ProfileDropdown JS: Disposing');
    if (isInitialized) {
        document.removeEventListener('click', handleOutsideClick, true);
        document.removeEventListener('keydown', handleKeyDown, true);
        isInitialized = false;
    }
    dotNetRef = null;
}