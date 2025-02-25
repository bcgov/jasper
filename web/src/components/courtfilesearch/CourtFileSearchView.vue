<template>
  <!-- todo: Extract this out to more generic location -->
  <v-overlay :opacity="0.333" v-model="isLoading" />

  <v-card style="min-height: 40px" v-if="errorCode > 0 && errorCode == '403'">
    <span> You are not authorized to access this page. </span>
  </v-card>
  <!------------------------------------------------------->
  <banner title="Court file search" color="#183a4a" bgColor="#62d3a4" />
  <v-container>
    <v-form
      class="search-form"
      v-if="isLookupDataMounted && isLookupDataReady"
      @submit.prevent="handleSubmit"
    >
      <v-row>
        <v-col cols="2" style="padding-right: 0">
          <v-select
            v-model="searchCriteria.searchBy"
            :items="[
              { text: 'File number', value: 'fileNumber' },
              { text: 'Last name', value: 'lastName' },
              { text: 'Organization', value: 'orgName' },
            ]"
            label="Search by"
            item-title="text"
            item-value="value"
            :clearable="false"
          />
        </v-col>
        <v-col cols="2">
          <v-text-field
            v-model="searchByCriteria"
            :label="searchLabel"
            :placeholder="searchPlaceholder"
            :error-messages="searchErrorMessages"
          />
        </v-col>
        <template v-if="searchCriteria.searchBy === 'fileNumber'">
          <v-col cols="1">
            <v-text-field
              label="Prefix"
              placeholder="AH"
              :rounded="false"
              v-model="searchCriteria.filePrefixTxt"
            />
          </v-col>
          <v-col cols="1">
            <v-text-field
              label="Seq #"
              placeholder="1"
              :rounded="false"
              v-model="searchCriteria.fileSuffixNo"
            />
          </v-col>
          <v-col cols="1" style="margin-top: 0.1rem">
            <v-text-field
              label="Type Ref"
              placeholder="8"
              :rounded="false"
              v-model="searchCriteria.mDocRefTypeCode"
            />
          </v-col>
        </template>
        <v-col v-else-if="searchCriteria.searchBy === 'lastName'">
          <v-text-field
            id="givenName"
            label="Given Name"
            placeholder="e.g. John"
            :rounded="false"
            v-model="searchCriteria.givenName"
          />
        </v-col>
        <v-col :cols="searchCriteria.searchBy === 'orgName' ? 3 : 2">
          <v-select
            v-model="searchCriteria.fileHomeAgencyId"
            :items="courtRooms"
            item-title="name"
            item-value="code"
            label="Location"
            :required="true"
            :error-messages="
              errors.isMissingLocation ? ['Location is required'] : []
            "
          />
        </v-col>
        <v-col :cols="searchCriteria.searchBy === 'orgName' ? 2 : ''">
          <v-select
            v-model="selectedDivision"
            label="Division"
            :items="divisions"
            :clearable="false"
            item-title="text"
            required
            @update:modelValue="handleDivisionChange"
          />
        </v-col>
        <v-col v-if="searchCriteria.isCriminal">
          <v-select
            label="Class"
            :items="classOptions"
            item-title="shortDesc"
            item-value="code"
            v-model="searchCriteria.class"
          />
        </v-col>
      </v-row>
      <v-row no-gutters>
        <v-col>
          <action-buttons @reset="handleReset" />
        </v-col>
      </v-row>
    </v-form>
    <court-file-search-result
      v-model="selectedFiles"
      class="mb-5 mt-5"
      :isLookupDataMounted="isLookupDataMounted"
      :isLookupDataReady="isLookupDataReady"
      :courtRooms="courtRooms"
      :classes="classes"
      :isCriminal="searchCriteria.isCriminal"
      :searchResults="searchResults"
      :isSearching="isSearching"
      @files-viewed="viewFiles"
      @add-selected="addSelectedFile"
      @remove-selected="removeSelectedFile"
      @clear-selected="clearSelectedFiles"
      :isSearchResultsOver="isSearchResultsOver"
    >
    </court-file-search-result>
  </v-container>
</template>

<script setup lang="ts">
  import CourtFileSearchResult from '@/components/courtfilesearch/CourtFileSearchResult.vue';
import { FilesService } from '@/services/FilesService';
import { LocationService } from '@/services/LocationService';
import { LookupService } from '@/services/LookupService';
import { useCourtFileSearchStore } from '@/stores';
import { KeyValueInfo, LookupCode } from '@/types/common';
import {
  CourtClassEnum,
  CourtFileSearchCriteria,
  FileDetail,
  SearchModeEnum,
} from '@/types/courtFileSearch';
import { roomsInfoType } from '@/types/courtlist';
import { computed, inject, onMounted, reactive, ref } from 'vue';

  const CRIMINAL_CODE = 'R';
  const SMALL_CLAIMS_CODE = 'I';
  const SEARCH_RESULT_LIMIT = 100;

  const courtFileSearchStore = useCourtFileSearchStore();
  const defaultLocation = '';
  let searchCriteria: CourtFileSearchCriteria = reactive({
    isCriminal: true,
    isFamily: false,
    isSmallClaims: false,
    searchBy: 'fileNumber',
    fileHomeAgencyId: defaultLocation,
  });
  const classOptions = ref<LookupCode[]>([]);
  const selectedFiles = ref<FileDetail[]>([]);
  const searchResults = ref<FileDetail[]>([]);
  const classes = ref<LookupCode[]>([]);
  const courtRooms = ref<roomsInfoType[]>([]);
  const selectedDivision = ref('isCriminal');
  const isLookupDataReady = ref(false);
  const isLookupDataMounted = ref(false);
  const isSearching = ref(false);
  const isSearchResultsOver = ref(false);
  const isLoading = ref(true);

  const divisions = [
    { value: 'isCriminal', text: 'Criminal' },
    { value: 'isFamily', text: 'Family' },
    { value: 'isSmallClaims', text: 'Small Claims' },
  ];
  const errorCode = ref('');
  const errors = reactive({
    isMissingFileNo: false,
    isMissingSurname: false,
    isMissingOrg: false,
    isMissingLocation: false,
  });

  const locationService = inject<LocationService>('locationService');
  const lookupService = inject<LookupService>('lookupService');
  const filesService = inject<FilesService>('filesService');

  if (!locationService || !lookupService || !filesService) {
    throw new Error('Services is undefined.');
  }

  onMounted(async () => {
    // We want to clear the data-table on page load
    courtFileSearchStore.clearSelectedFiles();
    await loadData();
    isLoading.value = false;
  });

  const searchLabel = computed(
    () =>
      ({
        fileNumber: 'File number',
        lastName: 'Surname',
        orgName: 'Organization',
      })[searchCriteria.searchBy]
  );

  const searchPlaceholder = computed(
    () =>
      ({
        fileNumber: 'i.e 256344-1',
        lastName: 'Smith',
        orgName: 'i.e. Steele Trading',
      })[searchCriteria.searchBy]
  );

  const searchErrorMessages = computed(
    () =>
      ({
        fileNumber: errors.isMissingFileNo ? ['File number is required'] : [],
        lastName: errors.isMissingSurname ? ['Surname is required'] : [],
        orgName: errors.isMissingOrg ? ['Organization is required'] : [],
      })[searchCriteria.searchBy]
  );

  const searchByCriteria = computed({
    get() {
      return {
        fileNumber: searchCriteria.fileNumberTxt,
        lastName: searchCriteria.lastName,
        orgName: searchCriteria.orgName,
      }[searchCriteria.searchBy];
    },
    set(value) {
      if (searchCriteria.searchBy === 'fileNumber') {
        searchCriteria.fileNumberTxt = value;
      } else if (searchCriteria.searchBy === 'lastName') {
        searchCriteria.lastName = value;
      } else {
        searchCriteria.orgName = value;
      }
    },
  });

  const loadData = async (): Promise<void> => {
    try {
      const [courtRoomsResp, courtClassesResp] = await Promise.all([
        locationService.getLocations(),
        lookupService.getCourtClasses(),
      ]);

      courtRooms.value = courtRoomsResp;
      classes.value = courtClassesResp;
      loadDataFromState();
      loadClasses();
      isLookupDataReady.value = true;
    } catch (err: unknown) {
      console.error(err);
    } finally {
      isLookupDataMounted.value = true;
    }
  };

  const handleDivisionChange = () => {
    Object.assign(searchCriteria, {
      isCriminal: false,
      isFamily: false,
      isSmallClaims: false,
      [selectedDivision.value]: true,
    });
  };

  const handleSubmit = async () => {
    sanitizeTextInputs();
    resetErrors();

    errors.isMissingFileNo =
      searchCriteria.searchBy === 'fileNumber' && !searchCriteria.fileNumberTxt;
    errors.isMissingLocation =
      searchCriteria.searchBy === 'fileNumber' &&
      !searchCriteria.fileHomeAgencyId;
    errors.isMissingSurname =
      searchCriteria.searchBy === 'lastName' && !searchCriteria.lastName;
    errors.isMissingOrg =
      searchCriteria.searchBy === 'orgName' && !searchCriteria.orgName;

    // Don't proceed if any of the validation flag is set to true
    const hasNoErrors = Object.values(errors).every((value) => value === false);
    if (!hasNoErrors) {
      return;
    }

    try {
      isSearching.value = true;
      searchResults.value.length = 0;
      isSearchResultsOver.value = false;
      courtFileSearchStore.clearSelectedFiles();
      selectedFiles.value = [];

      const { recCount, fileDetail } = searchCriteria.isCriminal
        ? await filesService.searchCriminalFiles(buildQueryParams())
        : await filesService.searchCivilFiles(buildQueryParams());

      // Make sure that to only show up to 100 results
      isSearchResultsOver.value = recCount > SEARCH_RESULT_LIMIT;
      searchResults.value = [...fileDetail.slice(0, SEARCH_RESULT_LIMIT)];
    } catch (err: unknown) {
      console.error(err);
    } finally {
      isSearching.value = false;
    }
  };

  const handleReset = () => {
    selectedDivision.value = 'isCriminal';
    courtFileSearchStore.clearSelectedFiles();
    selectedFiles.value = [];
    searchCriteria.isCriminal = true;
    searchCriteria.isFamily = false;
    searchCriteria.isSmallClaims = false;
    searchCriteria.fileHomeAgencyId = defaultLocation;
    searchCriteria.searchBy = 'fileNumber';
    searchCriteria.fileNumberTxt = undefined;
    searchCriteria.lastName = undefined;
    searchCriteria.givenName = undefined;
    searchCriteria.orgName = undefined;
    searchCriteria.class = undefined;
    searchCriteria.filePrefixTxt = undefined;
    searchCriteria.fileSuffixNo = undefined;
    searchCriteria.mDocRefTypeCode = undefined;

    loadClasses();
    resetErrors();

    searchResults.value.length = 0;
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
    errors.isMissingLocation = false;
  };

  const loadClasses = () => {
    searchCriteria.class = '';
    if (selectedDivision.value === 'isCriminal') {
      classOptions.value = classes.value.filter(
        (c) => c.longDesc === CRIMINAL_CODE
      );
    } else if (selectedDivision.value === 'isSmallClaims') {
      classOptions.value = classes.value.filter(
        (c) =>
          c.longDesc === SMALL_CLAIMS_CODE &&
          c.code !== CourtClassEnum[CourtClassEnum.C]
      );
    } else {
      classOptions.value = [];
    }
  };

  const buildQueryParams = () => {
    const queryParams = buildSearchByQueryParams();
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
      if (searchCriteria.isCriminal) {
        queryParams['fileNumberTxt'] = searchCriteria.fileNumberTxt;
      } else {
        queryParams['fileNumber'] = searchCriteria.fileNumberTxt;
      }
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
      searchResults.value = [...results];

      const idSelector = searchCriteria.isCriminal
        ? 'mdocJustinNo'
        : 'physicalFileId';
      const files = searchResults.value.filter((c) =>
        selectedFilesFromStore.some((f) => f.key === c[idSelector])
      );
      selectedFiles.value = [...files];
    }
  };

  const viewFiles = (selectedFiles: KeyValueInfo[]) => {
    courtFileSearchStore.addFilesForViewing({
      searchCriteria: searchCriteria,
      searchResults: searchResults.value,
      files: selectedFiles,
    });
    const caseDetailUrl = `/${searchCriteria.isCriminal ? 'criminal-file' : 'civil-file'}/${selectedFiles[0].key}`;
    window.open(caseDetailUrl);
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
</script>
