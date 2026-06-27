export type ChatRole = 'user' | 'assistant';

export interface ChatMessage {
  id: string;
  role: ChatRole;
  content: string;
  createdAt: string;
  pending?: boolean; // assistant still typing
  error?: boolean;
}

export interface ChatSession {
  id: string;
  title: string;
  lastMessage: string;
  updatedAt: string;
  messageCount: number;
}

export interface SendMessageRequest {
  sessionId: string | null;
  message: string;
}

export interface SendMessageResponse {
  sessionId: string;
  replyText: string;
}

export interface PromptSuggestion {
  icon: string;
  title: string;
  prompt: string;
  color: string;
}
