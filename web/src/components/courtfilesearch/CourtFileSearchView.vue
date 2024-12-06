<template>
  <b-card class="main-container px-0" no-body bg-variant="white">
    <b-card
      body-class="px-0"
      bg-variant="light"
      v-if="!isLookupDataMounted && !isLookupDataReady"
    >
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
      body-class="px-0"
      bg-variant="light"
      v-else-if="isLookupDataMounted && !isLookupDataReady"
    >
      <b-card style="min-height: 40px">
        <span v-if="errorCode != '0'">
          <span v-if="errorCode === '403'">
            You are not authorized to access this page.
          </span>
          <span v-else>
            Server is not responding.
            <b>({{ errorText }} "{{ errorCode }}")</b></span
          >
        </span>
        <span v-else> No Court File Search Found. </span>
      </b-card>
    </b-card>
    <b-card body-class="px-0" v-else>
      <h2>Court File Search</h2>
      <b-form
        class="search-form"
        v-if="isLookupDataMounted && isLookupDataReady"
        @submit.prevent="handleSubmit"
      >
        <b-form-group label="Location:" class="mr-3">
          <b-form-select v-model="searchCriteria.fileHomeAgencyId">
            <option value=""></option>
            <option
              v-for="option in courtRooms"
              :key="option.value"
              :value="option.value"
            >
              {{ option.text }}
            </option>
          </b-form-select>
        </b-form-group>

        <!-- Division -->
        <b-form-group label="Division*" class="mr-3">
          <b-button-group>
            <b-button
              value="isCriminal"
              @click="handleDivisionChange"
              :variant="
                searchCriteria.isCriminal ? 'primary' : 'outline-primary'
              "
            >
              Criminal
            </b-button>
            <b-button
              value="isFamily"
              @click="handleDivisionChange"
              :variant="searchCriteria.isFamily ? 'primary' : 'outline-primary'"
            >
              Family
            </b-button>
            <b-button
              value="isSmallClaims"
              @click="handleDivisionChange"
              :variant="
                searchCriteria.isSmallClaims ? 'primary' : 'outline-primary'
              "
            >
              Small Claims
            </b-button>
          </b-button-group>
        </b-form-group>

        <!-- Class -->
        <b-form-group label="Class:" class="mr-3">
          <b-form-select v-model="searchCriteria.class">
            <option value=""></option>
            <option
              v-for="option in classOptions"
              :key="option.code"
              :value="option.code"
            >
              {{ option.shortDesc }}
            </option>
          </b-form-select>
        </b-form-group>

        <!-- Search -->
        <div class="d-flex align-items-end">
          <b-form-group label="Search*" horizontal>
            <b-form-select class="search" v-model="searchCriteria.searchBy">
              <option value="fileNumber">File number</option>
              <option value="lastName">Surname</option>
              <option value="orgName">Organization</option>
            </b-form-select>
          </b-form-group>

          <b-form-group v-if="searchCriteria.searchBy === 'fileNumber'">
            <b-form-input
              class="search-input"
              :class="{ 'is-invalid': errors.isMissingFileNo }"
              placeholder="e.g. 99999999"
              v-model="searchCriteria.fileNumberTxt"
            ></b-form-input>
          </b-form-group>

          <b-form-group v-if="searchCriteria.searchBy === 'lastName'">
            <b-form-input
              class="search-input"
              placeholder="MacDonald"
              :class="{ 'is-invalid': errors.isMissingSurname }"
              v-model="searchCriteria.lastName"
            ></b-form-input>
          </b-form-group>

          <b-form-group v-if="searchCriteria.searchBy === 'orgName'">
            <b-form-input
              class="search-input"
              :class="{ 'is-invalid': errors.isMissingOrg }"
              placeholder="e.g. MegaCorp Inc."
              v-model="searchCriteria.orgName"
            ></b-form-input>
          </b-form-group>
          <!-- Submit -->
          <b-button
            variant="primary"
            class="search-btn mb-3"
            type="submit"
            :disabled="isSearching"
          >
            <b-icon icon="search"></b-icon>
            Search
          </b-button>
          <!-- Reset -->
          <b-button
            variant="link"
            class="reset mb-3"
            @click="() => handleReset(true)"
          >
            Reset search
          </b-button>
        </div>
        <div
          v-if="
            (searchCriteria.isCriminal &&
              searchCriteria.searchBy === 'fileNumber') ||
            searchCriteria.searchBy === 'lastName'
          "
        >
          <span>Optional...</span>
          <div class="d-flex p-3 bg-light">
            <div class="d-flex" v-if="searchCriteria.searchBy === 'fileNumber'">
              <b-form-group
                class="mr-3"
                label="Prefix"
                label-for="filePrefixTxt"
              >
                <b-form-input
                  id="filePrefixTxt"
                  placeholder="AH"
                  v-model="searchCriteria.filePrefixTxt"
                ></b-form-input>
              </b-form-group>

              <b-form-group
                class="mr-3"
                label="Seq Num"
                label-for="fileSuffixNo"
              >
                <b-form-input
                  id="fileSuffixNo"
                  placeholder="1"
                  v-model="searchCriteria.fileSuffixNo"
                ></b-form-input>
              </b-form-group>

              <b-form-group label="Type Ref" label-for="mDocRefTypeCode">
                <b-form-input
                  id="mDocRefTypeCode"
                  placeholder="B"
                  v-model="searchCriteria.mDocRefTypeCode"
                ></b-form-input>
              </b-form-group>
            </div>

            <b-form-group
              label="Given Name"
              label-for="givenName"
              v-if="searchCriteria.searchBy === 'lastName'"
            >
              <b-form-input
                id="givenName"
                placeholder="e.g. John"
                v-model="searchCriteria.givenName"
              ></b-form-input>
            </b-form-group>
          </div>
        </div>
      </b-form>
      <court-file-search-result
        class="mb-5"
        v-if="isSearching || hasSearched"
        :isLookupDataMounted="isLookupDataMounted"
        :isLookupDataReady="isLookupDataReady"
        :courtRooms="courtRooms"
        :classes="classes"
        :isCriminal="searchCriteria.isCriminal"
        :searchResults="searchResults"
        :isSearching="isSearching"
        @files-viewed="viewFiles"
        :selectedFiles="selectedFiles"
        @add-selected="addSelectedFile"
        @remove-selected="removeSelectedFile"
        @clear-selected="clearSelectedFiles"
        :isSearchResultsOver="isSearchResultsOver"
      >
      </court-file-search-result>
    </b-card>
  </b-card>
</template>
<script lang="ts">
  import CourtFileSearchResult from '@/components/courtfilesearch/CourtFileSearchResult.vue';
  import { FilesService } from '@/services/FilesService';
  import { LocationService } from '@/services/LocationService';
  import { LookupService } from '@/services/LookupService';
  import { useCommonStore, useCourtFileSearchStore } from '@/stores';
  import { KeyValueInfo, LookupCode } from '@/types/common';
  import {
    CourtClassEnum,
    CourtFileSearchCriteria,
    FileDetail,
    SearchModeEnum,
  } from '@/types/courtFileSearch';
  import { roomsInfoType } from '@/types/courtlist';
  import { defineComponent, inject, onMounted, reactive, ref } from 'vue';
  import { useRouter } from 'vue-router';

  const CRIMINAL_CODE = 'R';
  const SMALL_CLAIMS_CODE = 'I';
  const SEARCH_RESULT_LIMIT = 100;

  export default defineComponent({
    components: {
      CourtFileSearchResult,
    },
    setup() {
      const courtFileSearchStore = useCourtFileSearchStore();
      const commonStore = useCommonStore();
      const router = useRouter();

      let searchCriteria: CourtFileSearchCriteria = reactive({
        isCriminal: true,
        isFamily: false,
        isSmallClaims: false,
        searchBy: 'fileNumber',
        fileHomeAgencyId: commonStore.userInfo!.agencyCode,
      });
      const classOptions = ref<LookupCode[]>([]);
      const selectedFiles = ref<FileDetail[]>([]);
      const searchResults = ref<FileDetail[]>([]);
      const classes = ref<LookupCode[]>([]);
      const courtRooms = ref<roomsInfoType[]>([]);
      const isLookupDataReady = ref(false);
      const isLookupDataMounted = ref(false);
      const isSearching = ref(false);
      const hasSearched = ref(false);
      const isSearchResultsOver = ref(false);
      const defaultLocation = commonStore.userInfo!.agencyCode;

      const errorCode = ref('');
      const errorText = ref('');
      const errors = reactive({
        isMissingFileNo: false,
        isMissingSurname: false,
        isMissingOrg: false,
      });

      const locationService = inject<LocationService>('locationService');
      const lookupService = inject<LookupService>('lookupService');
      const filesService = inject<FilesService>('filesService');

      if (!locationService || !lookupService || !filesService) {
        throw new Error('Services is undefined.');
      }

      onMounted(async () => {
        await loadData();
      });

      const loadData = async (): Promise<void> => {
        try {
          const [courtRoomsResp, courtClassesResp] = await Promise.all([
            locationService.getCourtRooms(),
            lookupService.getCourtClasses(),
          ]);

          courtRooms.value = courtRoomsResp;
          classes.value = courtClassesResp;
          loadDataFromState();
          loadClasses();
          isLookupDataReady.value = true;
        } catch (err: unknown) {
          console.error(err);
          // errorCode = err.status;
          // errorText = err.statusText;
          // if (errorCode != 401) {
          //   $bvToast.toast(
          //     `Error - ${errorCode} - ${errorText}`,
          //     {
          //       title: 'An error has occured.',
          //       variant: 'danger',
          //       autoHideDelay: 10000,
          //     }
          //   );
          // }
          // console.log(errorCode);
        } finally {
          isLookupDataMounted.value = true;
        }
      };

      const handleDivisionChange = (event: Event) => {
        searchCriteria.isCriminal = false;
        searchCriteria.isFamily = false;
        searchCriteria.isSmallClaims = false;

        const target = event.target as HTMLInputElement;
        searchCriteria[target.value] = true;

        handleReset();
      };

      const handleSubmit = async () => {
        courtFileSearchStore.clearSelectedFiles();

        sanitizeTextInputs();
        resetErrors();

        errors.isMissingFileNo =
          searchCriteria.searchBy === 'fileNumber' &&
          !searchCriteria.fileNumberTxt;
        errors.isMissingSurname =
          searchCriteria.searchBy === 'lastName' && !searchCriteria.lastName;
        errors.isMissingOrg =
          searchCriteria.searchBy === 'orgName' && !searchCriteria.orgName;

        // Don't proceed if any of the validation flag is set to true
        const hasNoErrors = Object.values(errors).every(
          (value) => value === false
        );
        if (!hasNoErrors) {
          return;
        }

        try {
          isSearching.value = true;
          searchResults.value.length = 0;
          isSearchResultsOver.value = false;

          const { recCount, fileDetail } = searchCriteria.isCriminal
            ? await filesService.searchCriminalFiles(buildQueryParams())
            : await filesService.searchCivilFiles(buildQueryParams());

          // Make sure that to only show up to 100 results
          isSearchResultsOver.value = recCount > SEARCH_RESULT_LIMIT;
          searchResults.value = [...fileDetail.slice(0, SEARCH_RESULT_LIMIT)];
        } catch (err: unknown) {
          console.error(err);
          // errorCode = err.status;
          // errorText = err.statusText;
          // if (errorCode != 401) {
          //   $bvToast.toast(
          //     `Error - ${errorCode} - ${errorText}`,
          //     {
          //       title: 'An error has occured.',
          //       variant: 'danger',
          //       autoHideDelay: 10000,
          //     }
          //   );
          // }
          // console.log(errorCode);
        } finally {
          hasSearched.value = true;
          isSearching.value = false;
        }
      };

      const handleReset = (resetDivision = false) => {
        courtFileSearchStore.clearSelectedFiles();
        searchCriteria.isCriminal = resetDivision
          ? true
          : searchCriteria.isCriminal;
        searchCriteria.isFamily = resetDivision
          ? false
          : searchCriteria.isFamily;
        searchCriteria.isSmallClaims = resetDivision
          ? false
          : searchCriteria.isSmallClaims;
        searchCriteria.fileHomeAgencyId = defaultLocation;
        searchCriteria.searchBy = 'fileNumber';
        searchCriteria.fileNumberTxt = undefined;
        searchCriteria.lastName = undefined;
        searchCriteria.givenName = undefined;
        searchCriteria.orgName = undefined;
        searchCriteria.class = undefined;

        if (searchCriteria.isCriminal) {
          searchCriteria.filePrefixTxt = undefined;
          searchCriteria.fileSuffixNo = undefined;
          searchCriteria.mDocRefTypeCode = undefined;
        }

        loadClasses();
        resetErrors();

        searchResults.value.length = 0;
        hasSearched.value = false;
      };

      const sanitizeTextInputs = () => {
        searchCriteria.fileNumberTxt = searchCriteria.fileNumberTxt?.trim();
        searchCriteria.filePrefixTxt = searchCriteria.filePrefixTxt?.trim();
        searchCriteria.fileSuffixNo = searchCriteria.fileSuffixNo?.trim();
        searchCriteria.mDocRefTypeCode = searchCriteria.mDocRefTypeCode?.trim();

        searchCriteria.lastName = searchCriteria.lastName?.trim();
        searchCriteria.givenName = searchCriteria.givenName?.trim();

        searchCriteria.orgName = searchCriteria.orgName?.trim();
      };

      const resetErrors = () => {
        errors.isMissingFileNo = false;
        errors.isMissingSurname = false;
        errors.isMissingOrg = false;
      };

      const loadClasses = () => {
        if (searchCriteria.isCriminal) {
          classOptions.value = classes.value.filter(
            (c) => c.longDesc === CRIMINAL_CODE
          );
        } else if (searchCriteria.isFamily) {
          classOptions.value = [];
        } else if (searchCriteria.isSmallClaims) {
          classOptions.value = classes.value.filter(
            (c) =>
              c.longDesc === SMALL_CLAIMS_CODE &&
              c.code !== CourtClassEnum[CourtClassEnum.C]
          );
        }
      };

      const buildQueryParams = () => {
        const queryParams = buildSearchByQueryParams();

        // Class
        if (searchCriteria.isFamily) {
          queryParams['courtClass'] = CourtClassEnum.F;
        } else if (searchCriteria.class) {
          queryParams['courtClass'] = CourtClassEnum[searchCriteria.class];
        } else if (searchCriteria.isSmallClaims) {
          queryParams['courtClass'] = CourtClassEnum.C;
        }

        if (searchCriteria.fileHomeAgencyId) {
          queryParams['fileHomeAgencyId'] = searchCriteria.fileHomeAgencyId;
        }

        return queryParams;
      };

      const buildSearchByQueryParams = () => {
        const queryParams = {};

        if (searchCriteria.searchBy === 'fileNumber') {
          queryParams['searchMode'] = SearchModeEnum.FileNo;
          queryParams['fileNumberTxt'] = searchCriteria.fileNumberTxt;

          if (searchCriteria.filePrefixTxt) {
            queryParams['filePrefixTxt'] = searchCriteria.filePrefixTxt;
          }
          if (searchCriteria.fileSuffixNo) {
            queryParams['fileSuffixNo'] = searchCriteria.fileSuffixNo;
          }
          if (searchCriteria.mDocRefTypeCode) {
            queryParams['mDocRefTypeCode'] = searchCriteria.mDocRefTypeCode;
          }
        } else if (searchCriteria.searchBy === 'lastName') {
          queryParams['searchMode'] = SearchModeEnum.PartName;
          queryParams['lastName'] = searchCriteria.lastName;

          if (searchCriteria.givenName) {
            queryParams['givenName'] = searchCriteria.givenName;
          }
        } else {
          queryParams['searchMode'] = SearchModeEnum.PartName;
          queryParams['orgName'] = searchCriteria.orgName;
        }

        return queryParams;
      };

      const loadDataFromState = () => {
        const search = courtFileSearchStore.currentSearchCriteria;
        const results = courtFileSearchStore.currentSearchResults;
        const selectedFilesFromStore = courtFileSearchStore.selectedFiles;

        if (search && results) {
          //Object.assign(searchCriteria, search);
          searchResults.value = [...results];

          const idSelector = searchCriteria.isCriminal
            ? 'mdocJustinNo'
            : 'physicalFileId';
          const files = searchResults.value.filter((c) =>
            selectedFilesFromStore.some((f) => f.key === c[idSelector])
          );
          selectedFiles.value = [...files];

          hasSearched.value = true;
        }
      };

      const viewFiles = (selectedFiles: KeyValueInfo[]) => {
        courtFileSearchStore.addFilesForViewing({
          searchCriteria: searchCriteria,
          searchResults: searchResults.value,
          files: selectedFiles,
        });
        const caseDetailUrl = `/${searchCriteria.isCriminal ? 'criminal-file' : 'civil-file'}/${selectedFiles[0].key}`;
        router.push(caseDetailUrl);
      };

      const addSelectedFile = (file: FileDetail) => {
        selectedFiles.value.push(file);
      };

      const removeSelectedFile = (idSelector: string, fileId: string) => {
        selectedFiles.value = selectedFiles.value.filter(
          (c) => c[idSelector] !== fileId
        );
      };

      const clearSelectedFiles = () => {
        selectedFiles.value = [];
        courtFileSearchStore.clearSelectedFiles();
      };

      return {
        errorText,
        errorCode,
        courtRooms,
        classes,
        searchResults,
        selectedFiles,
        isLookupDataMounted,
        isLookupDataReady,
        hasSearched,
        isSearching,
        isSearchResultsOver,
        classOptions,
        searchCriteria,
        errors,
        viewFiles,
        addSelectedFile,
        clearSelectedFiles,
        removeSelectedFile,
        handleDivisionChange,
        handleSubmit,
        handleReset,
      };
    },
  });
</script>

<style scoped lang="scss">
  @import '../../assets/_custom.scss';

  .card {
    border: white;
  }

  .btn-group.active {
    color: $blue;
  }

  .transparent {
    background-color: transparent;
  }

  .reset,
  .reset:hover,
  .reset:focus {
    text-decoration: none !important;
    color: $primary !important;
    box-shadow: none;
  }

  .search {
    border-bottom-right-radius: 0;
    border-top-right-radius: 0;
  }

  .search-input {
    border-radius: 0;
  }

  .search-btn {
    border-bottom-left-radius: 0;
    border-top-left-radius: 0;
  }
</style>
