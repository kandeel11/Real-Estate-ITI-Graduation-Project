import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

import { AiComponent } from './ai.component';

describe('Company AiComponent', () => {
  let component: AiComponent;
  let fixture: ComponentFixture<AiComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AiComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(AiComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should not send chat messages while the plan does not include the chatbot', () => {
    component.draft = 'hello';
    component.send();
    expect(component.messages().length).toBe(0);
  });
});
