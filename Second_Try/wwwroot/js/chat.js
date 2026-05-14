// ── ARIA Chat Widget ─────────────────────────────────────────
(function () {
    const toggle   = document.getElementById('aria-toggle');
    const panel    = document.getElementById('aria-panel');
    const messages = document.getElementById('aria-messages');
    const input    = document.getElementById('aria-input');
    const sendBtn  = document.getElementById('aria-send');
    const closeBtn = document.getElementById('aria-close');
    const badge    = document.getElementById('aria-badge');

    if (!toggle || !panel) return;

    let isOpen   = false;
    let isBusy   = false;
    let hasGreeted = false;

    // ── Toggle panel open/close ───────────────────────────────
    toggle.addEventListener('click', () => {
        isOpen = !isOpen;
        panel.classList.toggle('open', isOpen);
        toggle.classList.toggle('open', isOpen);
        toggle.innerHTML = isOpen
            ? '<i class="fa-solid fa-xmark"></i>'
            : '<i class="fa-solid fa-robot"></i><span id="aria-badge" style="display:none"></span>';

        if (isOpen) {
            badge.style.display = 'none';
            input.focus();
            if (!hasGreeted) {
                hasGreeted = true;
                setTimeout(() => addBotMessage(
                    "👋 Hi! I'm **ARIA**, your SRC Travel assistant.\n\nI can help you with booking info, routes, how the system works, and more. What can I help you with today?"
                ), 400);
            }
        }
    });

    closeBtn?.addEventListener('click', () => {
        isOpen = false;
        panel.classList.remove('open');
        toggle.classList.remove('open');
        toggle.innerHTML = '<i class="fa-solid fa-robot"></i>';
    });

    // ── Chip click ────────────────────────────────────────────
    document.querySelectorAll('.aria-chip').forEach(chip => {
        chip.addEventListener('click', () => {
            if (isBusy) return;
            sendMessage(chip.textContent.trim());
            chip.closest('.aria-chips')?.remove();
        });
    });

    // ── Send on button click or Enter ─────────────────────────
    sendBtn.addEventListener('click', () => {
        const msg = input.value.trim();
        if (msg && !isBusy) sendMessage(msg);
    });

    input.addEventListener('keydown', e => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            const msg = input.value.trim();
            if (msg && !isBusy) sendMessage(msg);
        }
    });

    // Auto-resize input
    input.addEventListener('input', () => {
        input.style.height = 'auto';
        input.style.height = Math.min(input.scrollHeight, 100) + 'px';
    });

    // ── Core send function ────────────────────────────────────
    async function sendMessage(text) {
        if (isBusy) return;
        isBusy = true;

        input.value = '';
        input.style.height = 'auto';
        sendBtn.disabled = true;

        addUserMessage(text);
        const typingEl = addTypingIndicator();
        scrollToBottom();

        try {
            const res = await fetch('/api/chat/ask', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ message: text })
            });

            typingEl.remove();

            if (res.ok) {
                const data = await res.json();
                addBotMessage(data.reply);
            } else {
                addBotMessage("Sorry, something went wrong. Please try again.");
            }
        } catch {
            typingEl.remove();
            addBotMessage("I'm having trouble connecting. Please check your internet connection.");
        } finally {
            isBusy = false;
            sendBtn.disabled = false;
            input.focus();
            scrollToBottom();
        }
    }

    // ── Message renderers ─────────────────────────────────────
    function addUserMessage(text) {
        const div = document.createElement('div');
        div.className = 'aria-msg user';
        div.textContent = text;
        messages.appendChild(div);
    }

    function addBotMessage(text) {
        const div = document.createElement('div');
        div.className = 'aria-msg bot';
        // Simple markdown: **bold**, newlines
        div.innerHTML = formatMarkdown(text);
        messages.appendChild(div);

        // Show badge if panel is closed
        if (!isOpen) {
            badge.style.display = 'flex';
            badge.textContent = '1';
        }
    }

    function addTypingIndicator() {
        const div = document.createElement('div');
        div.className = 'aria-typing';
        div.innerHTML = '<span></span><span></span><span></span>';
        messages.appendChild(div);
        scrollToBottom();
        return div;
    }

    function formatMarkdown(text) {
        return text
            .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
            .replace(/\*(.*?)\*/g, '<em>$1</em>')
            .replace(/\n/g, '<br>');
    }

    function scrollToBottom() {
        requestAnimationFrame(() => {
            messages.scrollTop = messages.scrollHeight;
        });
    }
})();
