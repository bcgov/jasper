<template>
  <b-card bg-variant="white" no-body>
    <div>
      <h3
        class="mx-4 font-weight-normal"
        v-if="!showSections['Past Appearances']"
      >
        Last Three Past Appearances
      </h3>
      <hr class="mx-3 bg-light" style="height: 5px" />
    </div>

    <b-card v-if="!isDataReady && isMounted" no-body>
      <span class="text-muted ml-4 mb-5"> No past appearances. </span>
    </b-card>

    <b-card bg-variant="light" v-if="!isMounted && !isDataReady">
      <b-overlay :show="true">
        <b-card style="min-height: 100px" />
        <template v-slot:overlay>
          <div>
            <loading-spinner />
            <p id="loading-label">Loading ...</p>
          </div>
        </template>
      </b-overlay>
    </b-card>

    <criminal-past-appearances-table
      v-if="isDataReady"
      :SortedPastAppearances="SortedPastAppearances"
    />
  </b-card>
</template>

<script lang="ts">
  import CriminalAppearanceDetails from '@/components/criminal/CriminalAppearanceDetails.vue';
  import { beautifyDate } from '@/filters';
  import { useCommonStore, useCriminalFileStore } from '@/stores';
  import { criminalAppearancesListType } from '@/types/criminal';
  import { criminalApprDetailType } from '@/types/criminal/jsonTypes';
  import * as _ from 'underscore';
  import { computed, defineComponent, onMounted, ref } from 'vue';
  import CriminalPastAppearancesTable from './CriminalPastAppearancesTable.vue';

  enum appearanceStatus {
    UNCF = 'Unconfirmed',
    CNCL = 'Canceled',
    SCHD = 'Scheduled',
  }

  export default defineComponent({
    components: {
      CriminalAppearanceDetails,
      CriminalPastAppearancesTable,
    },
    setup() {
      const commonStore = useCommonStore();
      const criminalFileStore = useCriminalFileStore();

      const pastAppearancesList = ref<criminalAppearancesListType[]>([]);
      let pastAppearancesJson: criminalApprDetailType[] = [];

      const isMounted = ref(false);
      const isDataReady = ref(false);

      onMounted(() => {
        getPastAppearances();
      });

      const getPastAppearances = () => {
        const data = criminalFileStore.criminalFileInformation.detailsData;
        pastAppearancesJson = [...data.appearances.apprDetail];
        ExtractPastAppearancesInfo();
        if (pastAppearancesList.value.length) {
          isDataReady.value = true;
        }
        isMounted.value = true;
      };

      const ExtractPastAppearancesInfo = () => {
        const currentDate = new Date();

        pastAppearancesJson.forEach(
          (jApp: criminalApprDetailType, index: number) => {
            const appearanceDate = jApp.appearanceDt.split(' ')[0];
            if (new Date(appearanceDate) >= currentDate) return;

            const appInfo: criminalAppearancesListType = {
              index: index.toString(),
              date: appearanceDate,
              formattedDate: beautifyDate(appearanceDate),
              time: getTime(jApp.appearanceTm.split(' ')[1].substring(0, 5)),
              reason: jApp.appearanceReasonCd,
              reasonDescription: jApp.appearanceReasonDsc || '',
              duration: getDuration(
                jApp.estimatedTimeHour,
                jApp.estimatedTimeMin
              ),
              location: jApp.courtLocation || '',
              room: jApp.courtRoomCd,
              firstName: jApp.givenNm || '',
              lastName: jApp.lastNm || jApp.orgNm,
              accused: getNameOfParticipant(
                jApp.lastNm || jApp.orgNm,
                jApp.givenNm || ''
              ),
              status: jApp.appearanceStatusCd
                ? appearanceStatus[jApp.appearanceStatusCd]
                : '',
              statusStyle: getStatusStyle(
                jApp.appearanceStatusCd
                  ? appearanceStatus[jApp.appearanceStatusCd]
                  : ''
              ),
              presider: jApp.judgeInitials || '',
              judgeFullName: jApp.judgeInitials ? jApp.judgeFullNm : '',
              appearanceId: jApp.appearanceId,
              partId: jApp.partId,
              supplementalEquipment: jApp.supplementalEquipmentTxt,
              securityRestriction: jApp.securityRestrictionTxt,
              outOfTownJudge: jApp.outOfTownJudgeTxt,
              profSeqNo: jApp.profSeqNo,
            };

            pastAppearancesList.value.push(appInfo);
          }
        );
      };

      const getStatusStyle = (status) => {
        commonStore.updateStatusStyle(status);
        return commonStore.statusStyle;
      };

      const getNameOfParticipant = (lastName, givenName) => {
        commonStore.updateDisplayName({
          lastName: lastName,
          givenName: givenName,
        });
        return commonStore.displayName;
      };

      const getTime = (time) => {
        commonStore.updateTime(time);
        return commonStore.time;
      };

      const getDuration = (hr, min) => {
        commonStore.updateDuration({ hr: hr, min: min });
        return commonStore.duration;
      };

      const SortedPastAppearances = computed(
        (): criminalAppearancesListType[] => {
          if (criminalFileStore.showSections['Past Appearances']) {
            return pastAppearancesList.value;
          } else {
            return _.sortBy(pastAppearancesList.value, 'date')
              .reverse()
              .slice(0, 3);
          }
        }
      );

      return {
        showSections: criminalFileStore.showSections,
        isDataReady,
        isMounted,
        SortedPastAppearances,
      };
    },
  });
</script>

<style scoped>
  .card {
    border: white;
  }
</style>
