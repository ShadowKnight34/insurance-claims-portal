export interface UserProfile {
  id: string;
  email: string;
  fullName: string;
  role: 'Claimant' | 'ClaimsOfficer';
}

export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  user: UserProfile;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface ClaimSummary {
  id: string;
  referenceNumber: string;
  claimType: string;
  status: string;
  incidentDate: string;
  incidentLocation: string;
  estimatedLossAmount: number;
  approvedSettlementAmount: number | null;
  claimantName: string;
  assignedOfficerName: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface StatusHistory {
  id: string;
  fromStatus: string | null;
  toStatus: string;
  changedByName: string;
  notes: string | null;
  createdAt: string;
}

export interface Assignment {
  id: string;
  officerName: string;
  assignedByName: string;
  notes: string | null;
  createdAt: string;
}

export interface Assessment {
  id: string;
  officerName: string;
  notes: string;
  createdAt: string;
}

export interface InformationRequest {
  id: string;
  message: string;
  requestedByName: string;
  response: string | null;
  respondedAt: string | null;
  createdAt: string;
}

export interface ClaimDocument {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedByName: string;
  createdAt: string;
}

export interface ClaimDetail {
  id: string;
  referenceNumber: string;
  claimType: string;
  status: string;
  incidentDate: string;
  incidentLocation: string;
  incidentDescription: string;
  estimatedLossAmount: number;
  approvedSettlementAmount: number | null;
  rejectionReason: string | null;
  claimantId: string;
  claimantName: string;
  assignedOfficerId: string | null;
  assignedOfficerName: string | null;
  createdAt: string;
  updatedAt: string;
  allowedNextStatuses: string[];
  statusHistory: StatusHistory[];
  assignments: Assignment[];
  assessments: Assessment[];
  informationRequests: InformationRequest[];
  documents: ClaimDocument[];
}

export interface ClaimQuery {
  page?: number;
  pageSize?: number;
  status?: string;
  claimType?: string;
  search?: string;
  assignedOfficerId?: string;
  unassignedOnly?: boolean;
  assignedToMe?: boolean;
}

export interface CountByLabel {
  label: string;
  count: number;
}

export interface OfficerWorkload {
  officerId: string;
  officerName: string;
  openClaims: number;
  totalAssigned: number;
}

export interface Dashboard {
  totalClaims: number;
  pendingClaims: number;
  underReview: number;
  awaitingInformation: number;
  approved: number;
  rejected: number;
  settled: number;
  outstandingLiability: number;
  totalApprovedSettlement: number;
  byType: CountByLabel[];
  byStatus: CountByLabel[];
  officerWorkload: OfficerWorkload[];
}

export interface OfficerOption {
  id: string;
  fullName: string;
  email: string;
}

export interface ClaimFormValue {
  claimType: string;
  incidentDate: string;
  incidentLocation: string;
  incidentDescription: string;
  estimatedLossAmount: number;
}
