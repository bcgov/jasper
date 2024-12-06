<template>
  <b-card bg-variant="white" class="home">
    <b-card>
      <b-card-title class="my-0 ml-4 h1 text-primary">
        Supreme Court Viewer
      </b-card-title>
    </b-card>
    <b-row cols="2" body-class="py-0">
      <b-col md="6" cols="6">
        <b-card align="left" body-class="pt-1 pb-0">
          <b-card>
            <b-card-title class="h3 my-0 text-secondary">
              Search by File Number and Location:
            </b-card-title>
          </b-card>
          <b-card body-class="py-1">
            <b-card-text class="mb-1" for="filenumber">
              File Number:
            </b-card-text>
            <b-form-input
              id="filenumber"
              v-model="fileSearch.fileNumber"
              placeholder="Enter File Number"
            ></b-form-input>
          </b-card>
          <b-card body-class="pb-1">
            <b-card-text class="mb-1" for="civil/criminal">
              Civil/Criminal:
            </b-card-text>
            <b-form-select
              v-model="selectedFileSearch"
              :options="options"
            ></b-form-select>
          </b-card>
          <b-card>
            <b-card-text class="mb-1" for="location"> Location: </b-card-text>
            <b-form-select
              v-model="selectedCourtLocation"
              id="locationSelect"
              :disabled="!searchAllowed"
              :state="selectedCourtLocationState ? null : false"
              @change="LocationChanged"
              :options="courtRoomsAndLocations"
              style="height: 39px"
            >
            </b-form-select>
          </b-card>
          <b-card class="pb-0">
            <b-button
              variant="outline-primary text-dark"
              @click="navigateToFilesView(fileSearch)"
            >
              <b-icon-search class="mr-1" variant="dark"></b-icon-search>
              Search
            </b-button>
          </b-card>
        </b-card>
        <b-card align="left" body-class="py-1">
          <b-card>
            <b-card-title class="my-0 h3"> Search by File ID: </b-card-title>
          </b-card>
          <b-card body-class="py-1">
            <b-card-text class="mb-1" for="fileId"> File ID: </b-card-text>
            <b-form-input
              id="fileId"
              v-model="fileInformation.fileNumber"
              placeholder="Enter File ID"
            ></b-form-input>
          </b-card>
          <b-card>
            <b-card-text class="mb-1" for="civil/criminal">
              Civil/Criminal:
            </b-card-text>
            <b-form-select
              v-model="selected"
              :options="options"
            ></b-form-select>
          </b-card>
          <b-card>
            <b-button
              variant="outline-primary text-dark"
              @click="navigateToDocumentsView(fileInformation)"
            >
              <b-icon-search class="mr-1" variant="dark"></b-icon-search>
              Search
            </b-button>
          </b-card>
        </b-card>
      </b-col>
      <b-col md="6" cols="6">
        <b-card align="left" body-class="pt-1 pb-0">
          <b-card>
            <b-card-title class="h3 my-0"> Search by Court List: </b-card-title>
          </b-card>
          <b-card body-class="mt-0 pt-0">
            <b-button
              variant="outline-primary text-dark"
              @click="navigateToCourtList()"
            >
              <b-icon-search class="mr-1" variant="dark"></b-icon-search>
              Navigate to Court List Search
            </b-button>
          </b-card>
        </b-card>
      </b-col>
    </b-row>

    <b-modal v-model="openErrorModal" header-class="bg-warning text-light">
      <b-card class="h4 mx-2 py-2">
        <span v-if="errorCode == 403" class="p-0">
          You are not authorized to access this page.</span
        >
        <span v-else class="p-0">{{ errorText }}</span>
      </b-card>
      <template v-slot:modal-footer>
        <b-button variant="primary" @click="openErrorModal = false"
          >Ok</b-button
        >
      </template>
      <template v-slot:modal-header-close>
        <b-button
          variant="outline-warning"
          class="text-light closeButton"
          @click="openErrorModal = false"
          >&times;</b-button
        >
      </template>
    </b-modal>
  </b-card>
</template>

<script lang="ts">
  import { HttpService } from '@/services/HttpService';
  import { useCivilFileStore, useCriminalFileStore } from '@/stores';
  import {
    civilFileInformationType,
    fileSearchCivilInfoType,
  } from '@/types/civil';
  import { CourtRoomsJsonInfoType } from '@/types/common';
  import {
    courtRoomsAndLocationsInfoType,
    locationInfoType,
  } from '@/types/courtlist';
  import {
    criminalFileInformationType,
    fileSearchCriminalInfoType,
  } from '@/types/criminal';
  import * as _ from 'underscore';
  import { defineComponent, inject } from 'vue';

  export default defineComponent({
    name: 'HomeView',
    data() {
      const fileSearchCriminalInfo: fileSearchCriminalInfoType[] = [];
      const fileSearchCivilInfo: fileSearchCivilInfoType[] = [];
      const courtRoomsAndLocations: courtRoomsAndLocationsInfoType[] = [];
      const courtRoomsAndLocationsJson: CourtRoomsJsonInfoType[] = [];
      const civilFileStore = useCivilFileStore();
      const criminalFileStore = useCriminalFileStore();

      return {
        selected: 'criminal',
        selectedFileSearch: 'criminal',
        options: [
          { value: 'civil', text: 'Civil' },
          { value: 'criminal', text: 'Criminal' },
        ],
        fileInformation: {
          fileNumber: '',
        },
        fileSearch: {
          fileNumber: '',
        },
        errorCode: 0,
        errorText: '',
        openErrorModal: false,
        syncFlag: true,
        searchingRequest: false,
        searchAllowed: true,
        isLocationDataMounted: false,
        isLocationDataReady: false,
        courtRoomsAndLocationsJson,
        courtRoomsAndLocations,
        fileSearchCivilInfo,
        fileSearchCriminalInfo,
        selectedCourtLocation: {} as locationInfoType,
        selectedCourtLocationState: true,
        civilFileStore,
        criminalFileStore,
      };
    },
    mounted() {
      this.getListOfAvailableCourts();
    },
    methods: {
      navigateToDocumentsView(fileInformation): void {
        if (this.selected == 'civil') {
          this.UpdateCivilFile(fileInformation);
          this.$router.push({
            name: 'CivilCaseDetails',
            params: { fileNumber: fileInformation.fileNumber },
          });
        } else if (this.selected == 'criminal') {
          this.UpdateCriminalFile(fileInformation);
          this.$router.push({
            name: 'CriminalCaseDetails',
            params: { fileNumber: fileInformation.fileNumber },
          });
        }
      },
      navigateToFilesView(fileSearch): void {
        fileSearch.location = this.selectedCourtLocation.LocationID;
        if (this.selectedFileSearch == 'civil') {
          this.$router.push({
            name: 'CivilFileSearchResultsView',
            query: {
              fileNumber: fileSearch.fileNumber,
              location: fileSearch.location,
            },
          });
        } else if (this.selectedFileSearch == 'criminal') {
          this.$router.push({
            name: 'CriminalFileSearchResultsView',
            query: {
              fileNumber: fileSearch.fileNumber,
              location: fileSearch.location,
            },
          });
        }
      },
      getListOfAvailableCourts(): void {
        const httpService = inject<HttpService>('httpService');
        if (!httpService) {
          throw new Error('Service is undefined');
        }

        this.errorCode = 0;
        httpService
          .get<any>('api/location/court-rooms')
          .then(
            Response => Response,
            err => {
              this.errorCode = err.status;
              this.errorText = err.statusText;
              if (this.errorCode != 401) {
                // this.$bvToast.toast(
                //   `Error - ${this.errorCode} - ${this.errorText}`,
                //   {
                //     title: 'An error has occured.',
                //     variant: 'danger',
                //     autoHideDelay: 10000,
                //   }
                // );
              }
              console.log(this.errorCode);
            }
          )
          .then(data => {
            if (data) {
              this.courtRoomsAndLocationsJson = data;
              this.ExtractCourtRoomsAndLocationsInfo();
              if (this.courtRoomsAndLocations.length > 0) {
                this.isLocationDataReady = true;
              }
            }
            this.isLocationDataMounted = true;
          });
      },
      ExtractCourtRoomsAndLocationsInfo() {
        for (const jroomAndLocation of this.courtRoomsAndLocationsJson) {
          if (jroomAndLocation.courtRooms.length > 0) {
            const roomAndLocationInfo = {} as courtRoomsAndLocationsInfoType;
            roomAndLocationInfo.text =
              jroomAndLocation.name + ' (' + jroomAndLocation.code + ')';
            roomAndLocationInfo.value = {} as locationInfoType;
            roomAndLocationInfo.value.Location = jroomAndLocation.name;
            roomAndLocationInfo.value.LocationID = jroomAndLocation.code;
            this.courtRoomsAndLocations.push(roomAndLocationInfo);
          }
        }
        this.courtRoomsAndLocations = _.sortBy(
          this.courtRoomsAndLocations,
          'text'
        );
        this.selectedCourtLocation = this.courtRoomsAndLocations[0].value;
      },
      LocationChanged() {
        this.searchingRequest = false;
        this.selectedCourtLocationState = true;
        this.syncFlag = false;
        this.syncFlag = true;
      },
      navigateToCourtList(): void {
        this.$router.push({ name: 'CourtList' });
      },
      UpdateCivilFile(newCivilFileInformation: civilFileInformationType): void {
        this.civilFileStore.updateCivilFile(newCivilFileInformation);
      },
      UpdateCriminalFile(
        newCriminalFileInformation: criminalFileInformationType
      ): void {
        this.criminalFileStore.updateCriminalFile(newCriminalFileInformation);
      },
    },
  });
</script>

<style scoped>
  input,
  select {
    width: 300px;
  }

  .card {
    border: white;
  }
</style>
