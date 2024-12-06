<template>
  <b-card bg-variant="white" no-body>
    <div>
      <h2 class="mx-4 mt-5 font-weight-normal text-criminal">Criminal</h2>
      <custom-overlay
        :show="!loadCompleted"
        style="padding: 0 1rem; margin-left: auto; margin-right: 0.5rem"
      >
        <b-button
          @click="openFiles()"
          variant="outline-primary bg-success text-white"
          style="
            padding: 0.5rem 1.5rem;
            margin-left: auto;
            right: 0;
            bottom: 1rem;
            position: absolute;
          "
        >
          <b-icon-box-arrow-up-right
            class="mx-0 pl-0"
            variant="white"
            scale="1"
          ></b-icon-box-arrow-up-right>
          Open Selected
        </b-button>
      </custom-overlay>
      <hr class="mx-3 bg-criminal" style="height: 5px" />
    </div>

    <b-card bg-variant="light" v-if="isMounted && !isDataReady">
      <b-card style="min-height: 100px">
        <span v-if="errorCode == 404"
          >This <b>File-Number '{{ route.query.fileNumber }}'</b> at
          <b> location '{{ route.query.location }}' </b> doesn't exist in the
          <b>criminal</b> records.
        </span>
        <span v-else-if="errorCode == 200 || errorCode == 204">
          Bad Data in search results!
        </span>
        <span v-else-if="errorCode == 403">
          You are not authorized to access this file.
        </span>
        <span v-else>
          Server is not responding. <b>({{ errorText }})</b>
        </span>
      </b-card>
      <!-- <b-card> 
                <b-button id="backToLandingPage" variant="outline-primary text-dark bg-warning" @click="navigateToLandingPage">
                    <b-icon-house-door class="mr-1 ml-0" variant="dark" scale="1" ></b-icon-house-door>
                    Return to Main Page
                </b-button>        
            </b-card> -->
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
      class="mx-3"
      style="overflow: auto"
    >
      <b-table
        :items="SortedList"
        :fields="fields"
        borderless
        striped
        responsive="sm"
        :tbody-tr-class="rowClass"
      >
        <template
          v-for="(field, index) in fields"
          v-slot:[`head(${field.key})`]="data"
        >
          <h3 v-bind:key="index">{{ data.label }}</h3>
        </template>

        <template v-slot:head(select)>
          <b-form-checkbox
            class="m-0"
            v-model="allFilesChecked"
            @change="checkAllFiles"
            size="sm"
          />
        </template>

        <template v-slot:cell(select)="data">
          <b-form-checkbox
            size="sm"
            class="m-0"
            v-model="data.item.isChecked"
            @change="toggleSelectedFiles"
          />
        </template>

        <template v-slot:cell(fileNumber)="data">
          <b-button
            :style="data.field.cellStyle"
            size="sm"
            @click="OpenCriminalFilePage(data.item.fileId)"
            variant="outline-primary text-criminal"
            class="mr-2"
          >
            {{ data.value }}
          </b-button>
        </template>
        <template v-slot:cell(participants)="data">
          <span
            v-for="(participant, index) in data.value"
            v-bind:key="index"
            :style="data.field.cellStyle"
            v-b-tooltip.hover.top.html="participant.charge"
          >
            {{ participant.name }} <br />
          </span>
        </template>

        <template v-slot:cell(nextAppearance)="data">
          <span :style="data.field.cellStyle">
            {{ beautifyDate(data.value) }}
          </span>
        </template>
      </b-table>

      <!-- <b-card class="mb-5" align="right">         
                <b-button id="backToLandingPage" variant="outline-primary text-dark bg-warning" @click="navigateToLandingPage">
                    <b-icon-house-door class="mr-1 ml-0" variant="dark" scale="1" ></b-icon-house-door>
                    Return to Main Page
                </b-button>
            </b-card> -->
    </b-card>
  </b-card>
</template>

<script lang="ts">
  import { beautifyDate } from '@/filters';
  import { HttpService } from '@/services/HttpService';
  import { useCommonStore, useCriminalFileStore } from '@/stores';
  import {
    fileSearchCriminalInfoType,
    participantInfoType,
  } from '@/types/criminal';
  import * as _ from 'underscore';
  import {
    computed,
    defineComponent,
    inject,
    nextTick,
    onMounted,
    ref,
  } from 'vue';
  import { useRoute, useRouter } from 'vue-router';
  import CustomOverlay from '../CustomOverlay.vue';

  enum CourtLevel {
    'P' = 'Provincial',
    'S' = 'Supreme',
  }

  export default defineComponent({
    components: {
      CustomOverlay,
    },
    setup() {
      const commonStore = useCommonStore();
      const criminalFileStore = useCriminalFileStore();
      const httpService = inject<HttpService>('httpService');

      if (!httpService) {
        throw new Error('HttpService is not available!');
      }

      const router = useRouter();
      const route = useRoute();

      if (!httpService) {
        throw new Error('Service is undefined.');
      }

      const criminalList = ref<fileSearchCriminalInfoType[]>([]);
      const isMounted = ref(false);
      const isDataReady = ref(false);
      const loadCompleted = ref(true);
      const errorCode = ref(0);
      const errorText = ref('');
      const allFilesChecked = ref(false);
      //      const selectedFiles = ref<string[]>([]);

      const commonFieldStyles = {
        tdClass: 'border-top',
        cellStyle: 'font-size: 16px;',
        sortable: false,
        headerStyle: 'text-primary',
      };

      const createField = (key, label, additionalStyles = {}) => ({
        key,
        label,
        ...commonFieldStyles,
        ...additionalStyles,
      });

      const fields = [
        createField('select', '', {
          thClass: '',
        }),
        createField('fileNumber', 'File Number', {
          cellStyle: 'font-size:16px; font-weight: bold; border: none;',
        }),
        createField('participants', 'Participants', {
          cellStyle: 'white-space: pre-line',
        }),
        createField('nextAppearance', 'Next Appearance', {
          cellStyle: 'white-space: pre-line',
        }),
      ];

      onMounted(() => {
        getList();
        loadCompleted.value = true;
      });

      const getList = () => {
        httpService
          .get<any>(
            'api/files/criminal?location=' +
              route.query.location +
              '&fileNumber=' +
              route.query.fileNumber
          )
          .then(
            (Response) => Response,
            (err) => {
              // this.$bvToast.toast(`Error - ${err.url} - ${err.status} - ${err.statusText}`, {
              //   title: "An error has occured.",
              //   variant: "danger",
              //   autoHideDelay: 10000,
              // });
              errorCode.value = err.status;
              errorText.value = err.statusText;
              console.log(err);
              isMounted.value = true;
            }
          )
          .then((data) => {
            if (data) {
              if (data.length > 1) {
                // console.log(data)
                for (const criminalListIndex in data) {
                  const criminalListInfo = {} as fileSearchCriminalInfoType;
                  const jcriminalList = data[criminalListIndex];
                  const participantInfo: participantInfoType[] = [];
                  for (const participant of jcriminalList.participant) {
                    const firstName =
                      participant.givenNm.trim().length > 0
                        ? participant.givenNm
                        : '';
                    const lastName = participant.lastNm
                      ? participant.lastNm
                      : participant.orgNm;
                    commonStore.updateDisplayName({
                      lastName: lastName,
                      givenName: firstName,
                    });
                    const charges: string[] = [];
                    for (const charge of participant.charge) {
                      const chargeDesc = charge.sectionDscTxt
                        ? charge.sectionDscTxt
                        : '';
                      if (chargeDesc.length > 0) charges.push(chargeDesc);
                    }
                    participantInfo.push({
                      name: commonStore.displayName,
                      charge: charges.toString(),
                    });
                  }
                  criminalListInfo.participants = participantInfo;
                  criminalListInfo.fileNumber = jcriminalList.fileNumberTxt;
                  criminalListInfo.fileId = jcriminalList.justinNo;
                  criminalListInfo.nextAppearance = jcriminalList.nextApprDt;
                  const currentDate = new Date();
                  criminalListInfo.today =
                    currentDate == new Date(jcriminalList.nextApprDt);
                  criminalListInfo.level =
                    CourtLevel[jcriminalList.courtLevelCd];
                  criminalList.value.push(criminalListInfo);
                }
                if (criminalList.value.length) {
                  isDataReady.value = true;
                }
                isMounted.value = true;
              } else if (data.length == 1) {
                criminalFileStore.criminalFileInformation.fileNumber =
                  data[0].justinNo;
                criminalFileStore.updateCriminalFile(
                  criminalFileStore.criminalFileInformation
                );
                router.push({
                  name: 'CriminalCaseDetails',
                  params: {
                    fileNumber:
                      criminalFileStore.criminalFileInformation.fileNumber,
                  },
                });
              }
            }
          });
      };

      const OpenCriminalFilePage = (fileNumber) => {
        criminalFileStore.criminalFileInformation.fileNumber = fileNumber;
        criminalFileStore.updateCriminalFile(
          criminalFileStore.criminalFileInformation
        );
        const routeData = router.resolve({
          name: 'CriminalCaseDetails',
          params: {
            fileNumber: criminalFileStore.criminalFileInformation.fileNumber,
          },
        });
        window.open(routeData.href, '_blank');
      };

      const openFiles = () => {
        loadCompleted.value = false;
        for (const file of SortedList.value) {
          if (file.isChecked) {
            OpenCriminalFilePage(file.fileId);
          }
        }
        loadCompleted.value = true;
      };

      const checkAllFiles = (checked) => {
        for (const docInx in SortedList) {
          SortedList[docInx].isChecked = checked;
        }
      };

      const toggleSelectedFiles = () => {
        nextTick(() => {
          const checkedDocs = SortedList.value.filter((file) => {
            return file.isChecked;
          });

          if (checkedDocs.length == SortedList.value.length)
            allFilesChecked.value = true;
          else allFilesChecked.value = false;
        });
      };

      const SortedList = computed(() => {
        return _.sortBy(criminalList.value, 'nextAppearance').reverse();
      });

      const rowClass = (item, type) => {
        if (!item || type !== 'row') return;
        if (item.today) return 'table-warning';
      };

      // const navigateToLandingPage = () => {
      //   router.push({ name: 'Home' });
      // };

      return {
        isMounted,
        loadCompleted,
        openFiles,
        isDataReady,
        errorCode,
        errorText,
        SortedList,
        fields,
        rowClass,
        allFilesChecked,
        checkAllFiles,
        toggleSelectedFiles,
        OpenCriminalFilePage,
        beautifyDate,
        route,
      };
    },
  });
</script>

<style scoped>
  .card {
    border: white;
  }
</style>
