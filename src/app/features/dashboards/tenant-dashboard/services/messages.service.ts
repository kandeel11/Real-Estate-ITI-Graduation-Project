import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of } from 'rxjs';
import { environment } from '../../../../../environments/environment';

export interface TenantInquiry {
  id: string;
  propertyId: string;
  propertyTitle: string;
  message: string;
  status: 'new' | 'replied' | 'converted';
  createdAt: string;
}

export interface ChatMessage {
  id: string;
  senderId: string;
  senderName: string;
  senderRole: 'broker' | 'tenant' | 'owner';
  text: string;
  sentAt: string;
}

const MOCK_INQUIRIES: TenantInquiry[] = [
  {
    id: 'inq-1',
    propertyId: 'prop-1',
    propertyTitle: 'Modern Apartment – Downtown',
    message: 'Is the apartment still available for viewing?',
    status: 'replied',
    createdAt: new Date(Date.now() - 86400000 * 2).toISOString(),
  },
];

const MOCK_MESSAGES: Record<string, ChatMessage[]> = {
  'inq-1': [
    { id: 'm1', senderId: 'tenant', senderName: 'You', senderRole: 'tenant', text: 'Is the apartment still available for viewing?', sentAt: new Date(Date.now() - 3600000 * 5).toISOString() },
    { id: 'm2', senderId: 'broker', senderName: 'Ahmed', senderRole: 'broker', text: 'Yes! It is available. When would you like to schedule a visit?', sentAt: new Date(Date.now() - 3600000 * 4).toISOString() },
  ],
};

@Injectable({ providedIn: 'root' })
export class TenantMessagesService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  getInquiries(): Observable<TenantInquiry[]> {
    return this.http.get<TenantInquiry[]>(`${this.base}/inquiries/mine`).pipe(
      catchError(() => of(MOCK_INQUIRIES))
    );
  }

  getMessages(inquiryId: string): Observable<ChatMessage[]> {
    return this.http.get<ChatMessage[]>(`${this.base}/inquiries/${inquiryId}/messages`).pipe(
      catchError(() => of(MOCK_MESSAGES[inquiryId] ?? []))
    );
  }

  sendMessage(inquiryId: string, text: string): Observable<ChatMessage> {
    const mock: ChatMessage = {
      id: Date.now().toString(),
      senderId: 'tenant',
      senderName: 'You',
      senderRole: 'tenant',
      text,
      sentAt: new Date().toISOString(),
    };
    return this.http.post<ChatMessage>(`${this.base}/inquiries/${inquiryId}/messages`, { text }).pipe(
      catchError(() => of(mock))
    );
  }
}
