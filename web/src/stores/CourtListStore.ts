import { courtListInformationInfoType } from '@/types/courtlist';
import { defineStore } from 'pinia';

export const useCourtListStore = defineStore('CourtListStore', {
  state: () => ({
    courtListInformation: {} as courtListInformationInfoType,
  }),

  actions: {
    setCourtList(courtListInformation): void {
      this.courtListInformation = courtListInformation;
    },
    updateCourtList(newCourtListInformation): void {
      this.setCourtList(newCourtListInformation);
    },
  },
});
