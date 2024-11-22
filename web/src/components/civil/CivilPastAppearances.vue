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

    <b-card v-if="!isDataReady && isMounted">
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
      style="overflow: auto"
      no-body
      class="mx-3 mb-5"
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
          <span :class="data.field.cellClass" :style="data.field.cellStyle">
            <b-button
              style="transform: translate(-2px, -7px); font-size: 14px"
              size="sm"
              @click="
                OpenDetails(data);
                data.toggleDetails();
              "
              variant="outline-primary border-white  text-info"
              class="mr-2"
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
          <civil-appearance-details />
        </template>

        <template v-slot:cell(reason)="data">
          <b-badge
            :class="data.field.cellClass"
            variant="secondary"
            v-b-tooltip.hover.right
            :title="data.item.reasonDescription"
            :style="data.field.cellStyle"
          >
            {{ data.value }}
          </b-badge>
        </template>

        <template v-if="fromA2a" v-slot:cell(summary)="data">
          <span :class="data.field.cellClass" :style="data.field.cellStyle">
            <b-button
              variant="outline-primary text-info"
              style="transform: translate(0, 5px); border: 0px"
              class="mt-0"
              v-b-tooltip.hover.right
              title="Download Summary Sheet"
              @click="
                documentClick(
                  {
                    appearanceId: data.item.appearanceId,
                    appearanceDate: data.item.date,
                    documentDescription: 'CourtSummary',
                  },
                  data.item.appearanceId
                )
              "
              size="sm"
            >
              <b-icon icon="file-earmark-arrow-down" font-scale="2"></b-icon>
            </b-button>
          </span>
        </template>

        <template v-slot:cell(result)="data">
          <span
            v-if="data.value"
            :class="data.field.cellClass"
            variant="outline-primary border-white"
            v-b-tooltip.hover.right
            :title="data.item.resultDescription"
            :style="data.field.cellStyle"
          >
            {{ data.value }}
          </span>
        </template>

        <template v-slot:cell(presider)="data">
          <b-badge
            variant="secondary"
            v-if="data.value"
            :class="data.field.cellClass"
            :style="data.field.cellStyle"
            v-b-tooltip.hover.left
            :title="data.item.judgeFullName"
          >
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
  import { beautifyDate } from '@/filters';
  import { HttpService } from '@/services/HttpService';
  import { useCivilFileStore, useCommonStore } from '@/stores';
  import { civilAppearancesListType } from '@/types/civil';
  import { civilApprDetailType } from '@/types/civil/jsonTypes';
  import { CourtDocumentType, DocumentData } from '@/types/shared';
  import CivilAppearanceDetails from '@components/civil/CivilAppearanceDetails.vue';
  import * as _ from 'underscore';
  import { computed, defineComponent, inject, onMounted, ref } from 'vue';
  import { useRoute } from 'vue-router';
  import shared from '../shared';

  enum appearanceStatus {
    UNCF = 'Unconfirmed',
    CNCL = 'Canceled',
    SCHD = 'Scheduled',
  }

  export default defineComponent({
    components: {
      CivilAppearanceDetails,
    },
    setup() {
      const civilFileStore = useCivilFileStore();
      const commonStore = useCommonStore();
      const httpService = inject<HttpService>('httpService');
      const route = useRoute();

      const pastAppearancesList = ref<civilAppearancesListType[]>([]);

      const isMounted = ref(false);
      const isDataReady = ref(false);
      const pastAppearancesJson = ref<civilApprDetailType[]>([]);
      const sortBy = ref('date');
      const sortDesc = ref(true);
      const fromA2a = ref(false);

      let fields = ref([
        {
          key: 'date',
          label: 'Date',
          sortable: true,
          tdClass: 'border-top',
          headerStyle: 'text-primary',
          cellClass: 'text-info mt-2 d-inline-flex',
          cellStyle: 'display: inline-flex; font-size: 14px;',
        },
        {
          key: 'reason',
          label: 'Reason',
          sortable: true,
          tdClass: 'border-top',
          headerStyle: 'text-primary',
          cellClass: 'badge badge-secondary mt-2',
          cellStyle: 'font-size: 14px;',
        },
        {
          key: 'documentType',
          label: 'Document Type',
          sortable: false,
          tdClass: 'border-top',
          headerStyle: 'text',
          cellClass: 'text',
          cellStyle:
            'font-weight: normal;font-size: 14px; padding-top:12px;text-align:left;',
        },
        {
          key: 'result',
          label: 'Result',
          sortable: true,
          tdClass: 'border-top',
          headerStyle: 'text-primary',
          cellClass: 'badge badge-secondary mt-2',
          cellStyle: 'font-size: 14px;',
        },
        {
          key: 'time',
          label: 'Time',
          sortable: false,
          tdClass: 'border-top',
          headerStyle: 'text',
          cellClass: 'text',
          cellStyle: 'font-weight: normal; font-size: 14px; padding-top:12px',
        },
        {
          key: 'duration',
          label: 'Duration',
          sortable: false,
          tdClass: 'border-top',
          headerStyle: 'text',
          cellClass: 'text',
          cellStyle: 'font-weight: normal; font-size: 14px; padding-top:12px',
        },
        {
          key: 'location',
          label: 'Location',
          sortable: true,
          tdClass: 'border-top',
          headerStyle: 'text-primary',
          cellClass: 'text',
          cellStyle: 'font-weight: normal; font-size: 14px; padding-top:12px',
        },
        {
          key: 'room',
          label: 'Room',
          sortable: false,
          tdClass: 'border-top',
          headerStyle: 'text',
          cellClass: 'text',
          cellStyle: 'font-weight: normal; font-size: 14px; padding-top:12px',
        },
        {
          key: 'presider',
          label: 'Presider',
          sortable: true,
          tdClass: 'border-top',
          headerStyle: 'text-primary',
          cellClass: 'badge badge-secondary mt-2',
          cellStyle: 'font-size: 14px;',
        },
        {
          key: 'status',
          label: 'Status',
          sortable: true,
          tdClass: 'border-top',
          headerStyle: 'text-primary',
          cellClass: 'badge',
          cellStyle: 'font-size: 14px; width:110px;',
        },
      ]);

      onMounted(() => {
        getPastAppearances();
        isFromA2a();
      });

      const getPastAppearances = () => {
        const data = civilFileStore.civilFileInformation.detailsData;
        pastAppearancesJson.value = data.appearances.apprDetail;
        ExtractPastAppearancesInfo();
        if (pastAppearancesList.value.length) {
          isDataReady.value = true;
        }
        isMounted.value = true;
      };

      const ExtractPastAppearancesInfo = () => {
        const currentDate = new Date();
        for (const appIndex in pastAppearancesJson.value) {
          const appInfo = {} as civilAppearancesListType;
          const jApp = pastAppearancesJson.value[appIndex];

          appInfo.index = appIndex;
          appInfo.date = jApp.appearanceDt.split(' ')[0];
          if (new Date(appInfo.date) >= currentDate) continue;
          appInfo.formattedDate = beautifyDate(appInfo.date);
          appInfo.documentType = jApp.documentTypeDsc
            ? jApp.documentTypeDsc
            : '';
          appInfo.result = jApp.appearanceResultCd;
          appInfo.resultDescription = jApp.appearanceResultDsc
            ? jApp.appearanceResultDsc
            : '';
          appInfo.time = getTime(
            jApp.appearanceTm.split(' ')[1].substring(0, 5)
          );
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
          appInfo.status = jApp.appearanceStatusCd
            ? appearanceStatus[jApp.appearanceStatusCd]
            : '';
          appInfo.statusStyle = getStatusStyle(appInfo.status);
          appInfo.presider = jApp.judgeInitials ? jApp.judgeInitials : '';
          appInfo.judgeFullName = jApp.judgeInitials ? jApp.judgeFullNm : '';

          appInfo.appearanceId = jApp.appearanceId;
          appInfo.supplementalEquipment = jApp.supplementalEquipmentTxt;
          appInfo.securityRestriction = jApp.securityRestrictionTxt;
          appInfo.outOfTownJudge = jApp.outOfTownJudgeTxt;

          pastAppearancesList.value.push(appInfo);
        }
      };

      const getStatusStyle = (status) => {
        commonStore.updateStatusStyle(status);
        return commonStore.statusStyle;
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
          civilFileStore.civilAppearanceInfo.fileNo =
            civilFileStore.civilFileInformation.fileNumber;

          civilFileStore.civilAppearanceInfo.date = data.item.formattedDate;
          civilFileStore.civilAppearanceInfo.appearanceId =
            data.item.appearanceId;
          civilFileStore.civilAppearanceInfo.supplementalEquipmentTxt =
            data.item.supplementalEquipment;
          civilFileStore.civilAppearanceInfo.securityRestrictionTxt =
            data.item.securityRestriction;
          civilFileStore.civilAppearanceInfo.outOfTownJudgeTxt =
            data.item.outOfTownJudge;

          civilFileStore.updateCivilAppearanceInfo(
            civilFileStore.civilAppearanceInfo
          );
        }
      };

      const sortChanged = () => {
        SortedPastAppearances.value.forEach((item) => {
          item['_showDetails'] = false;
        });
      };

      const SortedPastAppearances = computed(() => {
        if (civilFileStore.showSections['Past Appearances']) {
          return pastAppearancesList.value;
        } else {
          return _.sortBy(pastAppearancesList.value, 'date')
            .reverse()
            .slice(0, 3);
        }
      });

      const documentClick = (document, appearanceId) => {
        httpService
          .get<any>(
            'api/files/civil/' +
              civilFileStore.civilFileInformation.fileNumber +
              '/appearance-detail/' +
              appearanceId
          )
          .then(
            (Response) => Response.json(),
            (err) => {
              //    this.$bvToast.toast(`Error - ${err.url} - ${err.status} - ${err.statusText}`, {
              //        title: "An error has occured.",
              //        variant: "danger",
              //        autoHideDelay: 10000,
              //    });
              console.log(err);
            }
          )
          .then((data) => {
            if (data) {
              const documentType =
                document.item == null
                  ? CourtDocumentType.CSR
                  : CourtDocumentType.Civil;
              const location = commonStore.courtRoomsAndLocations.filter(
                (location) => {
                  return location.locationId == data?.agencyId;
                }
              )[0]?.name;
              const documentData: DocumentData = {
                appearanceId: document.appearanceId,
                appearanceDate: data?.appearanceDt.substring(0, 10),
                courtLevel: data?.courtLevelCd,
                dateFiled: document.item
                  ? beautifyDate(document.item.dateFiled)
                  : '',
                documentId: document.item ? document.item.id : '',
                documentDescription: document.item
                  ? document.item.documentType
                  : document.documentDescription,
                fileId: civilFileStore.civilFileInformation.fileNumber,
                fileNumberText: data.fileNumberTxt,
                location: location ? location : '',
              };
              console.log(documentData);
              shared.openDocumentsPdf(documentType, documentData);
            } else {
              window.alert('bad data!');
            }
          });
      };

      const isFromA2a = () => {
        if (route.query?.fromA2A && fromA2a.value == false) {
          fromA2a.value = true;
          const newFields: any[] = [];
          fields.value.forEach((field) => {
            if (field.key == 'result') {
              newFields.push({
                key: 'summary',
                label: 'Court Summary',
                sortable: false,
                tdClass: 'border-top',
                headerStyle: 'text',
                cellClass: 'text',
                cellStyle:
                  'font-weight: normal;font-size: 14px; padding-top:12px;text-align:left;',
              });
            }
            newFields.push(field);
          });
          fields.value = newFields;
        }
      };

      return {
        showSections: civilFileStore.showSections,
        isDataReady,
        isMounted,
        SortedPastAppearances,
        sortBy,
        fields,
        sortDesc,
        sortChanged,
        OpenDetails,
        fromA2a,
        documentClick,
      };
    },
  });
</script>

<style scoped>
  .card {
    border: white;
  }
</style>
