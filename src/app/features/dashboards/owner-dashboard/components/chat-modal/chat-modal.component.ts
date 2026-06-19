import { Component, Input, Output, EventEmitter, OnInit, inject, signal, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { OwnerDataService } from '../../services/owner-data.service';
import { ChatMessage, InquiryDetail } from '../../models/owner.models';

@Component({
  selector: 'app-owner-chat-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe, DatePipe],
  templateUrl: './chat-modal.component.html',
  styleUrl: './chat-modal.component.css',
})
export class ChatModalComponent implements OnInit, AfterViewChecked {
  @Input({ required: true }) inquiry!: InquiryDetail;
  @Output() closed = new EventEmitter<void>();
  @Output() leadConverted = new EventEmitter<void>();
  @ViewChild('messageList') messageListRef!: ElementRef<HTMLElement>;

  private readonly svc = inject(OwnerDataService);

  loading = signal(true);
  messages = signal<ChatMessage[]>([]);
  text = '';
  sending = signal(false);
  converting = signal(false);
  error = signal<string | null>(null);
  private shouldScroll = false;

  ngOnInit(): void {
    this.svc.getMessages(this.inquiry.id).subscribe({
      next: msgs => { this.messages.set(msgs); this.loading.set(false); this.shouldScroll = true; },
      error: () => { this.error.set('OWNER.CHAT.LOAD_ERROR'); this.loading.set(false); },
    });
  }

  ngAfterViewChecked(): void {
    if (this.shouldScroll && this.messageListRef) {
      const el = this.messageListRef.nativeElement;
      el.scrollTop = el.scrollHeight;
      this.shouldScroll = false;
    }
  }

  send(): void {
    const t = this.text.trim();
    if (!t || this.sending()) return;
    this.sending.set(true);
    this.text = '';
    this.svc.sendMessage(this.inquiry.id, t).subscribe({
      next: msg => {
        this.messages.update(m => [...m, msg]);
        this.sending.set(false);
        this.shouldScroll = true;
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
