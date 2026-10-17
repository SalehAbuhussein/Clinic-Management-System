import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DoctorSelectorInputComponent } from './doctor-selector-input.component';

describe('DoctorSelectorInputComponent', () => {
  let component: DoctorSelectorInputComponent;
  let fixture: ComponentFixture<DoctorSelectorInputComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DoctorSelectorInputComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(DoctorSelectorInputComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
