
export type RealtimeAction =
  | 'Created'
  | 'Updated'
  | 'Deleted'
  | 'Completed';

export interface RealtimeEvent {
  entityId: number;
  action: RealtimeAction;
}

export type RealtimeEventType =
  | 'ClientChanged'
  | 'FollowUpChanged'
  | 'NoteChanged';
