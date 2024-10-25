<template>
    <b-card bg-variant="white" no-body>
        <div>
            <h3 class="mx-4 font-weight-normal"> Adjudicator Restrictions </h3>
            <hr class="mx-3 bg-light" style="height: 5px;"/> 
        </div>

        <b-card v-if="!(adjudicatorRestrictions.length>0)" no-body>
            <span class="text-muted ml-4 mb-5"> No adjudicator restrictions. </span>
        </b-card>

        <b-card bg-variant="white" v-if="isMounted && (adjudicatorRestrictions.length>0)" no-body class="mx-3 mb-5">           
            <b-table        
            borderless
            :items="adjudicatorRestrictions"
            :fields="fields"
            :sort-by.sync="sortBy"
            :sort-desc.sync="sortDesc"
            sort-icon-left
            small
            responsive="sm"
            >   
                <template v-for="(field,index) in fields" v-slot:[`head(${field.key})`]="data">
                    <b v-bind:key="index" :class="field.headerStyle" > {{ data.label }}</b>
                </template>
                <template v-slot:cell(status)="data" >                   
                    <b-badge 
                        variant="primary" 
                        :style="data.field.cellStyle"> 
                        {{ data.value }}
                    </b-badge>
                </template>
            </b-table>
        </b-card>
    </b-card>    
</template>

<script lang="ts">
import { Component, Vue } from "vue-property-decorator";
import { namespace } from "vuex-facing-decorator";
import {criminalFileInformationType} from '@/types/criminal';
import {AdjudicatorRestrictionsInfoType } from '@/types/common';
import "@store/modules/CriminalFileInformation";
const criminalState = namespace("CriminalFileInformation");

@Component
export default class  CriminalAdjudicatorRestrictions extends Vue {
 
  @criminalState.State
  public criminalFileInformation!: criminalFileInformationType;

  adjudicatorRestrictions: AdjudicatorRestrictionsInfoType[] = [];
    
  sortBy = 'adjudicator';
  sortDesc = false;
  isMounted = false;  

  fields =  
  [
      {key:'adjudicator',   label:'Adjudicator', sortable:true, tdClass: 'border-top',  headerStyle:'table-borderless text-primary'},       
      {key:'status',        label:'Status',      sortable:true, tdClass: 'border-top',  headerStyle:'text-primary', cellStyle: 'font-weight: normal; font-size: 16px;'},
      {key:'appliesTo',     label:'Applies to',  sortable:true, tdClass: 'border-top',  headerStyle:'text-primary'},
           
  ];

  mounted() {
    this.getAdjudicatorRestrictions();
  }

  public getAdjudicatorRestrictions(): void {         
      this.adjudicatorRestrictions = this.criminalFileInformation.adjudicatorRestrictionsInfo;
      this.isMounted = true;          
  }
  
}
</script>

<style scoped>
 .card {
        border: white;
    }
</style>