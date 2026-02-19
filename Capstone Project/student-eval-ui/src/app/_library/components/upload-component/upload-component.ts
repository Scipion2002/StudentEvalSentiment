import { ChangeDetectorRef, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { finalize, timeout } from 'rxjs';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-upload-component',
  imports: [CommonModule],
  templateUrl: './upload-component.html',
  styleUrls: ['./upload-component.scss'],
})
export class UploadComponent {

  selectedFile: File | null = null;
  status = '';
  statusReason = '';
  statusType: 'success' | 'error' | 'info' = 'info';
  importBatchId: string | null = null;
  uploading = false;
  analyzing = false;

  constructor(private api: ApiService, private cdr: ChangeDetectorRef) {}

  onFileChange(event: Event) {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
  }

  onDragOver(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
  }

  onDragLeave(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    
    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.selectedFile = files[0];
    }
  }

  upload() {
    if (!this.selectedFile) return;

    this.uploading = true;
    this.setStatus('Uploading...', 'info');
    this.importBatchId = null;

    this.api.uploadCourseEvals(this.selectedFile)
    .pipe(
      timeout(60000),
      finalize(() => {
        this.uploading = false;
        this.cdr.detectChanges();
      })
    )
    .subscribe({
      next: res => {
        const reason = res.reason ?? '';

        if (res.skipped) {
          this.setStatus('Upload skipped', 'info', reason || 'Import batch is already in use.');
          this.importBatchId = res.importBatchId;
          return;
        }

        this.setStatus('Upload complete', 'success', reason);
        this.importBatchId = res.importBatchId;
      },
      error: err => {
        this.setStatus('Upload failed', 'error', this.extractReason(err));
        console.error(err);
        this.cdr.detectChanges();
      }
    });
  }

  analyze() {
    if (!this.importBatchId) return;

    this.analyzing = true;
    this.setStatus('Analyzing batch...', 'info');

    this.api.analyzeBatch(this.importBatchId)
    .pipe(
      timeout(60000),
      finalize(() => {
        this.analyzing = false;
        this.cdr.detectChanges();
      })
    )
    .subscribe({
      next: res => {
        this.setStatus(
          `Analysis complete. Sentiment: ${res.sentimentUpdated}, Topics: ${res.topicsUpdated}`,
          'success'
        );
      },
      error: err => {
        this.setStatus('Analysis failed', 'error', this.extractReason(err));
        console.error(err);
        this.cdr.detectChanges();
      }
    });
  }

  private setStatus(
    message: string,
    type: 'success' | 'error' | 'info',
    reason: string | undefined = ''
  ) {
    this.status = message;
    this.statusType = type;
    this.statusReason = reason?.trim() ?? '';
  }

  private extractReason(err: unknown): string {
    const error = err as {
      name?: string;
      message?: string;
      error?: { reason?: string; message?: string } | string;
    };

    if (error?.name === 'TimeoutError') {
      return 'The request timed out. Please try again.';
    }

    if (typeof error?.error === 'string' && error.error.trim()) {
      return error.error;
    }

    if (error?.error && typeof error.error === 'object') {
      const backendObject = error.error as { reason?: string; message?: string };
      if (backendObject.reason?.trim()) {
        return backendObject.reason;
      }
      if (backendObject.message?.trim()) {
        return backendObject.message;
      }
    }

    return error?.message?.trim() ? error.message : '';
  }
}
