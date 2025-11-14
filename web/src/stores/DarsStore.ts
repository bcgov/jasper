import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useDarsStore = defineStore('dars', () => {
  const searchDate = ref<Date | null>(null);
  const searchRoom = ref<string>('');
  const isModalVisible = ref<boolean>(false);
  const searchLocationId = ref<string | null>(null);

  const setSearchCriteria = (
    date: Date | null,
    locationId: string | null,
    room: string
  ) => {
    searchDate.value = date;
    searchLocationId.value = locationId;
    searchRoom.value = room;
  };

  const resetSearchCriteria = () => {
    searchDate.value = null;
    searchLocationId.value = null;
    searchRoom.value = '';
  };

  const openModal = (
    date?: Date | null,
    locationId?: string | null,
    room?: string
  ) => {
    // If prefill data is provided, set it
    if (date !== undefined) {
      searchDate.value = date;
    }
    if (locationId !== undefined) {
      searchLocationId.value = locationId;
    }
    if (room !== undefined) {
      searchRoom.value = room || '';
    }

    isModalVisible.value = true;
  };

  const closeModal = () => {
    isModalVisible.value = false;
  };

  return {
    searchDate,
    searchRoom,
    searchLocationId,
    isModalVisible,
    setSearchCriteria,
    resetSearchCriteria,
    openModal,
    closeModal,
  };
});
