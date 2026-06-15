import { Component, inject, signal, computed } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators, FormGroup } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop'; // استدعاء مهم جداً لربط الفورم بالسيجنال
import { map } from 'rxjs'; // استدعاء للتعامل مع تغيرات الفورم
import { MaintenanceService } from '../../Services/maintenance';
import { MaintenanceCategory, MaintenanceCompany } from '../../Models';
import { DecimalPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

type Toast = { type: 'success' | 'error'; text: string } | null;
type View = 'list' | 'create';

@Component({
  selector: 'app-maintenance-companies',
  standalone: true,
  imports: [ReactiveFormsModule, DecimalPipe, TranslatePipe],
  templateUrl: './maintenance-companies.component.html',
  styleUrls: ['./maintenance-companies.component.scss'],
})
export class MaintenanceCompaniesComponent {
  private service = inject(MaintenanceService);
  private fb = inject(FormBuilder);

  // ===== View state =====
  view = signal<View>('list');

  // ===== Companies =====
  companies = signal<MaintenanceCompany[]>([]);
  loadingCompanies = signal(false);
  searchQuery = signal('');

  filteredCompanies = computed(() => {
    const q = this.searchQuery().toLowerCase().trim();
    if (!q) return this.companies();
    return this.companies().filter(
      (c) =>
        c.companyName.toLowerCase().includes(q) ||
        c.email.toLowerCase().includes(q) ||
        c.city.toLowerCase().includes(q) ||
        c.categories.some((cat) => cat.name.toLowerCase().includes(q)),
    );
  });

  // ===== Categories =====
  categories = signal<MaintenanceCategory[]>([]);
  loadingCategories = signal(false);
  showAddCategory = signal(false);
  selectedCategoryIds = signal<string[]>([]);
  toast = signal<Toast>(null);

  savingCategory = signal(false);
  savingCompany = signal(false);

  // خليناها Signal
  iconOptions = signal([
    'fa-screwdriver-wrench',
    'fa-plug',
    'fa-faucet',
    'fa-paint-roller',
    'fa-broom',
    'fa-bolt',
    'fa-snowflake',
    'fa-fire',
    'fa-wrench',
    'fa-hammer',
    'fa-toilet',
    'fa-shower',
    'fa-sink',
    'fa-temperature-half',
    'fa-fan',
    'fa-trash-can',
  ]);

  // ===== Forms =====
  categoryForm: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    icon: ['fa-screwdriver-wrench', [Validators.required]],
    description: ['', [Validators.required, Validators.minLength(5)]],
  });

  companyForm: FormGroup = this.fb.group({
    companyName: ['', [Validators.required, Validators.minLength(2)]],
    fullName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    phone: ['', [Validators.required, Validators.pattern(/^\+?[0-9\s-]{8,}$/)]],
    tempPassword: ['', [Validators.required, Validators.minLength(8)]],
    city: ['', [Validators.required]],
    serviceRadiusKm: [10, [Validators.required, Validators.min(1), Validators.max(500)]],
    logoUrl: [''],
    description: ['', [Validators.required, Validators.minLength(10)]],
  });

  // ===== ربط حالة الفورم بالـ Signals (التعديل الأهم) =====

  // 1. هنعمل Signal بيراقب الفورم كل ما اليوزر يكتب حرف، وبيشوف الفورم Valid ولا لأ
  isCompanyFormValid = toSignal(
    this.companyForm.statusChanges.pipe(map(() => this.companyForm.valid)),
    { initialValue: this.companyForm.valid },
  );

  selectedCount = computed(() => this.selectedCategoryIds().length);

  // 2. الـ computed دلوقتي جواه 2 Signals فهيحس بأي تغيير في الفورم أو في اختيار الـ Categories
  canSubmitCompany = computed(() => this.isCompanyFormValid() && this.selectedCount() > 0);

  constructor() {
    this.loadCompanies();
    this.loadCategories();
  }

  // ===== View switching =====
  goToCreate() {
    this.view.set('create');
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  backToList() {
    this.view.set('list');
    this.resetCompanyForm();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  // ===== Companies =====
  loadCompanies() {
    this.loadingCompanies.set(true);
    this.service.getCompanies().subscribe({
      next: (list) => {
        this.companies.set(list);
        this.loadingCompanies.set(false);
      },
      error: () => this.loadingCompanies.set(false),
    });
  }

  onSearchInput(event: Event) {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }

  // ===== Categories =====
  loadCategories() {
    this.loadingCategories.set(true);
    this.service.getCategories().subscribe({
      next: (list) => {
        this.categories.set(list);
        this.loadingCategories.set(false);
      },
      error: () => this.loadingCategories.set(false),
    });
  }

  toggleAddCategory() {
    this.showAddCategory.update((v) => !v);
    if (!this.showAddCategory()) this.categoryForm.reset({ icon: 'fa-screwdriver-wrench' });
  }

  // التعديل هنا بسيط عشان iconOptions بقت Signal
  pickIcon(icon: string) {
    this.categoryForm.patchValue({ icon });
  }

  submitCategory() {
    if (this.categoryForm.invalid) {
      this.categoryForm.markAllAsTouched();
      return;
    }
    this.savingCategory.set(true);
    this.service.createCategory(this.categoryForm.value).subscribe({
      next: (created) => {
        this.categories.update((list) => [...list, created]);
        this.categoryForm.reset({ icon: 'fa-screwdriver-wrench' });
        this.showAddCategory.set(false);
        this.savingCategory.set(false);
        this.showToast('success', 'Category added successfully ✨');
        this.loadCategories(); // إعادة تحميل الشركات لعرض التصنيف الجديد في قائمة كل شركة (حتى لو ما عنده شركات حالياً)
      },
      error: () => {
        this.savingCategory.set(false);
        this.showToast('error', 'Failed to add category');
      },
    });
  }

  removeCategory(c: MaintenanceCategory) {
    if (!confirm(`Delete "${c.name}"?`)) return;
    this.service.deleteCategory(c.id).subscribe({
      next: () => {
        this.categories.update((list) => list.filter((x) => x.id !== c.id));
        this.selectedCategoryIds.update((ids) => ids.filter((id) => id !== c.id));
        this.showToast('success', 'Category deleted');
      },
      error: () => this.showToast('error', 'Failed to delete category'),
    });
  }

  // ===== Company =====
  toggleCategorySelection(id: string) {
    this.selectedCategoryIds.update((ids) =>
      ids.includes(id) ? ids.filter((x) => x !== id) : [...ids, id],
    );
  }

  isCategorySelected(id: string): boolean {
    return this.selectedCategoryIds().includes(id);
  }

  submitCompany() {
    if (this.companyForm.invalid) {
      this.companyForm.markAllAsTouched();
      return;
    }
    if (this.selectedCategoryIds().length === 0) {
      this.showToast('error', 'Please select at least one category');
      return;
    }

    this.savingCompany.set(true);
    const body = {
      ...this.companyForm.value,
      categoryIds: this.selectedCategoryIds(),
    };

    this.service.createCompany(body).subscribe({
      next: (created) => {
        this.savingCompany.set(false);
        this.companies.update((list) => [created, ...list]);
        this.resetCompanyForm();
        this.view.set('list');
        window.scrollTo({ top: 0, behavior: 'smooth' });
        this.showToast('success', `Company "${created.companyName}" created successfully 🎉`);
        this.loadCompanies(); // إعادة تحميل الشركات لعرض الشركة الجديدة في القائمة مع بياناتها الكاملة (مثل التصنيفات)
      },
      error: () => {
        this.savingCompany.set(false);
        this.showToast('error', 'Failed to create company. Please try again.');
      },
    });
  }

  resetCompanyForm() {
    this.companyForm.reset({ serviceRadiusKm: 10 });
    this.selectedCategoryIds.set([]);
  }

  generatePassword() {
    const chars = 'ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789';
    let pass = '';
    for (let i = 0; i < 12; i++) {
      pass += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    this.companyForm.patchValue({ tempPassword: pass });
  }

  // ===== Helpers =====
  showToast(type: 'success' | 'error', text: string) {
    this.toast.set({ type, text });
    setTimeout(() => this.toast.set(null), 3500);
  }

  hasError(form: FormGroup, field: string, error?: string): boolean {
    const c = form.get(field);
    if (!c || !c.touched) return false;
    return error ? c.hasError(error) : c.invalid;
  }

  initials(name: string): string {
    return name
      .split(' ')
      .map((n) => n[0])
      .slice(0, 2)
      .join('')
      .toUpperCase();
  }

  formatDate(iso: string): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  }
}
