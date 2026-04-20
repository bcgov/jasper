import CivilCaseDetails from '@/components/civil/CivilCaseDetails.vue';
import { useCourtFileSearchStore } from '@/stores';
import { createPinia, setActivePinia } from 'pinia';
import { shallowMount, flushPromises } from '@vue/test-utils';
import { beforeEach, describe, expect, it, vi, type Mock } from 'vitest';
import { useRoute } from 'vue-router';

vi.mock('vue-router', () => ({
  useRoute: vi.fn(),
}));

describe('CivilCaseDetails.vue', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    (useRoute as unknown as Mock).mockReturnValue({
      params: {
        fileNumber: '123',
        section: undefined,
      },
    });
  });

  it('syncs selected files for the current civil case on load', async () => {
    const httpService = {
      get: vi.fn().mockResolvedValue({
        fileNumberTxt: 'CIV-123-TXT',
        physicalFileId: 'PF-123',
        sealedYN: 'N',
        party: [
          {
            partyId: 'P1',
            roleTypeDescription: 'Plaintiff',
            counsel: [],
            leftRightCd: 'L',
            givenNm: 'Jane',
            lastNm: 'Doe',
            orgNm: '',
          },
        ],
        hearingRestriction: [],
        document: [],
        referenceDocument: [],
        courtClassCd: 'A',
        activityClassCd: 'B',
      }),
      post: vi.fn(),
    };

    const darsService = {
      getTranscripts: vi.fn().mockResolvedValue([]),
    };

    const courtFileSearchStore = useCourtFileSearchStore();
    courtFileSearchStore.clearSelectedFiles();

    shallowMount(CivilCaseDetails, {
      global: {
        provide: {
          httpService,
          darsService,
        },
        stubs: {
          CourtFilesSelector: true,
          CivilSidePanel: true,
          CaseHeader: true,
        },
      },
    });

    await flushPromises();

    expect(httpService.get).toHaveBeenCalledWith('api/files/civil/123');
    expect(courtFileSearchStore.selectedFiles).toEqual([
      {
        key: '123',
        value: 'CIV-123-TXT',
      },
    ]);
  });
});
