import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
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

  constructor(private api: ApiService) {}

  load() {
    if (!this.importBatchId.trim()) return;

    this.loading = true;
    this.error = '';

    this.api.getSentimentCounts(this.importBatchId.trim(), this.targetType).subscribe({
      next: (res) => this.sentimentCounts = res,
      error: (err) => this.error = 'Failed to load sentiment counts'
    });

    this.api.getTopTopics(this.importBatchId.trim(), this.targetType, 10).subscribe({
      next: (res) => {
        this.topTopics = res;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load topics';
        this.loading = false;
      }
    });
  }
}
