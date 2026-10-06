import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LeaveCardComponent } from './leave-card.component';

describe('LeaveCardComponent', () => {
  let component: LeaveCardComponent;
  let fixture: ComponentFixture<LeaveCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LeaveCardComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(LeaveCardComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
