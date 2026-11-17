import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PermissionCheckerService } from 'abp-ng2-module';
import { FpmsApiService } from '@shared/fpms/api.service';

@Component({
    selector: 'fpms-health-banner',
    standalone: true,
    imports: [RouterLink],
    template: `
        @if (banner(); as b) {
            @if (dismissed() !== b.message) {
                <div class="alert alert-danger fpms-health-banner mb-0 rounded-0 no-print" role="alert">
                    <div class="d-flex align-items-center">
                        <i class="fas fa-exclamation-triangle mr-2" aria-hidden="true"></i>
                        <strong class="mr-2">{{ b.message }}</strong>
                        @if (isAdmin) {
                            <a routerLink="/app/system/health" class="alert-link mr-3">Open system health</a>
                            @if (b.details.length) {
                                <button type="button" class="btn btn-link btn-sm alert-link p-0" [attr.aria-expanded]="showDetails()" (click)="showDetails.set(!showDetails())">
                                    {{ showDetails() ? 'Hide details' : 'Show details' }}
                                </button>
                            }
                        }
                        <button type="button" class="btn btn-sm btn-outline-danger ml-auto" (click)="dismiss(b.message)">Dismiss</button>
                    </div>
                    @if (isAdmin && showDetails()) {
                        <ul class="mb-0 mt-1 pl-4">
                            @for (d of b.details; track $index) {
                                <li>{{ d }}</li>
                            }
                        </ul>
                    }
                </div>
            }
        }
    `,
})
export class HealthBannerComponent implements OnInit, OnDestroy {
    private api = inject(FpmsApiService);
    private permissions = inject(PermissionCheckerService);
    private timer: ReturnType<typeof setInterval> | undefined;

    banner = signal<{ message: string; details: string[] } | null>(null);
    showDetails = signal(false);
    dismissed = signal<string | null>(this.readDismissed());

    dismiss(message: string): void {
        this.dismissed.set(message);
        try {
            sessionStorage.setItem('fpms.healthBannerDismissed', message);
        } catch {
        }
    }

    private readDismissed(): string | null {
        try {
            return sessionStorage.getItem('fpms.healthBannerDismissed');
        } catch {
            return null;
        }
    }

    get isAdmin(): boolean {
        return this.permissions.isGranted('System.Health');
    }

    ngOnInit(): void {
        if (!this.isAdmin && !this.permissions.isGranted('Farm.Dashboard.Full')) {
            return;
        }
        this.load();
        this.timer = setInterval(() => this.load(), 5 * 60 * 1000);
    }

    ngOnDestroy(): void {
        clearInterval(this.timer);
    }

    private async load(): Promise<void> {
        try {
            const r = await this.api.get<{ needsAttention: boolean; message: string; details: string[] }>('HealthBanner', 'Get');
            this.banner.set(r?.needsAttention ? { message: r.message, details: r.details ?? [] } : null);
        } catch {
            this.banner.set(null);
        }
    }
}
