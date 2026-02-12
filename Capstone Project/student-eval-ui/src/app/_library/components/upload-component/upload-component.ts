import { Component } from '@angular/core';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-upload-component',
  imports: [],
  templateUrl: './upload-component.html',
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
