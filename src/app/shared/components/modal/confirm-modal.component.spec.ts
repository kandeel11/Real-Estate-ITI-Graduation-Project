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
    fixture.componentRef.setInput('title', 'Deactivate broker');
    fixture.componentRef.setInput('message', 'Are you sure?');
    fixture.detectChanges();
  });

  it('should create and render title + message', () => {
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('.modal-title')!.textContent).toContain('Deactivate broker');
    expect(el.querySelector('.modal-body')!.textContent).toContain('Are you sure?');
  });

  it('should emit confirmed on confirm click and cancelled on cancel click', () => {
    let confirmedCount = 0;
    let cancelledCount = 0;
    component.confirmed.subscribe(() => confirmedCount++);
    component.cancelled.subscribe(() => cancelledCount++);

    const buttons = fixture.nativeElement.querySelectorAll('.modal-footer button');
    (buttons[1] as HTMLButtonElement).click(); // confirm
    (buttons[0] as HTMLButtonElement).click(); // cancel

    expect(confirmedCount).toBe(1);
    expect(cancelledCount).toBe(1);
  });
});
