import {
  criminalAppearanceInfoType,
  criminalFileInformationType,
  participantSentencesDetailsInfoType,
} from '@/types/criminal';
import { defineStore } from 'pinia';

export const useCriminalFileStore = defineStore('CriminalFileStore', {
  state: () => ({
    criminalFileInformation: {} as criminalFileInformationType,
    criminalFileInfoLoaded: false,
    activeCriminalParticipantIndex: 0,

    criminalParticipantSentenceInformation:
      {} as participantSentencesDetailsInfoType,

    showSections: {
      'Case Details': true,
      'Future Appearances': false,
      'Past Appearances': false,
      Witnesses: false,
      Documents: false,
      'Sentence/Order Details': false,
    },
    criminalAppearanceInfo: {} as criminalAppearanceInfoType,
  }),
  actions: {
    setCriminalFile(criminalFileInformation): void {
      this.criminalFileInformation = criminalFileInformation;
    },
    updateCriminalFile(newCriminalFileInformation): void {
      this.setCriminalFile(newCriminalFileInformation);
    },
    setCriminalParticipantSentenceInformation(
      criminalParticipantSentenceInformation
    ): void {
      this.criminalParticipantSentenceInformation =
        criminalParticipantSentenceInformation;
    },
    updateCriminalParticipantSentenceInformation(
      newCriminalParticipantSentenceInformation
    ): void {
      this.setCriminalParticipantSentenceInformation(
        newCriminalParticipantSentenceInformation
      );
    },
    setCriminalFileInfoLoaded(criminalFileInfoLoaded): void {
      this.criminalFileInfoLoaded = criminalFileInfoLoaded;
    },
    updateCriminalFileInfoLoaded(newCriminalFileInfoLoaded): void {
      this.setCriminalFileInfoLoaded(newCriminalFileInfoLoaded);
    },
    setActiveCriminalParticipantIndex(activeCriminalParticipantIndex): void {
      this.activeCriminalParticipantIndex = activeCriminalParticipantIndex;
    },
    updateActiveCriminalParticipantIndex(
      newActiveCriminalParticipantIndex
    ): void {
      this.setActiveCriminalParticipantIndex(newActiveCriminalParticipantIndex);
    },
    setShowSections(showSections): void {
      this.showSections = showSections;
    },
    updateShowSections(newShowSections): void {
      this.setShowSections(newShowSections);
    },
    setCriminalAppearanceInfo(criminalAppearanceInfo): void {
      this.criminalAppearanceInfo = criminalAppearanceInfo;
    },
    updateCriminalAppearanceInfo(newCriminalAppearanceInfo): void {
      this.setCriminalAppearanceInfo(newCriminalAppearanceInfo);
    },
  },
});
