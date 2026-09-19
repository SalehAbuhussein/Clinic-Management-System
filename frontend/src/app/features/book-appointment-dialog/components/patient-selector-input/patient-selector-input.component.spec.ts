import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PatientSelectorInputComponent } from './patient-selector-input.component';

describe('PatientSelectorInputComponent', () => {
  let component: PatientSelectorInputComponent;
  let fixture: ComponentFixture<PatientSelectorInputComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PatientSelectorInputComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(PatientSelectorInputComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
