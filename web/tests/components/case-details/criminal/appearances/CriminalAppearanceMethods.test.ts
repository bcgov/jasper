import { mount } from '@vue/test-utils'
import { describe, it, expect } from 'vitest'
import CriminalAppearanceMethods from 'CMP/case-details/criminal/appearances/CriminalAppearanceMethods.vue'

describe('CriminalAppearanceMethods.vue', () => {
  it('renders appearance methods when provided', () => {
    const appearanceMethods = [
      {
        roleTypeDsc: 'Defendant',
        appearanceMethodDesc: 'In Person'
      },
      {
        roleTypeDsc: 'Witness',
        appearanceMethodDesc: 'Video Conference'
      }
    ]
    const wrapper = mount(CriminalAppearanceMethods, {
      props: { appearanceMethods }
    })
    expect(wrapper.text()).toContain('Defendant appearing by In Person')
    expect(wrapper.text()).toContain('Witness appearing by Video Conference')
  })

    it.each([
    { method: undefined },
    { method: null },
    { method: [] },
  ])('renders default text when appearanceMethods is falsy', ({ method }) => {
    const wrapper = mount(CriminalAppearanceMethods, {
      props: { appearanceMethods: method }
    })
    expect(wrapper.text()).toContain('No appearance methods.')
  })
})