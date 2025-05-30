import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import AppearanceMethods from 'CMP/case-details/civil/appearances/AppearanceMethods.vue';

describe('AppearanceMethods.vue', () => {
  it('renders correctly when appearanceMethod has data', () => {
    const appearanceMethod = [
      { roleTypeDesc: 'Plaintiff', appearanceMethodDesc: 'In Person' },
      { roleTypeDesc: 'Defendant', appearanceMethodDesc: 'Video Conference' },
    ];

    const wrapper = mount(AppearanceMethods, {
      props: { appearanceMethod },
    });

    expect(wrapper.html()).toContain('Plaintiff appearing by In Person');
    expect(wrapper.html()).toContain('Defendant appearing by Video Conference');
  });

  it('does not render anything when appearanceMethod is empty', () => {
    const appearanceMethod: any[] = [];

    const wrapper = mount(AppearanceMethods, {
      props: { appearanceMethod },
    });

    expect(wrapper.findAll('span')).is.empty;
  });

  it('renders the correct number of appearance methods', () => {
    const appearanceMethod = [
      { roleTypeDesc: 'Plaintiff', appearanceMethodDesc: 'In Person' },
      { roleTypeDesc: 'Defendant', appearanceMethodDesc: 'Video Conference' },
      { roleTypeDesc: 'Witness', appearanceMethodDesc: 'Phone' },
    ];

    const wrapper = mount(AppearanceMethods, {
      props: { appearanceMethod },
    });

    const renderedMethods = wrapper.findAll('div > span');
    expect(renderedMethods.length).toBe(appearanceMethod.length);
  });
});