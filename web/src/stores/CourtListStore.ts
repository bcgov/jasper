import { defineStore } from 'pinia';

export const useCourtListStore = defineStore('CourtListStore', {
  state: () => ({
    courtListInformation: {},
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
