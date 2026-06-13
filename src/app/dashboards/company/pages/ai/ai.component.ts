/**
 * Company AI Assistant Page
 * Premium-gated: the page renders fully, but on a non-Premium plan the
 * content is blurred behind an overlay with an upgrade CTA that routes to
 * the Subscription tab. Premium plans get the AI chatbot plus the feature
 * shortcuts the plan unlocks.
 */

import { Component, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { CompanySubscriptionService } from '../../services/company-subscription.service';
import { CompanyAiService } from '../../services/company-ai.service';

interface ChatMessage {
  role: 'user' | 'assistant';
  text: string;
}

@Component({
  selector: 'app-company-ai',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './ai.component.html',
  styleUrl: './ai.component.css',
})
export class AiComponent {
  private readonly subSvc = inject(CompanySubscriptionService);
  private readonly aiSvc = inject(CompanyAiService);

  subscription = toSignal(this.subSvc.getActive());

  loading = computed(() => this.subscription() === undefined);
  isPremium = computed(() =>
    (this.subscription()?.tier ?? '').startsWith('Premium')
  );

  /** Chat is gated by the plan FLAG, not the tier — Free Company includes it. */
  chatEnabled = computed(() => !!this.subscription()?.hasAiChatbot);

  // ── Chat state ──────────────────────────────────────────────────
  messages = signal<ChatMessage[]>([]);
  draft = '';
  sending = signal(false);
  chatError = signal<string | null>(null);
  private sessionId: string | null = null;

  features = computed(() => {
    const sub = this.subscription();
    return [
      {
        icon: 'fa-wand-magic-sparkles',
        title: 'AI Description Generator',
        text: 'Generates bilingual listing descriptions — available inside the property creation flow.',
        on: !!sub?.hasAiDescriptionGenerator,
      },
      {
        icon: 'fa-bullseye',
        title: 'AI Lead Prioritization',
        text: 'Ranks incoming inquiries so brokers respond to the hottest leads first.',
        on: !!sub?.hasAiLeadPrioritization,
      },
      {
        icon: 'fa-chart-line',
        title: 'Advanced Analytics',
        text: 'Deep performance insight across listings and brokers.',
        on: !!sub?.hasAdvancedAnalytics,
      },
    ];
  });

  send(): void {
    const text = this.draft.trim();
    if (!text || this.sending() || !this.chatEnabled()) return;

    this.messages.update((m) => [...m, { role: 'user', text }]);
    this.draft = '';
    this.sending.set(true);
    this.chatError.set(null);

    this.aiSvc.chat(this.sessionId, text).subscribe({
      next: (res) => {
        this.sessionId = res.sessionId;
        this.messages.update((m) => [...m, { role: 'assistant', text: res.replyText }]);
        this.sending.set(false);
      },
      error: (err) => {
        this.sending.set(false);
        this.chatError.set(err?.error?.error ?? 'The AI assistant is unavailable right now.');
      },
    });
  }
}
