import {
  Component,
  signal,
  computed,
  inject,
  ElementRef,
  ViewChild,
  AfterViewChecked,
  OnInit,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AiChatService } from '../../Services/ai-chat.service';
import { ChatMessage, ChatSession, PromptSuggestion } from '../../Models/chat.model';

@Component({
  selector: 'app-ai-chat',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ai-chat.component.html',
  styleUrls: ['./ai-chat.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AiChatComponent implements OnInit, AfterViewChecked {
  private svc = inject(AiChatService);

  @ViewChild('scrollArea') scrollArea?: ElementRef<HTMLDivElement>;
  private shouldScroll = false;

  // ── State ──────────────────────────────────────────────────────────────────
  messages  = signal<ChatMessage[]>([]);
  sessions  = signal<ChatSession[]>([]);
  sessionId = signal<string | null>(null);
  draft     = signal('');
  sending   = signal(false);
  sidebarOpen = signal(true);

  // ── Computed ───────────────────────────────────────────────────────────────
  hasMessages = computed(() => this.messages().length > 0);
  canSend     = computed(() => this.draft().trim().length > 0 && !this.sending());

  // ── Analytics-focused suggestions ─────────────────────────────────────────
  suggestions: PromptSuggestion[] = [
    {
      icon: 'fa-chart-line',
      title: 'Revenue Summary',
      prompt: 'What is the total rent revenue collected and how many payments are overdue?',
      color: 'indigo',
    },
    {
      icon: 'fa-building',
      title: 'Property Overview',
      prompt: 'Give me a full breakdown of properties by status and top cities.',
      color: 'green',
    },
    {
      icon: 'fa-users',
      title: 'User Growth',
      prompt: 'How many new users and companies signed up this month?',
      color: 'amber',
    },
    {
      icon: 'fa-credit-card',
      title: 'Subscriptions',
      prompt: 'Which subscription plan is most popular and what is the total subscription revenue?',
      color: 'purple',
    },
  ];

  ngOnInit() {
    this.svc.getSessions().subscribe({
      next: (s) => this.sessions.set(s),
      error: () => {},
    });
  }

  ngAfterViewChecked() {
    if (this.shouldScroll && this.scrollArea) {
      this.scrollArea.nativeElement.scrollTop =
        this.scrollArea.nativeElement.scrollHeight;
      this.shouldScroll = false;
    }
  }

  // ── Send ───────────────────────────────────────────────────────────────────
  usePrompt(prompt: string) {
    this.draft.set(prompt);
    this.send();
  }

  onKeydown(e: Event) {
    const ke = e as KeyboardEvent;
    if (ke.key === 'Enter' && !ke.shiftKey) {
      ke.preventDefault();
      this.send();
    }
  }

  send() {
    const text = this.draft().trim();
    if (!text || this.sending()) return;

    const userMsg: ChatMessage = {
      id: crypto.randomUUID(),
      role: 'user',
      content: text,
      createdAt: new Date().toISOString(),
    };

    const pendingMsg: ChatMessage = {
      id: crypto.randomUUID(),
      role: 'assistant',
      content: '',
      createdAt: new Date().toISOString(),
      pending: true,
    };

    this.messages.update((m) => [...m, userMsg, pendingMsg]);
    this.draft.set('');
    this.sending.set(true);
    this.shouldScroll = true;

    // ── Call the real analytics endpoint ──────────────────────────────────
    this.svc.sendAnalyticsMessage(text).subscribe({
      next: (answer) => {
        this.messages.update((list) =>
          list.map((msg) =>
            msg.id === pendingMsg.id
              ? { ...msg, content: answer, pending: false }
              : msg,
          ),
        );
        this.sending.set(false);
        this.shouldScroll = true;
      },
      error: () => {
        this.messages.update((list) =>
          list.map((msg) =>
            msg.id === pendingMsg.id
              ? {
                  ...msg,
                  content: 'The analytics assistant is temporarily unavailable. Please try again.',
                  pending: false,
                  error: true,
                }
              : msg,
          ),
        );
        this.sending.set(false);
        this.shouldScroll = true;
      },
    });
  }

  // ── Sessions (local UX only) ───────────────────────────────────────────────
  newChat() {
    this.messages.set([]);
    this.sessionId.set(null);
    this.draft.set('');
  }

  openSession(s: ChatSession) {
    this.sessionId.set(s.id);
    this.messages.set([
      {
        id: crypto.randomUUID(),
        role: 'assistant',
        content: `Continuing **"${s.title}"**. How can I help?`,
        createdAt: new Date().toISOString(),
      },
    ]);
    this.shouldScroll = true;
  }

  toggleSidebar() {
    this.sidebarOpen.update((v) => !v);
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  renderMarkdown(text: string): string {
    let html = text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
      .replace(/^•\s(.+)$/gm, '<li>$1</li>')
      .replace(/\n/g, '<br>');
    html = html.replace(/(<li>.*?<\/li>)(<br>)?/g, '$1');
    if (html.includes('<li>')) html = html.replace(/(<li>[\s\S]*<\/li>)/, '<ul>$1</ul>');
    return html;
  }

  formatTime(iso: string) {
    return new Date(iso).toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  relativeTime(iso: string) {
    const diff = Date.now() - new Date(iso).getTime();
    const h = Math.floor(diff / 3600e3);
    if (h < 1) return 'Just now';
    if (h < 24) return `${h}h ago`;
    const d = Math.floor(h / 24);
    return `${d}d ago`;
  }
}
