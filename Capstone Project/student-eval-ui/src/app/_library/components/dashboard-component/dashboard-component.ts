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
        position: 'bottom',
        labels: {
          generateLabels: (chart) => {
            const data = chart.data;
            if (data.labels && data.datasets.length) {
              const dataset = data.datasets[0];
              const total = (dataset.data as number[]).reduce((sum, val) => sum + val, 0);
              return data.labels.map((label, i) => {
                const value = dataset.data[i] as number;
                const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : '0.0';
                return {
                  text: `${label}: ${value} (${percentage}%)`,
                  fillStyle: (dataset.backgroundColor as string[])?.[i] || '#999',
                  hidden: false,
                  index: i
                };
              });
            }
            return [];
          }
        }
      },
      tooltip: {
        callbacks: {
          label: (context) => {
            const label = context.label || '';
            const value = context.parsed;
            const total = context.dataset.data.reduce((sum: number, val) => sum + (val as number), 0);
            const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : '0.0';
            return `${label}: ${value} responses (${percentage}%)`;
          }
        }
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

  topicDrilldownOpen = false;
  topicDrilldownLoading = false;
  topicDrilldownError = '';
  selectedTopic: TopTopic | null = null;
  topicDrilldownBySentiment: SentimentDrilldownResponseDto[] = [];

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

  onInstructorChange(value: string) {
    this.instructorFilter = value;
    this.refreshFiltersAndInsights(false, false);
  }

  onCourseChange(value: string) {
    this.courseFilter = value;
    this.refreshFiltersAndInsights(false, true);
  }

  onTopicChange(value: string) {
    this.topicClusterIdFilter = value;
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

  openSentimentDrilldown(sentiment: Sentiment) {
    if (!this.importBatchId) {
      return;
    }

    this.closeTopicDrilldown();

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

  onTopTopicClick(topic: TopTopic) {
    this.openTopicDrilldown(topic);
  }

  openTopicDrilldown(topic: TopTopic) {
    if (!this.importBatchId) {
      return;
    }

    this.closeSentimentDrilldown();

    this.topicDrilldownOpen = true;
    this.topicDrilldownLoading = true;
    this.topicDrilldownError = '';
    this.selectedTopic = topic;
    this.topicDrilldownBySentiment = [];

    const { instructorName, courseNumber } = this.getFilterRequestValues();
    const sentiments: Sentiment[] = ['Positive', 'Neutral', 'Negative'];

    forkJoin(
      sentiments.map((sentiment) =>
        this.api.getSentimentDrilldown({
          targetType: this.targetType,
          sentiment,
          importBatchId: this.importBatchId,
          instructorName,
          courseNumber,
          topicClusterId: topic.topicClusterId,
        }),
      ),
    ).subscribe({
      next: (responses) => {
        this.topicDrilldownBySentiment = responses;
        this.topicDrilldownLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.topicDrilldownError = 'Failed to load topic responses';
        this.topicDrilldownLoading = false;
        this.cdr.detectChanges();
      },
    });
  }

  closeTopicDrilldown() {
    this.topicDrilldownOpen = false;
    this.topicDrilldownLoading = false;
    this.topicDrilldownError = '';
    this.selectedTopic = null;
    this.topicDrilldownBySentiment = [];
  }

  onBatchChange(value: string) {
    this.importBatchId = value;

    if (!this.importBatchId) {
      this.clearLoadedData();
      return;
    }

    this.load();
  }

  onTargetChange(value: TargetType) {
    this.targetType = value;
    this.instructorFilter = '';
    this.courseFilter = '';
    this.topicClusterIdFilter = '';
    this.load();
  }

  onQuarterChange(value: string) {
    this.quarterFilter = value;
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

    if (this.importBatchId) {
      this.load();
      return;
    }

    this.clearLoadedData();
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

  private clearLoadedData() {
    this.sentimentCounts = [];
    this.topTopics = [];
    this.instructors = [];
    this.courses = [];
    this.topics = [];
    this.closeSentimentDrilldown();
    this.closeTopicDrilldown();
    this.error = '';
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

  // PDF Export Methods
  async exportCurrentView() {
    if (!this.importBatchId || this.sentimentCounts.length === 0) {
      alert('Please load data before exporting.');
      return;
    }

    const [{ default: jsPDF }, { default: html2canvas }] = await Promise.all([
      import('jspdf'),
      import('html2canvas'),
    ]);

    const pdf = new jsPDF('p', 'mm', 'a4');
    const pageWidth = pdf.internal.pageSize.getWidth();
    const margin = 15;
    let yPosition = margin;

    // Header
    pdf.setFontSize(20);
    pdf.text('Student Evaluation Analytics', margin, yPosition);
    yPosition += 10;

    pdf.setFontSize(10);
    pdf.setTextColor(100);
    pdf.text(`Export Date: ${new Date().toLocaleDateString()}`, margin, yPosition);
    yPosition += 10;

    // Filters Section
    pdf.setFontSize(12);
    pdf.setTextColor(0);
    pdf.text('Applied Filters', margin, yPosition);
    yPosition += 7;

    pdf.setFontSize(10);
    const selectedBatch = this.filteredBatches.find(b => b.importBatchId === this.importBatchId);
    pdf.text(`Batch: ${selectedBatch?.sourceFileName || 'N/A'}`, margin + 5, yPosition);
    yPosition += 5;
    pdf.text(`Quarter: ${this.quarterFilter || 'All'}`, margin + 5, yPosition);
    yPosition += 5;
    pdf.text(`Target Type: ${this.targetType}`, margin + 5, yPosition);
    yPosition += 5;
    if (this.instructorFilter) {
      pdf.text(`Instructor: ${this.instructorFilter}`, margin + 5, yPosition);
      yPosition += 5;
    }
    if (this.courseFilter) {
      pdf.text(`Course: ${this.courseFilter}`, margin + 5, yPosition);
      yPosition += 5;
    }
    if (this.topicClusterIdFilter) {
      const topic = this.topics.find(t => String(t.topicClusterId) === this.topicClusterIdFilter);
      const topicLabel = topic?.humanLabel
        ? this.resolveTopicLabelForPdf(topic.topicClusterId, topic.humanLabel)
        : this.topicClusterIdFilter;
      pdf.text(`Topic: ${topicLabel}`, margin + 5, yPosition);
      yPosition += 5;
    }
    yPosition += 5;

    // Sentiment Breakdown
    pdf.setFontSize(12);
    pdf.text('Sentiment Overview', margin, yPosition);
    yPosition += 7;

    const total = this.sentimentCounts.reduce((sum, s) => sum + s.count, 0);
    pdf.setFontSize(10);
    this.sentimentCounts.forEach(s => {
      const percentage = total > 0 ? ((s.count / total) * 100).toFixed(1) : '0.0';
      pdf.text(`${s.sentiment}: ${s.count} (${percentage}%)`, margin + 5, yPosition);
      yPosition += 5;
    });
    yPosition += 5;

    // Capture Chart
    const chartElement = document.querySelector('.chart-wrapper canvas') as HTMLCanvasElement;
    if (chartElement) {
      try {
        const canvas = await html2canvas(chartElement, { scale: 2 });
        const chartImage = canvas.toDataURL('image/png');
        const imgWidth = 100;
        const imgHeight = (canvas.height * imgWidth) / canvas.width;
        
        if (yPosition + imgHeight > pdf.internal.pageSize.getHeight() - margin) {
          pdf.addPage();
          yPosition = margin;
        }
        
        pdf.addImage(chartImage, 'PNG', margin, yPosition, imgWidth, imgHeight);
        yPosition += imgHeight + 10;
      } catch (error) {
        console.error('Error capturing chart:', error);
      }
    }

    // Top Topics
    if (this.topTopics.length > 0) {
      if (yPosition > pdf.internal.pageSize.getHeight() - 60) {
        pdf.addPage();
        yPosition = margin;
      }

      pdf.setFontSize(12);
      pdf.text('Top Topics', margin, yPosition);
      yPosition += 7;

      pdf.setFontSize(10);
      this.topTopics.slice(0, 10).forEach(topic => {
        if (yPosition > pdf.internal.pageSize.getHeight() - margin) {
          pdf.addPage();
          yPosition = margin;
        }
        const topicLabel = this.resolveTopicLabelForPdf(topic.topicClusterId, topic.humanLabel);
        pdf.text(`${topicLabel}: ${topic.count}`, margin + 5, yPosition);
        yPosition += 5;
      });
    }

    // Save PDF
    const fileName = `evaluation-report-${this.targetType.toLowerCase()}-${Date.now()}.pdf`;
    pdf.save(fileName);
  }

  async exportQuarterReport() {
    if (!this.quarterFilter) {
      alert('Please select a quarter to generate a quarterly report.');
      return;
    }

    const [{ default: jsPDF }] = await Promise.all([
      import('jspdf'),
    ]);

    const pdf = new jsPDF('p', 'mm', 'a4');
    const pageWidth = pdf.internal.pageSize.getWidth();
    const margin = 15;
    let yPosition = margin;

    // Header
    pdf.setFontSize(22);
    pdf.text('Quarterly Evaluation Report', margin, yPosition);
    yPosition += 10;

    pdf.setFontSize(14);
    pdf.setTextColor(100);
    pdf.text(`${this.quarterFilter}`, margin, yPosition);
    yPosition += 7;

    pdf.setFontSize(10);
    pdf.text(`Generated: ${new Date().toLocaleString()}`, margin, yPosition);
    yPosition += 15;

    // Get data for both target types
    const instructorDataPromise = this.fetchQuarterData('Instructor');
    const courseDataPromise = this.fetchQuarterData('Course');

    try {
      const [instructorData, courseData] = await Promise.all([instructorDataPromise, courseDataPromise]);

      // Instructor Section
      pdf.setFontSize(16);
      pdf.setTextColor(0);
      pdf.text('Instructor Analysis', margin, yPosition);
      yPosition += 8;

      yPosition = this.addDataSectionToPdf(pdf, instructorData, margin, yPosition, 'instructors');

      // Course Section
      if (yPosition > pdf.internal.pageSize.getHeight() - 80) {
        pdf.addPage();
        yPosition = margin;
      } else {
        yPosition += 10;
      }

      pdf.setFontSize(16);
      pdf.text('Course Analysis', margin, yPosition);
      yPosition += 8;

      yPosition = this.addDataSectionToPdf(pdf, courseData, margin, yPosition, 'courses');

      // Summary
      if (yPosition > pdf.internal.pageSize.getHeight() - 60) {
        pdf.addPage();
        yPosition = margin;
      } else {
        yPosition += 10;
      }

      pdf.setFontSize(16);
      pdf.text('Quarter Summary', margin, yPosition);
      yPosition += 8;

      pdf.setFontSize(10);
      const totalResponses = (instructorData.sentiment.total || 0) + (courseData.sentiment.total || 0);
      pdf.text(`Total Responses Analyzed: ${totalResponses}`, margin + 5, yPosition);
      yPosition += 5;

      const avgPositive = ((instructorData.sentiment.positive + courseData.sentiment.positive) / totalResponses * 100).toFixed(1);
      pdf.text(`Overall Positive Sentiment: ${avgPositive}%`, margin + 5, yPosition);
      yPosition += 5;

      const avgNegative = ((instructorData.sentiment.negative + courseData.sentiment.negative) / totalResponses * 100).toFixed(1);
      pdf.text(`Overall Negative Sentiment: ${avgNegative}%`, margin + 5, yPosition);
      yPosition += 10;

      // Overall Assessment
      pdf.setFontSize(11);
      pdf.setFont('helvetica', 'bold');
      pdf.text('Quarter Assessment:', margin + 5, yPosition);
      yPosition += 6;

      pdf.setFont('helvetica', 'normal');
      const positiveRate = parseFloat(avgPositive);
      let assessment = '';
      if (positiveRate >= 70) {
        assessment = 'The quarter shows strong positive sentiment across evaluations.';
      } else if (positiveRate >= 50) {
        assessment = 'The quarter shows moderate satisfaction with room for improvement.';
      } else {
        assessment = 'The quarter indicates concerns that may need attention.';
      }
      
      const splitText = pdf.splitTextToSize(assessment, pageWidth - (margin * 2 + 5));
      pdf.text(splitText, margin + 5, yPosition);

      // Save PDF
      const fileName = `quarter-report-${this.quarterFilter.replace(/\s/g, '-')}-${Date.now()}.pdf`;
      pdf.save(fileName);

    } catch (error) {
      console.error('Error generating quarterly report:', error);
      alert('Failed to generate quarterly report. Please try again.');
    }
  }

  private async fetchQuarterData(targetType: TargetType): Promise<InsightsResponseDto> {
    if (!this.importBatchId) {
      return { sentiment: { positive: 0, neutral: 0, negative: 0, total: 0 }, topTopics: [] };
    }

    return new Promise((resolve, reject) => {
      this.api.getInsights({
        targetType,
        importBatchId: this.importBatchId,
        instructorName: null,
        courseNumber: null,
        topicClusterId: null,
      }).subscribe({
        next: (data) => resolve(data),
        error: (err) => reject(err),
      });
    });
  }

  private addDataSectionToPdf(
    pdf: any,
    data: InsightsResponseDto,
    margin: number,
    startY: number,
    label: string
  ): number {
    let yPosition = startY;
    const pageHeight = pdf.internal.pageSize.getHeight();

    pdf.setFontSize(11);
    pdf.setFont('helvetica', 'bold');
    pdf.text('Sentiment Breakdown:', margin + 5, yPosition);
    yPosition += 6;

    pdf.setFont('helvetica', 'normal');
    pdf.setFontSize(10);
    const total = data.sentiment.total || data.sentiment.positive + data.sentiment.neutral + data.sentiment.negative;
    
    if (total > 0) {
      const posPercent = ((data.sentiment.positive / total) * 100).toFixed(1);
      const neuPercent = ((data.sentiment.neutral / total) * 100).toFixed(1);
      const negPercent = ((data.sentiment.negative / total) * 100).toFixed(1);

      pdf.text(`Positive: ${data.sentiment.positive} (${posPercent}%)`, margin + 10, yPosition);
      yPosition += 5;
      pdf.text(`Neutral: ${data.sentiment.neutral} (${neuPercent}%)`, margin + 10, yPosition);
      yPosition += 5;
      pdf.text(`Negative: ${data.sentiment.negative} (${negPercent}%)`, margin + 10, yPosition);
      yPosition += 5;
      pdf.text(`Total: ${total}`, margin + 10, yPosition);
      yPosition += 8;
    } else {
      pdf.text('No data available', margin + 10, yPosition);
      yPosition += 8;
    }

    if (data.topTopics && data.topTopics.length > 0) {
      if (yPosition > pageHeight - 60) {
        pdf.addPage();
        yPosition = margin;
      }

      pdf.setFont('helvetica', 'bold');
      pdf.text('Top Topics:', margin + 5, yPosition);
      yPosition += 6;

      pdf.setFont('helvetica', 'normal');
      data.topTopics.slice(0, 8).forEach(topic => {
        if (yPosition > pageHeight - margin) {
          pdf.addPage();
          yPosition = margin;
        }
        const topicLabel = this.resolveTopicLabelForPdf(topic.topicClusterId, topic.humanLabel);
        pdf.text(`- ${topicLabel}: ${topic.count}`, margin + 10, yPosition);
        yPosition += 5;
      });
    }

    return yPosition;
  }

  private resolveTopicLabelForPdf(topicClusterId: number, fallbackLabel: string): string {
    const mappedLabel = this.topics.find((topic) => topic.topicClusterId === topicClusterId)?.humanLabel;
    return this.normalizeTopicLabelForPdf(mappedLabel ?? fallbackLabel);
  }

  private normalizeTopicLabelForPdf(label: string): string {
    const cleaned = label
      .replace(/[\u00a0\u2007\u202f]/g, ' ')
      .trim();

    const suffixMatch = cleaned.match(/\s*(\(\d+\))\s*$/);
    const suffix = suffixMatch ? ` ${suffixMatch[1]}` : '';
    const base = suffixMatch ? cleaned.slice(0, cleaned.length - suffixMatch[0].length).trim() : cleaned;

    const spacedWords = base.split(/\s{2,}/).filter(Boolean);
    if (spacedWords.length > 1) {
      const rebuilt = spacedWords.map((word) => word.replace(/\s+/g, '')).join(' ');
      return `${rebuilt}${suffix}`.trim();
    }

    const compactedSingles = base.replace(/(?:\b[A-Za-z]\b\s*){3,}/g, (segment) =>
      segment.replace(/\s+/g, ''),
    );

    return `${compactedSingles.replace(/\s{2,}/g, ' ').trim()}${suffix}`.trim();
  }
}
