<template>
   <b-card bg-variant="white" no-body>
        <div>
            <h3 class="mx-4 font-weight-normal"> Crown Notes to JCM </h3>
            <hr class="mx-3 bg-light" style="height: 5px;"/> 
        </div>
        <b-card v-if="!(crownNotes.length>0)" no-body>
            <span class="text-muted ml-4 mb-5"> No crown notes to JCM. </span>
        </b-card>

        <b-card bg-variant="white" v-if="isMounted && (crownNotes.length>0)" no-body class="mx-3 mb-5">           
            <b-table        
            borderless
            :items="crownNotes"
            :fields="fields"            
            thead-class="d-none"
            responsive="sm"          
            striped
            >   
                <template v-for="(field,index) in fields" v-slot:[`cell(${field.key})`]="data" >                   
                    <span v-bind:key="index" style= "white-space: pre" > {{ data.value }}</span>
                </template>
            </b-table>
        </b-card>
    </b-card>
</template>

<script lang="ts">
import { Component, Vue } from 'vue-property-decorator';
import { namespace } from 'vuex-facing-decorator';
import {criminalFileInformationType, criminalCrownNotesInfoType} from '@/types/criminal';
import '@store/modules/CriminalFileInformation';
const criminalState = namespace('CriminalFileInformation');

@Component
export default class CriminalCrownNotes extends Vue {

    @criminalState.State
    public criminalFileInformation!: criminalFileInformationType;

    crownNotes: criminalCrownNotesInfoType[] = [];        
    isMounted = false;

    fields = [
        {key:'crownNotes', label: 'Crown Notes'}
    ];

    public getCrownNotes(): void {
       
        const data = this.criminalFileInformation.detailsData;
        
        if (data.trialRemark.length > 0) {
            for (const note of data.trialRemark) {
                const crownNote = {} as criminalCrownNotesInfoType;
                crownNote.crownNotes = note.commentTxt
                this.crownNotes.push(crownNote)
            }
        }        
        this.isMounted = true;
    }

    mounted () {              
        this.getCrownNotes();  
    }      
}
</script>

<style scoped>
 .card {
        border: white;
    }
</style>