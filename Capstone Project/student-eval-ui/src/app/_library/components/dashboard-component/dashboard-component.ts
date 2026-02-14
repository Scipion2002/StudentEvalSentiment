import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dashboard-component.html',
  styleUrl: './dashboard-component.scss'
})
export class DashboardComponent {
  importBatchId = '';
  targetType: 'Instructor' | 'Course' = 'Instructor';

  sentimentCounts: { sentiment: string; count: number }[] = [];
  topTopics: { topicClusterId: number; humanLabel: string; count: number }[] = [];

  loading = false;
  error = '';

  constructor(private api: ApiService, private cdr: ChangeDetectorRef) {}

  load() {
    if (!this.importBatchId.trim()) return;

    this.loading = true;
    this.error = '';

    const batchId = this.importBatchId.trim();
    const target = this.targetType;

    forkJoin([
      this.api.getSentimentCounts(batchId, target),
      this.api.getTopTopics(batchId, target, 10)
    ]).subscribe({
      next: ([sentiments, topics]) => {
        this.sentimentCounts = sentiments;
        this.topTopics = topics;
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
}
