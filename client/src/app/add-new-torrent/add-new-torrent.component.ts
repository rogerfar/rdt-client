import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { DownloadType, Torrent, TorrentFileAvailability } from '../models/torrent.model';
import { SettingsService } from '../settings.service';
import { ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';
import { TorrentService } from '../torrent.service';

@Component({
  selector: 'app-add-new-torrent',
  templateUrl: './add-new-torrent.component.html',
  styleUrls: ['./add-new-torrent.component.scss'],
  imports: [FormsModule, NgClass],
  standalone: true,
})
export class AddNewTorrentComponent implements OnInit {
  private destroyRef = inject(DestroyRef);
  private router = inject(Router);
  private torrentService = inject(TorrentService);
  private settingsService = inject(SettingsService);
  private activatedRoute = inject(ActivatedRoute);

  public type: 'torrent' | 'nzb' = 'torrent';
  public fileName: string;
  public magnetLink: string;
  public nzbLink: string;
  private currentTorrentFile: string;

  public provider: string;
  public downloadClient: number;

  private _category = '';
  public categories: string[] = [];
  public filteredCategories: string[] = [];
  public categoryDropdownOpen = false;
  public hostDownloadAction: number = 0;
  public downloadAction: number = 0;
  public finishedAction: number = 0;
  public finishedActionDelay: number = 0;
  public downloadMinSize: number = 0;
  public includeRegex: string = '';
  public excludeRegex: string = '';
  public torrentRetryAttempts: number = 1;
  public downloadRetryAttempts: number = 3;
  public torrentDeleteOnError: number = 0;
  public torrentLifetime: number = 0;
  public priority: number;

  public availableFiles: TorrentFileAvailability[];
  public downloadFiles: { [key: string]: boolean } = {};
  public allSelected: boolean;

  public saving = false;
  public error: string;

  public includeRegexError: string;
  public excludeRegexError: string;
  public regexSelected: TorrentFileAvailability[];

  private selectedFile: File | null = null;

  public get category(): string {
    return this._category;
  }

  public set category(value: string) {
    this._category = value;
    this.updateFilteredCategories();
  }

  ngOnInit(): void {
    this.activatedRoute.queryParams.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      if (params['type'] === 'nzb') {
        this.type = 'nzb';
      } else if (params['type'] === 'torrent') {
        this.type = 'torrent';
      }

      if (params['magnet']) {
        this.magnetLink = decodeURIComponent(params['magnet']);
        this.type = 'torrent';
      }
      if (params['nzb']) {
        this.nzbLink = decodeURIComponent(params['nzb']);
        this.type = 'nzb';
      }
    });

    this.settingsService
      .get()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((settings) => {
        const providerSetting = settings.find((m) => m.key === 'Provider:Provider');
        this.provider = providerSetting.enumValues[providerSetting.value as number];
        this.downloadClient = settings.find((m) => m.key === 'DownloadClient:Client')?.value as number;

        this.category = settings.find((m) => m.key === 'Gui:Default:Category')?.value as string;
        const categoriesSetting = settings.find((m) => m.key === 'General:Categories')?.value as string;
        this.categories = (categoriesSetting ?? '')
          .split(',')
          .map((c) => c.trim())
          .filter((c) => c.length > 0)
          .filter((c, i, arr) => arr.findIndex((a) => a.toLowerCase() === c.toLowerCase()) === i);
        const matchedCategory = this.categories.find((c) => c.toLowerCase() === (this.category ?? '').toLowerCase());
        if (matchedCategory) {
          this.category = matchedCategory;
        } else {
          this.updateFilteredCategories();
        }
        this.hostDownloadAction = this.downloadAction = settings.find((m) => m.key === 'Gui:Default:HostDownloadAction')
          ?.value as number;
        this.downloadAction =
          settings.find((m) => m.key === 'Gui:Default:OnlyDownloadAvailableFiles')?.value === true ? 1 : 0;
        this.finishedAction = settings.find((m) => m.key === 'Gui:Default:FinishedAction')?.value as number;
        this.finishedActionDelay = settings.find((m) => m.key == 'Gui:Default:FinishedActionDelay')?.value as number;
        this.downloadMinSize = settings.find((m) => m.key === 'Gui:Default:MinFileSize')?.value as number;
        this.includeRegex = settings.find((m) => m.key === 'Gui:Default:IncludeRegex')?.value as string;
        this.excludeRegex = settings.find((m) => m.key === 'Gui:Default:ExcludeRegex')?.value as string;
        this.torrentRetryAttempts = settings.find((m) => m.key === 'Gui:Default:TorrentRetryAttempts')?.value as number;
        this.downloadRetryAttempts = settings.find((m) => m.key === 'Gui:Default:DownloadRetryAttempts')
          ?.value as number;
        this.torrentDeleteOnError = settings.find((m) => m.key === 'Gui:Default:DeleteOnError')?.value as number;
        this.torrentLifetime = settings.find((m) => m.key === 'Gui:Default:TorrentLifetime')?.value as number;
        this.priority = settings.find((m) => m.key === 'Gui:Default:Priority')?.value as number;

        this.setFinishAction();
      });
  }

  private updateFilteredCategories(): void {
    if (!this.category) {
      this.filteredCategories = this.categories;
      return;
    }

    const search = this.category.toLowerCase();
    this.filteredCategories = this.categories.filter((value) => value.toLowerCase().includes(search));
  }

  public selectCategory(cat: string): void {
    this.category = cat;
    this.categoryDropdownOpen = false;
  }

  public onCategoryBlur(): void {
    setTimeout(() => (this.categoryDropdownOpen = false), 150);
  }

  public setFinishAction() {
    if (this.downloadClient === 2) {
      if (this.finishedAction === 1) {
        this.finishedAction = 3;
      } else if (this.finishedAction === 2) {
        this.finishedAction = 0;
      }
    }
  }

  public changeType(type: 'torrent' | 'nzb'): void {
    this.type = type;
    this.fileName = null;
    this.selectedFile = null;
  }

  public pickFile(evt: Event): void {
    const files = (evt.target as HTMLInputElement).files;

    if (files == null || files.length === 0) {
      return;
    }

    const file = files[0];

    this.fileName = file.name;

    this.selectedFile = file;

    this.checkFiles();
  }

  public ok(): void {
    this.saving = true;
    this.error = null;

    let downloadManualFiles: string = null;

    if (this.downloadAction === 2) {
      const selectedFiles = [];
      for (const filePath in this.downloadFiles) {
        if (this.downloadFiles[filePath] === true) {
          selectedFiles.push(filePath);
        }
      }

      if (selectedFiles.length === 0) {
        this.error = 'No files have been selected to download';
        return;
      }

      downloadManualFiles = selectedFiles.join(',');
    }

    const torrent = new Torrent();
    torrent.category = this.category;
    torrent.hostDownloadAction = this.hostDownloadAction;
    torrent.downloadAction = this.downloadAction;
    torrent.finishedAction = this.finishedAction;
    torrent.finishedActionDelay = this.finishedActionDelay;
    torrent.downloadMinSize = this.downloadMinSize;
    torrent.includeRegex = this.includeRegex;
    torrent.excludeRegex = this.excludeRegex;
    torrent.downloadManualFiles = downloadManualFiles;
    torrent.priority = this.priority;
    torrent.torrentRetryAttempts = this.torrentRetryAttempts;
    torrent.downloadRetryAttempts = this.downloadRetryAttempts;
    torrent.deleteOnError = this.torrentDeleteOnError;
    torrent.lifetime = this.torrentLifetime;
    torrent.downloadClient = this.downloadClient;

    if (this.type === 'torrent') {
      torrent.type = DownloadType.Torrent;
      if (this.magnetLink) {
        this.torrentService.uploadMagnet(this.magnetLink, torrent).subscribe({
          next: () => this.router.navigate(['/']),
          error: (err) => {
            this.error = err.error;
            this.saving = false;
          },
        });
      } else if (this.selectedFile) {
        this.torrentService.uploadFile(this.selectedFile, torrent).subscribe({
          next: () => this.router.navigate(['/']),
          error: (err) => {
            this.error = err.error;
            this.saving = false;
          },
        });
      } else {
        this.error = 'No magnet or file uploaded';
        this.saving = false;
      }
    } else {
      if (this.nzbLink) {
        this.torrentService.uploadNzbLink(this.nzbLink, torrent).subscribe({
          next: () => this.router.navigate(['/']),
          error: (err) => {
            this.error = err.error;
            this.saving = false;
          },
        });
      } else if (this.selectedFile) {
        this.torrentService.uploadNzbFile(this.selectedFile, torrent).subscribe({
          next: () => this.router.navigate(['/']),
          error: (err) => {
            this.error = err.error;
            this.saving = false;
          },
        });
      } else {
        this.error = 'No NZB link or file uploaded';
        this.saving = false;
      }
    }
  }

  public onPaste(): void {
    setTimeout(() => {
      this.checkFiles();
    }, 100);
  }

  public checkFiles(): void {
    if (this.type === 'nzb') {
      return;
    }
    if (this.magnetLink && this.magnetLink === this.currentTorrentFile) {
      return;
    }

    this.saving = true;
    this.error = null;
    this.availableFiles = null;
    this.downloadFiles = {};
    this.allSelected = true;

    if (this.magnetLink) {
      this.torrentService.checkFilesMagnet(this.magnetLink).subscribe({
        next: (result) => {
          this.saving = false;
          this.availableFiles = result;
          this.currentTorrentFile = this.magnetLink;
          result.forEach((file) => {
            this.downloadFiles[file.filename] = true;
          });
        },
        error: (err) => {
          this.error = err.error;
          this.saving = false;
        },
      });
    } else if (this.selectedFile) {
      this.torrentService.checkFiles(this.selectedFile).subscribe({
        next: (result) => {
          this.saving = false;
          this.availableFiles = result;
          result.forEach((file) => {
            this.downloadFiles[file.filename] = true;
          });
        },
        error: (err) => {
          this.error = err.error;
          this.saving = false;
        },
      });
    } else {
      this.saving = false;
    }
  }

  public verifyRegex(): void {
    this.includeRegexError = null;
    this.excludeRegexError = null;
    this.regexSelected = null;

    this.torrentService.verifyRegex(this.includeRegex, this.excludeRegex, this.magnetLink).subscribe((result) => {
      this.includeRegexError = result.includeError;
      this.excludeRegexError = result.excludeError;
      this.regexSelected = result.selectedFiles;
    });
  }
}
