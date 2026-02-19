import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { InsightFiltersResponseDto, InsightQuery, InsightsResponseDto, TargetType } from '../../insights.models';

export interface UploadResponse {
  importBatchId: string;
  insertedRows?: number;
  skipped?: boolean;
  reason?: string;
  pythonStdout?: string;
}

export interface AnalyzeBatchResponse {
  importBatchId: string;
  total: number;
  sentimentUpdated: number;
  topicsUpdated: number;
}

export interface BatchOverview {
  importBatchId: string;
  sourceFileName: string;
  createdUtc: string;
  fileSizeBytes: number;
}

export interface SentimentCount {
  sentiment: string;
  count: number;
}

export interface TopTopic {
  topicClusterId: number;
  humanLabel: string;
  count: number;
}

@Injectable({ providedIn: 'root' })
export class ApiService {

  // 🔑 matches launchSettings.json
  private readonly baseUrl = 'https://localhost:7165';

  constructor(private http: HttpClient) {}

  /** Upload CSV */
  uploadCourseEvals(file: File): Observable<UploadResponse> {
    const form = new FormData();
    // MUST match CourseEvalUploadRequest.File
    form.append('file', file);

    return this.http.post<UploadResponse>(
      `${this.baseUrl}/api/imports/course-evals`,
      form
    );
  }

  /** Run sentiment + topic inference */
  analyzeBatch(importBatchId: string): Observable<AnalyzeBatchResponse> {
    return this.http.post<AnalyzeBatchResponse>(
      `${this.baseUrl}/api/analyze/batch/${importBatchId}`,
      {}
    );
  }

  getSentimentCounts(importBatchId: string, targetType: 'Instructor' | 'Course') {
    return this.http.get<SentimentCount[]>(
      `${this.baseUrl}/api/reports/batch/${importBatchId}/sentiment-counts`,
      { params: { targetType } }
    );
  }

  getBatches() {
    return this.http.get<BatchOverview[]>(
      `${this.baseUrl}/api/imports/batches`
    );
  }

  getTopTopics(importBatchId: string, targetType: 'Instructor' | 'Course', top = 10) {
    return this.http.get<TopTopic[]>(
      `${this.baseUrl}/api/reports/batch/${importBatchId}/top-topics`,
      { params: { targetType, top } }
    );
  }

  getFilters(
    targetType: TargetType,
    importBatchId?: string | null,
    instructorName?: string | null,
    courseNumber?: string | null,
    topicClusterId?: number | null,
  ) {
    let params = new HttpParams().set('targetType', targetType);
    if (importBatchId) params = params.set('importBatchId', importBatchId);
    if (instructorName) params = params.set('instructorName', instructorName);
    if (courseNumber) params = params.set('courseNumber', courseNumber);
    if (topicClusterId != null) params = params.set('topicClusterId', topicClusterId);

    return this.http.get<InsightFiltersResponseDto>(`${this.baseUrl}/api/insight-filters`, { params });
  }

  getInsights(q: InsightQuery) {
    let params = new HttpParams().set('targetType', q.targetType);

    if (q.importBatchId) params = params.set('importBatchId', q.importBatchId);
    if (q.instructorName) params = params.set('instructorName', q.instructorName);
    if (q.courseNumber) params = params.set('courseNumber', q.courseNumber);
    if (q.topicClusterId != null) params = params.set('topicClusterId', q.topicClusterId);

    return this.http.get<InsightsResponseDto>(`${this.baseUrl}/api/insights`, { params });
  }
}
