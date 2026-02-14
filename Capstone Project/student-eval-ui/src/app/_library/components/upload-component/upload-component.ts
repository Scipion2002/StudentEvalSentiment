import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
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
  importBatchId: string | null = null;
  uploading = false;
  analyzing = false;

  constructor(private api: ApiService) {}

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
    this.status = 'Uploading...';
    this.importBatchId = null;

    this.api.uploadCourseEvals(this.selectedFile).subscribe({
      next: res => {
        this.uploading = false;

        if (res.skipped) {
          this.status = `Skipped: ${res.reason}`;
          this.importBatchId = res.importBatchId;
          return;
        }

        this.status = `Uploaded successfully (${res.insertedRows} rows)`;
        this.importBatchId = res.importBatchId;
      },
      error: err => {
        this.uploading = false;
        this.status = 'Upload failed';
        console.error(err);
      }
    });
  }

  analyze() {
    if (!this.importBatchId) return;

    this.analyzing = true;
    this.status = 'Analyzing batch...';

    this.api.analyzeBatch(this.importBatchId).subscribe({
      next: res => {
        this.analyzing = false;
        this.status =
          `Analysis complete. Sentiment: ${res.sentimentUpdated}, Topics: ${res.topicsUpdated}`;
      },
      error: err => {
        this.analyzing = false;
        this.status = 'Analysis failed';
        console.error(err);
      }
    });
  }
}
