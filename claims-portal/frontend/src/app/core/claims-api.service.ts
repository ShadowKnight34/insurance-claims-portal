import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';
import {
  ClaimDetail,
  ClaimDocument,
  ClaimFormValue,
  ClaimQuery,
  ClaimSummary,
  Dashboard,
  OfficerOption,
  PagedResult
} from './models';

@Injectable({ providedIn: 'root' })
export class ClaimsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  listClaims(query: ClaimQuery) {
    let params = new HttpParams()
      .set('page', query.page ?? 1)
      .set('pageSize', query.pageSize ?? 10);

    if (query.status) {
      params = params.set('status', query.status);
    }
    if (query.claimType) {
      params = params.set('claimType', query.claimType);
    }
    if (query.search) {
      params = params.set('search', query.search);
    }
    if (query.assignedOfficerId) {
      params = params.set('assignedOfficerId', query.assignedOfficerId);
    }
    if (query.unassignedOnly) {
      params = params.set('unassignedOnly', true);
    }
    if (query.assignedToMe) {
      params = params.set('assignedToMe', true);
    }

    return this.http.get<PagedResult<ClaimSummary>>(`${this.base}/api/claims`, { params });
  }

  getClaim(id: string) {
    return this.http.get<ClaimDetail>(`${this.base}/api/claims/${id}`);
  }

  createClaim(value: ClaimFormValue) {
    return this.http.post<ClaimDetail>(`${this.base}/api/claims`, value);
  }

  updateClaim(id: string, value: ClaimFormValue) {
    return this.http.put<ClaimDetail>(`${this.base}/api/claims/${id}`, value);
  }

  assign(id: string, officerId: string, notes: string) {
    return this.http.post<ClaimDetail>(`${this.base}/api/claims/${id}/assign`, {
      officerId,
      notes: notes || null
    });
  }

  requestInformation(id: string, message: string) {
    return this.http.post<ClaimDetail>(`${this.base}/api/claims/${id}/information-requests`, { message });
  }

  respond(id: string, requestId: string, response: string) {
    return this.http.post<ClaimDetail>(
      `${this.base}/api/claims/${id}/information-requests/${requestId}/respond`,
      { response }
    );
  }

  addAssessment(id: string, notes: string) {
    return this.http.post<ClaimDetail>(`${this.base}/api/claims/${id}/assessments`, { notes });
  }

  approve(id: string, settlementAmount: number, notes: string) {
    return this.http.post<ClaimDetail>(`${this.base}/api/claims/${id}/approve`, {
      settlementAmount,
      notes: notes || null
    });
  }

  reject(id: string, reason: string) {
    return this.http.post<ClaimDetail>(`${this.base}/api/claims/${id}/reject`, { reason });
  }

  settle(id: string) {
    return this.http.post<ClaimDetail>(`${this.base}/api/claims/${id}/settle`, {});
  }

  upload(id: string, file: File) {
    const body = new FormData();
    body.append('file', file, file.name);
    return this.http.post<ClaimDocument>(`${this.base}/api/claims/${id}/documents`, body);
  }

  download(claimId: string, documentId: string) {
    return this.http.get(`${this.base}/api/claims/${claimId}/documents/${documentId}/content`, {
      responseType: 'blob'
    });
  }

  dashboard() {
    return this.http.get<Dashboard>(`${this.base}/api/dashboard/statistics`);
  }

  officers() {
    return this.http.get<OfficerOption[]>(`${this.base}/api/officers`);
  }
}
