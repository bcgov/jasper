<template>
   <b-card  v-if= "isMounted" no-body> 
        <b-table-simple small responsive borderless>
            <b-thead>                    
                <b-tr >
                    <b-th v-for="(head,index) in fields" v-bind:key="index" variant="info" :class="head.headerStyle" >                            
                        <b-icon-caret-up-fill v-if="(index<2|| index==3) && dateSortDir=='asc' && sortBy==head.key" @click="sortClick(head)"></b-icon-caret-up-fill>
                        <b-icon-caret-down-fill v-else-if="(index<2|| index==3) && dateSortDir=='desc' && sortBy==head.key" @click="sortClick(head)"></b-icon-caret-down-fill>
                        <b-icon-caret-up v-else-if="(index<2|| index==3) && sortBy!=head.key" @click="sortClick(head)"></b-icon-caret-up>
                        {{head.key}}
                    </b-th>                        
                </b-tr>
            </b-thead>

            <b-tbody v-for="(counts,inx) in SortedParticipantSentencesCounts" v-bind:key="inx">
                <b-tr v-for="(sentence,index) in counts.sentenceDispositionType" v-bind:key="index" :style ="getRowStyle(index)">                            
                    
                    <b-td :rowspan="counts.len" v-if="index==0">{{counts.date | beautify_date}}</b-td>                            
                    <b-td :rowspan="counts.len" v-if="index==0">{{counts.count}}</b-td>
                    
                    <b-td>                                
                        <b> {{counts.chargeIssueCd[index]}} </b> 
                        <span v-if="counts.chargeIssueCd[index]">
                            &mdash; 
                            <b-badge
                                variant = "secondary"  
                                v-b-tooltip.hover.right 
                                :title="counts.chargeIssueDscFull[index]"> 
                                {{counts.chargeIssueDsc[index]}} 
                            </b-badge>
                        </span>
                    </b-td>
                    
                    <b-td :rowspan="counts.len" v-if="index==0">
                        <b-badge
                            v-if="counts.finding"
                            variant = "secondary"
                            v-b-tooltip.hover.right
                            :title="counts.findingDsc"> 
                            {{counts.finding}} 
                        </b-badge>
                    </b-td>

                    <b-td>
                        <b-badge
                            v-if="sentence"
                            variant = "secondary"  
                            v-b-tooltip.hover.right                             
                            :title="counts.sentenceDsc[index]"> 
                            {{sentence}} 
                        </b-badge>
                    </b-td>

                    <b-td> {{counts.term[index]}} </b-td>
                    <b-td> {{counts.amount[index]}} </b-td>
                    <b-td> {{counts.dueDateUntil[index] | beautify_date }} </b-td>
                    <b-td> {{counts.effectiveDate[index] | beautify_date }} </b-td>
                    
                </b-tr>                       
            </b-tbody>
        </b-table-simple>      
   </b-card> 
</template>

<script lang="ts">
import { Component, Vue} from 'vue-property-decorator';
import * as _ from 'underscore';
import { namespace } from 'vuex-facing-decorator';
import '@store/modules/CriminalFileInformation';
import {participantSentencesDetailsInfoType, participantSentencesInfoType} from '@/types/criminal';
const criminalState = namespace("CriminalFileInformation");

@Component
export default class CriminalSentenceDetails extends Vue {   

    @criminalState.State
    public criminalParticipantSentenceInformation!: participantSentencesDetailsInfoType
            
    sortBy = 'date';
    dateSortDir = 'desc';   
    isMounted = false;
    selectedParticipant = 0;
    participantSentences: participantSentencesInfoType[] = [];

    fields = [        
        {key:'date',                     label:'Date',                         sortable:true,   tdClass: 'border-top', headerStyle:'text-primary'},
        {key:'count',                    label:'Count',                        sortable:true,   tdClass: 'border-top', headerStyle:'text-primary'},
        {key:'chargeIssue',              label:'Charge/Issue',                 sortable:false,  tdClass: 'border-top', headerStyle:'text'},
        {key:'finding',                  label:'Finding',                      sortable:true,   tdClass: 'border-top', headerStyle:'text-danger'},        
        {key:'sentenceDispositionType',  label:'Sentence/ Disposition Type',   sortable:false,  tdClass: 'border-top', headerStyle:'text'},   
        {key:'term',                     label:'Term',                         sortable:false,  tdClass: 'border-top', headerStyle:'text'},
        {key:'amount',                   label:'Amount',                       sortable:false,  tdClass: 'border-top', headerStyle:'text'},
        {key:'dueDateUntil',             label:'Due Date/ Until',              sortable:false,  tdClass: 'border-top', headerStyle:'text'},
        {key:'effectiveDate',            label:'Effective Date',               sortable:false,  tdClass: 'border-top', headerStyle:'text'},
    ];
    
    public getParticipants(): void {       
         
        this.participantSentences = this.criminalParticipantSentenceInformation.participantSentences;
        this.selectedParticipant = this.criminalParticipantSentenceInformation.selectedParticipant;         
        this.isMounted = true;
    }

    mounted () {        
        this.getParticipants();  
    }
    
    get SortedParticipantSentencesCounts()
    {
        if(this.dateSortDir =='desc')
            return _.sortBy(this.participantSentences[this.selectedParticipant].counts, this.sortBy).reverse()
        else
            return _.sortBy(this.participantSentences[this.selectedParticipant].counts, this.sortBy)  
    } 

    get NumberOfCounts() { 
        return(this.participantSentences[this.selectedParticipant].counts.length) 
    }

    public getRowStyle(index) {
        if(index==0) return "border-top : 1px solid #999;"
        return '';
    }
    
    public sortClick(data)
    {
        this.sortBy = data.key
        if(this.dateSortDir =='desc')
            this.dateSortDir ='asc';
        else
            this.dateSortDir ='desc';
    }
}
</script>

<style scoped>
 .card {
        border: white;
    }

</style>