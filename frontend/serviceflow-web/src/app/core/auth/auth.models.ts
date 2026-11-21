export interface CurrentUser {
  userId: string;
  tenantId: string;
  email: string;
  displayName: string;
  roles: string[];
  tenantName: string;
  tenantSlug: string;
  tenantStatus: string;
}

export interface RegisterTenantRequest {
  businessName: string;
  slug: string;
  contactEmail?: string;
  contactPhone?: string;
  addressLine1?: string;
  city?: string;
  ownerDisplayName: string;
  ownerEmail: string;
  ownerPassword: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface InviteEmployeeRequest {
  email: string;
  roleName: string;
}

export interface InviteEmployeeResponse {
  invitationId: string;
  email: string;
  roleName: string;
  expiresAtUtc: string;
  invitationToken: string;
}

export interface AcceptInvitationRequest {
  token: string;
  displayName: string;
  password: string;
}
