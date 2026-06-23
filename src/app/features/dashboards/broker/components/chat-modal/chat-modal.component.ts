import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, inject, signal, ViewChild, ElementRef } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { Subscription } from 'rxjs';
import { BrokerDataService } from '../../services/broker-data.service';
import { ChatMessage, InquiryDetail } from '../../models/broker.models';
import { ChatRealtimeService } from '../../../../../core/services/chat-realtime.service';

@Component({
  selector: 'app-chat-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe, DatePipe],
  templateUrl: './chat-modal.component.html',
  styleUrl: './chat-modal.component.css',
})
export class ChatModalComponent implements OnInit, OnDestroy {
  @Input({ required: true }) inquiry!: InquiryDetail;
  @Output() closed = new EventEmitter<void>();
  @Output() leadConverted = new EventEmitter<void>();
  @ViewChild('messageList') messageListRef!: ElementRef<HTMLElement>;

  private readonly svc = inject(BrokerDataService);
  private readonly realtime = inject(ChatRealtimeService);
  private liveSub?: Subscription;

  loading = signal(true);
  messages = signal<ChatMessage[]>([]);
  text = '';
  sending = signal(false);
  converting = signal(false);
  error = signal<string | null>(null);

  ngOnInit(): void {
    this.svc.getMessages(this.inquiry.id).subscribe({
      next: msgs => { this.messages.set(msgs); this.loading.set(false); this.scrollToBottom(); },
      error: () => { this.error.set('BROKER.CHAT.LOAD_ERROR'); this.loading.set(false); },
    });

    // Live updates: append messages the tenant sends while the modal is open.
    this.realtime.join(this.inquiry.id);
    this.liveSub = this.realtime.on(this.inquiry.id).subscribe(msg => this.append(msg));
  }

  ngOnDestroy(): void {
    this.liveSub?.unsubscribe();
    this.realtime.leave(this.inquiry.id);
  }

  /** Append once (dedup by id) and scroll to the bottom. */
  private append(msg: ChatMessage): void {
    if (this.messages().some(m => m.id === msg.id)) return;
    this.messages.update(m => [...m, msg]);
    this.scrollToBottom();
  }

  /** Defer to the next frame so the new message is in the DOM before we scroll. */
  private scrollToBottom(): void {
    setTimeout(() => {
      const el = this.messageListRef?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    });
  }

  send(): void {
    const t = this.text.trim();
    if (!t || this.sending()) return;
    this.sending.set(true);
    this.text = '';
    this.svc.sendMessage(this.inquiry.id, t).subscribe({
      next: msg => {
        // The realtime echo may have appended this already — append dedupes.
        this.append(msg);
        this.sending.set(false);
      },
      error: () => this.sending.set(false),
    });
  }

  convertToLead(): void {
    if (this.converting()) return;
    this.converting.set(true);
    this.svc.convertToLead(this.inquiry.id).subscribe({
      next: () => { this.converting.set(false); this.leadConverted.emit(); },
      error: () => this.converting.set(false),
    });
  }

  onKeydown(e: KeyboardEvent): void {
    if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); this.send(); }
  }
}
