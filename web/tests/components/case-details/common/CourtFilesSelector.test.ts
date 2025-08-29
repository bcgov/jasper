import CourtFilesSelector from '@/components/case-details/common/CourtFilesSelector.vue';
import { CourtClassEnum } from '@/types/common';
import { getEnumName } from '@/utils/utils';
import { mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';
import { ref } from 'vue';

describe('CourtFilesSelector.vue', async () => {
  const files = ref([
    { key: 'file1', value: 'File 1' },
    { key: 'file2', value: 'File 2' },
  ]);
  const fileNumber = 'file1';

  it('renders the correct number of tabs', () => {
    const wrapper = mount(CourtFilesSelector, {
      props: { files: files.value, fileNumber },
    });
    const tabs = wrapper.findAll('v-tab');
    expect(tabs.length).toBe(files.value.length);
  });

  it('renders the correct tab labels', () => {
    const wrapper = mount(CourtFilesSelector, {
      props: { files: files.value, fileNumber },
    });
    files.value.forEach((file, index) => {
      expect(wrapper.findAll('v-tab')[index].text()).toBe(file.value);
    });
  });

  it('sets the correct active tab on click', async () => {
    const wrapper = mount(CourtFilesSelector, {
      props: { files: files.value, fileNumber },
    });
    const tab = wrapper.findAll('v-tab')[1];
    await tab.trigger('click');
    expect(wrapper.vm.fileNumber).toBe(files.value[1].key);
  });

  it.each([
    {
      courtClass: getEnumName(CourtClassEnum, CourtClassEnum.A),
      expectedStyle: 'criminal',
    },
    {
      courtClass: getEnumName(CourtClassEnum, CourtClassEnum.Y),
      expectedStyle: 'criminal',
    },
    {
      courtClass: getEnumName(CourtClassEnum, CourtClassEnum.T),
      expectedStyle: 'criminal',
    },
    {
      courtClass: getEnumName(CourtClassEnum, CourtClassEnum.C),
      expectedStyle: 'small-claims',
    },
    {
      courtClass: getEnumName(CourtClassEnum, CourtClassEnum.L),
      expectedStyle: 'small-claims',
    },
    {
      courtClass: getEnumName(CourtClassEnum, CourtClassEnum.M),
      expectedStyle: 'small-claims',
    },
    {
      courtClass: getEnumName(CourtClassEnum, CourtClassEnum.F),
      expectedStyle: 'family',
    },
  ])(
    'sets the correct court class style based on courtClassCd',
    ({ courtClass, expectedStyle }) => {
      const wrapper = mount(CourtFilesSelector, {
        props: {
          files: files.value,
          fileNumber,
          courtClass,
        },
      });
      const card = wrapper.find('v-card');
      expect(card.classes()).toContain(expectedStyle);
    }
  );

  it('defaults to unknown when court class is under not "criminal", "small-claim" or "family', () => {
    const wrapper = mount(CourtFilesSelector, {
      props: {
        files: files.value,
        fileNumber,
        courtClass: '',
      },
    });
    const card = wrapper.find('v-card');
    expect(card.classes()).toContain('unknown');
  });
});
