<template>
  <b-card bg-variant="white" no-body>
    <div>
      <h3
        class="mx-4 font-weight-normal"
        v-if="!showSections['Future Appearances']"
      >
        Next Three Future Appearances
      </h3>
      <hr class="mx-3 bg-light" style="height: 5px" />
    </div>

    <b-card v-if="!isDataReady && isMounted" no-body>
      <span class="text-muted ml-4 mb-5"> No future appearances. </span>
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
        :items="SortedFutureAppearances"
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
          <b-badge
            style="font-weight: normal; font-size: 16px; padding-top: 12px"
            variant="white"
          >
            {{ data.value }}
          </b-badge>
        </template>

        <template v-slot:cell(sate)="data">
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

      const futureAppearancesList = ref<criminalAppearancesListType[]>([]);
      let futureAppearancesJson: criminalApprDetailType[] = [];
      const isMounted = ref(false);
      const isDataReady = ref(false);
      const sortBy = ref('date');
      const sortDesc = ref(true);

      const baseField = {
        tdClass: 'border-top',
        headerStyle: 'text',
      };

      const createField = (key, label, sortable, additionalStyles = {}) => ({
        ...baseField,
        key,
        label,
        sortable,
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
        getFutureAppearances();
      });

      const getFutureAppearances = () => {
        const data = criminalFileStore.criminalFileInformation.detailsData;
        futureAppearancesJson = [...data.appearances.apprDetail];
        ExtractFutureAppearancesInfo();
        if (futureAppearancesList.value.length) {
          isDataReady.value = true;
        }
        isMounted.value = true;
      };

      const ExtractFutureAppearancesInfo = () => {
        const currentDate = new Date();

        futureAppearancesJson.forEach(
          (jApp: criminalApprDetailType, index: number) => {
            const appearanceDate = jApp.appearanceDt.split(' ')[0];
            if (new Date(appearanceDate) < currentDate) return;

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

            futureAppearancesList.value.push(appInfo);
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

      const OpenDetails = (data) => {
        if (!data.detailsShowing) {
          const { criminalFileInformation, criminalAppearanceInfo } =
            criminalFileStore;
          const { item } = data;

          const mappings = {
            fileNo: criminalFileInformation.fileNumber,
            courtLevel: criminalFileInformation.courtLevel,
            courtClass: criminalFileInformation.courtClass,
            date: item.formattedDate,
            appearanceId: item.appearanceId,
            partId: item.partId,
            supplementalEquipmentTxt: item.supplementalEquipment,
            securityRestrictionTxt: item.securityRestriction,
            outOfTownJudgeTxt: item.outOfTownJudge,
            profSeqNo: item.profSeqNo,
          };

          Object.keys(mappings).forEach((key) => {
            criminalAppearanceInfo[key] = mappings[key];
          });

          criminalFileStore.updateCriminalAppearanceInfo(
            criminalAppearanceInfo
          );
        }
      };

      const sortChanged = () => {
        SortedFutureAppearances.value.forEach((item) => {
          item['_showDetails'] = false;
        });
      };

      const SortedFutureAppearances = computed(
        (): criminalAppearancesListType[] => {
          if (criminalFileStore.showSections['Future Appearances']) {
            return futureAppearancesList.value;
          } else {
            return _.sortBy(futureAppearancesList, 'date')
              .reverse()
              .slice(0, 3);
          }
        }
      );

      return {
        showSections: criminalFileStore.showSections,
        isMounted,
        isDataReady,
        SortedFutureAppearances,
        fields,
        sortBy,
        sortDesc,
        sortChanged,
        OpenDetails,
      };
    },
  });
</script>

<style scoped>
  .card {
    border: white;
  }
</style>
