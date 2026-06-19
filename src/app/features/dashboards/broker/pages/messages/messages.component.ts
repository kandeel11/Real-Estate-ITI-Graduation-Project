import {
  Component, OnInit, AfterViewChecked, inject, signal, ViewChild, ElementRef,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { BrokerDataService } from '../../services/broker-data.service';
import { ChatMessage, InquiryDetail } from '../../models/broker.models';

@Component({
  selector: 'app-broker-messages',
  standalone: true,
  imports: [DatePipe, FormsModule, TranslatePipe],
  templateUrl: './messages.component.html',
  styleUrl: './messages.component.css',
})
export class MessagesComponent implements OnInit, AfterViewChecked {
  private readonly svc = inject(BrokerDataService);
  private readonly route = inject(ActivatedRoute);
  @ViewChild('msgList') msgListRef!: ElementRef<HTMLElement>;

  inquiries = signal<InquiryDetail[]>([]);
  selected = signal<InquiryDetail | null>(null);
  messages = signal<ChatMessage[]>([]);

  loadingInquiries = signal(true);
  loadingMessages = signal(false);
  sending = signal(false);
  text = '';
  private shouldScroll = false;

  ngOnInit(): void {
    this.svc.getInquiryDetails().subscribe({
      next: inqs => {
        this.inquiries.set(this.sortByLatest(inqs));
        this.loadingInquiries.set(false);
        // Deep-link from the Inquiries tab: ?inquiry=<id> opens that conversation.
        const id = this.route.snapshot.queryParamMap.get('inquiry');
        if (id) {
          const target = this.inquiries().find(i => i.id === id);
          if (target) this.selectInquiry(target);
        }
      },
      error: () => this.loadingInquiries.set(false),
    });
  }

  /** Newest activity first. */
  private sortByLatest(list: InquiryDetail[]): InquiryDetail[] {
    return [...list].sort((a, b) =>
      new Date(b.lastMessageAt ?? b.createdAt).getTime() -
      new Date(a.lastMessageAt ?? a.createdAt).getTime());
  }

  ngAfterViewChecked(): void {
    if (this.shouldScroll && this.msgListRef) {
      const el = this.msgListRef.nativeElement;
      el.scrollTop = el.scrollHeight;
      this.shouldScroll = false;
    }
  }

  selectInquiry(inq: InquiryDetail): void {
    if (this.selected()?.id === inq.id) return;
    this.selected.set(inq);
    this.messages.set([]);
    this.loadingMessages.set(true);
    this.svc.getMessages(inq.id).subscribe({
      next: msgs => { this.messages.set(msgs); this.loadingMessages.set(false); this.shouldScroll = true; },
      error: () => this.loadingMessages.set(false),
    });
  }

  send(): void {
    const t = this.text.trim();
    const inq = this.selected();
    if (!t || !inq || this.sending()) return;
    this.sending.set(true);
    this.text = '';
    this.svc.sendMessage(inq.id, t).subscribe({
      next: msg => {
        this.messages.update(m => [...m, msg]);
        this.inquiries.update(list => this.sortByLatest(list.map(i =>
          i.id === inq.id
            ? { ...i, lastMessage: msg.text, lastMessageAt: msg.sentAt, status: 'replied' as const }
            : i)));
        this.sending.set(false);
        this.shouldScroll = true;
      },
      error: () => this.sending.set(false),
    });
  }

  onKeydown(e: KeyboardEvent): void {
    if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); this.send(); }
  }

  lastMessage(inq: InquiryDetail): string {
    const text = inq.lastMessage ?? inq.message;
    return text.length > 50 ? text.slice(0, 50) + '…' : text;
  }

  lastTime(inq: InquiryDetail): string {
    return inq.lastMessageAt ?? inq.createdAt;
  }

  initials(name: string): string {
    return name.split(' ').slice(0, 2).map(w => w[0]).join('').toUpperCase() || '?';
  }

  /** Broker/owner side: anything not from the tenant is "mine". */
  isMine(msg: ChatMessage): boolean {
    return msg.senderRole !== 'tenant';
  }
}
