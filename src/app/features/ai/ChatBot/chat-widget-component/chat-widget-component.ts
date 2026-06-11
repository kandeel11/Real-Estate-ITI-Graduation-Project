import { CommonModule } from '@angular/common';
import {
  AfterViewChecked,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { HistoryMessage, ChatRole } from '../models/index';
import { ChatbotService } from '../Services/chatbot-service';

@Component({
  selector: 'app-chat-widget',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat-widget-component.html',
  styleUrls: ['./chat-widget-component.css'],
})
export class ChatWidgetComponent implements OnInit, OnDestroy, AfterViewChecked {
  @ViewChild('scrollBox') scrollBox?: ElementRef<HTMLDivElement>;

  isOpen = false;
  sessionId: string | null = null;
  messages: HistoryMessage[] = [];
  draft = '';
  sending = false;
  loadingHistory = false;
  errorMsg = '';

  private subs = new Subscription();

  constructor(private chatbot: ChatbotService) {}

  ngOnInit(): void {
    this.subs.add(
      this.chatbot.isOpen$.subscribe((open) => {
        this.isOpen = open;
        if (open && !this.sessionId) {
          // start a new session by default when widget opens
          this.sessionId = this.chatbot.generateSessionId();
        }
      }),
    );

    this.subs.add(
      this.chatbot.activeSessionId$.subscribe((id) => {
        if (id && id !== this.sessionId) {
          this.sessionId = id;
          this.loadHistory(id);
        }
      }),
    );
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
  }

  ngAfterViewChecked(): void {
    this.scrollToBottom();
  }

  toggle(): void {
    this.chatbot.toggle();
  }
  close(): void {
    this.chatbot.close();
  }

  newSession(): void {
    this.sessionId = this.chatbot.generateSessionId();
    this.messages = [];
    this.chatbot.setActiveSession(this.sessionId);
  }

  loadHistory(id: string): void {
    this.loadingHistory = true;
    this.messages = [];
    this.chatbot
      .getHistory(id)
      .pipe(finalize(() => (this.loadingHistory = false)))
      .subscribe({
        next: (h) => (this.messages = h || []),
        error: () => (this.errorMsg = 'Failed to load history'),
      });
  }

  send(): void {
    const text = this.draft.trim();
    if (!text || this.sending) return;
    if (!this.sessionId) this.sessionId = this.chatbot.generateSessionId();

    this.messages = [
      ...this.messages,
      { role: ChatRole.User, message: text, createdAt: new Date().toISOString() },
    ];
    this.draft = '';
    this.sending = true;
    this.errorMsg = '';

    this.chatbot
      .sendMessage({ sessionId: this.sessionId!, message: text })
      .pipe(finalize(() => (this.sending = false)))
      .subscribe({
        next: (res) => {
          if (res.sessionId && res.sessionId !== this.sessionId) {
            this.sessionId = res.sessionId;
            this.chatbot.setActiveSession(res.sessionId);
          }
          this.messages = [
            ...this.messages,
            {
              role: ChatRole.Assistant,
              message: res.reply ?? '',
              createdAt: res.createdAt ?? new Date().toISOString(),
            },
          ];
        },
        error: () => {
          this.errorMsg = 'Something went wrong while sending the message.';
          this.messages = [
            ...this.messages,
            { role: ChatRole.Assistant, message: '⚠️ Unable to get a response.' },
          ];
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
