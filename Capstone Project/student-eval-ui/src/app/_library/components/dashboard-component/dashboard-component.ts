import { Component, ChangeDetectorRef, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData } from 'chart.js';
import { forkJoin, switchMap } from 'rxjs';
import {
  ApiService,
  BatchOverview,
  SentimentCount,
  TopTopic
} from '../../services/api.service';
import {
  CourseOptionDto,
  InsightQuery,
  InsightsResponseDto,
  Sentiment,
  SentimentDrilldownResponseDto,
  TargetType,
  TopicOptionDto,
} from '../../../insights.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, BaseChartDirective],
  templateUrl: './dashboard-component.html',
  styleUrl: './dashboard-component.scss'
})
export class DashboardComponent implements OnInit {
  importBatchId = '';
  targetType: TargetType = 'Instructor';

  quarterFilter = '';
  instructorFilter = '';
  courseFilter = '';
  topicClusterIdFilter = '';

  batches: BatchOverview[] = [];
  filteredBatches: BatchOverview[] = [];
  batchesLoading = false;

  sentimentCounts: SentimentCount[] = [];
  topTopics: TopTopic[] = [];

  quarters: string[] = [];
  instructors: string[] = [];
  courses: CourseOptionDto[] = [];
  topics: TopicOptionDto[] = [];

  chartType: 'doughnut' = 'doughnut';
  sentimentChartData: ChartData<'doughnut'> = {
    labels: ['Positive', 'Neutral', 'Negative'],
    datasets: [
      {
        data: [0, 0, 0],
        label: 'Sentiment',
      },
    ],
  };
  chartOptions: ChartConfiguration<'doughnut'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'bottom'
      }
    }
  };

  loading = false;
  error = '';

  drilldownOpen = false;
  drilldownLoading = false;
  drilldownError = '';
  selectedSentiment: Sentiment | null = null;
  sentimentDrilldown: SentimentDrilldownResponseDto | null = null;

  constructor(private api: ApiService, private cdr: ChangeDetectorRef) {}

  ngOnInit() {
    this.loadBatches();
  }

  loadBatches() {
    this.batchesLoading = true;
    this.error = '';

    this.api.getBatches().subscribe({
      next: (batches) => {
        this.batches = [...batches].sort(
          (a, b) => new Date(b.createdUtc).getTime() - new Date(a.createdUtc).getTime()
        );
        this.quarters = this.uniqueQuarters(this.batches);
        this.filteredBatches = this.applyQuarterFilter(this.batches, this.quarterFilter);
        this.batchesLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'Failed to load batches';
        this.batchesLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  load() {
    if (!this.importBatchId) return;

    this.loading = true;
    this.error = '';

    const { instructorName, courseNumber } = this.getFilterRequestValues();

    forkJoin([
      this.api.getFilters(
        this.targetType,
        this.importBatchId,
        instructorName,
        courseNumber,
        this.getTopicClusterId(),
      ),
      this.api.getInsights(this.buildInsightQuery()),
    ]).subscribe({
      next: ([filters, insights]) => {
        this.instructors = filters.instructors ?? [];
        this.courses = filters.courses ?? [];
        this.topics = filters.topics ?? [];

        this.applyInsights(insights);

        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'Failed to load data';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  onInstructorChange() {
    this.refreshFiltersAndInsights(false, false);
  }

  onCourseChange() {
    this.refreshFiltersAndInsights(false, true);
  }

  onTopicChange() {
    this.refreshFiltersAndInsights(false, true);
  }

  onSentimentChartClick(event: { active?: object[] }) {
    const firstActive = event.active?.[0] as { index?: number } | undefined;
    const index = firstActive?.index;
    if (index == null) {
      return;
    }

    const labels = this.sentimentChartData.labels;
    if (!labels) {
      return;
    }

    const label = labels[index];
    if (typeof label !== 'string') {
      return;
    }

    const sentiment = this.parseSentiment(label);
    if (!sentiment) {
      return;
    }

    this.openSentimentDrilldown(sentiment);
  }

  onSentimentRowClick(value: string) {
    const sentiment = this.parseSentiment(value);
    if (!sentiment) {
      return;
    }

    this.openSentimentDrilldown(sentiment);
  }

  openSentimentDrilldown(sentiment: Sentiment) {
    if (!this.importBatchId) {
      return;
    }

    this.drilldownOpen = true;
    this.drilldownLoading = true;
    this.drilldownError = '';
    this.selectedSentiment = sentiment;
    this.sentimentDrilldown = null;

    const { instructorName, courseNumber } = this.getFilterRequestValues();

    this.api
      .getSentimentDrilldown({
        targetType: this.targetType,
        sentiment,
        importBatchId: this.importBatchId,
        instructorName,
        courseNumber,
        topicClusterId: this.getTopicClusterId(),
      })
      .subscribe({
        next: (response) => {
          this.sentimentDrilldown = response;
          this.drilldownLoading = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.drilldownError = 'Failed to load sentiment drilldown';
          this.drilldownLoading = false;
          this.cdr.detectChanges();
        },
      });
  }

  closeSentimentDrilldown() {
    this.drilldownOpen = false;
    this.drilldownLoading = false;
    this.drilldownError = '';
    this.selectedSentiment = null;
    this.sentimentDrilldown = null;
  }

  onTargetChange() {
    this.instructorFilter = '';
    this.courseFilter = '';
    this.topicClusterIdFilter = '';
    this.load();
  }

  onQuarterChange() {
    this.filteredBatches = this.applyQuarterFilter(this.batches, this.quarterFilter);

    if (
      this.importBatchId &&
      !this.filteredBatches.some((batch) => batch.importBatchId === this.importBatchId)
    ) {
      this.importBatchId = '';
    }

    this.instructorFilter = '';
    this.courseFilter = '';
    this.topicClusterIdFilter = '';
  }

  private loadInsightsOnly() {
    if (!this.importBatchId) {
      return;
    }

    this.loading = true;
    this.error = '';

    this.api.getInsights(this.buildInsightQuery()).subscribe({
      next: (insights) => {
        this.applyInsights(insights);
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'Failed to load data';
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }

  private refreshFiltersAndInsights(validateCourseSelection: boolean, includeTopicInFilterRequest: boolean) {
    if (!this.importBatchId) {
      return;
    }

    this.loading = true;
    this.error = '';

    const { instructorName, courseNumber } = this.getFilterRequestValues();
    const courseNumberForFilters = validateCourseSelection ? null : courseNumber;
    const topicClusterIdForFilters = includeTopicInFilterRequest ? this.getTopicClusterId() : null;

    this.api
      .getFilters(
        this.targetType,
        this.importBatchId,
        instructorName,
        courseNumberForFilters,
        topicClusterIdForFilters,
      )
      .pipe(
        switchMap((filters) => {
          this.instructors = filters.instructors ?? [];
          this.courses = filters.courses ?? [];
          this.topics = filters.topics ?? [];

          if (this.courseFilter && !this.courses.some((course) => course.courseNumber === this.courseFilter)) {
            this.courseFilter = '';
          }

          if (
            this.topicClusterIdFilter &&
            !this.topics.some((topic) => String(topic.topicClusterId) === this.topicClusterIdFilter)
          ) {
            this.topicClusterIdFilter = '';
          }

          return this.api.getInsights(this.buildInsightQuery());
        }),
      )
      .subscribe({
        next: (insights) => {
          this.applyInsights(insights);
          this.loading = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.error = 'Failed to load data';
          this.loading = false;
          this.cdr.detectChanges();
        },
      });
  }

  private getTopicClusterId(): number | null {
    return this.topicClusterIdFilter ? Number(this.topicClusterIdFilter) : null;
  }

  private getFilterRequestValues(): { instructorName: string | null; courseNumber: string | null } {
    return {
      instructorName: this.instructorFilter || null,
      courseNumber: this.courseFilter || null,
    };
  }

  private buildInsightQuery(): InsightQuery {
    const { instructorName, courseNumber } = this.getFilterRequestValues();

    return {
      targetType: this.targetType,
      importBatchId: this.importBatchId || null,
      instructorName,
      courseNumber,
      topicClusterId: this.getTopicClusterId(),
    };
  }

  private applyInsights(insights: InsightsResponseDto) {
    this.sentimentCounts = [
      { sentiment: 'Positive', count: insights.sentiment?.positive ?? 0 },
      { sentiment: 'Neutral', count: insights.sentiment?.neutral ?? 0 },
      { sentiment: 'Negative', count: insights.sentiment?.negative ?? 0 },
    ];

    this.topTopics = (insights.topTopics ?? []).map((topic) => ({
      topicClusterId: topic.topicClusterId,
      humanLabel: topic.humanLabel,
      count: topic.count,
    }));

    this.updateChartFromSentiments(this.sentimentCounts);
  }

  private uniqueQuarters(batchList: BatchOverview[]): string[] {
    const quarters = batchList.map((batch) => this.getQuarterLabel(batch.createdUtc));
    return [...new Set(quarters)].sort((a, b) => b.localeCompare(a));
  }

  private applyQuarterFilter(batchList: BatchOverview[], selectedQuarter: string): BatchOverview[] {
    if (!selectedQuarter) {
      return [...batchList];
    }

    return batchList.filter((batch) => this.getQuarterLabel(batch.createdUtc) === selectedQuarter);
  }

  private getQuarterLabel(createdUtc: string): string {
    const date = new Date(createdUtc);
    const quarter = Math.floor(date.getUTCMonth() / 3) + 1;
    return `Q${quarter} ${date.getUTCFullYear()}`;
  }

  private updateChartFromSentiments(sentimentCounts: SentimentCount[]) {
    const normalized = { positive: 0, neutral: 0, negative: 0 };

    for (const sentimentCount of sentimentCounts) {
      const sentiment = sentimentCount.sentiment.toLowerCase();
      if (sentiment === 'positive') {
        normalized.positive += sentimentCount.count;
      } else if (sentiment === 'neutral') {
        normalized.neutral += sentimentCount.count;
      } else if (sentiment === 'negative') {
        normalized.negative += sentimentCount.count;
      }
    }

    this.sentimentChartData = {
      labels: ['Positive', 'Neutral', 'Negative'],
      datasets: [
        {
          data: [normalized.positive, normalized.neutral, normalized.negative],
          backgroundColor: ['#22c55e', '#f59e0b', '#ef4444'],
        },
      ],
    };
  }

  private parseSentiment(value: string): Sentiment | null {
    if (value === 'Positive' || value === 'Neutral' || value === 'Negative') {
      return value;
    }

    return null;
  }
}
