import CriminalCaseDetails from '@/components/criminal/CriminalCaseDetails.vue';
import { useCourtFileSearchStore } from '@/stores';
import { createPinia, setActivePinia } from 'pinia';
import { shallowMount, flushPromises } from '@vue/test-utils';
import { beforeEach, describe, expect, it, vi, type Mock } from 'vitest';
import { useRoute } from 'vue-router';

vi.mock('vue-router', () => ({
  useRoute: vi.fn(),
}));

describe('CriminalCaseDetails.vue', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    (useRoute as unknown as Mock).mockReturnValue({
      params: {
        fileNumber: '789',
      },
    });
  });

  it('syncs selected files for the current criminal case when overview loads', async () => {
    const filesService = {
      criminalFileOverview: vi.fn().mockResolvedValue({
        justinNo: 'J-789',
        fileNumberTxt: 'CRIM-789-TXT',
        participant: [],
        crown: [],
        witness: [],
        appearances: { apprDetail: [] },
        hearingRestriction: [],
        courtLevelCd: 'P',
        courtClassCd: 'A',
        activityClassCd: 'B',
      }),
      criminalFileParticipants: vi.fn().mockResolvedValue([]),
      criminalFileAppearances: vi.fn().mockResolvedValue({ apprDetail: [] }),
      criminalFileHearingRestrictions: vi.fn().mockResolvedValue([]),
    };

    const httpService = {
      post: vi.fn(),
    };

    const darsService = {
      getTranscripts: vi.fn().mockResolvedValue([]),
    };

    const courtFileSearchStore = useCourtFileSearchStore();
    courtFileSearchStore.clearSelectedFiles();

    shallowMount(CriminalCaseDetails, {
      global: {
        provide: {
          filesService,
          httpService,
          darsService,
        },
        stubs: {
          CourtFilesSelector: true,
          CriminalSidePanel: true,
          CaseHeader: true,
        },
      },
    });

    await flushPromises();

    expect(filesService.criminalFileOverview).toHaveBeenCalledWith('789');
    expect(courtFileSearchStore.selectedFiles).toEqual([
      {
        key: '789',
        value: 'CRIM-789-TXT',
      },
    ]);
  });
});
