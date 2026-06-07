/**
 * Chat RAG Component
 * AI-powered chat with Retrieval-Augmented Generation for property information
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-chat-rag',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="chat-rag">
      <h1>AI Assistant</h1>
      <p class="placeholder">Chat RAG Component - Interactive chat with AI-powered property recommendations</p>
    </div>
  `,
  styles: [
    `
      .chat-rag {
        width: 100%;
      }
      .placeholder {
        color: #95a5a6;
        font-style: italic;
      }
    `,
  ],
})
export class ChatRagComponent {}
