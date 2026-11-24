import { money, statusLabel } from './labels';

describe('claim labels', () => {
  it('uses the assessment wording for additional information', () => {
    expect(statusLabel('AdditionalInformationRequired')).toBe('Awaiting information');
  });

  it('formats liability amounts in Singapore dollars', () => {
    expect(money(12600)).toContain('12,600.00');
  });

  it('treats a missing amount as zero', () => {
    expect(money(null)).toContain('0.00');
  });
});
