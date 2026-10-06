import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LeavesListComponent } from './leaves-list.component';

describe('LeavesListComponent', () => {
  let component: LeavesListComponent;
  let fixture: ComponentFixture<LeavesListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LeavesListComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(LeavesListComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
