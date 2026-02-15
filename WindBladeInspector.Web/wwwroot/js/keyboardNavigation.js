// Keyboard navigation for image browsing
window.setupKeyboardNavigation = (dotNetRef) => {
    document.addEventListener('keydown', (e) => {
        // Check if user is not typing in an input field
        if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA' || e.target.tagName === 'SELECT') {
            return;
        }

        // Left arrow key
        if (e.key === 'ArrowLeft') {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('NavigatePrevious');
        }

        // Right arrow key
        if (e.key === 'ArrowRight') {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('NavigateNext');
        }
    });
};