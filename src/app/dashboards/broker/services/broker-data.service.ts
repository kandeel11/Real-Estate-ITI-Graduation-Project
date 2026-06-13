import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { delay } from 'rxjs/operators';
import {
  BrokerProfile,
  BrokerStats,
  BrokerTask,
  BrokerActivity,
  Property,
  Lead,
  Inquiry,
  PerformanceStats,
  Rating,
} from '../models/broker.models';
import { environment } from '../../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class BrokerDataService {
  private http = inject(HttpClient);
  private useMock = !environment.production;
  private apiUrl = environment.apiUrl;

  getProfile(): Observable<BrokerProfile> {
    if (this.useMock)
      return of({
        id: '1',
        name: 'Sara Hassan',
        email: 'sara@acme.com',
        phone: '+20 100 000 0000',
        license: 'REAL-12345',
        companyName: 'Acme Real Estate',
        bio: 'Specializing in residential properties in Cairo.',
        photo: null,
        languages: ['Arabic', 'English'],
        specializations: ['Residential'],
        rating: 4.9,
        rankInCompany: 2,
        totalBrokers: 23,
      }).pipe(delay(400));
    return this.http.get<BrokerProfile>(`${this.apiUrl}/brokers/me`);
  }

  getStats(): Observable<BrokerStats> {
    if (this.useMock)
      return of({
        assignedProperties: 8,
        newInquiries: 12,
        closedDeals: 47,
        rating: 4.9,
        rankInCompany: 2,
        totalBrokers: 23,
      }).pipe(delay(400));
    return this.http.get<BrokerStats>(`${this.apiUrl}/brokers/me/stats`);
  }

  getTasks(): Observable<BrokerTask[]> {
    if (this.useMock)
      return of([
        { id: '1', text: 'Respond to 3 new inquiries', done: false },
        { id: '2', text: 'Show "6 October Villa" to Mr. Hassan at 3pm', done: false },
        { id: '3', text: 'Follow up on "Maadi Apartment" lead', done: false },
      ]).pipe(delay(400));
    return this.http.get<BrokerTask[]>(`${this.apiUrl}/brokers/me/tasks`);
  }

  getRecentActivity(): Observable<BrokerActivity[]> {
    if (this.useMock)
      return of([
        { id: '1', text: 'New inquiry on "Zamalek Studio"', timeAgo: '2h ago' },
        { id: '2', text: 'Lead "Maadi Apartment" moved to Negotiating', timeAgo: '5h ago' },
        { id: '3', text: 'Property "6 October Villa" got 12 new views', timeAgo: '1d ago' },
      ]).pipe(delay(400));
    return this.http.get<BrokerActivity[]>(`${this.apiUrl}/brokers/me/activity`);
  }

  getProperties(): Observable<Property[]> {
    if (this.useMock)
      return of([
        {
          id: '1',
          title: 'Maadi 3BR Apartment',
          price: '$8,000/mo',
          location: 'Maadi',
          beds: 3,
          baths: 2,
          owner: 'John Smith',
          status: 'active' as const,
          views: 245,
          inquiries: 17,
          newInquiries: 5,
          thumbnail: null,
        },
        {
          id: '2',
          title: '6 October Villa',
          price: '$25,000/mo',
          location: '6 October',
          beds: 5,
          baths: 4,
          owner: 'Acme Co.',
          status: 'active' as const,
          views: 178,
          inquiries: 12,
          newInquiries: 3,
          thumbnail: null,
        },
        {
          id: '3',
          title: 'Zamalek Studio',
          price: '$4,500/mo',
          location: 'Zamalek',
          beds: 1,
          baths: 1,
          owner: 'Sara F.',
          status: 'active' as const,
          views: 95,
          inquiries: 8,
          newInquiries: 2,
          thumbnail: null,
        },
      ]).pipe(delay(400));
    return this.http.get<Property[]>(`${this.apiUrl}/properties/mine`);
  }

  getLeads(): Observable<Lead[]> {
    if (this.useMock)
      return of([
        {
          id: '1',
          tenantName: 'John Smith',
          propertyTitle: 'Maadi Apt',
          status: 'new' as const,
          stageEnteredAt: Date.now() - 2 * 3600000,
        },
        {
          id: '2',
          tenantName: 'Layla Hassan',
          propertyTitle: 'Zamalek Studio',
          status: 'contacted' as const,
          stageEnteredAt: Date.now() - 3 * 86400000,
        },
        {
          id: '3',
          tenantName: 'Mahmoud Ali',
          propertyTitle: '6 October Villa',
          status: 'negotiating' as const,
          stageEnteredAt: Date.now() - 5 * 86400000,
          dealValue: '$25k/mo',
        },
        {
          id: '4',
          tenantName: 'Sara F.',
          propertyTitle: 'Zamalek Studio',
          status: 'new' as const,
          stageEnteredAt: Date.now() - 4 * 3600000,
        },
        {
          id: '5',
          tenantName: 'Karim S.',
          propertyTitle: 'Heliopolis Apt',
          status: 'contacted' as const,
          stageEnteredAt: Date.now() - 7 * 86400000,
        },
        {
          id: '6',
          tenantName: 'Nadia M.',
          propertyTitle: 'Maadi Apt',
          status: 'closed_won' as const,
          stageEnteredAt: Date.now() - 10 * 86400000,
        },
        {
          id: '7',
          tenantName: 'Omar K.',
          propertyTitle: '6 October Villa',
          status: 'closed_lost' as const,
          stageEnteredAt: Date.now() - 12 * 86400000,
        },
      ]).pipe(delay(400));
    return this.http.get<Lead[]>(`${this.apiUrl}/leads?brokerId=me`);
  }

  updateLeadStatus(leadId: string, status: string): Observable<void> {
    if (this.useMock) return of(undefined).pipe(delay(200));
    return this.http.put<void>(`${this.apiUrl}/leads/${leadId}/status`, { status });
  }

  getInquiries(): Observable<Inquiry[]> {
    if (this.useMock)
      return of([
        {
          id: '1',
          from: 'Layla Hassan',
          property: 'Zamalek Studio',
          status: 'new' as const,
          timeAgo: '2h ago',
          message:
            "Hi, I'm interested in this studio. Is it still available for January 1st move-in? Are utilities included?",
        },
        {
          id: '2',
          from: 'Mahmoud Ali',
          property: '6 October Villa',
          status: 'replied' as const,
          timeAgo: '1d ago',
          message: 'Interested in viewing, when can we schedule?',
          reply: 'Hi Mahmoud, available Saturday 3pm or Sunday...',
        },
        {
          id: '3',
          from: 'Ahmed S.',
          property: 'Maadi Apt',
          status: 'new' as const,
          timeAgo: '3h ago',
          message: 'Is this property pet friendly?',
        },
      ]).pipe(delay(400));
    return this.http.get<Inquiry[]>(`${this.apiUrl}/inquiries?assignedTo=me`);
  }

  replyToInquiry(id: string, reply: string): Observable<void> {
    if (this.useMock) return of(undefined).pipe(delay(300));
    return this.http.post<void>(`${this.apiUrl}/inquiries/${id}/reply`, { reply });
  }

  convertToLead(id: string): Observable<void> {
    if (this.useMock) return of(undefined).pipe(delay(300));
    return this.http.post<void>(`${this.apiUrl}/inquiries/${id}/convert`, {});
  }

  getPerformance(): Observable<PerformanceStats> {
    if (this.useMock)
      return of({
        dealsClosedThisMonth: 12,
        avgResponseTimeMinutes: 4.2,
        conversionRate: 23,
        rating: 4.9,
        ratingCount: 47,
        rankInCompany: 2,
        totalBrokers: '23',
        monthlyDeals: [
          { month: 'Aug', deals: 3 },
          { month: 'Sep', deals: 4 },
          { month: 'Oct', deals: 6 },
          { month: 'Nov', deals: 8 },
          { month: 'Dec', deals: 10 },
          { month: 'Jan', deals: 12 },
        ],
      }).pipe(delay(400));
    return this.http.get<PerformanceStats>(`${this.apiUrl}/brokers/me/performance`);
  }

  getRatings(): Observable<Rating[]> {
    if (this.useMock)
      return of([
        { id: '1', stars: 5, comment: 'Sara was amazing, very responsive!', reviewer: 'John S.' },
        { id: '2', stars: 5, comment: 'Great attention to detail.', reviewer: 'Layla H.' },
        { id: '3', stars: 4, comment: 'Good service overall.', reviewer: 'Mahmoud A.' },
      ]).pipe(delay(400));
    return this.http.get<Rating[]>(`${this.apiUrl}/brokers/me/ratings`);
  }
}
