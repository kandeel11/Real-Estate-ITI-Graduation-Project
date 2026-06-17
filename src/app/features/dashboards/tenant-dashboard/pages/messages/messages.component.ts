import {
  Component, OnInit, AfterViewChecked, inject, signal, ViewChild, ElementRef,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TenantMessagesService, TenantInquiry, ChatMessage } from '../../services/messages.service';

@Component({
  selector: 'app-tenant-messages',
  standalone: true,
  imports: [DatePipe, FormsModule],
  templateUrl: './messages.component.html',
  styleUrl: './messages.component.css',
})
export class TenantMessagesComponent implements OnInit, AfterViewChecked {
  private readonly svc = inject(TenantMessagesService);
  @ViewChild('msgList') msgListRef!: ElementRef<HTMLElement>;

  inquiries = signal<TenantInquiry[]>([]);
  selected = signal<TenantInquiry | null>(null);
  messages = signal<ChatMessage[]>([]);

  loadingInquiries = signal(true);
  loadingMessages = signal(false);
  sending = signal(false);
  text = '';
  private shouldScroll = false;

  ngOnInit(): void {
    this.svc.getInquiries().subscribe({
      next: inqs => { this.inquiries.set(this.sortByLatest(inqs)); this.loadingInquiries.set(false); },
      error: () => this.loadingInquiries.set(false),
    });
  }

  /** Newest activity first. */
  private sortByLatest(list: TenantInquiry[]): TenantInquiry[] {
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

  selectInquiry(inq: TenantInquiry): void {
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
        // Reflect the new last message in the list and float this chat to the top.
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

  lastMessage(inq: TenantInquiry): string {
    const text = inq.lastMessage ?? inq.message;
    return text.length > 50 ? text.slice(0, 50) + '…' : text;
  }

  lastTime(inq: TenantInquiry): string {
    return inq.lastMessageAt ?? inq.createdAt;
  }

  initials(name: string): string {
    return name.split(' ').slice(0, 2).map(w => w[0]).join('').toUpperCase() || '?';
  }

  isMine(msg: ChatMessage): boolean {
    return msg.senderRole === 'tenant';
  }
}
