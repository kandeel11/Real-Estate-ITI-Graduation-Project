import {
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
  signal, // ⭐
  effect,
  inject,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { ChatRole, HistoryMessage } from '../models';
import { ChatbotService } from '../Services/chatbot-service';

@Component({
  selector: 'app-chat-widget',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './chat-widget-component.html',
  styleUrls: ['./chat-widget-component.scss'],
})
export class ChatWidgetComponent implements OnInit, OnDestroy {
  @ViewChild('scrollBox') scrollBox?: ElementRef<HTMLDivElement>;

  private chatbot = inject(ChatbotService);

  // ⭐ Signals بدل properties عادية
  isOpen = signal(false);
  sessionId = signal<string | null>(null);
  messages = signal<HistoryMessage[]>([]);
  draft = signal('');
  sending = signal(false);
  loadingHistory = signal(false);
  errorMsg = signal('');

  private subs = new Subscription();

  constructor() {
    // auto-scroll لما الـ messages تتغير
    effect(() => {
      this.messages();
      this.sending();
      queueMicrotask(() => this.scrollToBottom());
    });
  }

  ngOnInit(): void {
    this.subs.add(this.chatbot.isOpen$.subscribe((open) => this.isOpen.set(open)));
    this.subs.add(
      this.chatbot.activeSessionId$.subscribe((id) => {
        if (id && id !== this.sessionId()) {
          this.sessionId.set(id);
          this.loadHistory(id);
        }
      }),
    );
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
  }

  toggle(): void {
    this.chatbot.toggle();
  }
  close(): void {
    this.chatbot.close();
  }

  newSession(): void {
    this.sessionId.set(null);
    this.messages.set([]);
    this.errorMsg.set('');
    this.chatbot.setActiveSession(null);
  }

  loadHistory(id: string): void {
    this.loadingHistory.set(true);
    this.messages.set([]);
    this.chatbot
      .getHistory(id)
      .pipe(finalize(() => this.loadingHistory.set(false)))
      .subscribe({
        next: (h) => this.messages.set(h || []),
        error: () => this.errorMsg.set('Failed to load history'),
      });
  }

  send(): void {
    const text = this.draft().trim();
    if (!text || this.sending()) return;

    this.messages.update((msgs) => [
      ...msgs,
      {
        role: ChatRole.User,
        message: text,
        createdAt: new Date().toISOString(),
      },
    ]);
    this.draft.set('');
    this.sending.set(true);
    this.errorMsg.set('');

    this.chatbot
      .sendMessage({
        sessionId: this.sessionId() ?? undefined,
        message: text,
      })
      .pipe(finalize(() => this.sending.set(false)))
      .subscribe({
        next: (res) => {
          if (res.sessionId && res.sessionId !== this.sessionId()) {
            this.sessionId.set(res.sessionId);
            this.chatbot.setActiveSession(res.sessionId);
          }
          this.messages.update((msgs) => [
            ...msgs,
            {
              role: ChatRole.Assistant,
              message: res.replyText ?? '',
              createdAt: new Date().toISOString(),
            },
          ]);
        },
        error: (err) => {
          this.errorMsg.set(err.message || 'Something went wrong.');
          this.messages.update((msgs) => [
            ...msgs,
            {
              role: ChatRole.Assistant,
              message: '⚠️ Unable to get a response.',
            },
          ]);
        },
      });
  }

  onEnter(e: KeyboardEvent): void {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      this.send();
    }
  }

  private scrollToBottom(): void {
    try {
      const el = this.scrollBox?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    } catch {}
  }
}
