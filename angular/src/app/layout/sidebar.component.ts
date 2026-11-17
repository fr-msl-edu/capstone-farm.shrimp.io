import { Component, ChangeDetectionStrategy, Renderer2, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AppAuthService } from '@shared/auth/app-auth.service';
import { SidebarLogoComponent } from './sidebar-logo.component';
import { SidebarUserPanelComponent } from './sidebar-user-panel.component';
import { SidebarMenuComponent } from './sidebar-menu.component';
import { navMini, setNavMini } from '@shared/fpms/nav-mode';

@Component({
    selector: 'sidebar',
    templateUrl: './sidebar.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: true,
    imports: [SidebarLogoComponent, SidebarUserPanelComponent, SidebarMenuComponent, RouterLink],
})
export class SidebarComponent implements OnInit {
    private _authService = inject(AppAuthService);
    private renderer = inject(Renderer2);

    ngOnInit(): void {
        this.renderer.removeClass(document.body, 'sidebar-collapse');
        this.renderer.removeClass(document.body, 'sidebar-open');
    }

    readonly navMini = navMini;
    private peekTimer: ReturnType<typeof setTimeout> | undefined;

    toggleMini(): void {
        this.peek(false);
        setNavMini(!navMini());
    }

    enter(event: PointerEvent): void {
        if (event.pointerType !== 'mouse' || !this.collapsed) {
            return;
        }
        clearTimeout(this.peekTimer);
        this.peekTimer = setTimeout(() => this.peek(true), 250);
    }

    leave(): void {
        clearTimeout(this.peekTimer);
        this.peek(false);
    }

    focusOut(event: FocusEvent): void {
        const to = event.relatedTarget as Node | null;
        if (!to || !(event.currentTarget as HTMLElement).contains(to)) {
            this.peek(false);
        }
    }

    private get collapsed(): boolean {
        return document.body.classList.contains('fpms-nav-mini');
    }

    peek(open: boolean): void {
        const on = open && this.collapsed;
        document.body.classList.toggle('fpms-nav-peek', on);
    }

    logout(): void {
        this._authService.logout();
    }
}
