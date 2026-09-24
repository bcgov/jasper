import { CourtLocation } from '@/types/CourtLocation';
import { flushPromises, mount } from '@vue/test-utils';
import CourtLocationInfoDialog from 'CMP/courtlist/CourtLocationInfoDialog.vue';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';

describe('CourtLocationInfoDialog.vue', () => {
  const mockCourtLocationService = {
    getCourtLocationByCode: vi.fn(),
  };

  const mockLocation: CourtLocation = {
    id: '1',
    code: '4801',
    name: 'Ahousaht',
    jasperName: 'Ahousaht',
    path: '',
    address1: '271 Main Street',
    address2: '',
    city: 'Ahousaht',
    isStaffed: false,
    iarSchedule: 'Thursday at 9 am',
    fxdSchedule: 'Wednesdays at 9:30 am',
    jcmPhones: [{ phone: '250-720-2439', notes: 'JCM office' }],
    emails: [{ email: 'jcm@example.com', notes: 'Port Alberni JCM' }],
    adultProbationOffice: {
      name: 'Port Alberni Community Corrections',
      address1: '3019-4th Avenue',
      address2: 'Port Alberni, BC',
      staffingNotes: '',
      phones: [{ phone: '250-720-2444', notes: '' }],
      tollFreePhone: { phone: '1-888-770-4770', notes: '' },
    },
    youthProbationOffice: {
      name: 'Port Alberni Youth Probation',
      address1: '4088 8th Avenue',
      address2: 'Port Alberni, BC',
      city: '',
      contactName: '',
      staffingNotes: '',
      phone: { phone: '250-720-2650', notes: '' },
    },
  };

  const mountDialog = (props: Record<string, unknown> = {}) =>
    mount(CourtLocationInfoDialog, {
      props: {
        modelValue: false,
        agencyIdCode: '4801',
        locationUrl: 'https://example.com/location',
        ...props,
      },
      global: {
        provide: { courtLocationService: mockCourtLocationService },
      },
    });

  const openDialog = async (
    wrapper: ReturnType<typeof mountDialog>
  ): Promise<void> => {
    await wrapper.setProps({ modelValue: true } as never);
    await flushPromises();
    await nextTick();
  };

  beforeEach(() => {
    mockCourtLocationService.getCourtLocationByCode.mockReset();
  });

  it('shows an error when no location code is available', async () => {
    const wrapper = mountDialog({ agencyIdCode: undefined });
    await openDialog(wrapper);

    expect(
      mockCourtLocationService.getCourtLocationByCode
    ).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain('No location code available.');
  });

  it('loads and displays location details when opened', async () => {
    mockCourtLocationService.getCourtLocationByCode.mockResolvedValue(
      mockLocation
    );
    const wrapper = mountDialog();
    await openDialog(wrapper);

    expect(
      mockCourtLocationService.getCourtLocationByCode
    ).toHaveBeenCalledWith('4801');
    expect(wrapper.text()).toContain('Ahousaht');
    expect(wrapper.text()).toContain('271 Main Street');
    expect(wrapper.text()).toContain('Ahousaht, BC');
    expect(wrapper.text()).toContain('Judicial Case Manager');
    expect(wrapper.text()).toContain('JCM Schedule');
    expect(wrapper.text()).toContain('Thursday at 9 am');
    expect(wrapper.text()).toContain('Wednesdays at 9:30 am');
  });

  it('shows a fallback when no JCM schedule is available', async () => {
    mockCourtLocationService.getCourtLocationByCode.mockResolvedValue({
      ...mockLocation,
      iarSchedule: '',
      fxdSchedule: '',
    });
    const wrapper = mountDialog();
    await openDialog(wrapper);

    expect(wrapper.text()).toContain('JCM Schedule Not Available');
  });

  it('renders phone, email and location links', async () => {
    mockCourtLocationService.getCourtLocationByCode.mockResolvedValue(
      mockLocation
    );
    const wrapper = mountDialog();
    await openDialog(wrapper);

    const hrefs = wrapper.findAll('a').map((a) => a.attributes('href'));
    expect(hrefs).toContain('tel:250-720-2439');
    expect(hrefs).toContain('mailto:jcm@example.com');
    expect(hrefs).toContain('https://example.com/location');
  });

  it.each([
    [true, 'Staffed'],
    [false, 'Unstaffed'],
  ])(
    'shows the correct staffing chip when isStaffed is %s',
    async (isStaffed, label) => {
      mockCourtLocationService.getCourtLocationByCode.mockResolvedValue({
        ...mockLocation,
        isStaffed,
      });
      const wrapper = mountDialog();
      await openDialog(wrapper);

      expect(wrapper.text()).toContain(label);
    }
  );

  it('renders the nearest probation offices', async () => {
    mockCourtLocationService.getCourtLocationByCode.mockResolvedValue(
      mockLocation
    );
    const wrapper = mountDialog();
    await openDialog(wrapper);

    expect(wrapper.text()).toContain('Nearest probation offices');
    expect(wrapper.text()).toContain('Port Alberni Community Corrections');
    expect(wrapper.text()).toContain('Toll Free');

    const hrefs = wrapper.findAll('a').map((a) => a.attributes('href'));
    expect(hrefs).toContain('tel:250-720-2444');
    expect(hrefs).toContain('tel:1-888-770-4770');
    expect(hrefs).toContain('tel:250-720-2650');
    expect(wrapper.text()).toContain('Port Alberni Youth Probation');
  });

  it('shows a fallback when no probation office information is available', async () => {
    mockCourtLocationService.getCourtLocationByCode.mockResolvedValue({
      ...mockLocation,
      adultProbationOffice: undefined,
      youthProbationOffice: undefined,
    });
    const wrapper = mountDialog();
    await openDialog(wrapper);

    expect(wrapper.text()).toContain(
      'No probation office information for this location'
    );
  });

  it('shows a fallback message when no details are returned', async () => {
    mockCourtLocationService.getCourtLocationByCode.mockResolvedValue(null);
    const wrapper = mountDialog();
    await openDialog(wrapper);

    expect(wrapper.text()).toContain('No details available for this location.');
  });

  it('shows an error message when the service call fails', async () => {
    mockCourtLocationService.getCourtLocationByCode.mockRejectedValue(
      new Error('boom')
    );
    const wrapper = mountDialog();
    await openDialog(wrapper);

    expect(wrapper.text()).toContain('Unable to load location details.');
  });

  it('closes the dialog when the close button is clicked', async () => {
    mockCourtLocationService.getCourtLocationByCode.mockResolvedValue(
      mockLocation
    );
    const wrapper = mountDialog();
    await openDialog(wrapper);

    await wrapper.find('[aria-label="Close dialog"]').trigger('click');

    expect(wrapper.emitted('update:modelValue')?.at(-1)).toEqual([false]);
  });
});
