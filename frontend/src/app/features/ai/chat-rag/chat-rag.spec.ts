import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ChatRag } from './chat-rag';

describe('ChatRag', () => {
  let component: ChatRag;
  let fixture: ComponentFixture<ChatRag>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChatRag],
    }).compileComponents();

    fixture = TestBed.createComponent(ChatRag);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
