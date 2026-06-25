import { ChatRole } from './chat-role';

export interface HistoryMessage {
  role: ChatRole;
  message: string;
  createdAt?: string;
}
