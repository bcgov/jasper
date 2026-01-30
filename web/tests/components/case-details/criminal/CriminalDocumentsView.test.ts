import shared from '@/components/shared';
import { useCriminalFileStore } from '@/stores';
import { DocumentRequestType } from '@/types/shared';
import { flushPromises, shallowMount } from '@vue/test-utils';
import DocumentsView from 'CMP/case-details/criminal/CriminalDocumentsView.vue';
import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';

describe('CriminalDocumentsView.vue', () => {
  let wrapper: any;
  let mockParticipantOne: any;
  let mockParticipantTwo: any;
  let mockParticipants: any[];
  let mockDocumentOne: any;

  beforeEach(() => {
    setActivePinia(createPinia());

    // Initialize criminal file store with required data
    const criminalFileStore = useCriminalFileStore();
    criminalFileStore.criminalFileInformation = {
      detailsData: {
        courtClassCd: 'A',
        courtLevelCd: 'P',
        fileNumberTxt: 'TEST-123',
        homeLocationAgencyName: 'Vancouver',
      },
      fileNumber: 'TEST-123',
      participantList: [],
      adjudicatorRestrictionsInfo: [],
      bans: [],
      courtLevel: 'Provincial',
      courtClass: 'Adult',
    } as any;

    mockDocumentOne = {
      issueDate: '2023-01-01',
      documentTypeDescription: 'Type A',
      category: 'bail',
      documentPageCount: 5,
      imageId: '123',
      docmDispositionDsc: 'Disposition',
    };
    mockParticipantOne = {
      fullName: 'John Doe',
      lastNm: 'Doe',
      givenNm: 'John',
      profSeqNo: 1,
      document: [mockDocumentOne],
      keyDocuments: [],
    };
    mockParticipantTwo = {
      fullName: 'Jane Smith',
      profSeqNo: 2,
      lastNm: 'Smith',
      givenNm: 'Jane',
      document: [
        {
          issueDate: '2023-02-01',
          documentTypeDescription: 'Type B',
          category: 'other',
          documentPageCount: 3,
          imageId: '456',
          docmDispositionDsc: 'Disposition',
        },
      ],
    };
    mockParticipants = [mockParticipantOne, mockParticipantTwo];
    wrapper = shallowMount(DocumentsView, {
      props: {
        participants: mockParticipants,
      },
    });
  });

  it('renders only all documents table when no key documents', () => {
    const sections = wrapper.findAll('v-card-text .text-h5');
    const tables = wrapper.findAll('v-data-table-virtual');

    expect(tables).toHaveLength(1);
    expect(sections).toHaveLength(2);
    expect(sections[0].text()).toContain('Key Documents');
    expect(sections[1].text()).toContain('All Documents');
  });

  it('renders both all and key document tables', () => {
    mockParticipants[0].keyDocuments.push(mockDocumentOne);
    const wrapper = shallowMount(DocumentsView, {
      props: {
        participants: mockParticipants,
      },
    });

    const sections = wrapper.findAll('v-card-text .text-h5');
    const tables = wrapper.findAll('v-data-table-virtual');

    expect(tables).toHaveLength(2);
    expect(sections).toHaveLength(2);
    expect(sections[0].text()).toContain('Key Documents');
    expect(sections[1].text()).toContain('All Documents');
  });

  it('renders correct all documents header with correct count', () => {
    mockParticipants.push(mockParticipantOne);
    const wrapper = shallowMount(DocumentsView, {
      props: {
        participants: mockParticipants,
      },
    });
    const sections = wrapper.findAll('v-card-text .text-h5');
    expect(sections).toHaveLength(2);
    expect(sections[1].text()).toBe('All Documents (3)');
  });

  it('renders correct key documents header with correct count', () => {
    mockParticipants[0].keyDocuments.push(mockDocumentOne);
    const wrapper = shallowMount(DocumentsView, {
      props: {
        participants: mockParticipants,
      },
    });
    const sections = wrapper.findAll('v-card-text .text-h5');
    expect(sections).toHaveLength(2);
    expect(sections[0].text()).toBe('Key Documents (1)');
  });

  it('does not render table when no documents', () => {
    mockParticipants.length = 0;
    mockParticipantOne.document = [];
    mockParticipants.push(mockParticipantOne);

    const wrapper = shallowMount(DocumentsView, {
      props: {
        participants: mockParticipants,
      },
    });
    const sections = wrapper.findAll('v-card-text .text-h5');
    const tables = wrapper.findAll('v-data-table-virtual');
    expect(tables).toHaveLength(0);
    expect(sections).toHaveLength(0);
  });

  it('computes unfilteredDocuments correctly', async () => {
    wrapper.vm.selectedCategory = 'ROP';

    const unfilteredDocuments = wrapper.vm.unfilteredDocuments;
    expect(unfilteredDocuments).toHaveLength(2);
  });

  it('filters documents by category', async () => {
    wrapper.vm.selectedCategory = 'bail';

    const documents = wrapper.vm.documents;
    expect(documents).toHaveLength(1);
    expect(documents[0].category).toBe('bail');
  });

  it('does not filter key documents by category', async () => {
    const bailKeyDoc = { ...mockDocumentOne, category: 'bail' };
    const otherKeyDoc = {
      issueDate: '2023-03-01',
      documentTypeDescription: 'Type C',
      category: 'other',
      documentPageCount: 2,
      imageId: '789',
    };
    mockParticipants[0].keyDocuments = [bailKeyDoc, otherKeyDoc];

    const wrapper = shallowMount(DocumentsView, {
      props: {
        participants: mockParticipants,
      },
    });

    (wrapper.vm as any).selectedCategory = 'bail';
    await nextTick();

    const documents = (wrapper.vm as any).documents;
    const keyDocuments = (wrapper.vm as any).keyDocuments;

    expect(documents).toHaveLength(1);
    expect(documents[0].category).toBe('bail');

    expect(keyDocuments).toHaveLength(2);
    expect(keyDocuments.some((doc: any) => doc.category === 'bail')).toBe(true);
    expect(keyDocuments.some((doc: any) => doc.category === 'other')).toBe(
      true
    );
  });

  it('filters both documents and key documents by accused', async () => {
    mockParticipants[0].keyDocuments = [mockDocumentOne];
    mockParticipants[1].keyDocuments = [
      {
        issueDate: '2023-03-01',
        documentTypeDescription: 'Type C',
        category: 'other',
        documentPageCount: 2,
        imageId: '789',
      },
    ];

    const wrapper = shallowMount(DocumentsView, {
      props: {
        participants: mockParticipants,
      },
    });

    (wrapper.vm as any).selectedAccused = 'Doe, John';
    await nextTick();

    const documents = (wrapper.vm as any).documents;
    const keyDocuments = (wrapper.vm as any).keyDocuments;

    expect(documents).toHaveLength(1);
    expect(documents[0].fullName).toBe('John Doe');

    expect(keyDocuments).toHaveLength(1);
    expect(keyDocuments[0].fullName).toBe('John Doe');
  });

  it('renders action-bar when two or more documents with imageIds are selected', async () => {
    wrapper.vm.selectedItems = [
      mockParticipantOne.document[0],
      mockParticipantTwo.document[0],
    ];

    await nextTick();

    expect(wrapper.findComponent({ name: 'ActionBar' }).exists()).toBe(true);
  });

  it('does not render action-bar when two or more documents without imageIds are selected', async () => {
    wrapper.vm.selectedItems = [{}, {}];

    await nextTick();

    expect(wrapper.findComponent({ name: 'ActionBar' }).exists()).toBe(false);
  });

  it('does not render action-bar when one document with imageId is selected', async () => {
    wrapper.vm.selectedItems = [mockParticipantOne.document[0]];

    await nextTick();

    expect(wrapper.findComponent({ name: 'ActionBar' }).exists()).toBe(false);
  });

  it('renders bail document with perfected label date', async () => {
    mockDocumentOne.docmDispositionDsc = 'Perfected';
    mockParticipants[0].keyDocuments.push(mockDocumentOne);
    wrapper = shallowMount(DocumentsView, {
      global: {
        stubs: {
          'v-data-table-virtual': {
            template: `
                        <slot name="item.documentTypeDescription" :item="items && items[0] ? items[0] : { category: 'bail' }"></slot>
                        `,
            props: ['headers', 'items', 'itemValue', 'columns'],
            methods: {
              isGroupOpen: () => true,
              toggleGroup: () => {},
            },
          },
          'v-banner': true,
          'v-card-text': true,
        },
      },
      props: {
        participants: mockParticipants,
      },
    });
    await flushPromises();

    expect(wrapper.text()).toContain('Perfected');
    expect(wrapper.text()).toContain('01-Jan-2023');
  });

  it('does not render bail document with unperfected label date', async () => {
    mockDocumentOne.docmDispositionDsc = 'Unperfected';
    mockParticipants[0].keyDocuments.push(mockDocumentOne);
    wrapper = shallowMount(DocumentsView, {
      global: {
        stubs: {
          'v-data-table-virtual': {
            template: `
                        <slot name="item.documentTypeDescription" :item="items && items[0] ? items[0] : { category: 'bail' }"></slot>
                        `,
            props: ['headers', 'items', 'itemValue', 'columns'],
            methods: {
              isGroupOpen: () => true,
              toggleGroup: () => {},
            },
          },
          'v-banner': true,
          'v-card-text': true,
        },
      },
      props: {
        participants: mockParticipants,
      },
    });
    await flushPromises();

    expect(wrapper.text()).not.toContain('Unperfected');
    expect(wrapper.text()).not.toContain('01-Jan-2023');
  });

  describe('openMergedDocuments', () => {
    beforeEach(() => {
      vi.spyOn(shared, 'openMergedDocuments');
    });

    it('should handle regular documents with File request type', () => {
      const regularDoc = {
        issueDate: '2023-01-01',
        documentTypeDescription: 'Regular Document',
        category: 'other',
        documentPageCount: 5,
        imageId: '123',
        docmDispositionDsc: 'Disposition',
        partId: 'part1',
      };

      mockParticipantOne.document = [regularDoc];
      wrapper = shallowMount(DocumentsView, {
        props: {
          participants: [mockParticipantOne],
        },
      });

      wrapper.vm.selectedItems = [
        { ...regularDoc, fullName: 'John Doe', profSeqNo: 1 },
      ];
      wrapper.vm.openMergedDocuments();

      expect(shared.openMergedDocuments).toHaveBeenCalledWith(
        expect.arrayContaining([
          expect.objectContaining({
            documentType: DocumentRequestType.File,
          }),
        ])
      );
    });

    it('should handle transcript documents with Transcript request type', () => {
      const transcriptDoc = {
        issueDate: '2023-01-01',
        documentTypeDescription: 'Transcript',
        category: 'Transcript',
        documentPageCount: 10,
        imageId: '456',
        docmDispositionDsc: 'Complete',
        partId: 'part2',
      };

      mockParticipantOne.document = [transcriptDoc];
      wrapper = shallowMount(DocumentsView, {
        props: {
          participants: [mockParticipantOne],
        },
      });

      wrapper.vm.selectedItems = [
        { ...transcriptDoc, fullName: 'John Doe', profSeqNo: 1 },
      ];
      wrapper.vm.openMergedDocuments();

      expect(shared.openMergedDocuments).toHaveBeenCalledWith(
        expect.arrayContaining([
          expect.objectContaining({
            documentType: DocumentRequestType.Transcript,
          }),
        ])
      );
    });

    it('should handle ROP documents with ROP request type', () => {
      const ropDoc = {
        issueDate: '2023-01-01',
        documentTypeDescription: 'ROP Document',
        category: 'ROP',
        documentPageCount: 15,
        imageId: '789',
        docmDispositionDsc: 'Disposition',
        partId: 'part3',
      };

      mockParticipantOne.document = [ropDoc];
      wrapper = shallowMount(DocumentsView, {
        props: {
          participants: [mockParticipantOne],
        },
      });

      wrapper.vm.selectedItems = [
        { ...ropDoc, fullName: 'John Doe', profSeqNo: 1 },
      ];
      wrapper.vm.openMergedDocuments();

      expect(shared.openMergedDocuments).toHaveBeenCalledWith(
        expect.arrayContaining([
          expect.objectContaining({
            documentType: DocumentRequestType.ROP,
          }),
        ])
      );
    });

    it('should handle mixed document types correctly', () => {
      const regularDoc = {
        issueDate: '2023-01-01',
        documentTypeDescription: 'Regular Document',
        category: 'other',
        documentPageCount: 5,
        imageId: '123',
        partId: 'part1',
      };

      const transcriptDoc = {
        issueDate: '2023-02-01',
        documentTypeDescription: 'Transcript',
        category: 'Transcript',
        documentPageCount: 10,
        imageId: '456',
        partId: 'part2',
      };

      const ropDoc = {
        issueDate: '2023-03-01',
        documentTypeDescription: 'ROP Document',
        category: 'ROP',
        documentPageCount: 15,
        imageId: '789',
        partId: 'part3',
      };

      mockParticipantOne.document = [regularDoc, transcriptDoc, ropDoc];
      wrapper = shallowMount(DocumentsView, {
        props: {
          participants: [mockParticipantOne],
        },
      });

      wrapper.vm.selectedItems = [
        { ...regularDoc, fullName: 'John Doe', profSeqNo: 1 },
        { ...transcriptDoc, fullName: 'John Doe', profSeqNo: 1 },
        { ...ropDoc, fullName: 'John Doe', profSeqNo: 1 },
      ];
      wrapper.vm.openMergedDocuments();

      expect(shared.openMergedDocuments).toHaveBeenCalledWith(
        expect.arrayContaining([
          expect.objectContaining({
            documentType: DocumentRequestType.File,
          }),
          expect.objectContaining({
            documentType: DocumentRequestType.Transcript,
          }),
          expect.objectContaining({
            documentType: DocumentRequestType.ROP,
          }),
        ])
      );
    });

    it('should filter out documents without imageId', () => {
      const docWithImage = {
        issueDate: '2023-01-01',
        documentTypeDescription: 'Document 1',
        category: 'other',
        documentPageCount: 5,
        imageId: '123',
        partId: 'part1',
      };

      const docWithoutImage = {
        issueDate: '2023-02-01',
        documentTypeDescription: 'Document 2',
        category: 'other',
        documentPageCount: 3,
        imageId: '',
        partId: 'part2',
      };

      mockParticipantOne.document = [docWithImage, docWithoutImage];
      wrapper = shallowMount(DocumentsView, {
        props: {
          participants: [mockParticipantOne],
        },
      });

      wrapper.vm.selectedItems = [
        { ...docWithImage, fullName: 'John Doe', profSeqNo: 1 },
        { ...docWithoutImage, fullName: 'John Doe', profSeqNo: 1 },
      ];
      wrapper.vm.openMergedDocuments();

      expect(shared.openMergedDocuments).toHaveBeenCalledWith(
        expect.arrayContaining([
          expect.objectContaining({
            documentType: DocumentRequestType.File,
          }),
        ])
      );
      expect(shared.openMergedDocuments).toHaveBeenCalledWith(
        expect.not.arrayContaining([
          expect.objectContaining({
            documentData: expect.objectContaining({
              documentDescription: 'Document 2',
            }),
          }),
        ])
      );
    });

    it('should include correct grouping keys and document name', () => {
      const doc = {
        issueDate: '2023-01-01',
        documentTypeDescription: 'Test Document',
        category: 'other',
        documentPageCount: 5,
        imageId: '123',
        partId: 'part1',
      };

      mockParticipantOne.document = [doc];
      wrapper = shallowMount(DocumentsView, {
        props: {
          participants: [mockParticipantOne],
        },
      });

      wrapper.vm.selectedItems = [
        { ...doc, fullNameLastFirst: 'John Doe', profSeqNo: 1 },
      ];
      wrapper.vm.openMergedDocuments();

      expect(shared.openMergedDocuments).toHaveBeenCalledWith(
        expect.arrayContaining([
          expect.objectContaining({
            groupKeyTwo: 'John Doe',
            documentName: 'Test Document',
          }),
        ])
      );
    });
  });
  describe('Category Display Title', () => {
    it('displays "All Documents" when no category is selected', () => {
      const sections = wrapper.findAll('v-card-text .text-h5');
      expect(sections[1].text()).toBe('All Documents (2)');
    });

    it('displays original category name when no special formatting applies', async () => {
      wrapper.vm.selectedCategory = 'bail';
      await nextTick();

      const sections = wrapper.findAll('v-card-text .text-h5');
      expect(sections[1].text()).toBe('bail (1)');
    });
  });

  describe('Unique Document Keys', () => {
    it('generates unique keys for documents with same category and issue date', async () => {
      const transcriptParticipant = {
        fullName: 'Thomas Magnum',
        profSeqNo: 113,
        lastNm: 'Magnum',
        givenNm: 'Thomas',
        partId: '156343.0999',
        document: [
          {
            partId: '156343.0999',
            category: 'Transcript',
            documentTypeDescription: 'Transcript - 1',
            hasFutureAppearance: false,
            docmClassification: 'Transcript',
            docmId: '147',
            issueDate: '2025-11-12',
            docmFormId: '',
            docmFormDsc: 'Transcript - 1',
            imageId: '147',
            documentPageCount: '1',
            transcriptOrderId: '465',
            transcriptDocumentId: '147',
            transcriptAppearanceId: '500575.0877',
          },
          {
            partId: '156343.0999',
            category: 'Transcript',
            documentTypeDescription: 'Transcript - 2',
            hasFutureAppearance: false,
            docmClassification: 'Transcript',
            docmId: '148',
            issueDate: '2025-11-12',
            docmFormId: '',
            docmFormDsc: 'Transcript - 2',
            imageId: '148',
            documentPageCount: '2',
            transcriptOrderId: '466',
            transcriptDocumentId: '148',
            transcriptAppearanceId: '500575.0877',
          },
          {
            partId: '156343.0999',
            category: 'Transcript',
            documentTypeDescription: 'Transcript - 1',
            hasFutureAppearance: false,
            docmClassification: 'Transcript',
            docmId: '145',
            issueDate: '2025-10-24',
            docmFormId: '',
            docmFormDsc: 'Transcript - 1',
            imageId: '145',
            documentPageCount: '1',
            transcriptOrderId: '463',
            transcriptDocumentId: '145',
            transcriptAppearanceId: '500547.0877',
          },
        ],
        keyDocuments: [],
      };

      const wrapper = shallowMount(DocumentsView, {
        props: {
          participants: [transcriptParticipant],
        },
      });

      const unfilteredDocuments = wrapper.vm.unfilteredDocuments;

      // Verify we have 3 documents
      expect(unfilteredDocuments).toHaveLength(3);

      // Extract all document IDs
      const documentIds = unfilteredDocuments.map((doc: any) => doc.id);

      // Verify all IDs are unique (no duplicates)
      const uniqueIds = new Set(documentIds);
      expect(uniqueIds.size).toBe(3);
      expect(documentIds).toHaveLength(3);

      // Verify the IDs contain the docmId to ensure uniqueness
      expect(documentIds[0]).toContain('147');
      expect(documentIds[1]).toContain('148');
      expect(documentIds[2]).toContain('145');

      // Filter to only show transcripts
      wrapper.vm.selectedCategory = 'Transcript';
      await nextTick();

      const filteredDocuments = wrapper.vm.documents;

      // Verify all 3 transcript documents are shown
      expect(filteredDocuments).toHaveLength(3);

      // Verify filtered documents also have unique IDs
      const filteredIds = filteredDocuments.map((doc: any) => doc.id);
      const uniqueFilteredIds = new Set(filteredIds);
      expect(uniqueFilteredIds.size).toBe(3);
    });

    it('generates unique keys using imageId when docmId is missing', async () => {
      const participantWithImageIds = {
        fullName: 'Test User',
        profSeqNo: 1,
        lastNm: 'User',
        givenNm: 'Test',
        partId: '123.0001',
        document: [
          {
            partId: '123.0001',
            category: 'Transcript',
            documentTypeDescription: 'Transcript - A',
            issueDate: '2025-11-12',
            imageId: 'IMG-001',
            documentPageCount: '1',
          },
          {
            partId: '123.0001',
            category: 'Transcript',
            documentTypeDescription: 'Transcript - B',
            issueDate: '2025-11-12',
            imageId: 'IMG-002',
            documentPageCount: '1',
          },
        ],
        keyDocuments: [],
      };

      const wrapper = shallowMount(DocumentsView, {
        props: {
          participants: [participantWithImageIds],
        },
      });

      const unfilteredDocuments = wrapper.vm.unfilteredDocuments;
      const documentIds = unfilteredDocuments.map((doc: any) => doc.id);

      // Verify unique keys using imageId
      expect(new Set(documentIds).size).toBe(2);
      expect(documentIds[0]).toContain('IMG-001');
      expect(documentIds[1]).toContain('IMG-002');
    });
  });
});
