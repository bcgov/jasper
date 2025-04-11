import CourtFilesSelector from '@/components/case-details/CourtFilesSelector.vue';
import { CourtClassEnum } from '@/types/courtFileSearch';
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

  it('renders the "View all documents" button', () => {
    const wrapper = mount(CourtFilesSelector, {
      props: { files: files.value, fileNumber },
    });
    const button = wrapper.find('v-btn');
    expect(button.text()).toBe('View all documents');
  });

  it('sets correct class name when court class division is under "criminal"', () => {
    const wrapper = mount(CourtFilesSelector, {
      props: {
        files: files.value,
        fileNumber,
        courtClass: CourtClassEnum[CourtClassEnum.A],
      },
    });
    const card = wrapper.find('v-card');
    expect(card.classes()).toContain('criminal');
  });

  it('sets correct class name when court class division is under "small-claim"', () => {
    const wrapper = mount(CourtFilesSelector, {
      props: {
        files: files.value,
        fileNumber,
        courtClass: CourtClassEnum[CourtClassEnum.C],
      },
    });
    const card = wrapper.find('v-card');
    expect(card.classes()).toContain('small-claims');
  });

  it('sets correct class name when court class is under "family"', () => {
    const wrapper = mount(CourtFilesSelector, {
      props: {
        files: files.value,
        fileNumber,
        courtClass: CourtClassEnum[CourtClassEnum.F],
      },
    });
    const card = wrapper.find('v-card');
    expect(card.classes()).toContain('family');
  });

  it('defaults the correct class name when court class is under not "criminal", "small-claim" or "family', () => {
    const wrapper = mount(CourtFilesSelector, {
      props: {
        files: files.value,
        fileNumber,
        courtClass: CourtClassEnum[CourtClassEnum.M],
      },
    });
    const card = wrapper.find('v-card');
    expect(card.classes()).toContain('criminal');
  });
});
