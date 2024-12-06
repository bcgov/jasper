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

    <b-card
      bg-variant="white"
      v-if="isDataReady"
      no-body
      class="mx-3 mb-5"
      style="overflow: auto"
    >
      <b-table
        :items="SortedPastAppearances"
        :fields="fields"
        :sort-by.sync="sortBy"
        :sort-desc.sync="sortDesc"
        :no-sort-reset="true"
        sort-icon-left
        borderless
        @sort-changed="sortChanged"
        small
        responsive="sm"
      >
        <template
          v-for="(field, index) in fields"
          v-slot:[`head(${field.key})`]="data"
        >
          <b v-bind:key="index" :class="field.headerStyle"> {{ data.label }}</b>
        </template>

        <template v-slot:cell()="data">
          <b-badge :style="data.field.cellStyle" variant="white">
            {{ data.value }}
          </b-badge>
        </template>

        <template v-slot:cell(date)="data">
          <span :class="data.field.cellClass" style="display: inline-flex">
            <b-button
              :style="data.field.cellStyle"
              size="sm"
              @click="
                OpenDetails(data);
                data.toggleDetails();
              "
              variant="outline-primary border-white text-info"
              class="mr-2 mt-1"
            >
              <b-icon-caret-right-fill
                v-if="!data.item['_showDetails']"
              ></b-icon-caret-right-fill>
              <b-icon-caret-down-fill
                v-if="data.item['_showDetails']"
              ></b-icon-caret-down-fill>
              {{ data.item.formattedDate }}
            </b-button>
          </span>
        </template>
        <template v-slot:row-details>
          <b-card>
            <criminal-appearance-details />
          </b-card>
        </template>

        <template v-slot:cell(reason)="data">
          <b-badge
            variant="secondary"
            v-b-tooltip.hover.right
            :title="data.item.reasonDescription"
            :style="data.field.cellStyle"
          >
            {{ data.value }}
          </b-badge>
        </template>

        <template v-slot:cell(presider)="data">
          <b-badge
            variant="secondary"
            v-if="data.value"
            v-b-tooltip.hover.left
            :title="data.item.judgeFullName"
            :style="data.field.cellStyle"
          >
            {{ data.value }}
          </b-badge>
        </template>

        <template v-slot:cell(accused)="data">
          <b-badge variant="white" :style="data.field.cellStyle" class="mt-2">
            {{ data.value }}
          </b-badge>
        </template>

        <template v-slot:cell(status)="data">
          <b :class="data.item.statusStyle" :style="data.field.cellStyle">
            {{ data.value }}
          </b>
        </template>
      </b-table>
    </b-card>
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

  enum appearanceStatus {
    UNCF = 'Unconfirmed',
    CNCL = 'Canceled',
    SCHD = 'Scheduled',
  }

  export default defineComponent({
    components: {
      CriminalAppearanceDetails,
    },
    setup() {
      const commonStore = useCommonStore();
      const criminalFileStore = useCriminalFileStore();

      const pastAppearancesList = ref<criminalAppearancesListType[]>([]);
      let pastAppearancesJson: criminalApprDetailType[] = [];

      const sortBy = 'dateref';
      const sortDesc = ref(true);
      const isMounted = ref(false);
      const isDataReady = ref(false);

      const commonStyles = {
        tdClass: 'border-top',
        headerStyle: 'text',
        cellStyle: 'font-weight: normal; font-size: 16px; padding-top:12px',
      };

      const createField = (key, label, sortable, additionalStyles = {}) => ({
        key,
        label,
        sortable,
        ...commonStyles,
        ...additionalStyles,
      });

      const fields = [
        createField('date', 'Date', true, {
          headerStyle: 'text-primary',
          cellStyle: 'transform: translate(0,-7px); font-size:16px',
          cellClass: 'text-info mt-2 d-inline-flex',
        }),
        createField('reason', 'Reason', true, {
          headerStyle: 'text-primary',
          cellStyle: 'margin-top: 10px; font-size: 14px;',
        }),
        createField('time', 'Time', false),
        createField('duration', 'Duration', false),
        createField('location', 'Location', true, {
          headerStyle: 'text-primary',
        }),
        createField('room', 'Room', false),
        createField('presider', 'Presider', true, {
          headerStyle: 'text-primary',
          cellStyle: 'margin-top: 10px; font-size: 14px;',
        }),
        createField('accused', 'Accused', true, {
          headerStyle: 'text-primary',
          cellStyle: 'font-size: 16px;',
        }),
        createField('status', 'Status', true, {
          headerStyle: 'text-primary',
          cellStyle: 'font-weight: normal; font-size: 16px; width:110px',
        }),
      ];

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
        for (const appIndex in pastAppearancesJson) {
          const appInfo = {} as criminalAppearancesListType;
          const jApp = pastAppearancesJson[appIndex];

          appInfo.index = appIndex;
          appInfo.date = jApp.appearanceDt.split(' ')[0];
          if (new Date(appInfo.date) >= currentDate) continue;
          appInfo.formattedDate = beautifyDate(appInfo.date);
          appInfo.time = getTime(jApp.appearanceTm.split(' ')[1].substr(0, 5));
          appInfo.reason = jApp.appearanceReasonCd;
          appInfo.reasonDescription = jApp.appearanceReasonDsc
            ? jApp.appearanceReasonDsc
            : '';

          appInfo.duration = getDuration(
            jApp.estimatedTimeHour,
            jApp.estimatedTimeMin
          );
          appInfo.location = jApp.courtLocation ? jApp.courtLocation : '';
          appInfo.room = jApp.courtRoomCd;

          appInfo.firstName = jApp.givenNm ? jApp.givenNm : '';
          appInfo.lastName = jApp.lastNm ? jApp.lastNm : jApp.orgNm;
          appInfo.accused = getNameOfParticipant(
            appInfo.lastName,
            appInfo.firstName
          );
          appInfo.status = jApp.appearanceStatusCd
            ? appearanceStatus[jApp.appearanceStatusCd]
            : '';
          appInfo.statusStyle = getStatusStyle(appInfo.status);
          appInfo.presider = jApp.judgeInitials ? jApp.judgeInitials : '';
          appInfo.judgeFullName = jApp.judgeInitials ? jApp.judgeFullNm : '';

          appInfo.appearanceId = jApp.appearanceId;
          appInfo.partId = jApp.partId;
          appInfo.supplementalEquipment = jApp.supplementalEquipmentTxt;
          appInfo.securityRestriction = jApp.securityRestrictionTxt;
          appInfo.outOfTownJudge = jApp.outOfTownJudgeTxt;
          appInfo.profSeqNo = jApp.profSeqNo;

          pastAppearancesList.value.push(appInfo);
        }
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

      const OpenDetails = (data) => {
        if (!data.detailsShowing) {
          criminalFileStore.criminalAppearanceInfo.fileNo =
            criminalFileStore.criminalFileInformation.fileNumber;
          criminalFileStore.criminalAppearanceInfo.courtLevel =
            criminalFileStore.criminalFileInformation.courtLevel;
          criminalFileStore.criminalAppearanceInfo.courtClass =
            criminalFileStore.criminalFileInformation.courtClass;
          criminalFileStore.criminalAppearanceInfo.date =
            data.item.formattedDate;
          criminalFileStore.criminalAppearanceInfo.appearanceId =
            data.item.appearanceId;
          criminalFileStore.criminalAppearanceInfo.partId = data.item.partId;
          criminalFileStore.criminalAppearanceInfo.supplementalEquipmentTxt =
            data.item.supplementalEquipment;
          criminalFileStore.criminalAppearanceInfo.securityRestrictionTxt =
            data.item.securityRestriction;
          criminalFileStore.criminalAppearanceInfo.outOfTownJudgeTxt =
            data.item.outOfTownJudge;
          criminalFileStore.criminalAppearanceInfo.profSeqNo =
            data.item.profSeqNo;
          criminalFileStore.updateCriminalAppearanceInfo(
            criminalFileStore.criminalAppearanceInfo
          );
        }
      };

      const sortChanged = () => {
        SortedPastAppearances.value.forEach((item) => {
          item['_showDetails'] = false;
        });
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
        fields,
        sortBy,
        sortDesc,
        SortedPastAppearances,
        OpenDetails,
        sortChanged,
      };
    },
  });
</script>

<style scoped>
  .card {
    border: white;
  }
</style>
