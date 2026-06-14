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

  // ===== State =====
  messages = signal<ChatMessage[]>([]);
  sessions = signal<ChatSession[]>([]);
  sessionId = signal<string | null>(null);
  draft = signal('');
  sending = signal(false);
  sidebarOpen = signal(true);

  // ===== Computed =====
  hasMessages = computed(() => this.messages().length > 0);
  canSend = computed(() => this.draft().trim().length > 0 && !this.sending());

  // ===== Prompt Suggestions =====
  suggestions: PromptSuggestion[] = [
    {
      icon: 'fa-chart-line',
      title: 'Market Trends',
      prompt: 'Summarize the current real estate market trends',
      color: 'indigo',
    },
    {
      icon: 'fa-users',
      title: 'User Insights',
      prompt: 'Give me an overview of registered users',
      color: 'green',
    },
    {
      icon: 'fa-building',
      title: 'Property Status',
      prompt: 'Which properties need verification?',
      color: 'amber',
    },
    {
      icon: 'fa-money-bill-trend-up',
      title: 'Revenue',
      prompt: 'How is revenue trending this month?',
      color: 'purple',
    },
  ];

  ngOnInit() {
    this.loadSessions();
  }

  ngAfterViewChecked() {
    if (this.shouldScroll && this.scrollArea) {
      this.scrollArea.nativeElement.scrollTop = this.scrollArea.nativeElement.scrollHeight;
      this.shouldScroll = false;
    }
  }

  loadSessions() {
    this.svc.getSessions().subscribe({
      next: (s) => this.sessions.set(s),
      error: () => {},
    });
  }

  // ===== Send =====
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

    this.svc.sendMessage({ sessionId: this.sessionId(), message: text }).subscribe({
      next: (res) => {
        this.sessionId.set(res.sessionId);
        this.messages.update((list) =>
          list.map((msg) =>
            msg.id === pendingMsg.id ? { ...msg, content: res.replyText, pending: false } : msg,
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
                  content: 'Sorry, something went wrong. Please try again.',
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

  // ===== Sessions =====
  newChat() {
    this.messages.set([]);
    this.sessionId.set(null);
    this.draft.set('');
  }

  openSession(s: ChatSession) {
    this.sessionId.set(s.id);
    // For mock: just clear and let user continue. Real: load history.
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

  // ===== Helpers =====
  // Lightweight markdown → HTML (bold, bullets, line breaks)
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
    return new Date(iso).toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
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
