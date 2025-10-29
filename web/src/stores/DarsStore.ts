import type { LocationInfo } from '@/types/courtlist';
import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useDarsStore = defineStore('dars', () => {
  const searchDate = ref<Date | null>(null);
  const searchLocation = ref<LocationInfo | null>(null);
  const searchRoom = ref<string>('');

  const setSearchCriteria = (
    date: Date | null,
    location: LocationInfo | null,
    room: string
  ) => {
    searchDate.value = date;
    searchLocation.value = location;
    searchRoom.value = room;
  };

  const resetSearchCriteria = () => {
    searchDate.value = null;
    searchLocation.value = null;
    searchRoom.value = '';
  };

  return {
    searchDate,
    searchLocation,
    searchRoom,
    setSearchCriteria,
    resetSearchCriteria,
  };
});
