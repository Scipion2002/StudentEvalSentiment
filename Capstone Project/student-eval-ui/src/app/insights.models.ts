export type TargetType = 'Instructor' | 'Course';

export interface CourseOptionDto {
  courseNumber: string;
  courseName: string;
}

export interface TopicOptionDto {
  topicClusterId: number;
  humanLabel: string;
}

export interface InsightFiltersResponseDto {
  importBatches: string[]; // GUIDs
  instructors: string[];
  courses: CourseOptionDto[];
  topics: TopicOptionDto[];
}

export interface SentimentBreakdownDto {
  positive: number;
  neutral: number;
  negative: number;
  total: number; // returned by backend as computed prop? if not, compute in UI
}

export interface TopicCountDto {
  topicClusterId: number;
  humanLabel: string;
  count: number;
}

export interface InsightsResponseDto {
  sentiment: SentimentBreakdownDto;
  topTopics: TopicCountDto[];
}

export interface InsightQuery {
  targetType: TargetType;
  importBatchId?: string | null;
  instructorName?: string | null;
  courseNumber?: string | null;
  topicClusterId?: number | null;
}
