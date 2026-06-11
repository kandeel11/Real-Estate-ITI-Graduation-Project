import { Component, inject, signal, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CurrencyPipe } from '@angular/common';
import { finalize } from 'rxjs';
import { AiChatMessage, AiService, SavedSearch, SmartSearchResult } from '../../services/ai.service';

interface ChatEntry {
  role: 'user' | 'assistant';
  content: string;
}

@Component({
  selector: 'app-ai',
  standalone: true,
  imports: [FormsModule, CurrencyPipe],
  templateUrl: './ai.component.html',
  styleUrl: './ai.component.css',
})
export class AiComponent implements AfterViewChecked {
  @ViewChild('messagesContainer') messagesContainer?: ElementRef<HTMLDivElement>;

  private readonly aiService = inject(AiService);

  protected activeTab = signal<'search' | 'chat'>('search');

  // Search state
  protected searchQuery = '';
  protected searchLoading = signal(false);
  protected searchResults = signal<SmartSearchResult[]>([]);
  protected searchDone = signal(false);
  protected savedSearches = signal<SavedSearch[]>([]);
  protected saveLoading = signal(false);

  // Chat state
  protected messages = signal<ChatEntry[]>([
    { role: 'assistant', content: 'Hi! I\'m your AI assistant. Ask me anything — about rental properties, lease terms, market trends, or anything else you need help with.' },
  ]);
  protected chatInput = '';
  protected chatLoading = signal(false);

  private shouldScroll = false;

  ngAfterViewChecked(): void {
    if (this.shouldScroll && this.messagesContainer) {
      const el = this.messagesContainer.nativeElement;
      el.scrollTop = el.scrollHeight;
      this.shouldScroll = false;
    }
  }

  search(): void {
    const q = this.searchQuery.trim();
    if (!q || this.searchLoading()) return;
    this.searchLoading.set(true);
    this.searchDone.set(false);
    this.aiService.smartSearch(q).pipe(finalize(() => {
      this.searchLoading.set(false);
      this.searchDone.set(true);
    })).subscribe({
      next: results => this.searchResults.set(results),
    });
  }

  saveSearch(): void {
    const q = this.searchQuery.trim();
    if (!q || this.saveLoading()) return;
    this.saveLoading.set(true);
    this.aiService.saveSearch(q).pipe(finalize(() => this.saveLoading.set(false))).subscribe({
      next: saved => this.savedSearches.update(cur => [saved, ...cur]),
    });
  }

  deleteSavedSearch(id: string): void {
    this.aiService.deleteSavedSearch(id).subscribe({
      next: () => this.savedSearches.update(cur => cur.filter(s => s.id !== id)),
    });
  }

  sendMessage(): void {
    const text = this.chatInput.trim();
    if (!text || this.chatLoading()) return;

    this.messages.update(msgs => [...msgs, { role: 'user', content: text }]);
    this.chatInput = '';
    this.chatLoading.set(true);
    this.shouldScroll = true;

    const history: AiChatMessage[] = this.messages().map(m => ({ role: m.role, content: m.content }));
    this.aiService.chat(history).pipe(finalize(() => {
      this.chatLoading.set(false);
      this.shouldScroll = true;
    })).subscribe({
      next: res => this.messages.update(msgs => [...msgs, { role: 'assistant', content: res.message }]),
      error: () => this.messages.update(msgs => [...msgs, { role: 'assistant', content: 'Sorry, I encountered an error. Please try again.' }]),
    });
  }
}
