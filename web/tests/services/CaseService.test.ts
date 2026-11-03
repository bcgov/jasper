import { CaseService } from '@/services/CaseService';
import { IHttpService } from '@/services/HttpService';
import { Case } from '@/types';
import { describe, expect, it, vi } from 'vitest';

describe('CaseService due date overrides', () => {
  it('sends only the override and displayed revision, handling conflict errors locally', async () => {
    const put = vi
      .fn()
      .mockResolvedValue({ succeeded: true, payload: [], errors: [] });
    const service = new CaseService({ put } as unknown as IHttpService);
    const row = {
      id: 'case/id',
      judgeId: 123,
      appearanceId: 'appearance',
      updatedDate: '2026-11-03T12:00:00Z',
    } as Case;
    await service.setDueDateOverride(row, null);
    expect(put).toHaveBeenCalledWith(
      'api/cases/case%2Fid/due-date-override',
      { overrideDueDate: null, updatedDate: row.updatedDate },
      {},
      'json',
      { suppressConflictLogout: true, suppressErrorNotification: true }
    );
  });
});
