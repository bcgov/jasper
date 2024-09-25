<script>
import dayGridPlugin from '@fullcalendar/daygrid';
import FullCalendar from '@fullcalendar/vue';

export default {
    components: {
        FullCalendar
    },
    props: {events: {type:Array, required:false}}, 
           methods: {
                    handleMonthChange() {
                        const calendarApi = this.$refs.calendar.getApi();
                        this.$emit('monthChange', calendarApi.getDate());
                    }
                },
    watch: {
        events: function(newValue, oldValue) {
            this.calendarOptions.events = newValue;
        }
    },

    data: function () {
        return {
            calendarOptions: {
                plugins: [dayGridPlugin],
                initialView: 'dayGridMonth',
                weekends: true,
                datesSet: this.handleMonthChange,
                events: [...this.events],
                headerToolbar: {
                    left: 'title', // exclude 'today' button
                    center: 'prev,next today customJumpToDate',
                    right: ''
                },
                 titleFormat:  { // customize the title format
                    month: 'long', // full month name
                    year: 'numeric' // full year
                },
                customButtons: { 
                                    customJumpToDate: {
                                        text: 'Jump to Date',
                                        click: () => {
                                            // Define what happens when the button is clicked
                                            console.log('Jump to Date');
                                        }
                                    }
                }
         
            }
        }
    }
}
</script>
<style>
.fc-day-sat, .fc-day-sun { background: #f0f0f0; }
.fc-toolbar-title:before{
        content:"Scheduled for ";
        font-size: 17x;
    }
    .fc .fc-toolbar-title{
        font-size: 17px;
    }
    .fc .fc-button-primary:disabled, .fc-today-button:hover, 
    .fc .fc-today-button{
        background: white!important;
        color: black!important;
        height:41px;
    }
.fc-next-button, .fc-prev-button {
        background-color: white!important;
        color: black!important;
        border:0!important;
}

.fc .fc-toolbar{
justify-content:flex-start;
}
    .fc-customJumpToDate-button.fc-button.fc-button-primary {
        background-color: white;
        color: black;
        border: 1px solid black;
        cursor: pointer;
        color: black;
        font-size:17px;
        height:41px;
    }
    .fc-toolbar-title{
         width: 265px; 
    }
        .fc .fc-day-today{
background: ###ffffff!important;
    }
    .fc .fc-day-selected{
background: #fdfaf4!important;
    }
</style>
<template>
    <div>
        <FullCalendar :options='calendarOptions' ref='calendar'/>
    </div>
</template>