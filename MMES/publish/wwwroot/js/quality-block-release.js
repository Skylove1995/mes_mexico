/**
 * QUALITY BLOCK / RELEASE PRODUCTION - CLIENT CONTROLLER
 * HAENG SUNG MEX-MES 3.0
 */

document.addEventListener('DOMContentLoaded', () => {
    let currentMode = 'block'; // 'block' or 'release'
    
    // UI Elements
    const btnBlockMode = document.getElementById('btn-mode-block');
    const btnReleaseMode = document.getElementById('btn-mode-release');
    const pidTextarea = document.getElementById('br-pid-input');
    const pidCountLabel = document.getElementById('br-pid-count');
    const reasonInput = document.getElementById('br-reason-input');
    const submitBtn = document.getElementById('br-submit-btn');
    const submitBtnText = document.getElementById('br-submit-text');
    const tableBody = document.getElementById('br-history-tbody');
    const refreshBtn = document.getElementById('br-refresh-btn');
    const searchInput = document.getElementById('br-search-input');
    const toastContainer = document.getElementById('br-toast-container');

    // --- 1. Mode Switch Handler ---
    function setMode(mode) {
        currentMode = mode;
        if (mode === 'block') {
            btnBlockMode.classList.add('is-active');
            btnReleaseMode.classList.remove('is-active');
            submitBtn.classList.add('is-block-mode');
            submitBtnText.textContent = '⚡ CONFIRM BLOCK';
        } else {
            btnReleaseMode.classList.add('is-active');
            btnBlockMode.classList.remove('is-active');
            submitBtn.classList.remove('is-block-mode');
            submitBtnText.textContent = '✔ CONFIRM RELEASE';
        }
    }

    if (btnBlockMode) btnBlockMode.addEventListener('click', () => setMode('block'));
    if (btnReleaseMode) btnReleaseMode.addEventListener('click', () => setMode('release'));

    // --- 2. PID Line Counter ---
    function updatePidCount() {
        if (!pidTextarea || !pidCountLabel) return;
        const text = pidTextarea.value || '';
        const lines = text
            .split('\n')
            .map(l => l.trim())
            .filter(l => l.length > 0);
        pidCountLabel.textContent = lines.length;
    }

    if (pidTextarea) {
        pidTextarea.addEventListener('input', updatePidCount);
        pidTextarea.addEventListener('paste', () => setTimeout(updatePidCount, 50));
    }

    // --- 3. Toast Notifications (Bilingual EN / ES) ---
    function showToast(title, message, isWarning = false) {
        if (!toastContainer) return;

        const toast = document.createElement('div');
        toast.className = `br-toast ${isWarning ? 'warning' : 'success'}`;

        const iconSvg = isWarning
            ? `<svg class="toast-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>`
            : `<svg class="toast-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="20 6 9 17 4 12"/></svg>`;

        toast.innerHTML = `
            ${iconSvg}
            <div class="toast-body">
                <span class="toast-title">${title}</span>
                <span class="toast-msg">${escapeHtml(message)}</span>
            </div>
        `;

        toastContainer.appendChild(toast);

        setTimeout(() => {
            toast.style.opacity = '0';
            toast.style.transform = 'translateX(30px)';
            toast.style.transition = 'all .2s ease';
            setTimeout(() => toast.remove(), 200);
        }, 5000);
    }

    // --- 4. Fetch Operation History ---
    async function loadHistory() {
        if (!tableBody) return;
        tableBody.innerHTML = `<tr><td colspan="6" style="text-align:center; padding:20px; color:#8B949E;">Loading operation log...</td></tr>`;

        try {
            const searchTerm = searchInput ? searchInput.value.trim() : '';
            const res = await fetch(`/api/quality/block-release/history?search=${encodeURIComponent(searchTerm)}`);
            const data = await res.json();

            if (!data.success || !data.data || data.data.length === 0) {
                tableBody.innerHTML = `<tr><td colspan="6" style="text-align:center; padding:24px; color:#6E7681;">No operation records found.</td></tr>`;
                return;
            }

            tableBody.innerHTML = data.data.map((item, idx) => {
                const isBlocked = item.status === 1;
                const statusBadge = isBlocked
                    ? `<span class="status-pill blocked">Blocked</span>`
                    : `<span class="status-pill released">Release</span>`;
                const rowClass = isBlocked ? 'row-blocked' : 'row-released';

                const actionPrefix = isBlocked ? 'Block' : 'Release';
                const formattedDetails = item.reason
                    ? `Action: <strong>${actionPrefix}</strong> | Reason: ${escapeHtml(item.reason)}`
                    : `Action: <strong>${actionPrefix}</strong>`;

                return `
                    <tr class="${rowClass}">
                        <td class="col-idx">${idx + 1}</td>
                        <td class="col-time">${escapeHtml(item.updatedAt)}</td>
                        <td class="col-pid">${escapeHtml(item.pid)}</td>
                        <td class="col-user">${escapeHtml(item.userBlock)}</td>
                        <td class="col-details">${formattedDetails}</td>
                        <td class="col-status">${statusBadge}</td>
                    </tr>
                `;
            }).join('');
        } catch (err) {
            console.error('Failed to load history:', err);
            tableBody.innerHTML = `<tr><td colspan="6" style="text-align:center; padding:20px; color:#F85149;">Error loading operation log.</td></tr>`;
        }
    }

    if (refreshBtn) refreshBtn.addEventListener('click', loadHistory);
    if (searchInput) {
        let timer = null;
        searchInput.addEventListener('input', () => {
            clearTimeout(timer);
            timer = setTimeout(loadHistory, 300);
        });
    }

    // --- 5. Submit Action Handler ---
    if (submitBtn) {
        submitBtn.addEventListener('click', async () => {
            const text = pidTextarea ? pidTextarea.value.trim() : '';
            const pids = text
                .split('\n')
                .map(l => l.trim())
                .filter(l => l.length > 0);

            if (pids.length === 0) {
                showToast(
                    '⚠️ ATTENTION / ADVERTENCIA',
                    '[EN] Please enter at least 1 PID! / [ES] ¡Por favor ingrese al menos 1 PID!',
                    true
                );
                return;
            }

            const reason = reasonInput ? reasonInput.value.trim() : '';

            submitBtn.disabled = true;
            const originalText = submitBtnText.textContent;
            submitBtnText.textContent = 'PROCESSING...';

            try {
                const res = await fetch('/api/quality/block-release/execute', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        action: currentMode,
                        pids: pids,
                        reason: reason
                    })
                });

                const data = await res.json();

                if (res.ok && data.success) {
                    // Render bilingual toasts for results
                    data.results.forEach(r => {
                        showToast(
                            r.isWarning ? '⚠️ WARNING / ADVERTENCIA' : '✔ SUCCESS / ÉXITO',
                            r.message,
                            r.isWarning
                        );
                    });

                    // Clear inputs if any success
                    if (data.successCount > 0) {
                        pidTextarea.value = '';
                        if (reasonInput) reasonInput.value = '';
                        updatePidCount();
                        loadHistory();
                    }
                } else {
                    showToast(
                        '❌ ERROR / ERROR',
                        data.message || '[EN] Execution failed! / [ES] ¡La ejecución falló!',
                        true
                    );
                }
            } catch (err) {
                console.error('Action failed:', err);
                showToast(
                    '❌ SYSTEM ERROR',
                    '[EN] Network connection error! / [ES] ¡Error de conexión de red!',
                    true
                );
            } finally {
                submitBtn.disabled = false;
                submitBtnText.textContent = originalText;
            }
        });
    }

    // Utility
    function escapeHtml(str) {
        if (!str) return '';
        return str
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    // Initial Load
    loadHistory();
});
