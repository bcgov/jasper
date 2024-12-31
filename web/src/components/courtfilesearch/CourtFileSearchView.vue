<template>
  <!-- todo: Extract this out to more generic location -->
  <v-overlay :opacity="0.333" v-model="isLoading" />
  <b-card
    body-class="px-0"
    bg-variant="light"
    v-if="isLookupDataMounted && !isLookupDataReady"
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
  <!------------------------------------------------------->
  <v-banner style="background-color: #62d3a4; color: #183a4a">
    <v-row class="my-3">
      <v-col>
        <h3>Court file search</h3>
      </v-col>
    </v-row>
  </v-banner>
  <v-container>
    <v-form
      class="search-form"
      v-if="isLookupDataMounted && isLookupDataReady"
      @submit.prevent="handleSubmit"
    >
      <v-row>
        <v-col cols="2">
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
        <v-col cols="3">
          <v-text-field
            v-if="searchCriteria.searchBy === 'fileNumber'"
            label="File number"
            placeholder="i.e 256344-1"
            v-model="searchCriteria.fileNumberTxt"
            :error-messages="
              errors.isMissingFileNo ? ['File number is required'] : []
            "
          />
          <v-text-field
            v-else-if="searchCriteria.searchBy === 'lastName'"
            label="Surname"
            placeholder="Smith"
            v-model="searchCriteria.lastName"
            :error-messages="
              errors.isMissingSurname ? ['Surname is required'] : []
            "
          />
          <v-text-field
            v-else-if="searchCriteria.searchBy === 'orgName'"
            label="Organization"
            placeholder="i.e. Steele Trading"
            v-model="searchCriteria.orgName"
            :error-messages="
              errors.isMissingOrg ? ['Organization is required'] : []
            "
          />
        </v-col>
        <v-col cols="2">
          <v-select
            v-model="searchCriteria.fileHomeAgencyId"
            :items="courtRooms"
            item-title="text"
            label="Location"
          />
        </v-col>
        <v-col cols="2">
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
        <v-col>
          <action-buttons @reset="handleReset(true)" />
        </v-col>
      </v-row>
      <v-row>
        <v-col cols="2">
          <v-select
            label="Class"
            :items="classOptions"
            item-title="shortDesc"
            item-value="code"
            v-model="searchCriteria.class"
          />
        </v-col>
        <v-col
          cols="3"
          v-if="showClassDropdown && searchCriteria.searchBy === 'lastName'"
        >
          <v-text-field
            id="givenName"
            label="Given Name"
            placeholder="e.g. John"
            v-model="searchCriteria.givenName"
          />
        </v-col>
        <template
          v-else-if="
            showClassDropdown && searchCriteria.searchBy === 'fileNumber'
          "
        >
          <v-col cols="1">
            <v-text-field
              label="Prefix"
              placeholder="AH"
              density="comfortable"
              :rounded="false"
              v-model="searchCriteria.filePrefixTxt"
            />
          </v-col>
          <v-col cols="1">
            <v-text-field
              label="Seq #"
              placeholder="1"
              density="comfortable"
              :rounded="false"
              v-model="searchCriteria.fileSuffixNo"
            />
          </v-col>
          <v-col cols="1">
            <v-text-field
              label="Type Ref"
              placeholder="8"
              density="comfortable"
              :rounded="false"
              v-model="searchCriteria.mDocRefTypeCode"
            />
          </v-col>
        </template>
      </v-row>
    </v-form>
    <court-file-search-result
      class="mb-5 mt-10"
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
  </v-container>
</template>

<script setup lang="ts">
  import CourtFileSearchResult from '@/components/courtfilesearch/CourtFileSearchResult.vue';
  import ActionButtons from '@/components/shared/Form/ActionButtons.vue';
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
  import { computed, inject, onMounted, reactive, ref } from 'vue';
  import { useRouter } from 'vue-router';

  const CRIMINAL_CODE = 'R';
  const SMALL_CLAIMS_CODE = 'I';
  const SEARCH_RESULT_LIMIT = 100;

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
  const isSearchResultsOver = ref(false);
  const isLoading = ref(true);
  const defaultLocation = commonStore.userInfo!.agencyCode;
  const divisions = [
    { value: 'isCriminal', text: 'Criminal' },
    { value: 'isFamily', text: 'Family' },
    { value: 'isSmallClaims', text: 'Small Claims' },
  ];
  const errorCode = ref('');
  const errorText = ref('');
  const filtersEnabled = ref(false);
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
    isLoading.value = false;
  });

  const toggleFilters = () => {
    filtersEnabled.value = !filtersEnabled.value;
  };

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

    handleReset();
  };

  const handleSubmit = async () => {
    courtFileSearchStore.clearSelectedFiles();

    sanitizeTextInputs();
    resetErrors();

    errors.isMissingFileNo =
      searchCriteria.searchBy === 'fileNumber' && !searchCriteria.fileNumberTxt;
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

  const handleReset = (resetDivision = false) => {
    courtFileSearchStore.clearSelectedFiles();
    searchCriteria.isCriminal = resetDivision
      ? true
      : searchCriteria.isCriminal;
    searchCriteria.isFamily = resetDivision ? false : searchCriteria.isFamily;
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

  const selectedDivision = ref('isCriminal');

  const showClassDropdown = computed(() => {
    return selectedDivision.value === 'isCriminal';
  });
</script>
