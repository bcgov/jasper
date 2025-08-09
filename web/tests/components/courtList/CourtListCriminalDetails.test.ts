import { describe, it, expect, vi, beforeEach } from 'vitest';
import { FilesService } from '@/services/FilesService';
import { mount, flushPromises } from '@vue/test-utils';
import CourtListCriminalDetails from 'CMP/courtlist/CourtListCriminalDetails.vue';
import { nextTick } from 'vue';

vi.mock('@/services/FilesService');

describe('CourtListCriminalDetails.vue', () => {
    let filesService: any;
    let wrapper: ReturnType<typeof mount>;

    const mockDetails = {
        appearanceMethods: [{ method: 'In Person' }],
        keyDocuments: [
            {
            issueDate: '2024-06-01',
            docmFormDsc: 'Form A',
            docmClassification: 'Type 1',
            documentPageCount: 3,
            },
        ],
        charges: [
            {
            printSeqNo: 1,
            statuteSectionDsc: '123',
            statuteDsc: 'Description',
            appearanceResultDesc: 'Result',
            findingDsc: 'Finding',
            },
        ],
    };

  beforeEach(async () => {
    filesService = {
      criminalAppearanceDetails: vi.fn().mockResolvedValue(mockDetails)
    };
    (FilesService as any).mockReturnValue(filesService);
    
    wrapper = mount(CourtListCriminalDetails, {
      global: {
        provide: {
          filesService,
        },
      },
      props: {
        fileId: 'file1',
        appearanceId: 'app1',
        partId: 'part1',
        seqNo: 'seq1',
      },
    });
    await flushPromises();
    await nextTick();
  });

  it('calls filesService.criminalAppearanceDetails on mount', () => {
    expect(filesService.criminalAppearanceDetails).toHaveBeenCalledWith(
      'file1',
      'app1',
      'part1'
    );
  });

  it('renders AppearanceMethods when appearanceMethods exist', () => {
    expect(wrapper.findComponent({name: 'AppearanceMethods'}).exists()).toBe(true);
  });

it('does not render AppearanceMethods when no appearanceMethods', () => {
    mockDetails.appearanceMethods = [];
        wrapper = mount(CourtListCriminalDetails, {
      global: {
        provide: {
          filesService,
        },
      },
      props: {
        fileId: 'file1',
        appearanceId: 'app1',
        partId: 'part1',
        seqNo: 'seq1',
      },
    });
    expect(wrapper.findComponent({name: 'AppearanceMethods'}).exists()).toBe(false);
  });

    it('renders key documents', () => {
      const cards = wrapper.findAll('v-card');
      const tables = wrapper.findAll('v-data-table-virtual');
  
      expect(tables).toHaveLength(2);
      expect(cards).toHaveLength(2);
      expect(cards[0].html()).toContain('Key Documents');
    });
});