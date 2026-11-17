export type CellState = 'na' | 'gap' | 'c1' | 'c2' | 'c3' | 'today-empty';

export interface CalendarDot {
    kind: 'm' | 'g' | 'c';
    label: string;
}

export interface CalendarCell {
    date: string;
    count: number;
    state: CellState;
    title: string;
    dots: CalendarDot[];
    water: 'in' | 'missing' | 'na';
    waterOut?: boolean;
    today: boolean;
}

const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

export function shortDate(date: string): string {
    const [, m, d] = date.split('-').map(Number);
    return `${MONTHS[m - 1]} ${d}`;
}

export function dotsOf(c: any): CalendarDot[] {
    const out: CalendarDot[] = [];
    if (c.mortality) out.push({ kind: 'm', label: 'mortality entered' });
    if (c.growthSampling) out.push({ kind: 'g', label: 'growth sampling entered' });
    if (c.chemical) out.push({ kind: 'c', label: 'chemical entered' });
    return out;
}

export function feedingsOf(d: any): number[] {
    return (d?.feedings as number[]) ?? [];
}

export function dayRecords(d: any): { count: number; total: number } {
    const all = d?.water === 3 ? [...feedingsOf(d)] : [...feedingsOf(d), d?.water ?? 0];
    return { count: all.filter((v) => v !== 0).length, total: all.length };
}

export function recordScale(count: number, total: number): 0 | 1 | 2 | 3 {
    if (count === 0) return 0;
    if (count >= total) return 3;
    return count * 2 < total ? 1 : 2;
}

export function feedingList(d: any, status: number): string {
    const nos = feedingsOf(d).map((v, i) => (v === status ? i + 1 : 0)).filter((n) => n);
    return nos.length ? (nos.length === 1 ? 'feeding ' : 'feedings ') + nos.join(', ') : '';
}

export function dayFeedings(d: any): { count: number; total: number } {
    const all = feedingsOf(d);
    return { count: all.filter((v) => v !== 0).length, total: all.length };
}

export function buildCalendarCells(
    row: any,
    dates: string[],
    today: string
): { cells: CalendarCell[]; gapDates: string[] } {
    const gapDates: string[] = [];
    const cells = dates.map<CalendarCell>((date) => {
        const c = (row.days as any[]).find((x) => String(x.date).substring(0, 10) === date);
        if (!c) {
            return { date, count: 0, state: 'na', title: `${shortDate(date)}: not stocked yet`, dots: [], water: 'na', today: date === today };
        }
        const all = dayRecords(c);
        const fed = dayFeedings(c);
        const water = c.water === 1 ? 'in' : c.water === 3 ? 'na' : 'missing';
        const dots = dotsOf(c);
        const extra = dots.length ? '. ' + dots.map((x) => x.label).join(', ') : '';
        const waterOut = !!c.waterOutOfRange;
        const title = `${shortDate(date)}: ${fed.count} of ${fed.total} feedings${water === 'in' ? (waterOut ? ', a water reading out of range' : ', water quality entered') : water === 'missing' ? ', water quality not entered' : ''}${extra}`;
        if (all.count === 0 && date === today) {
            return { date, count: 0, state: 'today-empty', title: `${shortDate(date)}: today, no entry yet`, dots, water, waterOut, today: true };
        }
        if (all.count < all.total && date !== today) {
            gapDates.push(date);
        }
        const scale = fed.total ? recordScale(fed.count, fed.total) : recordScale(all.count, all.total);
        const state: CellState = scale === 0 ? 'gap' : (`c${scale}` as CellState);
        return { date, count: fed.total ? fed.count : all.count, state, title: date === today ? `${title} (today)` : title, dots, water, waterOut, today: date === today };
    });
    return { cells, gapDates };
}

export const LOG_CALENDAR_STYLES = `
            .sq {
                position: relative;
                height: 32px;
                border-radius: 6px;
                display: flex;
                align-items: center;
                justify-content: center;
                font-size: 0.875rem;
                font-weight: 600;
                border: 1px solid transparent;
                box-sizing: border-box;
                color: var(--fpms-text);
            }
            i.sq {
                display: inline-block;
                width: 16px;
                height: 16px;
                vertical-align: middle;
                margin-right: 6px;
            }
            .sq.na {
                border-color: var(--fpms-line);
                background: transparent;
                pointer-events: none;
            }
            .sq.gap {
                background: var(--fpms-blank-fill);
                border-color: var(--fpms-blank-fill);
            }
            .sq.c1,
            .sq.c2 {
                background: var(--fpms-warn-bg);
                border-color: var(--fpms-warn-line);
            }
            .sq.c3 {
                background: var(--fpms-success-100);
                border-color: var(--fpms-success-300);
            }
            .sq.is-today,
            i.sq.is-today {
                box-shadow: 0 0 0 2px var(--fpms-primary);
            }
            .sq.today-empty {
                background: var(--fpms-surface);
                border: 1px dashed var(--fpms-control-border);
            }
            .dot,
            i.dot-key {
                width: 8px;
                height: 8px;
                border-radius: 50%;
                border: 1px solid var(--fpms-surface);
                box-sizing: border-box;
            }
            .dot {
                position: absolute;
                top: 2px;
                right: 2px;
            }
            .dot + .dot {
                right: 11px;
            }
            .dot + .dot + .dot {
                right: 20px;
            }
            i.dot-key {
                display: inline-block;
                margin-right: 6px;
            }
            .dot-m {
                background: var(--fpms-error);
            }
            .dot-g {
                background: var(--fpms-primary);
            }
            .dot-c {
                background: var(--fpms-chemical);
            }
            a.sq:hover {
                text-decoration: none;
                box-shadow: 0 0 0 2px var(--fpms-accent);
            }
`;
