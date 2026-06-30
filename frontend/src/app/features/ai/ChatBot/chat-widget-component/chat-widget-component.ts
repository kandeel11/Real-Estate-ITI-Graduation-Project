import {
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
  signal,
  effect,
  inject,
  computed,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { ChatRole, HistoryMessage, SessionSummary } from '../models';
import { ChatbotService } from '../Services/chatbot-service';
import { AuthService } from '../../../../core/services/auth.service';

type WidgetView = 'home' | 'messages' | 'chat' | 'help';

interface HelpCollection {
  id: string;
  title: string;
  description: string;
  articleCount: number;
}

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
  private auth    = inject(AuthService);

  isOpen         = signal(false);
  view           = signal<WidgetView>('home');
  prevView       = signal<WidgetView>('home');

  sessionId      = signal<string | null>(null);
  messages       = signal<HistoryMessage[]>([]);
  sessions       = signal<SessionSummary[]>([]);
  draft          = signal('');
  sending        = signal(false);
  loadingHistory = signal(false);
  loadingSessions = signal(false);
  errorMsg       = signal('');
  helpSearch     = signal('');

  readonly userName = computed(() => {
    const name = this.auth.user()?.fullName ?? '';
    return name.split(' ')[0] || 'there';
  });

  readonly recentSession = computed(() => this.sessions()[0] ?? null);

  readonly helpCollections: HelpCollection[] = [
    { id: '1', title: 'Getting Started',     description: 'Learn how to list and find properties on Estatein.', articleCount: 6  },
    { id: '2', title: 'Rent & Payments',     description: 'Understand payment schedules, invoices, and Stripe links.', articleCount: 8  },
    { id: '3', title: 'Leases & Contracts',  description: 'How leases work — signing, activation, and renewal.', articleCount: 5  },
    { id: '4', title: 'Troubleshooting',     description: 'Frequently asked questions and common fixes.', articleCount: 10 },
  ];

  readonly filteredCollections = computed(() => {
    const q = this.helpSearch().toLowerCase().trim();
    if (!q) return this.helpCollections;
    return this.helpCollections.filter(
      c => c.title.toLowerCase().includes(q) || c.description.toLowerCase().includes(q),
    );
  });

  private subs = new Subscription();

  constructor() {
    effect(() => {
      this.messages();
      this.sending();
      queueMicrotask(() => this.scrollToBottom());
    });
  }

  ngOnInit(): void {
    this.subs.add(
      this.chatbot.isOpen$.subscribe((open) => {
        this.isOpen.set(open);
        if (open) {
          this.view.set('home');
          if (this.sessions().length === 0) this.loadSessions();
        }
      }),
    );
    this.subs.add(
      this.chatbot.activeSessionId$.subscribe((id) => {
        if (id && id !== this.sessionId()) {
          this.sessionId.set(id);
          this.loadHistory(id);
        }
      }),
    );
  }

  ngOnDestroy(): void { this.subs.unsubscribe(); }

  toggle(): void { this.chatbot.toggle(); }
  close():  void { this.chatbot.close(); }

  goTo(v: WidgetView): void {
    if (v === 'messages' && this.sessions().length === 0) this.loadSessions();
    this.prevView.set(this.view());
    this.view.set(v);
  }

  goBack(): void { this.view.set(this.prevView()); }

  openNewChat(): void {
    this.prevView.set(this.view());
    this.sessionId.set(null);
    this.messages.set([]);
    this.errorMsg.set('');
    this.chatbot.setActiveSession(null);
    this.view.set('chat');
  }

  openSession(s: SessionSummary): void {
    this.prevView.set(this.view());
    this.sessionId.set(s.sessionId);
    this.loadHistory(s.sessionId);
    this.view.set('chat');
  }

  loadSessions(): void {
    this.loadingSessions.set(true);
    this.chatbot
      .getSessions()
      .pipe(finalize(() => this.loadingSessions.set(false)))
      .subscribe({ next: (list) => this.sessions.set(list ?? []), error: () => {} });
  }

  loadHistory(id: string): void {
    this.loadingHistory.set(true);
    this.messages.set([]);
    this.chatbot
      .getHistory(id)
      .pipe(finalize(() => this.loadingHistory.set(false)))
      .subscribe({
        next:  (h) => this.messages.set(h || []),
        error: ()  => this.errorMsg.set('Failed to load history'),
      });
  }

  send(): void {
    const text = this.draft().trim();
    if (!text || this.sending()) return;

    this.messages.update((msgs) => [
      ...msgs,
      { role: ChatRole.User, message: text, createdAt: new Date().toISOString() },
    ]);
    this.draft.set('');
    this.sending.set(true);
    this.errorMsg.set('');

    this.chatbot
      .sendMessage({ sessionId: this.sessionId() ?? undefined, message: text })
      .pipe(finalize(() => this.sending.set(false)))
      .subscribe({
        next: (res) => {
          if (res.sessionId && res.sessionId !== this.sessionId()) {
            this.sessionId.set(res.sessionId);
            this.chatbot.setActiveSession(res.sessionId);
            this.loadSessions();
          }
          this.messages.update((msgs) => [
            ...msgs,
            { role: ChatRole.Assistant, message: res.replyText ?? '', createdAt: new Date().toISOString() },
          ]);
        },
        error: (err) => {
          const msg = err?.error?.error ?? err?.error?.message ?? 'The AI assistant is temporarily unavailable.';
          this.messages.update((msgs) => [
            ...msgs,
            { role: ChatRole.Assistant, message: `⚠️ ${msg}` },
          ]);
        },
      });
  }

  onEnter(e: KeyboardEvent): void {
    if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); this.send(); }
  }

  timeAgo(iso?: string): string {
    if (!iso) return '';
    const diff = Date.now() - new Date(iso).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 1)  return 'now';
    if (mins < 60) return `${mins}m`;
    const hrs = Math.floor(mins / 60);
    if (hrs  < 24) return `${hrs}h`;
    const days = Math.floor(hrs / 24);
    if (days < 30) return `${days}d`;
    return `${Math.floor(days / 30)}mo`;
  }

  initials(title?: string): string {
    return (title ?? 'AI').trim().charAt(0).toUpperCase();
  }

  private scrollToBottom(): void {
    try {
      const el = this.scrollBox?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    } catch { /* ignore */ }
  }
}
