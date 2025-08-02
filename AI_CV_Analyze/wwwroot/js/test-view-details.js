// Test script cho chức năng View Details
console.log('Test script loaded');

// Function để test modal
function testModal() {
    const modal = document.getElementById('contentDetailModal');
    const modalTitle = document.getElementById('modalTitle');
    const modalContent = document.getElementById('modalContent');
    
    if (modal && modalTitle && modalContent) {
        modalTitle.textContent = 'Test Title';
        modalContent.innerHTML = 'This is a test content to verify modal functionality.';
        modal.classList.remove('hidden');
        modal.style.display = 'flex';
        console.log('Test modal shown');
        return true;
    } else {
        console.error('Modal elements not found');
        return false;
    }
}

// Function để test View Details buttons
function testViewDetailsButtons() {
    const buttons = document.querySelectorAll('.show-more-btn');
    console.log('Found', buttons.length, 'View Details buttons');
    
    buttons.forEach((btn, index) => {
        const card = btn.closest('.content-card');
        const title = card?.getAttribute('data-title');
        const content = card?.getAttribute('data-full-content');
        
        console.log(`Button ${index}:`, {
            title: title,
            hasContent: !!content,
            contentLength: content?.length || 0
        });
        
        // Test click
        btn.addEventListener('click', function(e) {
            console.log(`Button ${index} clicked`);
            e.preventDefault();
            e.stopPropagation();
            
            if (title && content) {
                const modal = document.getElementById('contentDetailModal');
                const modalTitle = document.getElementById('modalTitle');
                const modalContent = document.getElementById('modalContent');
                
                if (modal && modalTitle && modalContent) {
                    modalTitle.textContent = title;
                    modalContent.innerHTML = content;
                    modal.classList.remove('hidden');
                    modal.style.display = 'flex';
                    console.log('Modal shown successfully');
                }
            }
        });
    });
}

// Run tests when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function() {
        console.log('DOM loaded, running tests...');
        testViewDetailsButtons();
    });
} else {
    console.log('DOM already loaded, running tests...');
    testViewDetailsButtons();
}

// Export functions for manual testing
window.testModal = testModal;
window.testViewDetailsButtons = testViewDetailsButtons; 