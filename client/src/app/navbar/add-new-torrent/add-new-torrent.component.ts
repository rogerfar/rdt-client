import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { TorrentService } from 'src/app/torrent.service';

@Component({
  selector: 'app-add-new-torrent',
  templateUrl: './add-new-torrent.component.html',
  styleUrls: ['./add-new-torrent.component.scss'],
})
export class AddNewTorrentComponent implements OnInit {
  @Input()
  public get open(): boolean {
    return this.isActive;
  }

  public set open(val: boolean) {
    this.reset();
    this.isActive = val;
  }

  @Output()
  public openChange = new EventEmitter<boolean>();

  public isActive = false;

  public fileName: string;
  public magnetLink: string;
  public autoDownload: boolean;
  public autoUnpack: boolean;
  public autoDelete: boolean;

  public saving = false;
  public error: string;

  private selectedFile: File;

  constructor(private torrentService: TorrentService) {}

  ngOnInit(): void {}

  public reset(): void {
    this.fileName = '';
    this.magnetLink = '';
    this.autoDelete = false;
    this.autoDownload = true;

    this.saving = false;
    this.selectedFile = null;
    this.error = null;
  }

  public pickFile(evt: Event): void {
    const files = (evt.target as HTMLInputElement).files;

    if (files.length === 0) {
      return;
    }

    const file = files[0];

    this.fileName = file.name;

    this.selectedFile = file;
  }

  public ok(): void {
    this.saving = true;
    this.error = null;

    if (this.magnetLink) {
      this.torrentService
        .uploadMagnet(
          this.magnetLink,
          this.autoDownload,
          this.autoUnpack,
          this.autoDelete
        )
        .subscribe(
          () => {
            this.cancel();
          },
          (err) => {
            this.error = err.error;
            this.saving = false;
          }
        );
    } else if (this.selectedFile) {
      this.torrentService
        .uploadFile(
          this.selectedFile,
          this.autoDownload,
          this.autoUnpack,
          this.autoDelete
        )
        .subscribe(
          () => {
            this.cancel();
          },
          (err) => {
            this.error = err.error;
            this.saving = false;
          }
        );
    } else {
      this.cancel();
    }
  }

  public cancel(): void {
    this.isActive = false;
    this.openChange.emit(this.open);
  }
}
