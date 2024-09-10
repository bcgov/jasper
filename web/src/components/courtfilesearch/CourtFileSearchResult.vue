<template>
  <b-card body-class="pb-0" bg-variant="light">
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
      <b-card no-body>
        <b-navbar type="white" variant="white">
          <b-navbar-nav type="white" variant="white">
            <b-nav-text class="text-primary pt-0">
              <h2>Found {{ items.length }} Similar Cases</h2>
            </b-nav-text>
          </b-navbar-nav>
          <b-navbar-nav class="d-flex align-items-center ml-auto">
            <b-form-group label="Start Date:" class="mr-3 mb-0">
              <b-input-group>
                <b-form-input type="text" placeholder="YYYY-MM-DD"></b-form-input>
                <b-input-group-append>
                  <b-form-datepicker button-only right locale="en-US"></b-form-datepicker>
                </b-input-group-append>
              </b-input-group>
            </b-form-group>
            <b-form-group label="End Date:" class="mr-3 mb-0">
              <b-input-group>
                <b-form-input type="text" placeholder="YYYY-MM-DD"></b-form-input>
                <b-input-group-append>
                  <b-form-datepicker button-only right locale="en-US"></b-form-datepicker>
                </b-input-group-append>
              </b-input-group>
            </b-form-group>
            <b-button variant="primary" type="button" class="align-self-end">
              Search
            </b-button>
          </b-navbar-nav>
        </b-navbar>
      </b-card>
      <b-card class="pl-3" no-body>
        <b-card-text><small>Choose or refine your search</small></b-card-text>
      </b-card>
      <!-- Data -->
      <b-card bg-variant="white" class="mb-3" no-body v-if="courtRooms.length > 0">
        <b-table class="p-3" :fields="fields" :items="items" borderless small responsive="sm">
          <template v-for="(field, index) in fields" v-slot:[`head(${field.key})`]="data">
            <b v-bind:key="index" :class="field.headerStyle"> {{ data.label }}</b>
          </template>

          <template v-slot:cell(fileNumber)="data">
            <span v-if="data.item.ticketSeriesTxt">{{ data.item.ticketSeriesTxt }}</span>
            <span>{{ data.item.fileNumberTxt }}</span>
            <span v-if="data.item.mdocSeqNo">{{ `-${data.item.mdocSeqNo}` }}</span>
            <span v-if="data.item.mdocRefTypeCd">{{ `-${data.item.mdocRefTypeCd}` }}</span>
            <span v-if="data.item.sealStatusCd === 'SD'" class='text-danger'>
              <br />(sealed)
            </span>
          </template>
          <template v-slot:cell(accused)="data">
            <span :style="data.field.cellStyle">
              {{ data.item.participant.map(p => p.fullNm).join("; ") }}
            </span>
          </template>
          <template v-slot:cell(location)="data">
            {{ getLocation(data.item.fileHomeAgencyId) }}
          </template>
          <template v-slot:cell(level)="data">
            {{ getLevel(data.item.courtLevelCd) }}
          </template>
          <template v-slot:cell(class)="data">
            {{ getClass(data.item.courtClassCd) }}
          </template>
          <template v-slot:cell(action)="data">
            <b-button variant="primary" @click="() => handleCaseClick(data.item.mdocJustinNo)">View</b-button>
          </template>
        </b-table>
      </b-card>
    </div>
  </b-card>
</template>
<script lang="ts">
import { Component, Vue, Prop } from "vue-property-decorator";
import { roomsInfoType } from "@/types/courtlist";
import { LookupCode } from "@/types/common";

@Component
export default class CourtFileSearchResult extends Vue {
  @Prop({ type: Array, default: () => [] })
  courtRooms!: roomsInfoType[];

  @Prop({ type: Array, default: () => [] })
  levels!: LookupCode[];

  @Prop({ type: Array, default: () => [] })
  classes!: LookupCode[];

  @Prop({ type: Boolean, default: () => false })
  isSearching;

  isCriminal = true;
  fields = [
    {
      key: "fileNumber",
      label: "File Number",
      tdClass: "border-top"
    },
    {
      key: "accused",
      label: this.isCriminal ? "Accused" : "Party",
      tdClass: "border-top max-width-300"
    },
    {
      key: "location",
      label: "Location",
      tdClass: "border-top"
    },
    {
      key: "level",
      label: "Level",
      tdClass: "border-top"
    },
    {
      key: "class",
      label: "Classification",
      tdClass: "border-top"
    },
    {
      key: "action",
      label: "",
      tdClass: "border-top text-right"
    },
  ];

  items = [
    {
      "mdocJustinNo": "196",
      "physicalFileId": "834.0026",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "98012201",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "1998-01-22 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Franklin",
          "charge": [
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "252",
      "physicalFileId": "934.0026",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "123456",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "Y",
      "nextApprDt": "2017-08-09 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "smith, john",
          "charge": [
            {
              "sectionTxt": "CDS - 5",
              "sectionDscTxt": "Trafficking in Controlled Substance"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "267",
      "physicalFileId": "1021.0026",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "980202",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "S",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": null,
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smithers, Clint",
          "charge": [
            {
              "sectionTxt": "CCC - 380",
              "sectionDscTxt": "Fraud"
            },
            {
              "sectionTxt": "CCC - 334",
              "sectionDscTxt": "Theft (5,000)"
            },
            {
              "sectionTxt": "CCC - 344",
              "sectionDscTxt": "Robbery"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "311",
      "physicalFileId": "1096.0026",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "980209",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "S",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2016-08-16 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Jones, Henry",
          "charge": [
            {
              "sectionTxt": "CCC - 367",
              "sectionDscTxt": "Forgery"
            }
          ]
        },
        {
          "fullNm": "Smith, Brian",
          "charge": [
            {
              "sectionTxt": "CCC - 367",
              "sectionDscTxt": "Forgery"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "376",
      "physicalFileId": "1169.0026",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "98021302",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "1999-04-16 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Amil",
          "charge": [
            {
              "sectionTxt": "CCC - 82",
              "sectionDscTxt": "Explosives: making or possession for unlawful purp"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "402",
      "physicalFileId": "1207.0026",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "100",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "Y",
      "nextApprDt": "2005-01-12 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Allan, Woody",
          "charge": [
            {
              "sectionTxt": "CCC - 72",
              "sectionDscTxt": "Forcible Entry or Detainer"
            },
            {
              "sectionTxt": "CCC - 202",
              "sectionDscTxt": "Book-Making"
            }
          ]
        },
        {
          "fullNm": "Green, Gail",
          "charge": []
        },
        {
          "fullNm": "Hudsons Bay",
          "charge": []
        },
        {
          "fullNm": "Jackson, Michael",
          "charge": []
        },
        {
          "fullNm": "Jones, David",
          "charge": []
        },
        {
          "fullNm": "Jones, J",
          "charge": []
        },
        {
          "fullNm": "Jones, Jamie",
          "charge": []
        },
        {
          "fullNm": "Jones, Mike",
          "charge": []
        },
        {
          "fullNm": "Jones, Murray",
          "charge": []
        },
        {
          "fullNm": "Jones, Ray",
          "charge": []
        },
        {
          "fullNm": "Lotsacounts, Horrace",
          "charge": []
        },
        {
          "fullNm": "SMITH, JAMES",
          "charge": []
        },
        {
          "fullNm": "Tuphast, Joe",
          "charge": []
        },
        {
          "fullNm": "martin, pablo paulo",
          "charge": []
        }
      ]
    },
    {
      "mdocJustinNo": "430",
      "physicalFileId": "1247.0026",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "54321",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "Y",
      "nextApprDt": "2014-06-11 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Barnett, Bruce",
          "charge": [
            {
              "sectionTxt": "CCC - 129(a)",
              "sectionDscTxt": "obstruct an officer in the execution of his duty"
            }
          ]
        },
        {
          "fullNm": "Smith, Bill",
          "charge": [
            {
              "sectionTxt": "CCC - 129(a)",
              "sectionDscTxt": "obstruct an officer in the execution of his duty"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "520",
      "physicalFileId": "1379.0026",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "3000",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": "M",
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "T",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2014-02-03 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Brad",
          "charge": [
            {
              "sectionTxt": "MVA - 33",
              "sectionDscTxt": "Production of Licence and Liability Card, Duplicat"
            },
            {
              "sectionTxt": "MVA - 34",
              "sectionDscTxt": "Failure to obtain bc dl"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "521",
      "physicalFileId": "1380.0026",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "98030201",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": null,
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Paul",
          "charge": [
            {
              "sectionTxt": "CCC - 380",
              "sectionDscTxt": "Fraud"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "573",
      "physicalFileId": "1449.0026",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "98030401",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "Y",
      "nextApprDt": "1998-03-04 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Kerwal, Gurmit Singh",
          "charge": [
            {
              "sectionTxt": "CCC - 344(b)",
              "sectionDscTxt": "Robbery"
            },
            {
              "sectionTxt": "CCC - 344(b)",
              "sectionDscTxt": "Robbery"
            }
          ]
        },
        {
          "fullNm": "Smith, David",
          "charge": []
        }
      ]
    },
    {
      "mdocJustinNo": "593",
      "physicalFileId": "1470.0026",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "2345",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2017-07-07 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "FED",
      "participant": [
        {
          "fullNm": "Jones, Bob",
          "charge": [
            {
              "sectionTxt": "NCA - 5",
              "sectionDscTxt": "import into Canada a narcotic"
            }
          ]
        },
        {
          "fullNm": "smith, david andrew",
          "charge": [
            {
              "sectionTxt": "NCA - 5",
              "sectionDscTxt": "import into Canada a narcotic"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "878",
      "physicalFileId": "1824.0026",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "9864105",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": "PRAC",
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "Y",
      "inCustodyYN": "N",
      "nextApprDt": "1999-12-16 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Blair",
          "charge": [
            {
              "sectionTxt": "CCC - 249(3)",
              "sectionDscTxt": "Dangerous operation causing bodily harm"
            },
            {
              "sectionTxt": "CCC - 81(1)(d)",
              "sectionDscTxt": "Explosives: Intent bodily harm"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "1061",
      "physicalFileId": "2002.0026",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "5001",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "Y",
      "inCustodyYN": "N",
      "nextApprDt": "2020-09-30 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Jones, Marvin",
          "charge": [
            {
              "sectionTxt": "CCC - 334(b)",
              "sectionDscTxt": "Theft Under $5,000"
            },
            {
              "sectionTxt": "CCC - 334(b)",
              "sectionDscTxt": "Theft Under $5,000"
            },
            {
              "sectionTxt": "CCC - 334(b)",
              "sectionDscTxt": "Theft Under $5,000"
            }
          ]
        },
        {
          "fullNm": "Smith, Walter",
          "charge": [
            {
              "sectionTxt": "CCC - 334(b)",
              "sectionDscTxt": "Theft Under $5,000"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "1083",
      "physicalFileId": "2023.0026",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "98100100",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": null,
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith",
          "charge": [
            {
              "sectionTxt": "CCC - 65",
              "sectionDscTxt": "rioter"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "1084",
      "physicalFileId": "2024.0026",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "98100101",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "1998-04-04 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "JOhnson, Graham",
          "charge": []
        },
        {
          "fullNm": "Smith",
          "charge": [
            {
              "sectionTxt": "CCC - 65",
              "sectionDscTxt": "rioter"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "1086",
      "physicalFileId": "2026.0026",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "98100102",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": null,
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Horatio",
          "charge": [
            {
              "sectionTxt": "CCC - 65",
              "sectionDscTxt": "rioter"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "1124",
      "physicalFileId": "2066.0026",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "6002",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2020-02-10 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Lewis, Brian",
          "charge": [
            {
              "sectionTxt": "CDS - 4",
              "sectionDscTxt": "Possession of Controlled Substance"
            },
            {
              "sectionTxt": "CDS - 4",
              "sectionDscTxt": "Possession of Controlled Substance"
            }
          ]
        },
        {
          "fullNm": "Smith, John",
          "charge": [
            {
              "sectionTxt": "CDS - 4",
              "sectionDscTxt": "Possession of Controlled Substance"
            },
            {
              "sectionTxt": "CDS - 4",
              "sectionDscTxt": "Possession of Controlled Substance"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "1125",
      "physicalFileId": "2067.0026",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "85789",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "Y",
      "inCustodyYN": "N",
      "nextApprDt": "2002-06-04 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Roosevelt, Franklin George",
          "charge": [
            {
              "sectionTxt": "CCC - 253(a)",
              "sectionDscTxt": "Impaired Driving"
            },
            {
              "sectionTxt": "CCC - 129(a)",
              "sectionDscTxt": "obstruct an officer in the execution of his duty"
            }
          ]
        },
        {
          "fullNm": "SMITH, JAMES WILLIAM",
          "charge": [
            {
              "sectionTxt": "CCC - 129(a)",
              "sectionDscTxt": "obstruct an officer in the execution of his duty"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "1655",
      "physicalFileId": "2502.0026",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "98610",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "1998-06-15 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "SMITH, JAMES WILLIAM",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(b)",
              "sectionDscTxt": "Breaking, entering and committing"
            },
            {
              "sectionTxt": "CCC - 348(1)(b)",
              "sectionDscTxt": "Breaking, entering and committing"
            },
            {
              "sectionTxt": "CCC - 348(1)(b)",
              "sectionDscTxt": "Breaking, entering and committing"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "2495",
      "physicalFileId": "4565.0001",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "999",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "Y",
      "inCustodyYN": "N",
      "nextApprDt": "2021-03-16 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Harry's Flowers and Arrangements",
          "charge": [
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        },
        {
          "fullNm": "Smith, Harry",
          "charge": [
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "2755",
      "physicalFileId": "4921.0042",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "9901",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "1999-05-10 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "SMITH, DAVID",
          "charge": [
            {
              "sectionTxt": "CCC - 129(a)",
              "sectionDscTxt": "obstruct an officer in the execution of his duty"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        },
        {
          "fullNm": "SMITH, DAVID",
          "charge": [
            {
              "sectionTxt": "CCC - 129(a)",
              "sectionDscTxt": "obstruct an officer in the execution of his duty"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "2930",
      "physicalFileId": "5113.0042",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "201",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": "AC",
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "T",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "1999-12-24 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "SMith, Granny",
          "charge": [
            {
              "sectionTxt": "CCC - 82",
              "sectionDscTxt": "Explosives: making or possession for unlawful purp"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "2947",
      "physicalFileId": "5130.0042",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "8004",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": "JR",
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "T",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "1999-12-24 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Tom",
          "charge": [
            {
              "sectionTxt": "MVA - 100",
              "sectionDscTxt": "prohibition against driving for failing to stop"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "3017",
      "physicalFileId": "5198.0042",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "576",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "1999-05-10 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Amanda",
          "charge": [
            {
              "sectionTxt": "CCC - 82",
              "sectionDscTxt": "Explosives: making or possession for unlawful purp"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "3121",
      "physicalFileId": "5322.0042",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "414",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": "AE",
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "T",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "1999-12-24 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Bob, Jim",
          "charge": [
            {
              "sectionTxt": "CCC - 82",
              "sectionDscTxt": "Explosives: making or possession for unlawful purp"
            }
          ]
        },
        {
          "fullNm": "Grand, Allan",
          "charge": [
            {
              "sectionTxt": "CCC - 82",
              "sectionDscTxt": "Explosives: making or possession for unlawful purp"
            }
          ]
        },
        {
          "fullNm": "Smith, Anne",
          "charge": [
            {
              "sectionTxt": "CCC - 82",
              "sectionDscTxt": "Explosives: making or possession for unlawful purp"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "3123",
      "physicalFileId": "5324.0042",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "415",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": "AE",
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "T",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "1999-12-24 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, James",
          "charge": [
            {
              "sectionTxt": "CCC - 82",
              "sectionDscTxt": "Explosives: making or possession for unlawful purp"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "3129",
      "physicalFileId": "5330.0042",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "1000",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": "AE",
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "T",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2020-07-15 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "BIRO, Tracey",
          "charge": [
            {
              "sectionTxt": "CCC - 82",
              "sectionDscTxt": "Explosives: making or possession for unlawful purp"
            },
            {
              "sectionTxt": "CCC - 102(4)",
              "sectionDscTxt": "Firearm: posession while prohibited 102(4)"
            }
          ]
        },
        {
          "fullNm": "Bercowitz, David Mathew",
          "charge": [
            {
              "sectionTxt": "CCC - 82",
              "sectionDscTxt": "Explosives: making or possession for unlawful purp"
            },
            {
              "sectionTxt": "CCC - 102(4)",
              "sectionDscTxt": "Firearm: posession while prohibited 102(4)"
            }
          ]
        },
        {
          "fullNm": "Blahman, James",
          "charge": [
            {
              "sectionTxt": "CCC - 82",
              "sectionDscTxt": "Explosives: making or possession for unlawful purp"
            },
            {
              "sectionTxt": "CCC - 102(4)",
              "sectionDscTxt": "Firearm: posession while prohibited 102(4)"
            }
          ]
        },
        {
          "fullNm": "Bloggina, Joe",
          "charge": []
        },
        {
          "fullNm": "Cooper1, Adam",
          "charge": [
            {
              "sectionTxt": "CCC - 82",
              "sectionDscTxt": "Explosives: making or possession for unlawful purp"
            },
            {
              "sectionTxt": "CCC - 102(4)",
              "sectionDscTxt": "Firearm: posession while prohibited 102(4)"
            }
          ]
        },
        {
          "fullNm": "Cooper2, Adam",
          "charge": [
            {
              "sectionTxt": "CCC - 82",
              "sectionDscTxt": "Explosives: making or possession for unlawful purp"
            },
            {
              "sectionTxt": "CCC - 102(4)",
              "sectionDscTxt": "Firearm: posession while prohibited 102(4)"
            }
          ]
        },
        {
          "fullNm": "Gober, Gomer Pyle",
          "charge": [
            {
              "sectionTxt": "CCC - 82",
              "sectionDscTxt": "Explosives: making or possession for unlawful purp"
            },
            {
              "sectionTxt": "CCC - 102(4)",
              "sectionDscTxt": "Firearm: posession while prohibited 102(4)"
            }
          ]
        },
        {
          "fullNm": "SMITH, BOBBI-JEAN AaAaabb",
          "charge": [
            {
              "sectionTxt": "CCC - 82",
              "sectionDscTxt": "Explosives: making or possession for unlawful purp"
            },
            {
              "sectionTxt": "CCC - 102(4)",
              "sectionDscTxt": "Firearm: posession while prohibited 102(4)"
            }
          ]
        },
        {
          "fullNm": "Smith, Jane",
          "charge": [
            {
              "sectionTxt": "CCC - 82",
              "sectionDscTxt": "Explosives: making or possession for unlawful purp"
            },
            {
              "sectionTxt": "CCC - 102(4)",
              "sectionDscTxt": "Firearm: posession while prohibited 102(4)"
            }
          ]
        },
        {
          "fullNm": "Woofey",
          "charge": [
            {
              "sectionTxt": "CCC - 82",
              "sectionDscTxt": "Explosives: making or possession for unlawful purp"
            },
            {
              "sectionTxt": "CCC - 102(4)",
              "sectionDscTxt": "Firearm: posession while prohibited 102(4)"
            }
          ]
        },
        {
          "fullNm": "akls aeskt lkasg aith adfgj aodt sgh lkdgoi dlkgj adt shg sdutoi glkh so;iry slkh lkdjgoaieyt adg lkadutoiu adlght doar6 d g a dlk adkldjlkjg ladtu aj",
          "charge": [
            {
              "sectionTxt": "CCC - 82",
              "sectionDscTxt": "Explosives: making or possession for unlawful purp"
            },
            {
              "sectionTxt": "CCC - 102(4)",
              "sectionDscTxt": "Firearm: posession while prohibited 102(4)"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "3208",
      "physicalFileId": "5407.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "1010",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": "AE",
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "T",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "1999-12-24 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Jim",
          "charge": [
            {
              "sectionTxt": "CCC - 82",
              "sectionDscTxt": "Explosives: making or possession for unlawful purp"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "3229",
      "physicalFileId": "5451.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "100",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": "AA",
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "T",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2008-10-07 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, David",
          "charge": [
            {
              "sectionTxt": "MVA - 24",
              "sectionDscTxt": "Offences - licences & insurance"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "3289",
      "physicalFileId": "5527.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "4172001",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2000-04-17 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Joe",
          "charge": [
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            }
          ]
        },
        {
          "fullNm": "Smith, John",
          "charge": []
        }
      ]
    },
    {
      "mdocJustinNo": "3291",
      "physicalFileId": "5528.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "4172002",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2000-04-17 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Joe",
          "charge": [
            {
              "sectionTxt": "CCC - 344",
              "sectionDscTxt": "Robbery"
            }
          ]
        },
        {
          "fullNm": "Smith, Sam",
          "charge": []
        }
      ]
    },
    {
      "mdocJustinNo": "3371",
      "physicalFileId": "5989.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "55566",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2016-05-11 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Jones, Bill",
          "charge": [
            {
              "sectionTxt": "CCC - 100",
              "sectionDscTxt": "Firearm: posession while prohibited"
            }
          ]
        },
        {
          "fullNm": "Smith, Joe",
          "charge": [
            {
              "sectionTxt": "CCC - 100",
              "sectionDscTxt": "Firearm: posession while prohibited"
            },
            {
              "sectionTxt": "CCC - 319",
              "sectionDscTxt": "PUBLIC INCITEMENT OF HATRED"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "3373",
      "physicalFileId": "5990.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "330",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "Y",
      "inCustodyYN": "N",
      "nextApprDt": "2018-01-11 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "James, Bill",
          "charge": [
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            },
            {
              "sectionTxt": "CCC - 344",
              "sectionDscTxt": "Robbery"
            }
          ]
        },
        {
          "fullNm": "smith, will",
          "charge": [
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            },
            {
              "sectionTxt": "CCC - 344",
              "sectionDscTxt": "Robbery"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "3409",
      "physicalFileId": "6323.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "20801",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2014-04-22 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Dave",
          "charge": [
            {
              "sectionTxt": "CCC - 102(5)",
              "sectionDscTxt": "Firearm: posession while prohibited 102(5)"
            }
          ]
        },
        {
          "fullNm": "Smith, Joan Kelly",
          "charge": [
            {
              "sectionTxt": "CCC - 100",
              "sectionDscTxt": "Firearm: posession while prohibited"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "3416",
      "physicalFileId": "6384.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "21402",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "Y",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2003-01-06 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Dave",
          "charge": [
            {
              "sectionTxt": "CCC - 100",
              "sectionDscTxt": "Firearm: posession while prohibited"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "3480",
      "physicalFileId": "6868.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "5300",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2019-04-01 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Adams, James",
          "charge": [
            {
              "sectionTxt": "CCC - 322",
              "sectionDscTxt": "theft: definition"
            },
            {
              "sectionTxt": "CCC - 403(a)",
              "sectionDscTxt": "Personation with intent"
            }
          ]
        },
        {
          "fullNm": "Smith, Frank",
          "charge": [
            {
              "sectionTxt": "CCC - 322",
              "sectionDscTxt": "theft: definition"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "3770",
      "physicalFileId": "8674.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "122505",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "Y",
      "warrantYN": "N",
      "inCustodyYN": "Y",
      "nextApprDt": "2017-08-09 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Galloway, George",
          "charge": [
            {
              "sectionTxt": "CCC - 356",
              "sectionDscTxt": "Theft of Mail"
            },
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            },
            {
              "sectionTxt": "CCC - 352",
              "sectionDscTxt": "Possession of coin-operated device breaking instru"
            },
            {
              "sectionTxt": "CCC - 119(1)(b)",
              "sectionDscTxt": "offer bribe to judicial officer/mp/mla"
            },
            {
              "sectionTxt": "CCC - 270",
              "sectionDscTxt": "Assault Peace Officer"
            },
            {
              "sectionTxt": "CCC - 279.1",
              "sectionDscTxt": "Hostage taking"
            },
            {
              "sectionTxt": "CCC - 255(3)",
              "sectionDscTxt": "Impaired driving causing death"
            },
            {
              "sectionTxt": "CCC - 90(2)",
              "sectionDscTxt": "Prohibited weapon: in motor vehicle"
            },
            {
              "sectionTxt": "CCC - 90(1)",
              "sectionDscTxt": "Prohibited weapon possession"
            },
            {
              "sectionTxt": "CCC - 403(b)",
              "sectionDscTxt": "Personation with intent"
            }
          ]
        },
        {
          "fullNm": "Smith, Joe",
          "charge": [
            {
              "sectionTxt": "CCC - 356",
              "sectionDscTxt": "Theft of Mail"
            },
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            },
            {
              "sectionTxt": "CCC - 352",
              "sectionDscTxt": "Possession of coin-operated device breaking instru"
            },
            {
              "sectionTxt": "CCC - 119(1)(b)",
              "sectionDscTxt": "offer bribe to judicial officer/mp/mla"
            },
            {
              "sectionTxt": "CCC - 270",
              "sectionDscTxt": "Assault Peace Officer"
            },
            {
              "sectionTxt": "CCC - 279.1",
              "sectionDscTxt": "Hostage taking"
            },
            {
              "sectionTxt": "CCC - 255(3)",
              "sectionDscTxt": "Impaired driving causing death"
            },
            {
              "sectionTxt": "CCC - 90(2)",
              "sectionDscTxt": "Prohibited weapon: in motor vehicle"
            },
            {
              "sectionTxt": "CCC - 90(1)",
              "sectionDscTxt": "Prohibited weapon possession"
            },
            {
              "sectionTxt": "CCC - 403(b)",
              "sectionDscTxt": "Personation with intent"
            }
          ]
        },
        {
          "fullNm": "Younger, Cole",
          "charge": [
            {
              "sectionTxt": "CCC - 356",
              "sectionDscTxt": "Theft of Mail"
            },
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            },
            {
              "sectionTxt": "CCC - 352",
              "sectionDscTxt": "Possession of coin-operated device breaking instru"
            },
            {
              "sectionTxt": "CCC - 119(1)(b)",
              "sectionDscTxt": "offer bribe to judicial officer/mp/mla"
            },
            {
              "sectionTxt": "CCC - 270",
              "sectionDscTxt": "Assault Peace Officer"
            },
            {
              "sectionTxt": "CCC - 279.1",
              "sectionDscTxt": "Hostage taking"
            },
            {
              "sectionTxt": "CCC - 255(3)",
              "sectionDscTxt": "Impaired driving causing death"
            },
            {
              "sectionTxt": "CCC - 90(2)",
              "sectionDscTxt": "Prohibited weapon: in motor vehicle"
            },
            {
              "sectionTxt": "CCC - 90(1)",
              "sectionDscTxt": "Prohibited weapon possession"
            },
            {
              "sectionTxt": "CCC - 403(b)",
              "sectionDscTxt": "Personation with intent"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "3865",
      "physicalFileId": "9451.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "1243",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "Y",
      "nextApprDt": "2021-01-11 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Bob, Jim",
          "charge": [
            {
              "sectionTxt": "CCC - 139(2)",
              "sectionDscTxt": "Obsrtuct Justice"
            },
            {
              "sectionTxt": "CCC - 344",
              "sectionDscTxt": "Robbery"
            },
            {
              "sectionTxt": "CCC - 52",
              "sectionDscTxt": "sabotage"
            }
          ]
        },
        {
          "fullNm": "Smithers, Wilson",
          "charge": [
            {
              "sectionTxt": "CCC - 139(2)",
              "sectionDscTxt": "Obsrtuct Justice"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "3866",
      "physicalFileId": "9471.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "12555",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2014-08-07 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smithers, Wilson",
          "charge": [
            {
              "sectionTxt": "CCC - 264.1(1)(a)",
              "sectionDscTxt": "Uttering threats"
            },
            {
              "sectionTxt": "CCC - 334(b)",
              "sectionDscTxt": "Theft Under $5,000"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "3870",
      "physicalFileId": "9511.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "3093",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2017-05-15 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Jones, Jeremy Watson",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        },
        {
          "fullNm": "SMITHERS, WAYNE NEWTON",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "3920",
      "physicalFileId": "9894.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "31201",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2002-03-14 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Joseph",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(b)",
              "sectionDscTxt": "Breaking, entering and committing"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "3921",
      "physicalFileId": "9895.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "31202",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2017-08-09 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Joseph",
          "charge": [
            {
              "sectionTxt": "CCC - 122",
              "sectionDscTxt": "breach of trust by public officer"
            },
            {
              "sectionTxt": "CCC - 336",
              "sectionDscTxt": "Breach of Trust"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "3961",
      "physicalFileId": "10012.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "3000",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": "M",
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": null,
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Brad",
          "charge": []
        }
      ]
    },
    {
      "mdocJustinNo": "4019",
      "physicalFileId": "10400.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "100000",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": "AA",
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "T",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2020-02-14 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Bercowitz, David Mathew",
          "charge": [
            {
              "sectionTxt": "MVA - 100",
              "sectionDscTxt": "prohibition against driving for failing to stop"
            }
          ]
        },
        {
          "fullNm": "Smith, Jane",
          "charge": [
            {
              "sectionTxt": "MVA - 100",
              "sectionDscTxt": "prohibition against driving for failing to stop"
            }
          ]
        },
        {
          "fullNm": "akls aeskt lkasg aith adfgj aodt sgh lkdgoi dlkgj adt shg sdutoi glkh so;iry slkh lkdjgoaieyt adg lkadutoiu adlght doar6 d g a dlk adkldjlkjg ladtu aj",
          "charge": [
            {
              "sectionTxt": "MVA - 100",
              "sectionDscTxt": "prohibition against driving for failing to stop"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "4021",
      "physicalFileId": "10420.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "1223",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": "QW",
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "T",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": null,
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Hoe",
          "charge": [
            {
              "sectionTxt": "MVA - 12",
              "sectionDscTxt": "Number plates"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "4088",
      "physicalFileId": "10696.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "13001",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": null,
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "SMITH, BOBBI-JEAN",
          "charge": [
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "12417",
      "physicalFileId": "21170.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "101010",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2016-03-18 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, James",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 357",
              "sectionDscTxt": "Bringing into Canada property obtained by crime"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "35159",
      "physicalFileId": "43712.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "7152003",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": null,
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "SMITHERS, WAYNE NEWTON",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "35350",
      "physicalFileId": "44323.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "200403031",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "Y",
      "nextApprDt": "2014-04-23 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "SMITH, BOBBI-JEAN",
          "charge": [
            {
              "sectionTxt": "CCC - 811",
              "sectionDscTxt": "Breach - Threat. Recognizance"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "35426",
      "physicalFileId": "44671.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "200403311",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "Y",
      "nextApprDt": "2004-03-31 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "SMITH, JAMES",
          "charge": [
            {
              "sectionTxt": "CCC - 145(1)(a)",
              "sectionDscTxt": "Escape from lawful custody"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "35432",
      "physicalFileId": "44751.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "200404131",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "Y",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2004-04-13 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "SMITH, BOBBI-JEAN",
          "charge": [
            {
              "sectionTxt": "CCC - 437",
              "sectionDscTxt": "False alarm of fire"
            },
            {
              "sectionTxt": "CCC - 811",
              "sectionDscTxt": "Breach - Threat. Recognizance"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "35460",
      "physicalFileId": "44872.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "200404274",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2004-04-27 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "SMITH-JONES, BOBBIE",
          "charge": [
            {
              "sectionTxt": "CCC - 145(4)",
              "sectionDscTxt": "Failing to appear or to comply with summons"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "35463",
      "physicalFileId": "44910.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "200404282",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2004-04-28 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "SMITH-JONES, BOBBIE",
          "charge": [
            {
              "sectionTxt": "CCC - 145(5)",
              "sectionDscTxt": "Fail to appear on AN/PTA/Recog"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "35464",
      "physicalFileId": "44929.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "200404291",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2004-05-06 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "SMITH-JONES, BOBBIE",
          "charge": [
            {
              "sectionTxt": "CCC - 145",
              "sectionDscTxt": "Escape Fail to Appear"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "35620",
      "physicalFileId": "45912.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "2005300",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": null,
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Aaron Shawn",
          "charge": [
            {
              "sectionTxt": "CCC - 403(a)",
              "sectionDscTxt": "Personation with intent"
            },
            {
              "sectionTxt": "CCC - 323",
              "sectionDscTxt": "theft of oysters"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "35722",
      "physicalFileId": "46410.0002",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "20051",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2005-04-04 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "smith, joe",
          "charge": [
            {
              "sectionTxt": "CCC - 100",
              "sectionDscTxt": "Firearm: posession while prohibited"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "35830",
      "physicalFileId": "46681.0002",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "2",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": null,
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, David",
          "charge": []
        }
      ]
    },
    {
      "mdocJustinNo": "35985",
      "physicalFileId": "48021.0002",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "10012",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2017-08-09 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, james",
          "charge": [
            {
              "sectionTxt": "WAP - 100",
              "sectionDscTxt": "Test"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "36036",
      "physicalFileId": "48403.0002",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "1996",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "Y",
      "nextApprDt": "2021-01-12 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Brown, Charles",
          "charge": [
            {
              "sectionTxt": "CCC - 103(4)",
              "sectionDscTxt": "Prohibited Possession"
            },
            {
              "sectionTxt": "CCC - 206(1)(b)",
              "sectionDscTxt": "Lottery scheme"
            },
            {
              "sectionTxt": "CCC - 327(1)",
              "sectionDscTxt": "Possession Device to Steal"
            }
          ]
        },
        {
          "fullNm": "Jones, Bobby",
          "charge": [
            {
              "sectionTxt": "CCC - 103(4)",
              "sectionDscTxt": "Prohibited Possession"
            },
            {
              "sectionTxt": "CCC - 206(1)(b)",
              "sectionDscTxt": "Lottery scheme"
            },
            {
              "sectionTxt": "CCC - 327(1)",
              "sectionDscTxt": "Possession Device to Steal"
            }
          ]
        },
        {
          "fullNm": "Smith, Allison",
          "charge": [
            {
              "sectionTxt": "CCC - 103(4)",
              "sectionDscTxt": "Prohibited Possession"
            },
            {
              "sectionTxt": "CCC - 206(1)(b)",
              "sectionDscTxt": "Lottery scheme"
            },
            {
              "sectionTxt": "CCC - 327(1)",
              "sectionDscTxt": "Possession Device to Steal"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "36120",
      "physicalFileId": "49181.0002",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "56789",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2007-10-18 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Benjamin Warren",
          "charge": [
            {
              "sectionTxt": "CCC - 254(5)",
              "sectionDscTxt": "Failure or refusal to provide sample"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "36449",
      "physicalFileId": "51021.0734",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "43210098",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "Y",
      "inCustodyYN": "N",
      "nextApprDt": "2008-03-29 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "smithj",
          "charge": [
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            },
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            },
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            },
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            },
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            },
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            },
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            },
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            },
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            },
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            },
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            },
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            },
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            },
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            },
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            },
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "36533",
      "physicalFileId": "51661.0734",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "121333",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": "QW",
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "T",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2008-07-23 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Joe",
          "charge": [
            {
              "sectionTxt": "MVA - 12",
              "sectionDscTxt": "Number plates"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "36785",
      "physicalFileId": "53219.0734",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "9911",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2009-08-20 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Sally",
          "charge": [
            {
              "sectionTxt": "CCC - 344",
              "sectionDscTxt": "Robbery"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "36786",
      "physicalFileId": "53220.0734",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "9898",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2014-05-30 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Bob",
          "charge": [
            {
              "sectionTxt": "CCC - 344",
              "sectionDscTxt": "Robbery"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "37036",
      "physicalFileId": "54620.0734",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "20101105",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2017-08-09 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "SMIGH, Emery John",
          "charge": [
            {
              "sectionTxt": "CCC - 103(6)(b)",
              "sectionDscTxt": "Prohibited Possession"
            }
          ]
        },
        {
          "fullNm": "Smith, Josephine Mary",
          "charge": [
            {
              "sectionTxt": "CCC - 103(6)(b)",
              "sectionDscTxt": "Prohibited Possession"
            },
            {
              "sectionTxt": "CCC - 104(3)(b)",
              "sectionDscTxt": "alter/deface/remove serial no."
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "37052",
      "physicalFileId": "54665.0734",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "401101",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2010-11-12 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Ed",
          "charge": [
            {
              "sectionTxt": "CCC - 102(7)",
              "sectionDscTxt": "Firearm: posession while prohibited 102(7)"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "37127",
      "physicalFileId": "55275.0734",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "20110706",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2014-07-22 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Chestnut, Carla",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        },
        {
          "fullNm": "Johnson, Louise",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        },
        {
          "fullNm": "Jones, Bill",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        },
        {
          "fullNm": "Smithson, Peter",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        },
        {
          "fullNm": "Tester, Ima Big",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "37316",
      "physicalFileId": "56315.0734",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "20111027",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "Y",
      "nextApprDt": "2017-08-15 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Cooper, Tim",
          "charge": [
            {
              "sectionTxt": "CCC - 334(a)",
              "sectionDscTxt": "Theft Over $5,000"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        },
        {
          "fullNm": "Johnson, Joey",
          "charge": [
            {
              "sectionTxt": "CCC - 334(a)",
              "sectionDscTxt": "Theft Over $5,000"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        },
        {
          "fullNm": "Jones, Bill",
          "charge": [
            {
              "sectionTxt": "CCC - 334(a)",
              "sectionDscTxt": "Theft Over $5,000"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        },
        {
          "fullNm": "Smith, Mike",
          "charge": [
            {
              "sectionTxt": "CCC - 334(a)",
              "sectionDscTxt": "Theft Over $5,000"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        },
        {
          "fullNm": "Test, Ima",
          "charge": [
            {
              "sectionTxt": "CCC - 334(a)",
              "sectionDscTxt": "Theft Over $5,000"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        },
        {
          "fullNm": "Thomas, Barney",
          "charge": [
            {
              "sectionTxt": "CCC - 334(a)",
              "sectionDscTxt": "Theft Over $5,000"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        },
        {
          "fullNm": "Ulyses, Ron",
          "charge": [
            {
              "sectionTxt": "CCC - 334(a)",
              "sectionDscTxt": "Theft Over $5,000"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "37832",
      "physicalFileId": "59353.0734",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "415",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "Y",
      "nextApprDt": "2017-06-22 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "POL",
      "participant": [
        {
          "fullNm": "Smith-Simmons, Jack",
          "charge": [
            {
              "sectionTxt": "CCC - 334",
              "sectionDscTxt": "Theft (5,000)"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "37833",
      "physicalFileId": "59354.0734",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "2719",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "Y",
      "nextApprDt": "2021-02-03 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "POL",
      "participant": [
        {
          "fullNm": "Smith-Simmons, Toby",
          "charge": [
            {
              "sectionTxt": "CCC - 334",
              "sectionDscTxt": "Theft (5,000)"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "38593",
      "physicalFileId": "60057.0734",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "201724",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2017-12-13 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smitherton, Paul",
          "charge": [
            {
              "sectionTxt": "CCC - 334(b)",
              "sectionDscTxt": "Theft Under $5,000"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "38708",
      "physicalFileId": "60131.0734",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "10020",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": "E",
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": null,
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smithers, Paul",
          "charge": [
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "38709",
      "physicalFileId": "60132.0734",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "10021",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": "E",
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": null,
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smithers, Paul",
          "charge": [
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "38722",
      "physicalFileId": "60161.0734",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "20180816",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "Y",
      "nextApprDt": null,
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Jason",
          "charge": [
            {
              "sectionTxt": "CCC - 334",
              "sectionDscTxt": "Theft (5,000)"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "39152",
      "physicalFileId": "61505.0734",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "20200110",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "Y",
      "inCustodyYN": "N",
      "nextApprDt": "2020-04-21 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Jones, Bobby",
          "charge": [
            {
              "sectionTxt": "CCC - 334",
              "sectionDscTxt": "Theft (5,000)"
            },
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            },
            {
              "sectionTxt": "CCC - 334",
              "sectionDscTxt": "Theft (5,000)"
            }
          ]
        },
        {
          "fullNm": "Natguy, Bobby",
          "charge": [
            {
              "sectionTxt": "CCC - 334",
              "sectionDscTxt": "Theft (5,000)"
            },
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            },
            {
              "sectionTxt": "CCC - 334",
              "sectionDscTxt": "Theft (5,000)"
            }
          ]
        },
        {
          "fullNm": "Smith, Bob",
          "charge": [
            {
              "sectionTxt": "CCC - 334",
              "sectionDscTxt": "Theft (5,000)"
            },
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            },
            {
              "sectionTxt": "CCC - 334",
              "sectionDscTxt": "Theft (5,000)"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "39160",
      "physicalFileId": "61606.0734",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "4514551",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": "EZ",
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "T",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2020-02-21 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Jack",
          "charge": [
            {
              "sectionTxt": "MVR - 25.02",
              "sectionDscTxt": "No valid inspection certificate"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "39322",
      "physicalFileId": "62189.0734",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "20200924",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2020-10-07 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Michael",
          "charge": [
            {
              "sectionTxt": "CCC - 348",
              "sectionDscTxt": "Breaking and Entering"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "39894",
      "physicalFileId": "63884.0734",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "815741",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2022-11-28 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Jones, Arthur",
          "charge": [
            {
              "sectionTxt": "CCC - 334",
              "sectionDscTxt": "Theft (5,000)"
            }
          ]
        },
        {
          "fullNm": "Smith, Robbie",
          "charge": [
            {
              "sectionTxt": "CCC - 334",
              "sectionDscTxt": "Theft (5,000)"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "39906",
      "physicalFileId": "63989.0734",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "4111221",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2022-11-30 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, George",
          "charge": [
            {
              "sectionTxt": "CCC - 103(1)",
              "sectionDscTxt": "Prohibited Possession"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "39990",
      "physicalFileId": "64248.0734",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "79431",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": null,
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Robert",
          "charge": [
            {
              "sectionTxt": "CCC - 320.14(1)(b)",
              "sectionDscTxt": "Operation while over 80 BAC"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "40434",
      "physicalFileId": "65546.0734",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "7529",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2024-03-01 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Amil",
          "charge": [
            {
              "sectionTxt": "CCC - 266",
              "sectionDscTxt": "Assault"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "40460",
      "physicalFileId": "65650.0734",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "240224",
      "mdocSeqNo": "1",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "S",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2024-04-30 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "SMITH, RON",
          "charge": [
            {
              "sectionTxt": "CCC - 267(1)(b)",
              "sectionDscTxt": "Assault causing bodily harm"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "38825",
      "physicalFileId": "1207.0026",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "100",
      "mdocSeqNo": "2",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": null,
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smithx, James",
          "charge": []
        }
      ]
    },
    {
      "mdocJustinNo": "1087",
      "physicalFileId": "2024.0026",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "98100101",
      "mdocSeqNo": "2",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": null,
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith",
          "charge": [
            {
              "sectionTxt": "CCC - 66",
              "sectionDscTxt": "unlawful assembly"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "1126",
      "physicalFileId": "2066.0026",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "6002",
      "mdocSeqNo": "2",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2018-10-04 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Lewis, Brian",
          "charge": [
            {
              "sectionTxt": "CDS - 5",
              "sectionDscTxt": "Trafficking in Controlled Substance"
            },
            {
              "sectionTxt": "CDS - 4",
              "sectionDscTxt": "Possession of Controlled Substance"
            },
            {
              "sectionTxt": "CDS - 4",
              "sectionDscTxt": "Possession of Controlled Substance"
            }
          ]
        },
        {
          "fullNm": "Smith, John",
          "charge": [
            {
              "sectionTxt": "CDS - 4",
              "sectionDscTxt": "Possession of Controlled Substance"
            },
            {
              "sectionTxt": "CDS - 4",
              "sectionDscTxt": "Possession of Controlled Substance"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "3708",
      "physicalFileId": "2347.0026",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "5092",
      "mdocSeqNo": "2",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2009-05-02 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Brown, Bill",
          "charge": [
            {
              "sectionTxt": "CCC - 278.3",
              "sectionDscTxt": "Application for production"
            }
          ]
        },
        {
          "fullNm": "Jones, Sam",
          "charge": [
            {
              "sectionTxt": "CCC - 278.3",
              "sectionDscTxt": "Application for production"
            }
          ]
        },
        {
          "fullNm": "Province of British Columbia",
          "charge": []
        },
        {
          "fullNm": "Sammies Soso Good Foods Ltd",
          "charge": []
        },
        {
          "fullNm": "Smith, Blair",
          "charge": []
        }
      ]
    },
    {
      "mdocJustinNo": "2329",
      "physicalFileId": "4108.0002",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "8970",
      "mdocSeqNo": "2",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": "BB",
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2014-02-20 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Jim",
          "charge": [
            {
              "sectionTxt": "CCC - 319",
              "sectionDscTxt": "PUBLIC INCITEMENT OF HATRED"
            }
          ]
        },
        {
          "fullNm": "billy, bob",
          "charge": [
            {
              "sectionTxt": "CCC - 319",
              "sectionDscTxt": "PUBLIC INCITEMENT OF HATRED"
            }
          ]
        },
        {
          "fullNm": "campbell, wendy",
          "charge": [
            {
              "sectionTxt": "CCC - 319",
              "sectionDscTxt": "PUBLIC INCITEMENT OF HATRED"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "35681",
      "physicalFileId": "4565.0001",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "999",
      "mdocSeqNo": "2",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2022-06-30 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Harry",
          "charge": [
            {
              "sectionTxt": "CCC - 100",
              "sectionDscTxt": "Firearm: posession while prohibited"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "3138",
      "physicalFileId": "4777.0042",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "3240001",
      "mdocSeqNo": "2",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2015-06-15 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Billy, Bob",
          "charge": [
            {
              "sectionTxt": "CCC - 100(4)",
              "sectionDscTxt": "Firearms Prohibition"
            }
          ]
        },
        {
          "fullNm": "Smith, Joe",
          "charge": [
            {
              "sectionTxt": "CCC - 100(4)",
              "sectionDscTxt": "Firearms Prohibition"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "3372",
      "physicalFileId": "5989.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "55566",
      "mdocSeqNo": "2",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": "C",
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2016-02-04 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Jones, Bill",
          "charge": []
        },
        {
          "fullNm": "Smith, Joe",
          "charge": [
            {
              "sectionTxt": "CCC - 100",
              "sectionDscTxt": "Firearm: posession while prohibited"
            },
            {
              "sectionTxt": "CCC - 319",
              "sectionDscTxt": "PUBLIC INCITEMENT OF HATRED"
            },
            {
              "sectionTxt": "CCC - 344",
              "sectionDscTxt": "Robbery"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "3374",
      "physicalFileId": "5990.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "330",
      "mdocSeqNo": "2",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "Y",
      "nextApprDt": "2018-10-11 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "James, Bill",
          "charge": []
        },
        {
          "fullNm": "smith, will",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 344(a)",
              "sectionDscTxt": "Robbery using firearm"
            },
            {
              "sectionTxt": "CCC - 252(1)(a)",
              "sectionDscTxt": "Failure to stop at scene of accident"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "3871",
      "physicalFileId": "9511.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "3093",
      "mdocSeqNo": "2",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2014-10-01 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "SMITHERS, WAYNE NEWTON",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "37158",
      "physicalFileId": "21170.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "101010",
      "mdocSeqNo": "2",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2016-02-24 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, James",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 357",
              "sectionDscTxt": "Bringing into Canada property obtained by crime"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "12424",
      "physicalFileId": "21173.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "101010",
      "mdocSeqNo": "2",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "S",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2008-08-13 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, James",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 357",
              "sectionDscTxt": "Bringing into Canada property obtained by crime"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "35162",
      "physicalFileId": "43712.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "7152003",
      "mdocSeqNo": "2",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": null,
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "SMITHERS, WAYNE NEWTON",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "37615",
      "physicalFileId": "53136.0734",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "38496",
      "mdocSeqNo": "2",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": null,
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "smith, Robert",
          "charge": [
            {
              "sectionTxt": "CCC - 104(3)(b)",
              "sectionDscTxt": "alter/deface/remove serial no."
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "39847",
      "physicalFileId": "63665.0734",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "2200100",
      "mdocSeqNo": "2",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": null,
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smithers, Samuel",
          "charge": [
            {
              "sectionTxt": "CCC - 334",
              "sectionDscTxt": "Theft (5,000)"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "40435",
      "physicalFileId": "65546.0734",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "7529",
      "mdocSeqNo": "2",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": "C",
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2024-03-01 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Amil",
          "charge": [
            {
              "sectionTxt": "CCC - 266",
              "sectionDscTxt": "Assault"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "40461",
      "physicalFileId": "65650.0734",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "240224",
      "mdocSeqNo": "2",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "S",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2024-04-30 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "SMITH, RON",
          "charge": [
            {
              "sectionTxt": "CCC - 267(1)(b)",
              "sectionDscTxt": "Assault causing bodily harm"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "943",
      "physicalFileId": "1892.0026",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "1059880",
      "mdocSeqNo": "3",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "Y",
      "inCustodyYN": "N",
      "nextApprDt": "2014-03-27 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, John Alexander",
          "charge": [
            {
              "sectionTxt": "CCC - 266",
              "sectionDscTxt": "Assault"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "12425",
      "physicalFileId": "21173.0045",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "101010",
      "mdocSeqNo": "3",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": null,
      "courtLevelCd": "S",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": null,
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, James",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 357",
              "sectionDscTxt": "Bringing into Canada property obtained by crime"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "38461",
      "physicalFileId": "50361.0002",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "2201",
      "mdocSeqNo": "37",
      "ticketSeriesTxt": null,
      "mdocRefTypeCd": "K",
      "courtLevelCd": "P",
      "courtClassCd": "A",
      "warrantYN": "N",
      "inCustodyYN": "Y",
      "nextApprDt": "2021-08-16 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Baddie, William",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 355(b)",
              "sectionDscTxt": "PSP Under $5,000"
            },
            {
              "sectionTxt": "CCC - 742.6",
              "sectionDscTxt": "Breach of Conditional Sentence Order"
            },
            {
              "sectionTxt": "CCC - 117.05",
              "sectionDscTxt": "Firearm Seizure"
            },
            {
              "sectionTxt": "CCC - 810(1)",
              "sectionDscTxt": "Fear of injury or damage by another person"
            },
            {
              "sectionTxt": "CCC - 348(1)(c)",
              "sectionDscTxt": "Breaking out"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        },
        {
          "fullNm": "Badguy, Gill",
          "charge": [
            {
              "sectionTxt": "MVA - 100(2)",
              "sectionDscTxt": "Failing to stop for peace officer"
            },
            {
              "sectionTxt": "CCC - 356(1)(b)",
              "sectionDscTxt": "Theft from mail: possession of article stolen from"
            },
            {
              "sectionTxt": "CCC - 278.3",
              "sectionDscTxt": "Application for production"
            },
            {
              "sectionTxt": "CCC - 742.6",
              "sectionDscTxt": "Breach of Conditional Sentence Order"
            },
            {
              "sectionTxt": "CCC - 117.05",
              "sectionDscTxt": "Firearm Seizure"
            },
            {
              "sectionTxt": "CCC - 111(1)",
              "sectionDscTxt": "Firearm application"
            },
            {
              "sectionTxt": "CCC - 490(2)(a)",
              "sectionDscTxt": "Detention of Things Seized"
            },
            {
              "sectionTxt": "CCC - 490(2)(a)",
              "sectionDscTxt": "Detention of Things Seized"
            },
            {
              "sectionTxt": "CCC - 100",
              "sectionDscTxt": "Firearm: posession while prohibited"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 356(1)(b)",
              "sectionDscTxt": "Theft from mail: possession of article stolen from"
            },
            {
              "sectionTxt": "CCC - 490.012",
              "sectionDscTxt": "Application for order: Sex Offender Info Reg Act"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 355(b)",
              "sectionDscTxt": "PSP Under $5,000"
            }
          ]
        },
        {
          "fullNm": "Badman, Dogooder",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        },
        {
          "fullNm": "Ban, No",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        },
        {
          "fullNm": "Blue, Boy",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 356(1)(a)",
              "sectionDscTxt": "Theft from mail"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        },
        {
          "fullNm": "Bluw, Tom",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 356(1)(a)",
              "sectionDscTxt": "Theft from mail"
            },
            {
              "sectionTxt": "CCC - 352",
              "sectionDscTxt": "Possession of coin-operated device breaking instru"
            }
          ]
        },
        {
          "fullNm": "Brown, Bill",
          "charge": [
            {
              "sectionTxt": "CCC - 357",
              "sectionDscTxt": "Bringing into Canada property obtained by crime"
            },
            {
              "sectionTxt": "CCC - 356(1)(a)",
              "sectionDscTxt": "Theft from mail"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 372(1)",
              "sectionDscTxt": "False message"
            }
          ]
        },
        {
          "fullNm": "Brown, Roy Allen",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(c)",
              "sectionDscTxt": "Breaking out"
            },
            {
              "sectionTxt": "CCC - 348(1)(b)",
              "sectionDscTxt": "Breaking, entering and committing"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 357",
              "sectionDscTxt": "Bringing into Canada property obtained by crime"
            },
            {
              "sectionTxt": "CCC - 253(a)",
              "sectionDscTxt": "Impaired Driving"
            }
          ]
        },
        {
          "fullNm": "Commu, Corrie",
          "charge": [
            {
              "sectionTxt": "CCC - 356(1)(b)",
              "sectionDscTxt": "Theft from mail: possession of article stolen from"
            }
          ]
        },
        {
          "fullNm": "Dup4, Kyle",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        },
        {
          "fullNm": "Francis, Fred David Michael",
          "charge": [
            {
              "sectionTxt": "CCC - 356(1)(a)",
              "sectionDscTxt": "Theft from mail"
            }
          ]
        },
        {
          "fullNm": "GREEN, BOBBY",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        },
        {
          "fullNm": "Gilbert, Roland",
          "charge": [
            {
              "sectionTxt": "CCC - 100",
              "sectionDscTxt": "Firearm: posession while prohibited"
            }
          ]
        },
        {
          "fullNm": "Guy, New",
          "charge": [
            {
              "sectionTxt": "CCC - 278.3",
              "sectionDscTxt": "Application for production"
            },
            {
              "sectionTxt": "CCC - 102(5)",
              "sectionDscTxt": "Firearm: posession while prohibited 102(5)"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 356(1)(b)",
              "sectionDscTxt": "Theft from mail: possession of article stolen from"
            },
            {
              "sectionTxt": "CCC - 490.012",
              "sectionDscTxt": "Application for order: Sex Offender Info Reg Act"
            }
          ]
        },
        {
          "fullNm": "Hroip, Test2",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "MVA - 110(1)",
              "sectionDscTxt": "Default of Insurer"
            },
            {
              "sectionTxt": "CCC - 357",
              "sectionDscTxt": "Bringing into Canada property obtained by crime"
            },
            {
              "sectionTxt": "CCC - 810.1",
              "sectionDscTxt": "Where fear of sexual offence"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 348(1)(c)",
              "sectionDscTxt": "Breaking out"
            },
            {
              "sectionTxt": "CCC - 356(1)(a)",
              "sectionDscTxt": "Theft from mail"
            },
            {
              "sectionTxt": "CCC - 357",
              "sectionDscTxt": "Bringing into Canada property obtained by crime"
            },
            {
              "sectionTxt": "CCC - 357",
              "sectionDscTxt": "Bringing into Canada property obtained by crime"
            },
            {
              "sectionTxt": "CCC - 356(1)(a)",
              "sectionDscTxt": "Theft from mail"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        },
        {
          "fullNm": "Joes Pizza Parlor",
          "charge": [
            {
              "sectionTxt": "MVA - 100(2)",
              "sectionDscTxt": "Failing to stop for peace officer"
            },
            {
              "sectionTxt": "CCC - 356(1)(b)",
              "sectionDscTxt": "Theft from mail: possession of article stolen from"
            },
            {
              "sectionTxt": "CCC - 278.3",
              "sectionDscTxt": "Application for production"
            },
            {
              "sectionTxt": "CCC - 117.03(3)",
              "sectionDscTxt": "Application"
            },
            {
              "sectionTxt": "CCC - 117.05",
              "sectionDscTxt": "Firearm Seizure"
            },
            {
              "sectionTxt": "CCC - 111(1)",
              "sectionDscTxt": "Firearm application"
            },
            {
              "sectionTxt": "CCC - 490(2)(a)",
              "sectionDscTxt": "Detention of Things Seized"
            },
            {
              "sectionTxt": "CCC - 490(2)(a)",
              "sectionDscTxt": "Detention of Things Seized"
            },
            {
              "sectionTxt": "CCC - 58(1)(a)",
              "sectionDscTxt": "fraud use of cert. of citizenship"
            }
          ]
        },
        {
          "fullNm": "Johnson, Dave",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 356(1)(a)",
              "sectionDscTxt": "Theft from mail"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        },
        {
          "fullNm": "Johnson, Kyle",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(c)",
              "sectionDscTxt": "Breaking out"
            },
            {
              "sectionTxt": "CCC - 348(1)(b)",
              "sectionDscTxt": "Breaking, entering and committing"
            }
          ]
        },
        {
          "fullNm": "Johnson, Kyle",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 348(1)(b)",
              "sectionDscTxt": "Breaking, entering and committing"
            },
            {
              "sectionTxt": "CCC - 348(1)(c)",
              "sectionDscTxt": "Breaking out"
            },
            {
              "sectionTxt": "CCC - 357",
              "sectionDscTxt": "Bringing into Canada property obtained by crime"
            },
            {
              "sectionTxt": "CCC - 356(1)(a)",
              "sectionDscTxt": "Theft from mail"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        },
        {
          "fullNm": "Jones, Jeremy Watson",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        },
        {
          "fullNm": "Jones, John",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 356(1)(a)",
              "sectionDscTxt": "Theft from mail"
            },
            {
              "sectionTxt": "CCC - 117.03(3)",
              "sectionDscTxt": "Application"
            },
            {
              "sectionTxt": "CCC - 810.1",
              "sectionDscTxt": "Where fear of sexual offence"
            }
          ]
        },
        {
          "fullNm": "Korell, Lyle",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        },
        {
          "fullNm": "Moosewaypayo, Darcy Fredrick",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 490.029014(4567)(abc)(xyz)",
              "sectionDscTxt": "Notice and Obligation to Comply"
            }
          ]
        },
        {
          "fullNm": "Orange, Chocolate",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 356(1)(b)",
              "sectionDscTxt": "Theft from mail: possession of article stolen from"
            }
          ]
        },
        {
          "fullNm": "Pesto, Olscan",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        },
        {
          "fullNm": "SMITH, BOBBI-JEAN",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        },
        {
          "fullNm": "Smithx, James",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 357",
              "sectionDscTxt": "Bringing into Canada property obtained by crime"
            }
          ]
        },
        {
          "fullNm": "Snack, Joey",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 356(1)(a)",
              "sectionDscTxt": "Theft from mail"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            }
          ]
        },
        {
          "fullNm": "Soso, Sammy",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(b)",
              "sectionDscTxt": "Breaking, entering and committing"
            }
          ]
        },
        {
          "fullNm": "Test, Seal",
          "charge": [
            {
              "sectionTxt": "MVA - 100(2)",
              "sectionDscTxt": "Failing to stop for peace officer"
            },
            {
              "sectionTxt": "CCC - 356(1)(b)",
              "sectionDscTxt": "Theft from mail: possession of article stolen from"
            },
            {
              "sectionTxt": "CCC - 278.3",
              "sectionDscTxt": "Application for production"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 356(1)(b)",
              "sectionDscTxt": "Theft from mail: possession of article stolen from"
            },
            {
              "sectionTxt": "CCC - 490.012",
              "sectionDscTxt": "Application for order: Sex Offender Info Reg Act"
            }
          ]
        },
        {
          "fullNm": "Walling, Lorne",
          "charge": [
            {
              "sectionTxt": "MVA - 100(2)",
              "sectionDscTxt": "Failing to stop for peace officer"
            },
            {
              "sectionTxt": "CCC - 356(1)(b)",
              "sectionDscTxt": "Theft from mail: possession of article stolen from"
            },
            {
              "sectionTxt": "CCC - 278.3",
              "sectionDscTxt": "Application for production"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 356(1)(b)",
              "sectionDscTxt": "Theft from mail: possession of article stolen from"
            },
            {
              "sectionTxt": "CCC - 490.012",
              "sectionDscTxt": "Application for order: Sex Offender Info Reg Act"
            }
          ]
        },
        {
          "fullNm": "Young, Caution Letter",
          "charge": [
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 354(1)(a)",
              "sectionDscTxt": "Illegal possession of goods valued at less $1,000"
            },
            {
              "sectionTxt": "CCC - 352",
              "sectionDscTxt": "Possession of coin-operated device breaking instru"
            }
          ]
        },
        {
          "fullNm": "aorjg",
          "charge": [
            {
              "sectionTxt": "CCC - 100",
              "sectionDscTxt": "Firearm: posession while prohibited"
            }
          ]
        },
        {
          "fullNm": "black",
          "charge": [
            {
              "sectionTxt": "MVA - 100(2)",
              "sectionDscTxt": "Failing to stop for peace officer"
            },
            {
              "sectionTxt": "CCC - 356(1)(b)",
              "sectionDscTxt": "Theft from mail: possession of article stolen from"
            },
            {
              "sectionTxt": "CCC - 278.3",
              "sectionDscTxt": "Application for production"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 356(1)(b)",
              "sectionDscTxt": "Theft from mail: possession of article stolen from"
            },
            {
              "sectionTxt": "CCC - 490.012",
              "sectionDscTxt": "Application for order: Sex Offender Info Reg Act"
            }
          ]
        },
        {
          "fullNm": "kjhkh",
          "charge": [
            {
              "sectionTxt": "MVA - 100(2)",
              "sectionDscTxt": "Failing to stop for peace officer"
            },
            {
              "sectionTxt": "CCC - 356(1)(b)",
              "sectionDscTxt": "Theft from mail: possession of article stolen from"
            },
            {
              "sectionTxt": "CCC - 278.3",
              "sectionDscTxt": "Application for production"
            },
            {
              "sectionTxt": "CCC - 348(1)(a)",
              "sectionDscTxt": "Breaking and entering with intent"
            },
            {
              "sectionTxt": "CCC - 356(1)(b)",
              "sectionDscTxt": "Theft from mail: possession of article stolen from"
            },
            {
              "sectionTxt": "CCC - 490.012",
              "sectionDscTxt": "Application for order: Sex Offender Info Reg Act"
            }
          ]
        }
      ]
    },
    {
      "mdocJustinNo": "221",
      "physicalFileId": "896.0026",
      "fileHomeAgencyId": "83.0001",
      "fileNumberTxt": "555666666666",
      "mdocSeqNo": "9999",
      "ticketSeriesTxt": "14",
      "mdocRefTypeCd": "GGGGGG",
      "courtLevelCd": "P",
      "courtClassCd": "T",
      "warrantYN": "N",
      "inCustodyYN": "N",
      "nextApprDt": "2014-07-10 00:00:00.0",
      "pcssCourtDivisionCd": "R",
      "sealStatusCd": "NA",
      "approvalCrownAgencyTypeCd": "PRV",
      "participant": [
        {
          "fullNm": "Smith, Joe",
          "charge": [
            {
              "sectionTxt": "MVA - 14(2)",
              "sectionDscTxt": "Fail to notify change of name/address of owner"
            }
          ]
        }
      ]
    }
  ];

  getLocation(fileHomeAgencyId: string) {
    return this.courtRooms.find(r => r.value === fileHomeAgencyId)?.text || '';
  }

  getLevel(courtLevelCd: string) {
    return this.levels.find(l => l.code === courtLevelCd)?.shortDesc || '';
  }

  getClass(courtClassCd: string) {
    return this.classes.find(l => l.code === courtClassCd)?.shortDesc || '';
  }

  public handleCaseClick(mdocJustinNo: string) {
    console.log(mdocJustinNo);
  }
}
</script>

<style scoped>
.card {
  border: white;
}
</style>