
export interface ClientNote {
  id: number;
  clientId: number;
  content: string;
  createdAt: string;
}

export interface CreateClientNoteRequest {
  content: string;
}
