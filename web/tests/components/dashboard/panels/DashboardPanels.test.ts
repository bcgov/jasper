
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import DashboardPanels from 'CMP/dashboard/panels/DashboardPanels.vue'

const mockData = [
  { id: 1, name: 'Judgement 1' },
  { id: 2, name: 'Judgement 2' }
]

const createReservedJudgementService = () => ({
  get: vi.fn().mockResolvedValue(mockData)
})

describe('DashboardPanels.vue', () => {
  let reservedJudgementService: any

  beforeEach(() => {
    reservedJudgementService = createReservedJudgementService()
  })

  function mountComponent() {
    return mount(DashboardPanels, {
      global: {
        provide: {
          reservedJudgementService
        }
      }
    })
  }

  it('renders panel title and count', async () => {
    const wrapper = mountComponent()
    expect(wrapper.text()).toContain('Reserved judgments & decisions')
    expect(wrapper.text()).not.toContain('(2)')
  })

  it('loads judgements when panel is expanded', async () => {
    const wrapper = mountComponent()
    await wrapper.vm.$nextTick()
    wrapper.vm.expanded = 'reserved-judgement'
    await flushPromises()
    expect(reservedJudgementService.get).toHaveBeenCalled()
    expect(wrapper.text()).toContain('2')
    expect(wrapper.text()).toContain('(2)')
  })

  it('clears judgements when panel is collapsed', async () => {
    const wrapper = mountComponent()
    wrapper.vm.expanded = 'reserved-judgement'
    await flushPromises()
    expect(wrapper.text()).toContain('2')
    wrapper.vm.expanded = ''
    await flushPromises()
    expect(wrapper.findComponent({ name: 'ReservedJudgementTable' }).props('data')).toEqual([])
  })

  it('shows skeleton loader while loading', async () => {
    reservedJudgementService.get = vi.fn(() => new Promise(() => {}))
    const wrapper = mountComponent();
    wrapper.vm.expanded = 'reserved-judgement';
    await wrapper.vm.$nextTick()
    expect(wrapper.find('v-skeleton-loader').exists()).toBe(true)
  })

  it('logs error if service fails', async () => {
    const errorSpy = vi.spyOn(console, 'error').mockImplementation(() => {})
    reservedJudgementService.get = vi.fn().mockRejectedValue(new Error('fail'))
    const wrapper = mountComponent()
    wrapper.vm.expanded = 'reserved-judgement'
    await flushPromises()
    expect(errorSpy).toHaveBeenCalledWith(
      'Failed to load reserved judgements:',
      expect.any(Error)
    )
    errorSpy.mockRestore()
  })
})