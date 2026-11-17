import { Injectable, computed, inject, signal } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';
import { PermissionCheckerService } from 'abp-ng2-module';
import { AppSessionService } from '@shared/session/app-session.service';
import { SectionAction, SectionTab, activeTab, effectiveUrl, pathOf, sectionOfUrl } from './sections';

@Injectable({ providedIn: 'root' })
export class SectionNav {
    private router = inject(Router);
    private permission = inject(PermissionCheckerService);
    private session = inject(AppSessionService);

    readonly url = signal(pathOf(effectiveUrl(this.router.url)));

    readonly section = computed(() => sectionOfUrl(this.url()));
    readonly label = computed(() => this.section()?.label ?? '');

    readonly tabs = computed(() => {
        const section = this.section();
        if (!section) {
            return [];
        }
        return section.tabs
            .map((t) => {
                const pages = t.children?.length ? this.allowed(t.children) : null;
                return { label: t.label, route: t.route, link: pages ? pages[0]?.route : t.route, show: pages ? pages.length > 0 : this.allowed([t]).length > 0 };
            })
            .filter((t) => t.show);
    });

    private readonly active = computed(() => {
        const section = this.section();
        return section ? activeTab(section, this.url()) : undefined;
    });
    readonly activeRoute = computed(() => this.active()?.tab.route);
    readonly activeTabLabel = computed(() => this.active()?.tab.label ?? '');
    readonly activeChildRoute = computed(() => this.active()?.child?.route);
    readonly activeLabel = computed(() => (this.active()?.child ?? this.active()?.tab)?.label ?? '');

    readonly subTabs = computed(() => this.allowed(this.active()?.tab.children ?? []));

    readonly actions = computed<SectionAction[]>(() => {
        const active = this.active();
        if (!active) {
            return [];
        }
        const page = (active.child ?? active.tab).route;
        const onOwnPage = this.url() === page;
        return (this.section()?.actions ?? []).filter(
            (a) => a.onTabs.includes(active.tab.route) && (a.onDetail || onOwnPage) && this.permission.isGranted(a.permission)
        );
    });

    readonly barVisible = computed(() => !!this.activeRoute() && (this.tabs().length > 1 || this.actions().length > 0));

    readonly counts = signal<Record<string, number>>({});

    constructor() {
        this.router.events.pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd)).subscribe((e) => this.url.set(pathOf(effectiveUrl(e.urlAfterRedirects))));
    }

    setCount(route: string, count: number): void {
        if (this.counts()[route] !== count) {
            this.counts.update((c) => ({ ...c, [route]: count }));
        }
    }

    private allowed(tabs: SectionTab[]): SectionTab[] {
        return tabs.filter((t) => (!t.permission || this.permission.isGranted(t.permission)) && (!t.setting || !!this.session.tenant?.[t.setting]));
    }
}
