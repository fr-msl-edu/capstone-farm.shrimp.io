export const PRICE_METHODS = [
    { value: 1, label: 'Fixed price' },
    { value: 2, label: 'Base ABW price' },
    { value: 3, label: 'Base count price' },
    { value: 4, label: 'From a price table' },
];

const money = (v: number) => Math.round(v * 100) / 100;

export function num(v: any): number | null {
    return v === '' || v == null || isNaN(Number(v)) ? null : Number(v);
}

export function wholeCount(count: number): number {
    return Math.round(count);
}

export function countFromAbw(abwG: number | null): number | null {
    return abwG && abwG > 0 ? wholeCount(1000 / (Math.round(abwG * 100) / 100)) : null;
}

export function baseAbwPrice(base: number, baseAbwG: number, stepPerG: number, abwG: number): number {
    return money(base + (Math.round(abwG * 100) / 100 - baseAbwG) * stepPerG);
}

export function baseCountPrice(base: number, baseCount: number, stepPerCount: number, count: number): number {
    return money(base + (baseCount - wholeCount(count)) * stepPerCount);
}

export function tableRow(table: any, count: number | null): any {
    if (!table || count == null) {
        return null;
    }
    const c = wholeCount(count);
    return (
        [...(table.rows ?? [])]
            .sort((a, b) => (a.minCountPerKg ?? 0) - (b.minCountPerKg ?? 0))
            .find((r) => (r.minCountPerKg == null || c >= r.minCountPerKg) && (r.maxCountPerKg == null || c <= r.maxCountPerKg)) ?? null
    );
}

export function referencePrice(t: any, abwG: number | null, countPerKg: number | null, tables: any[]): number | null {
    const base = num(t.basePricePerKg);
    switch (t.priceMethod) {
        case 2: {
            const baseAbw = num(t.baseAbwG), step = num(t.stepPerG);
            return base == null || baseAbw == null || step == null || abwG == null ? null : baseAbwPrice(base, baseAbw, step, abwG);
        }
        case 3: {
            const baseCount = num(t.baseCountPerKg), step = num(t.stepPerCount);
            return base == null || baseCount == null || step == null || countPerKg == null ? null : baseCountPrice(base, baseCount, step, countPerKg);
        }
        case 4: {
            const row = tableRow(tables.find((x) => x.id === t.priceTableId), countPerKg);
            return row ? Number(row.pricePerKg) : null;
        }
        default:
            return null;
    }
}

export function pricingBody(t: any): any {
    const m = t.priceMethod ?? 1;
    return {
        priceMethod: m,
        basePricePerKg: m === 2 || m === 3 ? num(t.basePricePerKg) : null,
        baseAbwG: m === 2 ? num(t.baseAbwG) : null,
        stepPerG: m === 2 ? num(t.stepPerG) : null,
        priceAbwG: m === 2 ? num(t.priceAbwG) : null,
        baseCountPerKg: m === 3 ? num(t.baseCountPerKg) : null,
        stepPerCount: m === 3 ? num(t.stepPerCount) : null,
        priceCountPerKg: m === 3 || m === 4 ? num(t.priceCountPerKg) : null,
        priceTableId: m === 4 ? t.priceTableId || null : null,
    };
}

export function applyTerms(target: any, terms: any): boolean {
    if (!terms || terms.priceMethod !== target.priceMethod) {
        return false;
    }
    switch (terms.priceMethod) {
        case 2:
            Object.assign(target, { basePricePerKg: terms.basePricePerKg, baseAbwG: terms.baseAbwG, stepPerG: terms.stepPerG });
            return true;
        case 3:
            Object.assign(target, { basePricePerKg: terms.basePricePerKg, baseCountPerKg: terms.baseCountPerKg, stepPerCount: terms.stepPerCount });
            return true;
        case 4:
            target.priceTableId = terms.priceTableId;
            return true;
    }
    return false;
}

export function termsEmpty(t: any): boolean {
    switch (t.priceMethod) {
        case 2:
            return num(t.basePricePerKg) == null && num(t.baseAbwG) == null && num(t.stepPerG) == null;
        case 3:
            return num(t.basePricePerKg) == null && num(t.baseCountPerKg) == null && num(t.stepPerCount) == null;
        case 4:
            return !t.priceTableId;
        default:
            return false;
    }
}

export function usedAbw(t: any, harvestAbwG: number | null): number | null {
    const v = num(t.priceAbwG) ?? harvestAbwG ?? null;
    return v == null ? null : Math.round(Number(v) * 100) / 100;
}

export function usedCount(t: any, defaultCountPerKg: number | null): number | null {
    const typed = num(t.priceCountPerKg);
    return typed != null ? wholeCount(typed) : (defaultCountPerKg ?? null);
}

export function priceWorking(t: any, abwG: number | null, countPerKg: number | null, tables: any[]): string {
    const php = (v: any) => '₱' + Number(v).toFixed(2);
    switch (t.priceMethod) {
        case 2:
            return `${php(t.basePricePerKg)} + (${Number(abwG).toFixed(2)} − ${Number(t.baseAbwG).toFixed(2)}) × ${php(t.stepPerG)}`;
        case 3:
            return `${php(t.basePricePerKg)} + (${t.baseCountPerKg} − ${countPerKg}) × ${php(t.stepPerCount)}`;
        case 4: {
            const table = (tables ?? []).find((x) => x.id === t.priceTableId);
            return `${table?.name ?? ''}, ${tableRow(table, countPerKg)?.label ?? ''} (count ${countPerKg})`;
        }
    }
    return '';
}

export function priceOverridden(pricePerKg: any, reference: number | null): boolean {
    return reference !== null && pricePerKg !== null && pricePerKg !== '' && pricePerKg !== undefined && Math.abs(Number(pricePerKg) - reference) >= 0.01;
}
