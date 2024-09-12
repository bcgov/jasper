<template>
  <b-card no-body bg-variant="white">
    <b-card bg-variant="light" v-if="!isLookupDataMounted && !isLookupDataReady">
      <b-overlay :show="true">
        <b-card style="min-height: 100px;" />
        <template v-slot:overlay>
          <div>
            <loading-spinner />
            <p id="loading-label">Loading ...</p>
          </div>
        </template>
      </b-overlay>
    </b-card>
    <b-card bg-variant="light" v-else-if="isLookupDataMounted && !isLookupDataReady">
      <b-card style="min-height: 40px;">
        <span v-if="errorCode > 0">
          <span v-if="errorCode == 403"> You are not authorized to access this page. </span>
          <span v-else>
            Server is not responding. <b>({{ errorText }} "{{ errorCode }}")</b></span>
        </span>
        <span v-else> No Court File Search Found. </span>
      </b-card>
    </b-card>
    <b-card v-else>
      <b-navbar type="white" variant="white">
        <h2 class="mb-0">Court File Search</h2>
      </b-navbar>
      <b-row class="mb-2" body-class="py-0">
        <b-col md="8">
          <b-card v-if="isLookupDataMounted && isLookupDataReady" body-class="py-1">
            <b-form @submit.prevent="handleSubmit">
              <!-- Division -->
              <b-form-group label="Division:" label-cols="3" label-align="right">
                <b-button-group>
                  <b-button value="criminal" @click="handleDivisionChange"
                    :variant="searchCriteria.isCriminal ? 'primary' : 'outline-primary'">
                    Criminal
                  </b-button>
                  <b-button value="civil" @click="handleDivisionChange"
                    :variant="!searchCriteria.isCriminal ? 'primary' : 'outline-primary'">
                    Civil
                  </b-button>
                </b-button-group>
              </b-form-group>
              <!-- File Number or Party Name -->
              <b-form-group label="File Number or Party Name:" label-cols="3" label-align="right">
                <b-form-radio-group v-model="searchCriteria.selectedFileNoOrParty" name="file-radio-group">
                  <div class="radio-container p-2 rounded d-flex"
                    :class="{ 'bg-info': searchCriteria.selectedFileNoOrParty === 'file' }">
                    <b-form-radio class=" mt-2" value="file"> File Number </b-form-radio>
                    <b-row class="flex-grow-1" v-if="searchCriteria.selectedFileNoOrParty === 'file'">
                      <b-col md="5" class="text-right">
                        <b-form-input placeholder="e.g. 99999999" v-model="searchCriteria.fileNumber"></b-form-input>
                        <span class="text-danger" v-show="errors.isMissingFileNoOrParty">Field required</span>
                      </b-col>
                      <b-col md="6" offset-md="1" v-if="searchCriteria.isCriminal">
                        <b-card bg-variant="light" class="ml-1" body-class="p-2">
                          <b-form-group class="mb-0" label="Optional..." label-align="center">
                            <b-form-group label-cols="4" label="Prefix" label-for="prefix" label-align="right">
                              <b-form-input id="prefix" placeholder="AH" v-model="searchCriteria.prefix"></b-form-input>
                            </b-form-group>
                            <b-form-group label-cols="4" label="Seq Num" label-for="seqNum" label-align="right">
                              <b-form-input id="seqNum" placeholder="1" v-model="searchCriteria.seqNum"></b-form-input>
                            </b-form-group>
                            <b-form-group label-cols="4" label="Type Ref" label-for="typeRef" label-align="right">
                              <b-form-input id="typeRef" placeholder="B"
                                v-model="searchCriteria.typeRef"></b-form-input>
                            </b-form-group>
                          </b-form-group>
                        </b-card>
                      </b-col>
                    </b-row>
                  </div>
                  <div class="radio-container p-2 rounded d-flex"
                    :class="{ 'bg-info': searchCriteria.selectedFileNoOrParty === 'surname' }">
                    <b-form-radio class="mt-2" value="surname"> Surname </b-form-radio>
                    <b-row v-if="searchCriteria.selectedFileNoOrParty === 'surname'">
                      <b-col class="text-right">
                        <b-form-input v-model="searchCriteria.surname"></b-form-input>
                        <span class="text-danger" v-show="errors.isMissingSurname">Field required</span>
                      </b-col>
                      <b-col>
                        <b-form-input placeholder="Given Name" v-model="searchCriteria.givenName"></b-form-input>
                      </b-col>
                    </b-row>
                  </div>
                  <div class="radio-container p-2 rounded d-flex"
                    :class="{ 'bg-info': searchCriteria.selectedFileNoOrParty === 'org' }">
                    <b-form-radio class="mt-2" value="org"> Organisation </b-form-radio>
                    <b-row v-if="searchCriteria.selectedFileNoOrParty === 'org'">
                      <b-col class="text-right">
                        <b-form-input placeholder="e.g. MegaCorp Inc." v-model="searchCriteria.org"></b-form-input>
                        <span class="text-danger" v-show="errors.isMissingOrg">Field required</span>
                      </b-col>
                    </b-row>
                  </div>
                </b-form-radio-group>
              </b-form-group>
              <b-form-group label="Class:" label-cols="3" label-align="right">
                <b-row>
                  <b-col cols="3">
                    <b-form-select v-model="searchCriteria.class">
                      <option value=""></option>
                      <option v-for="option in classOptions" :key="option.code" :value="option.code">
                        {{ option.shortDesc }}
                      </option>
                    </b-form-select>
                  </b-col>
                </b-row>
              </b-form-group>
              <!-- location -->
              <b-form-group label="Location:" label-cols="3" label-align="right">
                <b-row>
                  <b-col cols="5">
                    <b-form-select v-model="searchCriteria.location" :options="courtRooms"></b-form-select>
                  </b-col>
                </b-row>
              </b-form-group>
              <b-row>
                <b-col offset-md="3">
                  <b-button variant="primary" type="submit" :disabled="isSearching">
                    <b-icon icon="search"></b-icon>
                    Search
                  </b-button>
                  <b-button variant="outline-primary" class="ml-3" type="button" @click="() => handleReset(true)">
                    Reset Search
                  </b-button>
                </b-col>
              </b-row>
            </b-form>
          </b-card>
        </b-col>
      </b-row>
      <court-file-search-result v-if="isSearching || hasSearched" :isLookupDataMounted="isLookupDataMounted"
        :isLookupDataReady="isLookupDataReady" :courtRooms="courtRooms" :classes="classes"
        :isCriminal="searchCriteria.isCriminal" :searchResults="searchResults" :isSearching="isSearching">
      </court-file-search-result>
    </b-card>
  </b-card>
</template>
<script lang="ts">
import { CourtRoomsJsonInfoType, LookupCode } from "@/types/common";
import { CourtFileSearchCriteria, FileDetail, SearchModeEnum } from "@/types/courtFileSearch";
import { roomsInfoType } from "@/types/courtlist";
import CourtFileSearchResult from "@components/courtfilesearch/CourtFileSearchResult.vue";
import { Component, Vue } from "vue-property-decorator";

const CRIMINAL_CODE = "R";
const CIVIL_CODE = ["I", "F"];

@Component({
  components: {
    CourtFileSearchResult
  }
})
export default class CourtFileSearchView extends Vue {
  errorCode = 0;
  errorText = "";
  courtRooms: roomsInfoType[] = [];
  classes: LookupCode[] = [];
  searchResults: FileDetail[] = [];

  isLookupDataMounted = false;
  isLookupDataReady = false;
  hasSearched = false;
  isSearching = false;
  defaultLocation = this.$store.state.CommonInformation.userInfo.agencyCode;

  classOptions: LookupCode[] = [];

  searchCriteria: CourtFileSearchCriteria = {
    isCriminal: true,
    selectedFileNoOrParty: 'file',
    location: this.defaultLocation,
  };

  errors = {
    isMissingFileNoOrParty: false,
    isMissingSurname: false,
    isMissingOrg: false
  };

  async mounted() {
    this.loadLookups();
  }

  public async loadLookups(): Promise<void> {
    try {
      const [courtRooms, courtClasses] = await Promise.all([
        this.$locationService.getCourtRooms(),
        this.$lookupService.getCourtClasses(),
      ]);

      this.courtRooms = courtRooms;
      this.classes = courtClasses;
      this.loadClasses();
      this.isLookupDataReady = true;
    } catch (err) {
      this.errorCode = err.status;
      this.errorText = err.statusText;
      if (this.errorCode != 401) {
        this.$bvToast.toast(`Error - ${this.errorCode} - ${this.errorText}`, {
          title: "An error has occured.",
          variant: "danger",
          autoHideDelay: 10000,
        });
      }
      console.log(this.errorCode);
    } finally {
      this.isLookupDataMounted = true;
    }
  }

  public handleDivisionChange() {
    this.searchCriteria.isCriminal = !this.searchCriteria.isCriminal;
    this.handleReset();
  }

  public async handleSubmit() {
    this.isSearching = true;
    this.sanitizeTextInputs();
    this.resetErrors();

    this.errors.isMissingFileNoOrParty = this.searchCriteria.selectedFileNoOrParty === 'file' && !this.searchCriteria.fileNumber;
    this.errors.isMissingSurname = this.searchCriteria.selectedFileNoOrParty === 'surname' && !this.searchCriteria.surname;
    this.errors.isMissingOrg = this.searchCriteria.selectedFileNoOrParty === 'org' && !this.searchCriteria.org;

    // Don't proceed if any of the validation flag is set to true
    const hasNoErrors = Object.values(this.errors).every(value => value === false);
    if (!hasNoErrors) {
      return;
    }

    const queryParams = this.buildQueryParams();

    this.searchResults = this.searchCriteria.isCriminal
      ? (await this.$filesService.searchCriminalFiles(queryParams)).fileDetail
      : (await this.$filesService.searchCivilFiles(queryParams)).fileDetail;

    this.hasSearched = true;
    this.isSearching = false;
  }

  public handleReset(resetDivision = false): void {
    this.searchCriteria.isCriminal = resetDivision ? true : this.searchCriteria.isCriminal;
    this.searchCriteria.location = this.defaultLocation;
    this.searchCriteria.selectedFileNoOrParty = 'file';
    this.searchCriteria.fileNumber = undefined;
    this.searchCriteria.surname = undefined;
    this.searchCriteria.givenName = undefined;
    this.searchCriteria.org = undefined;
    this.searchCriteria.class = undefined

    if (this.searchCriteria.isCriminal) {
      this.searchCriteria.prefix = undefined;
      this.searchCriteria.seqNum = undefined;
      this.searchCriteria.typeRef = undefined;
    }

    this.loadClasses();
    this.resetErrors();

    this.searchResults = [];
    this.hasSearched = false;
  }

  sanitizeTextInputs(): void {
    this.searchCriteria.fileNumber = this.searchCriteria.fileNumber?.trim();
    this.searchCriteria.prefix = this.searchCriteria.prefix?.trim();
    this.searchCriteria.seqNum = this.searchCriteria.seqNum?.trim();
    this.searchCriteria.typeRef = this.searchCriteria.typeRef?.trim();

    this.searchCriteria.surname = this.searchCriteria.surname?.trim();
    this.searchCriteria.givenName = this.searchCriteria.givenName?.trim();

    this.searchCriteria.org = this.searchCriteria.org?.trim();
  }

  resetErrors(): void {
    this.errors.isMissingFileNoOrParty = false;
    this.errors.isMissingSurname = false;
    this.errors.isMissingOrg = false;
  }

  loadCourtRooms(courtRooms: CourtRoomsJsonInfoType[]): void {
    const sortedCourtRooms = courtRooms
      .sort((a, b) => a.name.toLocaleLowerCase().localeCompare(b.name.toLowerCase()))

    sortedCourtRooms.map(cr => {
      this.courtRooms.push({
        text: cr.name,
        value: cr.code
      })
    });
  }

  loadClasses(): void {
    this.classOptions = this.searchCriteria.isCriminal
      ? this.classes.filter(c => c.longDesc === CRIMINAL_CODE)
      : this.classes.filter(c => CIVIL_CODE.includes(c.longDesc));
  }

  buildQueryParams(): string {
    const queryParams: string[] = [];

    if (this.searchCriteria.selectedFileNoOrParty === 'file') {
      queryParams.push(`searchMode=${SearchModeEnum.FileNo}`)
      queryParams.push(`fileNumberTxt=${this.searchCriteria.fileNumber}`)

      if (this.searchCriteria.prefix) {
        queryParams.push(`filePrefixTxt=${this.searchCriteria.prefix}`)
      }
      if (this.searchCriteria.seqNum) {
        queryParams.push(`fileSuffixNo=${this.searchCriteria.seqNum}`)
      }
      if (this.searchCriteria.typeRef) {
        queryParams.push(`mdocRefTypeCd=${this.searchCriteria.typeRef}`)
      }
    }
    else if (this.searchCriteria.selectedFileNoOrParty === 'surname') {
      queryParams.push(`searchMode=${SearchModeEnum.PartName}`)
      queryParams.push(`lastNm=${this.searchCriteria.surname}`);

      if (this.searchCriteria.givenName) {
        queryParams.push(`givenNm=${this.searchCriteria.givenName}`);
      }
    }
    else {
      queryParams.push(`searchMode=${SearchModeEnum.PartName}`)
      queryParams.push(`orgNm=${this.searchCriteria.org}`);
    }

    if (this.searchCriteria.class) {
      queryParams.push(`courtClassCd=${this.searchCriteria.class}`);
    }

    queryParams.push(`fileHomeAgencyId=${this.searchCriteria.location}`);

    return queryParams.join("&");
  }
}
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
</style>