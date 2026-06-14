import {
  Component,
  computed,
  inject,
  signal,
  OnInit,
  ChangeDetectionStrategy,
  DestroyRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { UserService } from '../../Services/user.service';
import { AdminUser, UserRole, UserStatus, ROLE_OPTIONS } from '../../Models/user.model';

type Toast = { type: 'success' | 'error'; text: string } | null;

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './users.component.html',
  styleUrls: ['./users.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsersComponent implements OnInit {
  private svc = inject(UserService);
  private destroyRef = inject(DestroyRef);

  // ===== State =====
  users = signal<AdminUser[]>([]);
  loading = signal(false);
  total = signal(0);
  toast = signal<Toast>(null);
  processing = signal<string | null>(null);

  // ===== Inline Ban State =====
  banningUserId = signal<string | null>(null);
  banReasonInput = signal('');

  // ===== Filters =====
  searchQuery = signal('');
  private searchSubject = new Subject<string>(); // تحسين عشان نقلل الضغط ع السيرفر

  roleFilter = signal<UserRole | 'all'>('all');
  statusFilter = signal<UserStatus>('all');
  currentPage = signal(1);
  pageSize = signal(10);

  // ===== Constants =====
  readonly ROLE_OPTIONS = ROLE_OPTIONS;
  readonly STATUS_OPTIONS: { key: UserStatus; labelKey: string; icon: string }[] = [
    { key: 'all', labelKey: 'ADMIN.USERS.ALL_USERS', icon: 'fa-users' },
    { key: 'active', labelKey: 'ADMIN.USERS.ACTIVE', icon: 'fa-circle-check' },
    { key: 'banned', labelKey: 'ADMIN.USERS.BANNED', icon: 'fa-ban' },
  ];

  // ===== Computed =====
  totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize())));

  visiblePages = computed<(number | '...')[]>(() => {
    const total = this.totalPages(),
      c = this.currentPage();
    const pages: (number | '...')[] = [];
    if (total <= 7) {
      for (let i = 1; i <= total; i++) pages.push(i);
      return pages;
    }
    pages.push(1);
    if (c > 3) pages.push('...');
    for (let i = Math.max(2, c - 1); i <= Math.min(total - 1, c + 1); i++) pages.push(i);
    if (c < total - 2) pages.push('...');
    pages.push(total);
    return pages;
  });

  pageStart = computed(() =>
    this.total() === 0 ? 0 : (this.currentPage() - 1) * this.pageSize() + 1,
  );
  pageEnd = computed(() => Math.min(this.currentPage() * this.pageSize(), this.total()));

  ngOnInit() {
    this.loadUsers();

    // تشغيل الـ Debounce الخاص بالبحث
    this.searchSubject
      .pipe(
        debounceTime(400), // استنى 400 ملي ثانية قبل ما تبعت
        distinctUntilChanged(), // متبعتش لو نفس الكلمة
        takeUntilDestroyed(this.destroyRef), // تنظيف تلقائي للـ memory leak
      )
      .subscribe((query) => {
        this.searchQuery.set(query);
        this.currentPage.set(1);
        this.loadUsers();
      });
  }

  loadUsers() {
    this.loading.set(true);
    this.svc
      .getUsers({
        page: this.currentPage(),
        pageSize: this.pageSize(),
        search: this.searchQuery(),
        role: this.roleFilter() === 'all' ? undefined : this.roleFilter(),
      })
      .subscribe({
        next: (res) => {
          let items = res.items;
          // ملحوظة: لو الباك-إند دعم الـ Status مستقبلاً، يُفضل تبعتها مع المتغيرات فوق وتمسح الفلتر ده.
          if (this.statusFilter() === 'active') items = items.filter((u) => !u.banReason);
          if (this.statusFilter() === 'banned') items = items.filter((u) => !!u.banReason);

          this.users.set(items);
          this.total.set(res.total);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.showToast('error', 'Failed to load users');
        },
      });
  }

  // ===== Filter Actions =====
  onSearch(e: Event) {
    const value = (e.target as HTMLInputElement).value;
    this.searchSubject.next(value);
  }

  clearSearch() {
    this.searchQuery.set('');
    this.searchSubject.next('');
  }

  setRole(r: UserRole | 'all') {
    this.roleFilter.set(r);
    this.currentPage.set(1);
    this.loadUsers();
  }

  setStatus(s: UserStatus) {
    this.statusFilter.set(s);
    this.currentPage.set(1);
    this.loadUsers();
  }

  clearFilters() {
    this.searchQuery.set('');
    this.roleFilter.set('all');
    this.statusFilter.set('all');
    this.currentPage.set(1);
    this.loadUsers();
  }

  hasFilters = computed(
    () => this.searchQuery().trim() || this.roleFilter() !== 'all' || this.statusFilter() !== 'all',
  );

  // ===== Pagination =====
  goToPage(p: number | '...') {
    if (typeof p === 'number') {
      this.currentPage.set(p);
      this.loadUsers();
    }
  }

  nextPage() {
    if (this.currentPage() < this.totalPages()) {
      this.currentPage.update((p) => p + 1);
      this.loadUsers();
    }
  }

  prevPage() {
    if (this.currentPage() > 1) {
      this.currentPage.update((p) => p - 1);
      this.loadUsers();
    }
  }

  // ===== Inline Ban Toggle =====
  toggleBanForm(userId: string) {
    if (this.banningUserId() === userId) {
      this.banningUserId.set(null);
      this.banReasonInput.set('');
    } else {
      this.banningUserId.set(userId);
      this.banReasonInput.set('');
    }
  }

  cancelBan() {
    this.banningUserId.set(null);
    this.banReasonInput.set('');
  }

  confirmBan(user: AdminUser) {
    const reason = this.banReasonInput().trim();
    if (reason.length < 5) return;
    this.processing.set(user.id);
    this.svc.banUser(user.id, reason).subscribe({
      next: () => {
        this.banningUserId.set(null);
        this.banReasonInput.set('');
        this.processing.set(null);
        this.showToast('success', `"${user.fullName}" has been banned`);
        this.loadUsers();
      },
      error: () => {
        this.processing.set(null);
        this.showToast('error', 'Failed to ban user');
      },
    });
  }

  confirmUnban(user: AdminUser) {
    this.processing.set(user.id);
    this.svc.unbanUser(user.id).subscribe({
      next: () => {
        this.processing.set(null);
        this.showToast('success', `"${user.fullName}" has been unbanned`);
        this.loadUsers();
      },
      error: () => {
        this.processing.set(null);
        this.showToast('error', 'Failed to unban user');
      },
    });
  }

  // ===== Helpers =====
  isBanned(u: AdminUser) {
    return !!u.banReason || !!u.bannedAt;
  }

  roleInfo(role: UserRole) {
    return (
      ROLE_OPTIONS.find((r) => r.key === role) ?? { label: role, icon: 'fa-user', color: 'gray' }
    );
  }

  avatarLetter(name: string) {
    return (name || '?').charAt(0).toUpperCase();
  }

  formatDate(iso: string | null) {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  }

  formatDateTime(iso: string | null) {
    if (!iso) return '—';
    return new Date(iso).toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  showToast(type: 'success' | 'error', text: string) {
    this.toast.set({ type, text });
    setTimeout(() => this.toast.set(null), 3500);
  }
}
