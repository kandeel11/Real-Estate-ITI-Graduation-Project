import { Injectable } from '@angular/core';
import { Observable, of, delay } from 'rxjs';
import { CategoryEarnings, MonthlyEarningPoint, Ticket, TicketStats } from '../models';

@Injectable({ providedIn: 'root' })
export class MaintenancePortalService {
  getStats(): Observable<TicketStats> {
    return of<TicketStats>({
      totalTickets: 87,
      openTickets: 5,
      inProgressTickets: 12,
      completedTickets: 68,
      cancelledTickets: 2,
      totalEarnings: 124_500,
      monthlyEarnings: 18_750,
      pendingPayments: 6_200,
      earningsGrowth: 14.8,
      averageRating: 4.7,
    }).pipe(delay(300));
  }

  getMonthlyEarnings(): Observable<MonthlyEarningPoint[]> {
    return of<MonthlyEarningPoint[]>([
      { month: 'Jan', earnings: 8_500, tickets: 6 },
      { month: 'Feb', earnings: 11_200, tickets: 8 },
      { month: 'Mar', earnings: 9_800, tickets: 7 },
      { month: 'Apr', earnings: 14_500, tickets: 11 },
      { month: 'May', earnings: 16_700, tickets: 13 },
      { month: 'Jun', earnings: 18_750, tickets: 14 },
    ]).pipe(delay(400));
  }

  getCategoryEarnings(): Observable<CategoryEarnings[]> {
    return of([
      {
        categoryId: '1',
        categoryName: 'Plumbing',
        categoryIcon: 'fa-faucet',
        ticketsCount: 34,
        totalEarnings: 48_200,
      },
      {
        categoryId: '2',
        categoryName: 'Electrical',
        categoryIcon: 'fa-bolt',
        ticketsCount: 28,
        totalEarnings: 39_500,
      },
      {
        categoryId: '3',
        categoryName: 'AC Repair',
        categoryIcon: 'fa-snowflake',
        ticketsCount: 18,
        totalEarnings: 27_300,
      },
      {
        categoryId: '4',
        categoryName: 'Painting',
        categoryIcon: 'fa-paint-roller',
        ticketsCount: 7,
        totalEarnings: 9_500,
      },
    ]).pipe(delay(350));
  }

  getRecentTickets(): Observable<Ticket[]> {
    return of<Ticket[]>([
      {
        id: '1',
        ticketNumber: 'TKT-1247',
        title: 'Leaking kitchen faucet',
        description: 'Faucet leaks under the sink, needs urgent repair.',
        categoryId: '1',
        categoryName: 'Plumbing',
        categoryIcon: 'fa-faucet',
        customerName: 'Sarah Jenkins',
        propertyTitle: 'Marina Bay Apt',
        propertyAddress: 'Unit 4B, Marina Bay Tower',
        status: 'in_progress',
        priority: 'high',
        price: 850,
        paymentStatus: 'pending',
        createdAt: '2026-06-13T08:30:00Z',
        scheduledAt: '2026-06-13T14:00:00Z',
      },
      {
        id: '2',
        ticketNumber: 'TKT-1246',
        title: 'AC not cooling properly',
        description: 'Central AC unit running but not cooling.',
        categoryId: '3',
        categoryName: 'AC Repair',
        categoryIcon: 'fa-snowflake',
        customerName: 'Michael Chen',
        propertyTitle: 'Grandview Tower',
        propertyAddress: 'Unit 12A, Grandview Tower',
        status: 'open',
        priority: 'urgent',
        price: 1_400,
        paymentStatus: 'pending',
        createdAt: '2026-06-13T07:15:00Z',
      },
      {
        id: '3',
        ticketNumber: 'TKT-1245',
        title: 'Outlet not working in bedroom',
        description: 'Master bedroom outlet stopped working.',
        categoryId: '2',
        categoryName: 'Electrical',
        categoryIcon: 'fa-bolt',
        customerName: 'Layla Hassan',
        propertyTitle: 'Pine Ridge Villa',
        propertyAddress: 'Unit 2A, Pine Ridge Complex',
        status: 'completed',
        priority: 'medium',
        price: 650,
        paymentStatus: 'paid',
        createdAt: '2026-06-12T10:00:00Z',
        completedAt: '2026-06-12T16:30:00Z',
      },
      {
        id: '4',
        ticketNumber: 'TKT-1244',
        title: 'Paint touch-up living room',
        description: 'Several scratches on the living room wall.',
        categoryId: '4',
        categoryName: 'Painting',
        categoryIcon: 'fa-paint-roller',
        customerName: 'Omar Khalil',
        propertyTitle: 'Riverside Plaza',
        propertyAddress: 'Unit 5D, Riverside Plaza',
        status: 'completed',
        priority: 'low',
        price: 1_200,
        paymentStatus: 'paid',
        createdAt: '2026-06-11T13:00:00Z',
        completedAt: '2026-06-11T19:00:00Z',
      },
      {
        id: '5',
        ticketNumber: 'TKT-1243',
        title: 'Water heater replacement',
        description: 'Old water heater needs to be replaced.',
        categoryId: '1',
        categoryName: 'Plumbing',
        categoryIcon: 'fa-faucet',
        customerName: 'Fatima Ahmed',
        propertyTitle: 'Garden Tower',
        propertyAddress: 'Unit 7C, Garden Tower',
        status: 'in_progress',
        priority: 'medium',
        price: 2_300,
        paymentStatus: 'pending',
        createdAt: '2026-06-10T09:30:00Z',
      },
    ]).pipe(delay(500));
  }
}
