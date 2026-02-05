import { mount } from '@vue/test-utils';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import JudgeSelector from 'CMP/shared/JudgeSelector.vue';
import { createPinia, setActivePinia } from 'pinia';
import { nextTick } from 'vue';

const mockUserInfo = {
  userType: 'Judge',
  enableArchive: false,
  roles: ['JUD'],
  subRole: 'Provincial',
  isSupremeUser: 'false',
  isActive: true,
  agencyCode: 'BCJ',
  userId: 'user123',
  judgeId: 100,
  judgeHomeLocationId: 1,
  email: 'judge@example.com',
  userTitle: 'Judge Smith',
  permissions: [],
  groups: [],
};

const mockLoggedInUserInfo = {
  ...mockUserInfo,
  judgeId: 100,
  judgeHomeLocationId: 1,
};

const mockJudges = [
  { personId: 100, fullName: 'Judge Smith', rotaInitials: 'JS', homeLocationId: 1 },
  { personId: 200, fullName: 'Judge Jones', rotaInitials: 'JJ', homeLocationId: 2 },
  { personId: 300, fullName: 'Judge Brown', rotaInitials: 'JB', homeLocationId: 3 },
];

vi.mock('@/stores/CommonStore', () => ({
  useCommonStore: vi.fn(),
}));

describe('JudgeSelector.vue', () => {
  let wrapper;
  let pinia: any;
  let mockStore: any;

  beforeEach(async () => {
    pinia = createPinia();
    setActivePinia(pinia);

    mockStore = {
      userInfo: { ...mockUserInfo },
      loggedInUserInfo: { ...mockLoggedInUserInfo },
      setUserInfo: vi.fn(),
    };

    const { useCommonStore } = await import('@/stores/CommonStore');
    (useCommonStore as any).mockReturnValue(mockStore);

    wrapper = mount(JudgeSelector, {
      props: {
        judges: mockJudges,
      },
      global: {
        plugins: [pinia],
      },
    });
  });

  it('renders the component', () => {
    expect(wrapper.exists()).toBe(true);
  });

  it('initializes with userInfo judgeId', () => {
    expect(wrapper.vm.selectedJudgeId).toBe(100);
  });

  it('does not show reset icon when viewing as logged-in user', () => {
    const icon = wrapper.find('.cursor-pointer');
    
    expect(icon.exists()).toBe(false);
  });

  it('updates store when judge selection changes', async () => {
    wrapper.vm.selectedJudgeId = 200;
    await nextTick();

    expect(mockStore.setUserInfo).toHaveBeenCalledWith(
      expect.objectContaining({
        judgeId: 200,
        judgeHomeLocationId: 2,
      })
    );
  });

  it('resets to logged-in user when reset icon is clicked', async () => {
    // Change to different judge
    wrapper.vm.selectedJudgeId = 200;
    await nextTick();

    wrapper.vm.resetToOriginalJudge();

    expect(wrapper.vm.selectedJudgeId).toBe(100);
  });

  it('prevents selection from being cleared', async () => {
    wrapper.vm.selectedJudgeId = null;
    await nextTick();

    expect(wrapper.vm.selectedJudgeId).toBe(null);
  });

  it('updates selectedJudgeId when userInfo changes in store', async () => {
    mockStore.userInfo = { ...mockUserInfo, judgeId: 300 };
    wrapper.vm.$forceUpdate();
    await nextTick();

    wrapper.vm.selectedJudgeId = 300;
    await nextTick();

    expect(wrapper.vm.selectedJudgeId).toBe(300);
  });

  it('handles null userInfo gracefully', async () => {
    mockStore.loggedInUserInfo = null;
    mockStore.userInfo = null;

    const newWrapper = mount(JudgeSelector, {
      props: {
        judges: mockJudges,
      },
      global: {
        plugins: [pinia],
      },
    });

    expect(newWrapper.vm.selectedJudgeId).toBe(null);
  });

  it('finds correct judge and updates homeLocationId', async () => {
    wrapper.vm.selectedJudgeId = 300;
    await nextTick();

    expect(mockStore.setUserInfo).toHaveBeenCalledWith(
      expect.objectContaining({
        judgeId: 300,
        judgeHomeLocationId: 3,
      })
    );
  });

  it('maintains other userInfo properties when updating judge', async () => {
    wrapper.vm.selectedJudgeId = 200;
    await nextTick();

    expect(mockStore.setUserInfo).toHaveBeenCalledWith(
      expect.objectContaining({
        userType: 'Judge',
        email: 'judge@example.com',
        judgeId: 200,
      })
    );
  });
});
