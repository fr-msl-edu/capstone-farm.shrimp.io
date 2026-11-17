export interface SectionTab {
    label: string;
    route: string;
    permission?: string;
    setting?: 'usesSizeGrading';
    children?: SectionTab[];
    count?: { service: string; method: string };
}

export interface Section {
    key: string;
    label: string;
    tabs: SectionTab[];
    prefixes: string[];
    actions?: SectionAction[];
}

export interface SectionAction {
    label: string;
    route: string;
    icon: string;
    permission: string;
    onTabs: string[];
    onDetail?: boolean;
}

export const SECTIONS: Section[] = [
    {
        key: 'operations',
        label: 'Operations',
        prefixes: ['/app/logbook'],
        tabs: [
            { label: 'Feeding', route: '/app/logbook/feeding', permission: 'Farm.Logs' },
            { label: 'Record operations', route: '/app/logbook/new', permission: 'Farm.Logs.Create' },
            { label: 'Daily log status', route: '/app/logbook/status', permission: 'Farm.Logs' },
            { label: 'Logbooks', route: '/app/logbook', permission: 'Farm.Logs' },
            { label: 'Printable log sheets', route: '/app/logbook/sheets', permission: 'Farm.Logs' },
        ],
    },
    {
        key: 'production',
        label: 'Production',
        prefixes: ['/app/production'],
        tabs: [
            { label: 'Cycles', route: '/app/production/cycles', permission: 'Farm.Production' },
            { label: 'Batches', route: '/app/production/batches', permission: 'Farm.Production' },
        ],
        actions: [
            {
                label: 'Stock a batch',
                route: '/app/production/create-batch',
                icon: 'fas fa-shrimp',
                permission: 'Farm.Production.Stock',
                onTabs: ['/app/production/cycles', '/app/production/batches'],
                onDetail: true,
            },
        ],
    },
    {
        key: 'harvest',
        label: 'Harvest and sales',
        prefixes: ['/app/harvests', '/app/sales'],
        tabs: [
            { label: 'Harvests', route: '/app/harvests', permission: 'Farm.Harvests' },
            { label: 'Sales', route: '/app/sales', permission: 'Farm.Sales' },
            {
                label: 'Setup',
                route: '/app/harvests/size-grades',
                children: [
                    { label: 'Size grades', route: '/app/harvests/size-grades', permission: 'Farm.Harvests', setting: 'usesSizeGrading', count: { service: 'SizeGrade', method: 'GetAll' } },
                    { label: 'Crate types', route: '/app/harvests/crate-types', permission: 'Farm.Harvests', count: { service: 'ContainerType', method: 'GetAll' } },
                    { label: 'Price tables', route: '/app/sales/price-tables', permission: 'Farm.Sales', count: { service: 'PriceTable', method: 'GetAll' } },
                ],
            },
        ],
        actions: [
            { label: 'Record harvest', route: '/app/harvests/new', icon: 'fas fa-plus', permission: 'Farm.Harvests.Create', onTabs: ['/app/harvests'] },
            { label: 'Record sale', route: '/app/sales/new', icon: 'fas fa-plus', permission: 'Farm.Sales', onTabs: ['/app/sales'] },
        ],
    },
    {
        key: 'resources',
        label: 'Resources',
        prefixes: ['/app/resources', '/app/inventory'],
        tabs: [
            { label: 'Hatchery sources', route: '/app/resources/hatcheries', permission: 'Farm.Hatcheries' },
            { label: 'Fry deliveries', route: '/app/resources/fry-deliveries', permission: 'Farm.FryDeliveries' },
            { label: 'Buyers', route: '/app/resources/buyers', permission: 'Farm.Buyers' },
            { label: 'Inventory', route: '/app/inventory', permission: 'Farm.Inventory' },
        ],
    },
    {
        key: 'farm',
        label: 'Farm',
        prefixes: ['/app/farm'],
        tabs: [
            { label: 'Profile', route: '/app/farm/profile', permission: 'Farm.Profile' },
            { label: 'Farm standards', route: '/app/farm/standards', permission: 'Farm.Profile' },
            { label: 'Users', route: '/app/farm/users', permission: 'Farm.Users' },
            { label: 'Permits and licenses', route: '/app/farm/documents', permission: 'Farm.Compliance' },
            { label: 'Activity log', route: '/app/farm/activity', permission: 'Farm.ActivityLog' },
        ],
    },
];

function startsWithPath(url: string, prefix: string): boolean {
    return url === prefix || url.startsWith(prefix + '/');
}

export function effectiveUrl(url: string): string {
    const [path, query = ''] = (url || '').split('?');
    if (/^\/app\/reports\/trace(\/|$)/.test(path.split('#')[0])) {
        const from = /(?:^|&)from=([a-z]+)/.exec(query)?.[1];
        if (from === 'sales') return '/app/sales';
        if (from === 'harvests') return '/app/harvests';
        if (from === 'reports' || /^\/app\/reports\/trace\/lot\//.test(path)) return '/app/reports';
        return '/app/production/batches';
    }
    return url;
}

export function pathOf(url: string): string {
    return (url || '').split(/[?#]/)[0];
}

export function sectionOfUrl(url: string): Section | undefined {
    const path = pathOf(url);
    return SECTIONS.find((s) => s.prefixes.some((p) => startsWithPath(path, p)));
}

export function leafTabs(section: Section): SectionTab[] {
    return section.tabs.flatMap((t) => (t.children?.length ? t.children : [t]));
}

export function activeTab(section: Section, url: string): { tab: SectionTab; child?: SectionTab } | undefined {
    const path = pathOf(url);
    let best: { tab: SectionTab; child?: SectionTab } | undefined;
    let bestLength = -1;
    for (const tab of section.tabs) {
        const pages = tab.children?.length ? tab.children.map((child) => ({ tab, child })) : [{ tab, child: undefined }];
        for (const page of pages) {
            const route = (page.child ?? page.tab).route;
            if (startsWithPath(path, route) && route.length > bestLength) {
                best = page;
                bestLength = route.length;
            }
        }
    }
    return best;
}

export function activeTabRoute(section: Section, url: string): string | undefined {
    return activeTab(section, url)?.tab.route;
}
