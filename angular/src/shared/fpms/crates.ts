export function parseCrateNos(text: string): { nos: number[]; error: string | null } {
    const nos = new Set<number>();
    for (const part of (text ?? '').split(/[\s,;]+/).filter((p) => p)) {
        const m = /^(\d+)(?:\s*[-–]\s*(\d+))?$/.exec(part);
        if (!m) {
            return { nos: [], error: `"${part}" is not a crate number or a range such as 1-10.` };
        }
        const from = Number(m[1]);
        const to = m[2] ? Number(m[2]) : from;
        if (from < 1 || to < from || to - from > 2000) {
            return { nos: [], error: `"${part}" is not a valid range.` };
        }
        for (let n = from; n <= to; n++) {
            nos.add(n);
        }
    }
    return { nos: [...nos].sort((a, b) => a - b), error: null };
}

export function formatCrateNos(nos: number[]): string {
    const sorted = [...new Set(nos)].sort((a, b) => a - b);
    const parts: string[] = [];
    for (let i = 0; i < sorted.length; i++) {
        let j = i;
        while (j + 1 < sorted.length && sorted[j + 1] === sorted[j] + 1) {
            j++;
        }
        parts.push(j > i ? `${sorted[i]}-${sorted[j]}` : `${sorted[i]}`);
        i = j;
    }
    return parts.join(', ');
}

export function crateBands(crates: any[], perBlock: number, span: number): any[][][] {
    const blocks: any[][] = [];
    for (let i = 0; i < crates.length; i += perBlock) {
        blocks.push(crates.slice(i, i + perBlock));
    }
    const perBand = Math.max(2, Math.floor(15 / span));
    const bands: any[][][] = [];
    for (let i = 0; i < blocks.length; i += perBand) {
        bands.push(blocks.slice(i, i + perBand));
    }
    return bands;
}

export function crateKg(crates: any[]): number {
    return Math.round((crates ?? []).reduce((s: number, c: any) => s + (Number(c.kg) || 0), 0) * 1000) / 1000;
}

export interface WhereRow {
    to: string;
    receipt?: string;
    saleId?: string;
    crates?: number;
    kg: number;
    amount?: number;
}

export function whereItWent(h: any, sales: any[], reasonLabels: Record<number, string>): WhereRow[] {
    if (!h) {
        return [];
    }
    const rows: WhereRow[] = (sales ?? []).map((s: any) => {
        const crates = (h.crates ?? []).filter((c: any) => c.saleId === s.saleId).length;
        return { to: s.buyerName, receipt: s.dispatchReceiptNo, saleId: s.saleId, crates: crates || undefined, kg: s.kg, amount: s.amount };
    });
    if (!rows.length && h.soldKg > 0) {
        rows.push({ to: 'Sold', kg: h.soldKg });
    }
    for (const d of h.disposals ?? []) {
        rows.push({ to: `Not sold: ${reasonLabels[d.reason] ?? 'Other'}${d.note ? ' (' + d.note + ')' : ''}`, kg: d.kg });
    }
    if (h.unaccountedKg > 0) {
        rows.push({ to: 'Not yet accounted for', kg: h.unaccountedKg });
    }
    return rows;
}
