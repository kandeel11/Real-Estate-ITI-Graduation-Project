import {
  Component, OnInit, OnDestroy, inject, signal, ViewChild, ElementRef,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { Subscription } from 'rxjs';
import { OwnerDataService } from '../../services/owner-data.service';
import { ChatMessage, InquiryDetail } from '../../models/owner.models';
import { ChatRealtimeService } from '../../../../../core/services/chat-realtime.service';

@Component({
  selector: 'app-owner-messages',
  standalone: true,
  imports: [DatePipe, FormsModule, TranslatePipe],
  templateUrl: './messages.component.html',
  styleUrl: './messages.component.css',
})
export class MessagesComponent implements OnInit, OnDestroy {
  private readonly svc = inject(OwnerDataService);
  private readonly route = inject(ActivatedRoute);
  private readonly realtime = inject(ChatRealtimeService);
  @ViewChild('msgList') msgListRef!: ElementRef<HTMLElement>;

  private liveSub?: Subscription;

  inquiries = signal<InquiryDetail[]>([]);
  selected = signal<InquiryDetail | null>(null);
  messages = signal<ChatMessage[]>([]);

  loadingInquiries = signal(true);
  loadingMessages = signal(false);
  sending = signal(false);
  text = '';

  ngOnInit(): void {
    this.svc.getInquiryDetails().subscribe({
      next: inqs => {
        this.inquiries.set(this.sortByLatest(inqs));
        this.loadingInquiries.set(false);
        this.startRealtime(inqs.map(i => i.id));
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

  /** Join every thread so the list (preview + ordering) updates live, not just the open chat. */
  private startRealtime(ids: string[]): void {
    this.realtime.joinMany(ids);
    this.liveSub = this.realtime.onAny().subscribe(({ inquiryId, message }) =>
      this.onIncoming(inquiryId, message));
  }

  private onIncoming(inquiryId: string, msg: ChatMessage): void {
    this.bumpInquiry(inquiryId, msg);
    if (this.selected()?.id === inquiryId) this.appendToOpen(msg);
  }

  /** Refresh a thread's list preview + timestamp and float it to the top. */
  private bumpInquiry(inquiryId: string, msg: ChatMessage): void {
    this.inquiries.update(list => this.sortByLatest(list.map(i =>
      i.id === inquiryId
        ? { ...i, lastMessage: msg.text, lastMessageAt: msg.sentAt }
        : i)));
  }

  /** Append to the open conversation once (dedup by id) and scroll to the bottom. */
  private appendToOpen(msg: ChatMessage): void {
    if (this.messages().some(m => m.id === msg.id)) return;
    this.messages.update(m => [...m, msg]);
    this.scrollToBottom();
  }

  /** Newest activity first. */
  private sortByLatest(list: InquiryDetail[]): InquiryDetail[] {
    return [...list].sort((a, b) =>
      new Date(b.lastMessageAt ?? b.createdAt).getTime() -
      new Date(a.lastMessageAt ?? a.createdAt).getTime());
  }

  /** Defer to the next frame so the new message is in the DOM before we scroll. */
  private scrollToBottom(): void {
    setTimeout(() => {
      const el = this.msgListRef?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    });
  }

  selectInquiry(inq: InquiryDetail): void {
    if (this.selected()?.id === inq.id) return;
    this.selected.set(inq);
    this.messages.set([]);
    this.loadingMessages.set(true);
    this.svc.getMessages(inq.id).subscribe({
      next: msgs => { this.messages.set(msgs); this.loadingMessages.set(false); this.scrollToBottom(); },
      error: () => this.loadingMessages.set(false),
    });
  }

  ngOnDestroy(): void {
    this.liveSub?.unsubscribe();
    this.realtime.leaveAll();
  }

  send(): void {
    const t = this.text.trim();
    const inq = this.selected();
    if (!t || !inq || this.sending()) return;
    this.sending.set(true);
    this.text = '';
    this.svc.sendMessage(inq.id, t).subscribe({
      next: msg => {
        // The realtime echo may have appended this already — appendToOpen/bump dedupe.
        this.appendToOpen(msg);
        this.bumpInquiry(inq.id, msg);
        this.sending.set(false);
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
