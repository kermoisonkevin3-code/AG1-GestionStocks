/**
 * AG1 – JavaScript Principal
 * Gestion UI, confirmations, notifications
 */

// ---- Confirmations destructives ----
document.querySelectorAll('[data-confirm]').forEach(el => {
    el.addEventListener('click', e => {
        if (!confirm(el.dataset.confirm)) e.preventDefault();
    });
});

// ---- Auto-dismiss alerts ----
document.querySelectorAll('.alert').forEach(alert => {
    setTimeout(() => {
        alert.style.transition = 'opacity 0.5s';
        alert.style.opacity = '0';
        setTimeout(() => alert.remove(), 500);
    }, 4000);
});

// ---- Highlight ligne active sidebar ----
const currentPath = window.location.pathname.toLowerCase();
document.querySelectorAll('.nav-item').forEach(link => {
    const href = link.getAttribute('href')?.toLowerCase();
    if (href && href !== '/' && currentPath.startsWith(href)) {
        link.classList.add('active');
    }
});

// ---- Tooltip stock couleur dynamique ----
document.querySelectorAll('.stock-bar-fill').forEach(bar => {
    const width = parseFloat(bar.style.width);
    if (width === 0) bar.classList.add('fill-rupture');
    else if (width < 40) bar.classList.add('fill-faible');
    else bar.classList.add('fill-ok');
});

// ---- Recherche live (debounce 400ms) ----
const searchInputs = document.querySelectorAll('input[name="search"]');
searchInputs.forEach(input => {
    let timer;
    input.addEventListener('input', () => {
        clearTimeout(timer);
        timer = setTimeout(() => {
            const form = input.closest('form');
            if (form && input.value.length >= 2) form.submit();
        }, 500);
    });
});

// ---- Confirmation avant changement de statut commande ----
const statutSelect = document.querySelector('select[name="statut"]');
const statutForm   = statutSelect?.closest('form');
if (statutForm) {
    const originalValue = statutSelect.value;
    statutForm.addEventListener('submit', e => {
        const newVal = statutSelect.value;
        if (newVal === 'expediee' && originalValue !== 'expediee') {
            if (!confirm('⚠ Passer en "Expédiée" va décrémenter le stock des articles. Continuer ?')) {
                e.preventDefault();
            }
        } else if (newVal === 'annulee') {
            if (!confirm('Annuler cette commande ? Cette action ne peut pas être annulée.')) {
                e.preventDefault();
            }
        }
    });
}

// ---- Toast notification (global) ----
window.showToast = function(message, type = 'success') {
    const t = document.createElement('div');
    t.style.cssText = `
        position:fixed;bottom:24px;right:24px;z-index:9999;
        background:${type==='success'?'var(--green)':'var(--red)'};
        color:${type==='success'?'#000':'#fff'};
        padding:12px 20px;border-radius:10px;
        font-family:'Inter',sans-serif;font-weight:600;font-size:0.875rem;
        box-shadow:0 8px 24px rgba(0,0,0,0.4);
        animation:slideUp 0.3s ease;
    `;
    t.textContent = message;
    document.body.appendChild(t);
    setTimeout(() => { t.style.opacity='0'; t.style.transition='opacity 0.4s'; setTimeout(()=>t.remove(),400); }, 3500);
};

// Keyframes injection
const s = document.createElement('style');
s.textContent = `@keyframes slideUp { from{opacity:0;transform:translateY(16px)} to{opacity:1;transform:translateY(0)} }`;
document.head.appendChild(s);
