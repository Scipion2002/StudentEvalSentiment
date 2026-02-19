import { Component, ChangeDetectorRef, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData } from 'chart.js';
import { forkJoin } from 'rxjs';
import {
  ApiService,
  BatchOverview,
  SentimentCount,
  TopTopic
} from '../../services/api.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, BaseChartDirective],
  templateUrl: './dashboard-component.html',
  styleUrl: './dashboard-component.scss'
})
export class DashboardComponent implements OnInit {
  importBatchId = '';
  targetType: 'Instructor' | 'Course' = 'Instructor';
  quarterFilter = '';
  topicFilter = '';

  batches: BatchOverview[] = [];
  filteredBatches: BatchOverview[] = [];
  batchesLoading = false;

  sentimentCounts: SentimentCount[] = [];
  topTopics: TopTopic[] = [];
  quarters: string[] = [];
  filteredTopics: TopTopic[] = [];

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

    const batchId = this.importBatchId;
    const target = this.targetType;

    forkJoin([
      this.api.getSentimentCounts(batchId, target),
      this.api.getTopTopics(batchId, target, 25)
    ]).subscribe({
      next: ([sentiments, topics]) => {
        this.sentimentCounts = sentiments;
        this.topTopics = topics;

        this.updateChartFromSentiments(this.sentimentCounts);
        this.applyTopicFilter();

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

  onFilterChange() {
    this.applyTopicFilter();
  }

  onTargetChange() {
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
  }

  private applyTopicFilter() {
    const query = this.topicFilter.trim().toLowerCase();
    this.filteredTopics = query
      ? this.topTopics.filter((topic) => topic.humanLabel.toLowerCase().includes(query))
      : [...this.topTopics];
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
}
