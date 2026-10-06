import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LeaveDetailDialogComponent } from './leave-detail-dialog.component';

describe('LeaveDetailDialogComponent', () => {
  let component: LeaveDetailDialogComponent;
  let fixture: ComponentFixture<LeaveDetailDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LeaveDetailDialogComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(LeaveDetailDialogComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
