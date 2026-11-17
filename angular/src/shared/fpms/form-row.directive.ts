import { AfterViewInit, Directive, ElementRef, OnDestroy, inject } from '@angular/core';

@Directive({ selector: '.form-row', standalone: true })
export class FormRowAlignDirective implements AfterViewInit, OnDestroy {
    private readonly row = inject(ElementRef<HTMLElement>).nativeElement as HTMLElement;
    private resize?: ResizeObserver;
    private mutate?: MutationObserver;
    private frame = 0;

    ngAfterViewInit(): void {
        if (typeof ResizeObserver === 'undefined') {
            return;
        }
        this.resize = new ResizeObserver(() => this.schedule());
        this.resize.observe(this.row);
        this.mutate = new MutationObserver(() => this.schedule());
        this.mutate.observe(this.row, { childList: true, subtree: true, characterData: true });
        this.schedule();
    }

    ngOnDestroy(): void {
        this.resize?.disconnect();
        this.mutate?.disconnect();
        cancelAnimationFrame(this.frame);
    }

    private schedule(): void {
        cancelAnimationFrame(this.frame);
        this.frame = requestAnimationFrame(() => this.align());
    }

    private align(): void {
        this.mutate?.disconnect();
        const cells = Array.from(this.row.children).filter((c): c is HTMLElement => c instanceof HTMLElement && c.offsetParent !== null);
        const labelOf = (c: HTMLElement): HTMLElement | null => c.querySelector(':scope > label:first-child, :scope > legend:first-child, :scope > .fpms-group-label:first-child');
        for (const c of cells) {
            const l = labelOf(c);
            if (l) {
                l.style.minHeight = '';
            }
            c.style.removeProperty('--fpms-label-block');
        }
        const lines: HTMLElement[][] = [];
        for (const c of cells) {
            const top = c.offsetTop;
            const line = lines.find((l) => Math.abs(l[0].offsetTop - top) < 4);
            line ? line.push(c) : lines.push([c]);
        }
        for (const line of lines) {
            if (line.length < 2) {
                if (!labelOf(line[0])) {
                    line[0].style.setProperty('--fpms-label-block', '0px');
                }
                continue;
            }
            const labels = line.map(labelOf).filter((l): l is HTMLElement => !!l);
            if (!labels.length) {
                continue;
            }
            const tallest = Math.max(...labels.map((l) => l.getBoundingClientRect().height));
            const margin = parseFloat(getComputedStyle(labels[0]).marginBottom) || 0;
            for (const l of labels) {
                if (l.getBoundingClientRect().height < tallest - 0.5) {
                    l.style.minHeight = `${tallest}px`;
                }
            }
            for (const c of line) {
                if (!labelOf(c)) {
                    c.style.setProperty('--fpms-label-block', `${tallest + margin}px`);
                }
            }
        }
        this.mutate?.observe(this.row, { childList: true, subtree: true, characterData: true });
    }
}
