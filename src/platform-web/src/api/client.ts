const API_KEY = import.meta.env.VITE_API_KEY || 'da-demo-key-change-in-production';
const BASE = import.meta.env.VITE_API_BASE || '';

async function api<T>(path: string, options: RequestInit = {}): Promise<T> {
  const res = await fetch(`${BASE}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      'X-Api-Key': API_KEY,
      ...(options.headers || {}),
    },
  });
  if (!res.ok) {
    const err = await res.text();
    throw new Error(err || res.statusText);
  }
  return res.json();
}

export interface EligibilityCheck {
  id: string;
  patientId: string;
  locationId: string;
  status: string;
  confidenceScore: number;
  benefitSummary?: string;
  completedAt?: string;
  patient?: { firstName: string; lastName: string };
  benefits?: {
    planName: string;
    annualMaximumRemaining: number;
    deductibleRemaining: number;
    coinsurancePercent: number;
  };
}

export interface Claim {
  id: string;
  externalClaimId: string;
  payerName: string;
  status: string;
  dateOfService: string;
  totalChargeAmount: number;
  payerClaimId?: string;
  lineCount: number;
}

export interface Remittance {
  id: string;
  eraReference: string;
  payerName: string;
  paymentDate: string;
  totalPaymentAmount: number;
  status: string;
  lineCount: number;
  postedLines: number;
}

export interface WorkItem {
  id: string;
  type: string;
  status: string;
  priority: string;
  title: string;
  description: string;
  aiSummary?: string;
  suggestedAction?: string;
  carcCode?: string;
  createdAt: string;
}

export interface Location {
  id: string;
  name: string;
  externalClinicId: string;
  pmsType: string;
  region?: string;
}

export interface Patient {
  id: string;
  firstName: string;
  lastName: string;
  externalPatientId: string;
  locationId: string;
}

export interface Kpis {
  totalClaims: number;
  openWorkItems: number;
  denialRate: number;
  eligibilityVerifiedRate: number;
  remittancesReceived: number;
  claimsSubmitted: number;
}

export const apiClient = {
  health: () => api<{ status: string }>('/v1/health'),
  locations: () => api<Location[]>('/v1/locations'),
  kpis: (locationId: string) => api<Kpis>(`/v1/locations/${locationId}/kpis`),
  patients: (locationId?: string) =>
    api<Patient[]>(`/v1/patients${locationId ? `?locationId=${locationId}` : ''}`),
  eligibility: {
    list: (locationId?: string) =>
      api<EligibilityCheck[]>(`/v1/eligibility${locationId ? `?locationId=${locationId}` : ''}`),
    trigger: (body: { locationId: string; patientId: string; coverageId?: string }) =>
      api<EligibilityCheck>('/v1/eligibility/check', { method: 'POST', body: JSON.stringify(body) }),
  },
  claims: {
    list: (status?: string) =>
      api<Claim[]>(`/v1/claims${status ? `?status=${status}` : ''}`),
    ingest: (locationId: string) =>
      api<Claim>('/v1/claims/ingest', { method: 'POST', body: JSON.stringify({ locationId }) }),
    submit: (id: string) =>
      api<unknown>(`/v1/claims/${id}/submit`, { method: 'POST' }),
  },
  remittances: {
    list: () => api<Remittance[]>('/v1/remittances'),
    poll: () => api<{ count: number }>('/v1/remittances/poll', { method: 'POST' }),
    autoPost: (id: string) =>
      api<{ postedCount: number }>(`/v1/remittances/${id}/post`, { method: 'POST' }),
  },
  workItems: {
    list: (status?: string, type?: string) => {
      const params = new URLSearchParams();
      if (status) params.set('status', status);
      if (type) params.set('type', type);
      const q = params.toString();
      return api<WorkItem[]>(`/v1/work-items${q ? `?${q}` : ''}`);
    },
    aiSummary: (id: string) =>
      api<WorkItem>(`/v1/work-items/${id}/ai-summary`, { method: 'POST' }),
    update: (id: string, body: { status?: string; assignedTo?: string; resolutionNotes?: string }) =>
      api<WorkItem>(`/v1/work-items/${id}`, { method: 'PATCH', body: JSON.stringify(body) }),
  },
};
