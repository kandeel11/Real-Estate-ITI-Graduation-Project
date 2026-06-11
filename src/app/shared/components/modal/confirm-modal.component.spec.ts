import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ConfirmModalComponent } from './confirm-modal.component';

describe('ConfirmModalComponent', () => {
  let fixture: ComponentFixture<ConfirmModalComponent>;
  let component: ConfirmModalComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ConfirmModalComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ConfirmModalComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('show', true);
    fixture.componentRef.setInput('title', 'Deactivate broker');
    fixture.componentRef.setInput('message', 'Are you sure?');
    fixture.componentRef.setInput('confirmLabel', 'Deactivate');
    fixture.detectChanges();
  });

  it('should render title + message when shown', () => {
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('.cm-title')!.textContent).toContain('Deactivate broker');
    expect(el.querySelector('.cm-message')!.textContent).toContain('Are you sure?');
  });

  it('should render nothing when show is false', () => {
    fixture.componentRef.setInput('show', false);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.cm-box')).toBeNull();
  });

  it('should emit confirmed and cancelled from the footer buttons', () => {
    let confirmedCount = 0;
    let cancelledCount = 0;
    component.confirmed.subscribe(() => confirmedCount++);
    component.cancelled.subscribe(() => cancelledCount++);

    const buttons = fixture.nativeElement.querySelectorAll('.cm-footer button');
    (buttons[1] as HTMLButtonElement).click(); // confirm
    (buttons[0] as HTMLButtonElement).click(); // cancel

    expect(confirmedCount).toBe(1);
    expect(cancelledCount).toBe(1);
  });
});
