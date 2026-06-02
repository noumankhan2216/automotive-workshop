import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map } from 'rxjs/operators';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { BreakpointObserver } from '@angular/cdk/layout';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../../core/services/auth.service';

/** Below this width the sidebar collapses into an overlay drawer (tablet + mobile). */
const COLLAPSE_QUERY = '(max-width: 1024px)';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatButtonModule
  ],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss'
})
export class ShellComponent {
  private readonly router = inject(Router);
  private readonly breakpoints = inject(BreakpointObserver);

  /** True when the viewport is tablet-sized or smaller. */
  readonly collapsed = toSignal(
    this.breakpoints.observe(COLLAPSE_QUERY).pipe(map(state => state.matches)),
    { initialValue: this.breakpoints.isMatched(COLLAPSE_QUERY) }
  );

  readonly navItems = [
    { label: 'Dashboard', route: '/dashboard', icon: 'dashboard' },
    { label: 'Scheduler', route: '/scheduler', icon: 'calendar_month' },
    { label: 'Estimates', route: '/estimates', icon: 'request_quote' },
    { label: 'Work Orders', route: '/work-orders', icon: 'build' },
    { label: 'Invoices', route: '/invoices', icon: 'receipt_long' },
    { label: 'Parts', route: '/parts', icon: 'inventory_2' },
    { label: 'Reports', route: '/reports', icon: 'assessment' },
    { label: 'Customers', route: '/customers', icon: 'people' },
    { label: 'Vehicles', route: '/vehicles', icon: 'directions_car' }
  ];

  private readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map(e => e.urlAfterRedirects)
    ),
    { initialValue: this.router.url }
  );

  readonly activeLabel = computed(() => {
    const url = this.currentUrl();
    const match = [...this.navItems]
      .sort((a, b) => b.route.length - a.route.length)
      .find(i => url === i.route || url.startsWith(i.route + '/'));
    return match?.label ?? 'Dashboard';
  });

  readonly initials = computed(() => {
    const name = this.auth.currentUser()?.fullName ?? '';
    return name
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map(p => p[0]?.toUpperCase())
      .join('') || 'U';
  });

  constructor(protected readonly auth: AuthService) {}
}
