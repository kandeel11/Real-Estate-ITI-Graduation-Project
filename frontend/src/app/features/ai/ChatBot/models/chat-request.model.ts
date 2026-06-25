export interface ChatRequest {
  sessionId?: string; // ← optional (أول رسالة بدونه)
  message: string;
}
