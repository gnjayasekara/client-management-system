
export interface FollowUp {
  id: number;
  clientId: number;
  title: string;
  dueDate: string;
  isCompleted: boolean;
}

export interface CreateFollowUpRequest {
  clientId: number;
  title: string;
  dueDate: string;
}


export interface UpdateFollowUpRequest {
  clientId: number;
  title: string;
  dueDate: string;
  isCompleted: boolean;
}

