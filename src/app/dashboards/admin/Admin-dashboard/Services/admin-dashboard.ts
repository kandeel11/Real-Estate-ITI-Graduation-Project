import { Injectable } from '@angular/core';
import { Observable, of, delay, throwError } from 'rxjs';
import {
  AdminUser,
  Company,
  DashboardStats,
  MonthlyRevenuePoint,
  PendingProperty,
  RentPayment,
} from '../Models';

@Injectable({ providedIn: 'root' })
export class AdminDashboardService {
  // ===== STATS =====
  getStats(): Observable<DashboardStats> {
    return of({
      totalUsers: 1247,
      usersGrowth: 12.5,
      totalCompanies: 84,
      companiesGrowth: 8.3,
      pendingProperties: 17,
      monthlyRevenue: 245800,
      revenueGrowth: 14.2,
    }).pipe(delay(300));
  }

  // ===== MONTHLY REVENUE CHART =====
  getMonthlyRevenue(): Observable<MonthlyRevenuePoint[]> {
    return of([
      { month: 'Jan', amount: 18500 },
      { month: 'Feb', amount: 22100 },
      { month: 'Mar', amount: 19800 },
      { month: 'Apr', amount: 25400 },
      { month: 'May', amount: 28900 },
      { month: 'Jun', amount: 24500 },
      { month: 'Jul', amount: 31200 },
      { month: 'Aug', amount: 29700 },
      { month: 'Sep', amount: 33800 },
      { month: 'Oct', amount: 36100 },
      { month: 'Nov', amount: 38200 },
      { month: 'Dec', amount: 41500 },
    ]).pipe(delay(400));
  }

  // ===== PENDING PROPERTIES =====
  getPendingProperties(): Observable<PendingProperty[]> {
    return of<PendingProperty[]>([
      {
        id: '1',
        title: 'Modern Villa with Garden',
        propertyType: 'Villa',
        listingType: 'For Sale',
        price: 850000,
        city: 'New Cairo',
        district: '5th Settlement',
        areaInMeters: 350,
        bedrooms: 5,
        bathrooms: 4,
        mainImageUrl: 'https://images.unsplash.com/photo-1568605114967-8130f3a36994?w=400',
        ownerName: 'Ahmed Hassan',
        companyName: 'Prime Estates',
        submittedAt: '2026-06-11T14:30:00Z',
        status: 'pending',
      },
      {
        id: '2',
        title: 'Downtown Studio Apartment',
        propertyType: 'Apartment',
        listingType: 'For Rent',
        price: 12000,
        city: 'Cairo',
        district: 'Zamalek',
        areaInMeters: 75,
        bedrooms: 1,
        bathrooms: 1,
        mainImageUrl: 'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=400',
        ownerName: 'Sara Mohamed',
        companyName: 'Urban Homes',
        submittedAt: '2026-06-11T10:15:00Z',
        status: 'pending',
      },
      {
        id: '3',
        title: 'Luxury Penthouse with Sea View',
        propertyType: 'Penthouse',
        listingType: 'For Sale',
        price: 1200000,
        city: 'Alexandria',
        district: 'Sidi Gaber',
        areaInMeters: 280,
        bedrooms: 4,
        bathrooms: 3,
        mainImageUrl: 'https://images.unsplash.com/photo-1545324418-cc1a3fa10c00?w=400',
        ownerName: 'Mohamed Ali',
        companyName: 'Coast Properties',
        submittedAt: '2026-06-10T16:45:00Z',
        status: 'pending',
      },
      {
        id: '4',
        title: 'Family Townhouse in Compound',
        propertyType: 'Townhouse',
        listingType: 'For Rent',
        price: 25000,
        city: 'Sheikh Zayed',
        district: 'Beverly Hills',
        areaInMeters: 220,
        bedrooms: 3,
        bathrooms: 3,
        mainImageUrl: 'https://images.unsplash.com/photo-1570129477492-45c003edd2be?w=400',
        ownerName: 'Nour Adel',
        companyName: 'Green Valley Estates',
        submittedAt: '2026-06-10T09:20:00Z',
        status: 'pending',
      },
    ]).pipe(delay(500));
  }

  approveProperty(id: string): Observable<void> {
    console.log('[Mock] Approving property', id);
    return of(undefined).pipe(delay(400));
  }

  rejectProperty(id: string, reason?: string): Observable<void> {
    console.log('[Mock] Rejecting property', id, reason);
    return of(undefined).pipe(delay(400));
  }

  // ===== RENT PAYMENTS =====
  getRecentPayments(): Observable<RentPayment[]> {
    return of<RentPayment[]>([
      {
        id: '1',
        propertyId: 'p1',
        propertyTitle: 'Unit 4B - Garden Tower',
        tenantName: 'Sarah Jenkins',
        amount: 2400,
        dueDate: '2026-06-01',
        paidDate: '2026-06-11T22:15:00Z',
        status: 'paid',
      },
      {
        id: '2',
        propertyId: 'p2',
        propertyTitle: 'Unit 12A - Marina Bay',
        tenantName: 'Michael Chen',
        amount: 3150,
        dueDate: '2026-06-01',
        paidDate: '2026-06-11T22:13:00Z',
        status: 'paid',
      },
      {
        id: '3',
        propertyId: 'p3',
        propertyTitle: 'Unit 8C - Grandview Tower',
        tenantName: 'Layla Hassan',
        amount: 1850,
        dueDate: '2026-06-05',
        status: 'pending',
      },
      {
        id: '4',
        propertyId: 'p4',
        propertyTitle: 'Unit 2A - Pine Ridge',
        tenantName: 'Omar Khalil',
        amount: 2200,
        dueDate: '2026-05-28',
        status: 'overdue',
      },
      {
        id: '5',
        propertyId: 'p5',
        propertyTitle: 'Unit 5D - Riverside Plaza',
        tenantName: 'Fatima Ahmed',
        amount: 2750,
        dueDate: '2026-06-01',
        paidDate: '2026-06-10T14:00:00Z',
        status: 'paid',
      },
    ]).pipe(delay(450));
  }

  // ===== LATEST USERS =====
  getLatestUsers(): Observable<AdminUser[]> {
    return of<AdminUser[]>([
      {
        id: 'u1',
        name: 'Ahmed Hassan',
        email: 'ahmed@example.com',
        role: 'owner',
        status: 'active',
        joinedAt: '2026-06-11T09:00:00Z',
      },
      {
        id: 'u2',
        name: 'Sara Mohamed',
        email: 'sara@example.com',
        role: 'tenant',
        status: 'active',
        joinedAt: '2026-06-10T15:30:00Z',
      },
      {
        id: 'u3',
        name: 'Mohamed Ali',
        email: 'mohamed@example.com',
        role: 'agent',
        status: 'pending',
        joinedAt: '2026-06-10T11:20:00Z',
      },
      {
        id: 'u4',
        name: 'Nour Adel',
        email: 'nour@example.com',
        role: 'owner',
        status: 'active',
        joinedAt: '2026-06-09T18:45:00Z',
      },
      {
        id: 'u5',
        name: 'Karim Salah',
        email: 'karim@example.com',
        role: 'tenant',
        status: 'active',
        joinedAt: '2026-06-09T14:10:00Z',
      },
    ]).pipe(delay(350));
  }

  // ===== TOP COMPANIES =====
  getTopCompanies(): Observable<Company[]> {
    return of<Company[]>([
      {
        id: 'c1',
        name: 'Prime Estates',
        ownerName: 'Khaled Mostafa',
        email: 'info@primeestates.com',
        propertiesCount: 47,
        status: 'verified',
        joinedAt: '2025-08-15T00:00:00Z',
      },
      {
        id: 'c2',
        name: 'Urban Homes',
        ownerName: 'Yasmine Fawzy',
        email: 'contact@urbanhomes.com',
        propertiesCount: 32,
        status: 'verified',
        joinedAt: '2025-09-22T00:00:00Z',
      },
      {
        id: 'c3',
        name: 'Coast Properties',
        ownerName: 'Tarek Saad',
        email: 'hello@coast.com',
        propertiesCount: 28,
        status: 'verified',
        joinedAt: '2025-11-03T00:00:00Z',
      },
      {
        id: 'c4',
        name: 'Pyramid Realty',
        ownerName: 'Heba Ibrahim',
        email: 'info@pyramid.com',
        propertiesCount: 21,
        status: 'pending',
        joinedAt: '2026-04-18T00:00:00Z',
      },
    ]).pipe(delay(400));
  }
}
