
export interface RecentClient {
  id: number;
  name: string;
  company: string | null;
  email: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface DashboardStats {
  totalClients: number;
  activeClients: number;
  inactiveClients: number;
  pendingFollowUps: number;
  recentClients: RecentClient[];
}
