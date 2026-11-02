import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Torrent } from '../models/torrent.model';
import { TorrentService } from '../torrent.service';

const LINKS_FRAGMENT_PREFIX = 'links=';

@Component({
  selector: 'app-ddl',
  templateUrl: './ddl.component.html',
  styleUrls: ['./ddl.component.scss'],
  imports: [FormsModule, NgClass, RouterLink],
  changeDetection: ChangeDetectionStrategy.Eager,
  standalone: true,
})
export class DdlComponent implements OnInit {
  private activatedRoute = inject(ActivatedRoute);
  private router = inject(Router);
  private torrentService = inject(TorrentService);

  public postUrl: string;
  public links: string = '';

  public torrent: Torrent;

  public loading: boolean = true;
  public notFound: boolean;
  public alreadyAdded: boolean;

  public saving: boolean;
  public error: string;

  ngOnInit(): void {
    this.activatedRoute.queryParams.subscribe((params) => {
      this.postUrl = params['post'];
      this.links = this.readLinksFragment();

      this.load();
    });
  }

  public get hasLinks(): boolean {
    return this.links != null && this.links.trim().length > 0;
  }

  public confirm(): void {
    if (!this.hasLinks) {
      return;
    }

    this.saving = true;
    this.error = null;

    const torrentId = this.torrent.torrentId;

    this.torrentService.addDdlLinks({ torrentId, links: this.links }).subscribe({
      next: () => this.router.navigate([`/torrent/${torrentId}`]),
      error: (err) => {
        this.error = err.error;
        this.saving = false;
      },
    });
  }

  private load(): void {
    this.loading = true;
    this.notFound = false;
    this.alreadyAdded = false;
    this.error = null;
    this.torrent = null;

    if (!this.postUrl) {
      this.loading = false;
      this.notFound = true;

      return;
    }

    this.torrentService.getDdl(this.postUrl).subscribe({
      next: (torrent) => {
        this.loading = false;
        this.torrent = torrent;
        this.alreadyAdded = torrent.downloads.length > 0 || torrent.completed != null;
      },
      error: (err: HttpErrorResponse) => {
        this.loading = false;

        if (err.status === 404) {
          this.notFound = true;
        } else {
          this.error = err.error;
        }
      },
    });
  }

  // The links are handed over in the URL fragment instead of a query parameter so they never reach
  // the server logs. The router already decodes the fragment once, so the value read here is the
  // plain, newline separated list the bookmarklet encoded.
  private readLinksFragment(): string {
    const fragment = this.activatedRoute.snapshot.fragment;

    if (!fragment) {
      return '';
    }

    if (fragment.startsWith(LINKS_FRAGMENT_PREFIX)) {
      return fragment.substring(LINKS_FRAGMENT_PREFIX.length);
    }

    return fragment;
  }
}
