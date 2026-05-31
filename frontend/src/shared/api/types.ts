// DTO types mirrored from the .NET backend
// Auth.Application.* / Tournament.Application.* / Rating.Application.*

export type Role = 'Player' | 'Organizer' | 'Admin';
export type AccountStatus = 'Active' | 'PendingApproval' | 'Rejected' | 'Blocked';

export interface CurrentUserResponse {
  userId: string;
  email: string;
  role: Role;
  accountStatus: string;
  nickname?: string | null;
  organizerName?: string | null;
  contactHandle?: string | null;
}

export interface AuthResponse {
  accessToken: string;
  expiresAtUtc: string;
  user: CurrentUserResponse;
}

export interface LoginRequest { login: string; password: string; }
export interface RegisterPlayerRequest { nickname: string; email: string; password: string; contactHandle: string; }
export interface RegisterOrganizerRequest { organizerName: string; email: string; password: string; contactHandle: string; }
export interface ChangePasswordRequest { currentPassword: string; newPassword: string; }
export interface UpdateContactHandleRequest { contactHandle: string; }

// ===== Match details (single match view) =====
export interface MatchTeamMemberDetails {
  playerId: string;
  nickname: string;
  elo: number;
  isCaptain: boolean;
  contactHandle: string | null;
}

export interface MatchTeamDetails {
  id: string;
  name: string;
  captainPlayerId: string;
  seed: number;
  averageElo: number;
  members: MatchTeamMemberDetails[];
}

export interface MatchOrganizerDetails {
  organizerId: string;
  organizerName: string | null;
  contactHandle: string | null;
}

export interface MatchDetailsResponse {
  tournamentId: string;
  tournamentTitle: string;
  tournamentDescription: string | null;
  disciplineCode: string;
  format: string;
  teamSize: number;
  tournamentStatus: TournamentStatus | string;
  matchId: string;
  matchNumber: number;
  roundNumber: number;
  matchStatus: string;
  /** Rounds — sum across all maps. Used for ELO weighting. */
  winnerScore: number | null;
  loserScore: number | null;
  /** Maps won in the series (e.g. 2 / 1 for Bo3). Display-only. */
  winnerMaps: number | null;
  loserMaps: number | null;
  winnerTeamId: string | null;
  createdAtUtc: string;
  completedAtUtc: string | null;
  organizer: MatchOrganizerDetails;
  teamA: MatchTeamDetails | null;
  teamB: MatchTeamDetails | null;
  canSeeContacts: boolean;
}

// ===== Notifications =====
export type NotificationKind = 'MatchCreated';

export interface NotificationResponse {
  id: string;
  type: NotificationKind | string;
  title: string;
  body: string;
  linkUrl: string;
  payloadJson: string;
  createdAtUtc: string;
  readAtUtc: string | null;
}

export interface NotificationListResponse {
  items: NotificationResponse[];
  totalCount: number;
  unreadCount: number;
  pageNumber: number;
  pageSize: number;
}

// ===== Tournament =====
export type TournamentStatus =
  | 'Open'
  | 'Full'
  | 'InProgress'
  | 'Completed'
  | 'Cancelled';

export interface TournamentListItemResponse {
  id: string;
  title: string;
  description?: string | null;
  disciplineCode: string;
  format: string;
  swissRounds?: number | null;
  teamSize: number;
  maxPlayers: number;
  organizerId: string;
  status: TournamentStatus | string;
  currentRoundNumber: number;
  currentPlayersCount: number;
  createdAtUtc: string;
  startedAtUtc?: string | null;
  completedAtUtc?: string | null;
  cancelledAtUtc?: string | null;
}

export interface TeamMemberResponse {
  playerId: string;
  nickname: string;
  elo: number;
}

export interface TeamResponse {
  id: string;
  name: string;
  captainPlayerId: string;
  seed: number;
  averageElo: number;
  members: TeamMemberResponse[];
}

export interface MatchResponse {
  id: string;
  matchNumber: number;
  teamAId?: string | null;
  teamBId?: string | null;
  winnerTeamId?: string | null;
  loserTeamId?: string | null;
  status: string;
  /** Rounds — sum across all maps. Used for ELO weighting. */
  winnerScore?: number | null;
  loserScore?: number | null;
  /** Maps won in the series (e.g. 2 / 1 for Bo3). Display-only. */
  winnerMaps?: number | null;
  loserMaps?: number | null;
  isTechnicalDefeat: boolean;
  createdAtUtc: string;
  completedAtUtc?: string | null;
}

export interface RoundResponse {
  id: string;
  number: number;
  bracketType: string;
  status: string;
  createdAtUtc: string;
  completedAtUtc?: string | null;
  matches: MatchResponse[];
}

export interface TournamentParticipantResponse {
  id: string;
  playerId: string;
  playerNickname: string;
  registeredAtUtc: string;
  leftAtUtc?: string | null;
  isActive: boolean;
}

export interface TournamentDetailsResponse extends TournamentListItemResponse {
  participants: TournamentParticipantResponse[];
  teams: TeamResponse[];
  rounds: RoundResponse[];
  canRegister: boolean;
  canLeave: boolean;
}

export interface CreateTournamentRequest {
  title: string;
  description?: string | null;
  disciplineCode: string;
  format: string;
  swissRounds?: number | null;
  teamSize: number;
  maxPlayers: number;
}

export interface AdminCreateTournamentRequest extends CreateTournamentRequest {
  organizerId: string;
}

export interface UpdateTournamentRequest {
  title: string;
  description?: string | null;
}

export interface CompleteMatchRequest {
  winnerTeamId: string;
  /** Rounds — sum across all maps. Required unless isTechnicalDefeat. */
  winnerScore?: number | null;
  loserScore?: number | null;
  /** Maps won in the series. Required unless isTechnicalDefeat. */
  winnerMaps?: number | null;
  loserMaps?: number | null;
  isTechnicalDefeat: boolean;
}

// ===== Rating =====
export interface PlayerRatingResponse {
  id: string;
  playerId: string;
  disciplineCode: string;
  elo: number;
  wins: number;
  losses: number;
  matchesPlayed: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface RatingHistoryResponse {
  id: string;
  playerId: string;
  disciplineCode: string;
  oldElo: number;
  newElo: number;
  delta: number;
  matchId?: string | null;
  tournamentId?: string | null;
  reason: string;
  createdAtUtc: string;
}

// ===== Admin =====
export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages?: number;
}

export interface OrganizerApplicationResponse {
  id: string;
  email: string;
  status: string;
  organizerName: string;
  contactHandle?: string | null;
  createdAtUtc: string;
  approvedAtUtc?: string | null;
  rejectedAtUtc?: string | null;
}

export interface AdminUserResponse {
  id: string;
  email: string;
  role: Role | string;
  status: string;
  nickname?: string | null;
  organizerName?: string | null;
  contactHandle?: string | null;
  createdAtUtc: string;
  approvedAtUtc?: string | null;
  rejectedAtUtc?: string | null;
  blockedAtUtc?: string | null;
  createdByAdminId?: string | null;
}

export interface CreateAdminUserRequest {
  role: Role | string;
  email: string;
  password: string;
  nickname?: string | null;
  organizerName?: string | null;
  contactHandle?: string | null;
}

export interface ResetPasswordRequest { temporaryPassword?: string | null; }
export interface ResetPasswordResponse { userId: string; temporaryPassword?: string | null; }

export interface AdminUsersQuery {
  pageNumber?: number;
  pageSize?: number;
  role?: string;
  status?: string;
  search?: string;
}

export interface OrganizerApplicationsQuery {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
}

// ===== Generic problem details =====
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
}
