import { civilFileInformationType } from '@/types/civil';
import { defineStore } from 'pinia';

export const useCivilFileStore = defineStore('CivilFileStore', {
  state: () => ({
    civilFileInformation: {} as civilFileInformationType,
    civilFileInfoLoaded: false,
    hasNonParty: false,
    showSections: {
      'Case Details': true,
      'Future Appearances': false,
      'Past Appearances': false,
      'All Documents': false,
      Documents: false,
      'Provided Documents': false,
    },
    civilAppearanceInfo: {
      supplementalEquipmentTxt: '',
      securityRestrictionTxt: '',
      outOfTownJudgeTxt: '',
      fileNo: '',
      appearanceId: '',
      date: '',
    },
  }),
  actions: {
    setCivilFile(civilFileInformation): void {
      this.civilFileInformation = civilFileInformation;
    },
    updateCivilFile(newCivilFileInformation): void {
      this.setCivilFile(newCivilFileInformation);
    },
    setCivilFileInfoLoaded(civilFileInfoLoaded): void {
      this.civilFileInfoLoaded = civilFileInfoLoaded;
    },
    updateCivilFileInfoLoaded(newCivilFileInfoLoaded): void {
      this.setCivilFileInfoLoaded(newCivilFileInfoLoaded);
    },
    setHasNonParty(hasNonParty: boolean): void {
      this.hasNonParty = hasNonParty;
    },
    updateHasNonParty(newHasNonParty: boolean): void {
      this.setHasNonParty(newHasNonParty);
    },
    setShowSections(showSections): void {
      this.showSections = showSections;
    },
    updateShowSections(newShowSections): void {
      this.setShowSections(newShowSections);
    },
    setCivilAppearanceInfo(civilAppearanceInfo): void {
      this.civilAppearanceInfo = civilAppearanceInfo;
    },
    updateCivilAppearanceInfo(newCivilAppearanceInfo): void {
      this.setCivilAppearanceInfo(newCivilAppearanceInfo);
    },
  },
});
