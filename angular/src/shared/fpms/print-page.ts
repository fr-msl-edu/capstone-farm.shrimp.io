import { DestroyRef, inject } from '@angular/core';

export function printPage(id: string, initialFooter = ''): (footer: string) => void {
    const el = document.createElement('style');
    el.id = id;
    document.head.appendChild(el);
    inject(DestroyRef).onDestroy(() => el.remove());
    const quote = (v: string) => '"' + (v ?? '').replace(/["\\]/g, '').replace(/\s+/g, ' ') + '"';
    const set = (footer: string) => {
        const boxes =
            `@bottom-left { content: ${quote(footer)}; font-size: 8pt; color: #555; } ` +
            `@bottom-right { content: "Page " counter(page) " of " counter(pages); font-size: 8pt; color: #555; }`;
        el.textContent =
            `@page { size: A4 portrait; margin: 14mm 12mm 16mm 12mm; ${boxes} } ` +
            `@page fpms-wide { size: A4 landscape; margin: 12mm 12mm 16mm 12mm; ${boxes} }`;
    };
    set(initialFooter);
    return set;
}
