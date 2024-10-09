<script>
import dayGridPlugin from '@fullcalendar/daygrid';
import interactionPlugin from '@fullcalendar/interaction';
import FullCalendar from '@fullcalendar/vue';

export default {
    components: {
        FullCalendar
    },
    props: { events: { type: Array, required: false }, sizeChange: { type: Number, requred: false } },
    methods: {
        handleMonthChange() {
            const calendarApi = this.$refs.calendar.getApi();
            this.$emit('monthChange', calendarApi.getDate());
        },
        customEventContent(arg) {
            return {
                html: `
                        <div class="custom-events-container">
                        ${arg.event.extendedProps.city ? "<div class='city'>" + arg.event.extendedProps.city + (arg.event.extendedProps.city === 'Kelowna' ? "<span class='camera'></span>" : '') + "</div>" : ""}
                         <div class="loc">${arg.event.extendedProps.location}</div>
                        <div class="eventTitle">${arg.event.title}</div>
                        </div>
                        `
            };
        },
        handleDateClick() {
            this.showDateModal = true;
        },
        handleEventClick() {
            console.log('event clicked')
        }
    },
    watch: {
        events: function (newValue) {
            this.calendarOptions.events = [...newValue];
        },
        sizeChange: function (newValue) {
            console.log(newValue);
            this.$refs.calendar.getApi().updateSize();
        },
        deep: true,
    },
    data: function () {
        return {
            showDateModal: false,
            selectedDate: null,
            calendarOptions: {
                plugins: [dayGridPlugin, interactionPlugin],
                initialView: 'dayGridMonth',
                weekends: true,
                datesSet: this.handleMonthChange,
                events: this.events,
                eventContent: this.customEventContent,
                customButtons: {
                    customJumpToDate: {
                        text: `Schedule for ${new Date().toLocaleDateString('en-US', { year: 'numeric', month: 'long' })}`,
                        click: () => {
                            // Define what happens when the button is clicked
                            console.log('Jump to Date');
                        }
                    },
                    customIntervalSelector: {
                        text: 'Month',
                        click: () => {
                            // Define what happens when the button is clicked
                            console.log('IntervalSelector');
                        }
                    }
                },
                headerToolbar: {
                    left: '', // exclude 'today' button
                    center: 'customJumpToDate today customIntervalSelector',
                    right: ''
                },
                titleFormat: { // customize the title format
                    month: 'long', // full month name
                    year: 'numeric' // full year
                },
                eventClick: this.handleEventClick,
                dateClick: this.handleDateClick

            }
        }
    }
}
</script>
<style>
.camera {
    width: 15px;
    height: 10px;
    position: absolute;
    right: 15px;
    top: 5px;
    display: block;
    border: 0 !important;
    background-image: url('../../assets/video.svg');
    background-position: center;
    background-size: contain;
}

.fc-day:hover {
    background-color: #B4E5FF;
}

.fc .fc-customJumpToDate-button:hover,
.fc .fc-customJumpToDate-button:active,
.fc .fc-customJumpToDate-button:focus,
.fc .fc-customJumpToDate-button {
    background-color: transparent !important;
    padding-right: 30px;
    border: 0;
    color: #4092C1 !important;
    outline: none;
    position: relative;
    font-size: 16px;
    font-family: "Work Sans", sans-serif;
    margin-right: 10px;
    box-shadow: none !important;
}

.fc .fc-customJumpToDate-button:after {
    content: "";
    position: absolute;
    right: 0;
    bottom: 8px;
    background-image: url('../../assets/arrow-down.svg');
    background-repeat: no-repeat;
    background-position: center;
    background-size: 20px 20px;
    width: 20px;
    display: block;
    height: 20px;
}

.fc .fc-customIntervalSelector-button:hover,
.fc .fc-customIntervalSelector-button:active,
.fc .fc-customIntervalSelector-button:focus,
.fc .fc-customIntervalSelector-button {
    background-color: transparent !important;
    padding-right: 30px;
    padding-left: 30px;
    border: 0;
    color: #4092C1 !important;
    outline: none !important;
    position: relative;
    font-size: 16px;
    font-family: "Work Sans", sans-serif;
    box-shadow: none !important;
}

.fc .fc-customIntervalSelector-button:before {
    position: absolute;
    left: 0;
    content: '';
    bottom: 10px;
    background-image: url('../../assets/calendar-select.svg');
    background-repeat: no-repeat;
    background-position: center;
    background-size: 15px 15px;
    width: 20px;
    display: block;
    height: 20px;
}

.fc .fc-customIntervalSelector-button:after {
    position: absolute;
    right: 0;
    content: '';
    bottom: 10px;
    background-image: url('../../assets/arrow-down.svg');
    background-repeat: no-repeat;
    background-position: center;
    background-size: 15px 15px;
    width: 20px;
    display: block;
    height: 20px;
}

.fc-col-header-cell {
    border-left: 0 !important;
    border-right: 0 !important;
    border-top: 0 !important;

    font-family: "Work Sans", sans-serif;
    text-transform: capitalize;

}

.fc-toolbar-chunk:nth-child(2) {
    width: 100%;
    display: Flex;
    align-items: center;
    justify-content: center;
}

.fc .fc-daygrid-day-top {
    flex-direction: row;
    font-family: "Work Sans", sans-serif;
    font-weight: bold;
}

.fc-day-sat,
.fc-day-sun {
    background: #f0f0f0;
}

.fc-toolbar-title:before {
    content: "Schedule for ";
    font-size: 17px;
}

.fc .fc-toolbar-title {
    font-size: 17px;
}

.fc .fc-button-primary:disabled,
.fc-today-button:hover,
.fc .fc-today-button {
    width: 120px;
    cursor: pointer;
    padding: 8px 0;
    font-size: 16px;
    border: 1px solid #7e807e;
    background-color: #fff;
    color: #7e807e;
    border-radius: 32px;
    font-family: sans-serif;
    transition: all ease-in-out 0.4s;
    text-transform: capitalize;
}

.fc-next-button,
.fc-prev-button {
    background-color: white !important;
    color: black !important;
    border: 0 !important;
}

.fc .fc-toolbar {
    justify-content: flex-start;
}

.fc-toolbar-title {
    width: 265px;
}

.fc .fc-day-today {
    background: #f4fafd !important;
    color: #1d98d7 !important;
}

.fc .fc-day-selected {
    background: #fdfaf4 !important;
    color: #d7961e !important;
}

.custom-events-container {
    display: flex;
    align-items: flex-start;
    justify-content: flex-start;
    flex-direction: column;
    font-size: 12px !important;
}

.eventTitle {
    color: blue;
    font-weight: bold;
}

.city {
    font-family: WorkSans-Medium, "Work Sans Medium", "Work Sans", sans-serif;
    font-weight: 500;
    color: rgb(49, 49, 50);
}

.loc {

    font-family: WorkSans-Regular, "Work Sans", sans-serif;
    font-weight: 400;
    color: rgb(49, 49, 50);
}

.title {
    font-family: WorkSans-Bold, "Work Sans Bold", "Work Sans", sans-serif;
    font-weight: 700;
    color: rgb(30, 152, 215);
}
</style>
<template>
    <div>
        <FullCalendar :events='events' :options='calendarOptions' ref='calendar' />
        <b-modal id="day-click-modal" ref="dayModal" v-model="showDateModal">
            <div>
                Day Clicked
            </div>
        </b-modal>
    </div>

</template>
