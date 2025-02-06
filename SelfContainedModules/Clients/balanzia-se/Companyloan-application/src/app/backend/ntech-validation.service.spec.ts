import { TestBed } from '@angular/core/testing';

import { NTechValidationService } from './ntech-validation.service';
import { FormControl } from '@angular/forms';

describe('NTechValidationService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: NTechValidationService = TestBed.get(NTechValidationService);
    expect(service).toBeTruthy();
  });

  it('should accept valid civic regnrs', () => {
    const service: NTechValidationService = TestBed.get(NTechValidationService);
    expect(service.isValidCivicNr('197805175622')).toBeTruthy()
    expect(service.isValidCivicNr('7805175622')).toBeTruthy()
    expect(service.isValidCivicNr('19780517-5622')).toBeTruthy()
  });  

  it('should reject invalid civic regnrs', () => {
    const service: NTechValidationService = TestBed.get(NTechValidationService);
    expect(service.isValidCivicNr('197805175623')).toBeFalsy()
    expect(service.isValidCivicNr('197805175622x')).toBeFalsy()
    expect(service.isValidCivicNr('7805175623')).toBeFalsy()
  }); 
  
  it('should require @ in email', () => {
    const service: NTechValidationService = TestBed.get(NTechValidationService);
    expect(service.isValidEmail('a@b')).toBeTruthy()
    expect(service.isValidCivicNr('a.b')).toBeFalsy()
  });
  
  it('should reject letters in phonenrs', () => {
    const service: NTechValidationService = TestBed.get(NTechValidationService);
    expect(service.isValidPhone('+46 (0) 70 99 88 77')).toBeTruthy()
    expect(service.isValidPhone('+46 (0) A 70 99 88 77')).toBeFalsy()
  });

  it('should allow spaces in integers', () => {
    const service: NTechValidationService = TestBed.get(NTechValidationService);
    let validIntegers = [['0', 0], ['1', 1], [' 0', 0], ['1 ', 1], ['1000', 1000], ['1 000', 1000], ['1 234 567', 1234567]]
    let v = service.getPositiveIntegerValidator()
    for(let p of validIntegers) {
        let str = p[0] as string        
        let nr = p[1] as number
        expect(v(new FormControl(str))).toEqual(null, 'valid: ' + str)
        expect(service.parseInteger(str)).toBe(nr, 'parse: ' + str)
    }
  });
});
