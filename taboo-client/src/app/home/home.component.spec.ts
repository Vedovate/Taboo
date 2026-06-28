import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HttpClient } from '@angular/common/http';

import { HomeComponent } from './home.component';
import { LogoPlaceholderComponent } from './logo-placeholder/logo-placeholder.component';
import { LucideAngularModule } from '@lucide/angular';
import { TranslateService } from '../services/translate.service';

describe('HomeComponent', () => {
  let component: HomeComponent;
  let fixture: ComponentFixture<HomeComponent>;
  let httpTestingController: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [HomeComponent, LogoPlaceholderComponent],
      imports: [HttpClientTestingModule, LucideAngularModule],
      providers: [TranslateService],
    }).compileComponents();

    fixture = TestBed.createComponent(HomeComponent);
    component = fixture.componentInstance;
    httpTestingController = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should toggle language', () => {
    const initialLang = component.currentLanguage;
    component.toggleLanguage();
    expect(component.currentLanguage).not.toBe(initialLang);
  });
});
