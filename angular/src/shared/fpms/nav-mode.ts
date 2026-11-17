import { signal } from '@angular/core';

const KEY = 'fpms.nav';

function read(): boolean {
    try {
        return localStorage.getItem(KEY) === 'mini';
    } catch {
        return false;
    }
}

export const navMini = signal(read());

export function isDesktopWidth(): boolean {
    return window.matchMedia('(min-width: 992px)').matches;
}

let watching = false;

export function applyNavMode(): void {
    document.body.classList.toggle('fpms-nav-mini', navMini() || !isDesktopWidth());
    if (!watching) {
        watching = true;
        window.matchMedia('(min-width: 992px)').addEventListener('change', () => applyNavMode());
    }
}

export function setNavMini(mini: boolean): void {
    navMini.set(mini);
    try {
        localStorage.setItem(KEY, mini ? 'mini' : 'full');
    } catch {
    }
    applyNavMode();
}
