<template>
  <b-card bg-variant="light">
    <b-card bg-variant="light" v-if="isSearching">
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
    <div v-if="!isSearching">
      <!-- Navbar -->
      <b-card no-body class="mb-3">
        <b-navbar type="white" variant="white">
          <b-navbar-nav type="white" variant="white">
            <b-nav-text class="text-primary">
              <h2 class="mb-0">Search Results</h2>
            </b-nav-text>
          </b-navbar-nav>
        </b-navbar>
      </b-card>
      <!-- Data -->
      <b-card bg-variant="white" no-body v-if="courtRooms.length > 0">
        <b-table class="p-3 mb-0" :fields="filteredFields" :items="searchResults" borderless small responsive="md"
          sort-icon-left
          empty-text="No cases matching the filter criteria were found. Please check to ensure the search criteria is correct."
          show-empty>
          <template #head(nextApprDt)="data">
            <span class="text-danger no-wrap">{{ data.label }}</span>
          </template>
          <template v-slot:cell(location)="data">
            {{ getLocation(data.item.fileHomeAgencyId) }}
          </template>
          <template v-slot:cell(courtClassCd)="data">
            <span :class="getClassColor(data.item.courtClassCd)">
              {{ getClass(data.item.courtClassCd) }}
            </span>
          </template>
          <template v-slot:cell(warrantYN)="data">
            <b-badge v-if="data.item.warrantYN === 'Y'" variant="primary text-light" :style="data.field.cellStyle">
              <span>W</span>
            </b-badge>
          </template>
          <template v-slot:cell(inCustodyYN)="data">
            <b-badge v-if="data.item.inCustodyYN === 'Y'" variant="primary text-light" :style="data.field.cellStyle">
              IC
            </b-badge>
          </template>
          <template v-slot:cell(nextApprDt)="data">
            <span>
              {{ data.item.nextApprDt | beautify_date }}
            </span>
          </template>
          <template v-slot:cell(action)="data">
            <div class="d-flex justify-content-end no-wrap">
              <b-button variant="outline-primary" class="mr-3 py-0">Add File</b-button>
              <b-button variant="primary" @click="() => handleCaseClick(data.item.mdocJustinNo)">Add File and
                View</b-button>
            </div>
          </template>
        </b-table>
      </b-card>
    </div>
  </b-card>
</template>
<script lang="ts">
import { LookupCode } from "@/types/common";
import { CourtClassEnum, FileDetail } from '@/types/courtFileSearch';
import { roomsInfoType } from "@/types/courtlist";
import { Component, Prop, Vue } from "vue-property-decorator";

@Component
export default class CourtFileSearchResult extends Vue {
  @Prop({ type: Array, default: () => [] })
  courtRooms!: roomsInfoType[];

  @Prop({ type: Array, default: () => [] })
  searchResults!: FileDetail[];

  @Prop({ type: Array, default: () => [] })
  classes!: LookupCode[];

  @Prop({ type: Boolean, default: () => false })
  isSearching;

  @Prop({ type: Boolean, default: () => true })
  isCriminal;

  allFields = [
    {
      key: "location",
      label: "Location",
      tdClass: "border-top",
      sortable: true
    },
    {
      key: "courtClassCd",
      label: "Class",
      tdClass: "border-top",
      sortable: true
    },
    {
      key: "fileNumberTxt",
      label: "File Number",
      tdClass: "border-top",
      sortable: true,
      sortByFormatted: true,
      formatter: (value, key, item) => {
        return `${item.ticketSeriesTxt ?? ""}${item.fileNumberTxt}${item.mdocSeqNo ? "-" + item.mdocSeqNo : ""}${item.mdocRefTypeCd ? "-" + item.mdocRefTypeCd : ""}`;
      }
    },
    {
      key: "participant",
      label: "Participant",
      tdClass: "border-top max-width-300",
      sortable: true,
      sortByFormatted: true,
      formatter: (value, key, item) => {
        return item.participant.map(p => p.fullNm).join("; ");
      }
    },
    {
      key: "charges",
      label: "Charges",
      tdClass: "border-top max-width-300",
      sortable: true,
      sortByFormatted: true,
      formatter: (value, key, item) => {
        return item.participant.flatMap(p => p.charge).map(c => c.sectionTxt).join("; ");
      }
    },
    {
      key: "warrantyYN",
      label: "OW",
      tdClass: "border-top",
      sortable: true
    },
    {
      key: "inCustodyYN",
      label: "IC",
      tdClass: "border-top",
      sortable: true
    },
    {
      key: "nextApprDt",
      label: "Next Appearance",
      tdClass: "border-top",
      sortable: true
    },
    {
      key: "action",
      label: "",
      tdClass: "border-top"
    },
  ];

  get filteredFields() {
    return this.isCriminal
      ? this.allFields
      : this.allFields.filter(f => !['charges', 'warrantyYN', 'inCustodyYN'].includes(f.key));
  }

  getLocation(fileHomeAgencyId: string) {
    return this.courtRooms.find(r => r.value === fileHomeAgencyId)?.text || '';
  }

  getClass(courtClassCd: string) {
    return this.classes.find(l => l.code === courtClassCd)?.shortDesc || '';
  }

  getClassColor(courtClassCd: string) {
    const classValue = CourtClassEnum[courtClassCd];

    switch (classValue) {
      case CourtClassEnum.A:
      case CourtClassEnum.Y:
        return 'text-blue';

      case CourtClassEnum.F:
        return 'text-green';

      case CourtClassEnum.C:
        return 'text-purple';
      default:
        return "";
    }
  }

  public handleCaseClick(mdocJustinNo: string) {
    console.log(mdocJustinNo);
  }
}
</script>

<style scoped lang="scss">
@import '../../assets/_custom.scss';

.card {
  border: white;
}

.text-blue {
  color: $blue-100;
}

.text-green {
  color: $green;
}

.text-purple {
  color: $purple;
}

table thead tr th {
  color: $green;
}

.no-wrap {
  white-space: nowrap;
}
</style>