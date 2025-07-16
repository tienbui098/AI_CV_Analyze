// EditSuggestions.js
document.addEventListener('DOMContentLoaded', function () {
    // Add copy button functionality
    const suggestionContent = document.querySelector('.suggestion-content');
    if (suggestionContent) {
        const copyBtn = document.createElement('button');
        copyBtn.className = 'copy-btn flex items-center';
        copyBtn.innerHTML = '<i class="far fa-copy mr-2"></i> Copy';

        copyBtn.addEventListener('click', function () {
            const textToCopy = document.querySelector('.ai-suggestion-text').textContent;
            navigator.clipboard.writeText(textToCopy).then(() => {
                copyBtn.innerHTML = '<i class="fas fa-check mr-2"></i> Copied!';
                setTimeout(() => {
                    copyBtn.innerHTML = '<i class="far fa-copy mr-2"></i> Copy';
                }, 2000);
            });
        });

        suggestionContent.appendChild(copyBtn);
    }

    // Format and highlight content
    const aiSuggestions = document.querySelector('.ai-suggestion-text');
    if (aiSuggestions) {
        // Highlight important keywords
        const keywords = ['nên', 'khuyến nghị', 'cải thiện', 'thêm', 'bổ sung', 'tối ưu', 'quan trọng'];
        keywords.forEach(keyword => {
            const regex = new RegExp(`(${keyword})`, 'gi');
            aiSuggestions.innerHTML = aiSuggestions.innerHTML.replace(
                regex,
                '<span class="highlight-keyword">$1</span>'
            );
        });

        // Add smooth scrolling for long content
        const sectionBlocks = document.querySelectorAll('.section-block');
        sectionBlocks.forEach((block, index) => {
            if (index > 0) {
                block.style.scrollMarginTop = '20px';
                block.id = `section-${index}`;
            }
        });
    }

    // Add animation to cards
    const cards = document.querySelectorAll('.suggestion-card');
    cards.forEach((card, index) => {
        card.style.opacity = '0';
        card.style.transform = 'translateY(20px)';
        card.style.transition = 'all 0.5s ease';

        setTimeout(() => {
            card.style.opacity = '1';
            card.style.transform = 'translateY(0)';
        }, 100 + (index * 100));
    });
});