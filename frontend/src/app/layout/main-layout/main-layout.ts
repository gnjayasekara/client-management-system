
import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { Sidebar } from '../sidebar/sidebar';
import { Header } from '../header/header';

import { OnInit, OnDestroy, inject } from '@angular/core';
import { RealtimeService } from '../../core/services/realtime.service';

@Component({
  selector: 'app-main-layout',
  imports: [RouterOutlet, Sidebar, Header],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.css'
})
export class MainLayout implements OnInit, OnDestroy {
  readonly menuOpen = signal(false);
  
  private readonly realtimeService = inject(RealtimeService);

  ngOnInit(): void {
    void this.realtimeService.start();
  }

  ngOnDestroy(): void {
    void this.realtimeService.stop();
  }


  toggleMenu(): void {
    this.menuOpen.update(value => !value);
  }

  closeMenu(): void {
    this.menuOpen.set(false);
  }
}
