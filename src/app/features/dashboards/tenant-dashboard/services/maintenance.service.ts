import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, catchError } from 'rxjs';
import { environment } from '../../../../../environments/environment';

export type TicketStatus =
  | 'Open'
  | 'WaitingForDecision'
  | 'WaitingForTenantDecision'
  | 'InProgress'
  | 'Completed'
  | 'Cancelled';

export type TicketPriority = 'Low' | 'Medium' | 'Emergency';

export interface Ticket {
  id: string;
  ticketNumber: string;
  title: string;
  description: string;
  priority: TicketPriority;
  status: TicketStatus;
  propertyAddress: string;
  attachmentCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface StatusHistoryItem {
  id: string;
  status: TicketStatus;
  note?: string;
  createdAt: string;
  createdByName: string;
}

export interface AiEvaluation {
  responsibleParty: string;
  category: string;
  confidenceScore: number;
  reasoning: string;
}

export interface TicketComment {
  id: string;
  userId: string;
  userName: string;
  text: string;
  createdAt: string;
}

export interface TicketAttachment {
  id: string;
  fileName: string;
  url: string;
  type: string;
}

export interface TicketDetail extends Ticket {
  statusHistory: StatusHistoryItem[];
  aiEvaluation: AiEvaluation | null;
  comments: TicketComment[];
  attachments: TicketAttachment[];
}

export interface CreateTicketDto {
  title: string;
  description: string;
  priority: TicketPriority;
  propertyId: string;
}

export interface SubmitTicketDto {
  propertyAddress: string;
  tenantPhone: string;
}

const MOCK_TICKETS: Ticket[] = [
  {
    id: 'ticket-001', ticketNumber: 'TCK-0001',
    title: 'HVAC system not cooling properly',
    description: 'The air conditioning unit has not been cooling the apartment for the past 3 days. Temperature inside is consistently above 85°F despite the thermostat being set to 72°F.',
    priority: 'Emergency', status: 'WaitingForTenantDecision',
    propertyAddress: '42 Oak Street, Suite 3B, New York, NY 10001',
    attachmentCount: 2,
    createdAt: new Date(Date.now() - 172800000).toISOString(),
    updatedAt: new Date(Date.now() - 86400000).toISOString(),
  },
  {
    id: 'ticket-002', ticketNumber: 'TCK-0002',
    title: 'Leaking faucet in master bathroom',
    description: 'The bathroom faucet has been dripping continuously. Water stains are appearing under the sink cabinet.',
    priority: 'Medium', status: 'InProgress',
    propertyAddress: '42 Oak Street, Suite 3B, New York, NY 10001',
    attachmentCount: 1,
    createdAt: new Date(Date.now() - 432000000).toISOString(),
    updatedAt: new Date(Date.now() - 259200000).toISOString(),
  },
  {
    id: 'ticket-003', ticketNumber: 'TCK-0003',
    title: 'Broken window latch in living room',
    description: 'The window latch on the east-facing living room window is broken and does not secure properly.',
    priority: 'Low', status: 'Open',
    propertyAddress: '42 Oak Street, Suite 3B, New York, NY 10001',
    attachmentCount: 0,
    createdAt: new Date(Date.now() - 604800000).toISOString(),
    updatedAt: new Date(Date.now() - 604800000).toISOString(),
  },
  {
    id: 'ticket-004', ticketNumber: 'TCK-0004',
    title: 'Broken blinds in master bedroom',
    description: 'The venetian blinds in the master bedroom are broken and need replacement.',
    priority: 'Low', status: 'Completed',
    propertyAddress: '42 Oak Street, Suite 3B, New York, NY 10001',
    attachmentCount: 1,
    createdAt: new Date(Date.now() - 1209600000).toISOString(),
    updatedAt: new Date(Date.now() - 864000000).toISOString(),
  },
];

const MOCK_TICKET_DETAIL: TicketDetail = {
  ...MOCK_TICKETS[0],
  statusHistory: [
    { id: 'sh-1', status: 'Open', note: 'Ticket submitted by tenant', createdAt: new Date(Date.now() - 172800000).toISOString(), createdByName: 'Sarah Johnson' },
    { id: 'sh-2', status: 'WaitingForDecision', note: 'AI evaluation completed.', createdAt: new Date(Date.now() - 86400000).toISOString(), createdByName: 'System' },
    { id: 'sh-3', status: 'WaitingForTenantDecision', note: 'Please choose how you want to proceed.', createdAt: new Date(Date.now() - 43200000).toISOString(), createdByName: 'System' },
  ],
  aiEvaluation: {
    responsibleParty: 'Landlord',
    category: 'HVAC',
    confidenceScore: 92,
    reasoning: "Based on the description, this appears to be a mechanical failure of the HVAC system. The system is unable to maintain the set temperature, suggesting a compressor or refrigerant issue that falls under landlord's maintenance responsibility.",
  },
  comments: [
    { id: 'c-1', userId: 'u-001', userName: 'Sarah Johnson', text: "It's been 3 days and the temperature inside is unbearable. Please send someone ASAP.", createdAt: new Date(Date.now() - 172800000).toISOString() },
  ],
  attachments: [
    { id: 'a-1', fileName: 'hvac_unit.jpg', url: 'https://placehold.co/400x300/dce9ff/4f46e5?text=HVAC+Unit', type: 'image/jpeg' },
    { id: 'a-2', fileName: 'temperature_reading.jpg', url: 'https://placehold.co/400x300/dce9ff/4f46e5?text=Thermometer', type: 'image/jpeg' },
  ],
};

@Injectable({ providedIn: 'root' })
export class MaintenanceService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  getTickets(): Observable<Ticket[]> {
    // TODO: replace with real API call
    return of(MOCK_TICKETS);
    // return this.http.get<Ticket[]>(`${this.base}/maintenance/tickets`).pipe(catchError(() => of([])));
  }

  getTicketDetail(id: string): Observable<TicketDetail> {
    // TODO: replace with real API call
    return of(MOCK_TICKET_DETAIL);
    // return this.http.get<TicketDetail>(`${this.base}/maintenance/tickets/${id}`);
  }

  createTicket(data: CreateTicketDto): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.base}/maintenance/tickets`, data);
  }

  submitTicket(id: string, data: SubmitTicketDto): Observable<void> {
    return this.http.post<void>(`${this.base}/maintenance/tickets/${id}/submit`, data);
  }

  uploadAttachment(ticketId: string, file: File): Observable<TicketAttachment> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<TicketAttachment>(`${this.base}/maintenance/tickets/${ticketId}/attachments`, form);
  }

  getAiEvaluation(id: string): Observable<AiEvaluation | null> {
    return this.http.get<AiEvaluation>(`${this.base}/maintenance/tickets/${id}/ai-evaluation`)
      .pipe(catchError(() => of(null)));
  }

  makeDecision(id: string, decision: 'platform' | 'external'): Observable<void> {
    return this.http.post<void>(`${this.base}/maintenance/tickets/${id}/decision`, { decision });
  }

  getComments(id: string): Observable<TicketComment[]> {
    return this.http.get<TicketComment[]>(`${this.base}/maintenance/tickets/${id}/comments`)
      .pipe(catchError(() => of([])));
  }

  addComment(id: string, text: string): Observable<TicketComment> {
    return this.http.post<TicketComment>(`${this.base}/maintenance/tickets/${id}/comments`, { text });
  }
}
