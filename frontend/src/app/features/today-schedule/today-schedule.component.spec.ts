import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TodayScheduleComponent } from './today-schedule.component';

describe('TodayScheduleComponent', () => {
  let component: TodayScheduleComponent;
  let fixture: ComponentFixture<TodayScheduleComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TodayScheduleComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(TodayScheduleComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
