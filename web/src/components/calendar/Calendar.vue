<script>
import dayGridPlugin from '@fullcalendar/daygrid';
import interactionPlugin from '@fullcalendar/interaction';
import FullCalendar from '@fullcalendar/vue';
import { MonthPicker } from 'vue-month-picker';
export default {
    components: {
        FullCalendar,
        MonthPicker
    },

    props: { events: { type: Array, required: false }, sizeChange: { type: Number, requred: false }, isMySchedule: { type: Boolean, requred: false } },
    methods: {
        changeMonth(date) {
            this.date = date;
            this.$refs.calendar.getApi().gotoDate(date.from);
            this.$refs.jumpMonthModal.hide();
            this.calendarOptions.customButtons['customJumpToDate'].text = `Schedule for ${date.month} ${date.year}`;

            this.$refs.calendar.getApi().render();
            this.currentSelectedMonth = new Date(date.from).getMonth() + 1;
            this.currentSelectedYear = date.year;
        },
        handleMonthChange(info) {
            const calendarApi = this.$refs.calendar.getApi();
            this.$emit('monthChange', calendarApi.getDate());
            const today = new Date();
            if (today >= info.start && today < info.end) {
                this.calendarOptions.customButtons['customJumpToDate'].text = `Schedule for ${new Date().toLocaleDateString('en-US', { year: 'numeric', month: 'long' }).replace(',', '')}`;
                this.currentSelectedMonth = new Date().getMonth() + 1;
                this.currentSelectedYear = new Date().getFullYear();
            }
        },
        customEventContent(arg) {
            if (arg.event.extendedProps.assignment) {
                if (this.isMySchedule) {
                    return {
                        html: `
                        <div class="custom-events-container">
                        ${"<div class='city'>" + arg.event.extendedProps.assignment.locationName + (arg.event.extendedProps.assignment.isVideo ? "<span class='camera'></span>" : '') + "</div>"}
                        <div class="eventTitle" style="margin-top: 10px;">${arg.event.extendedProps.showAM ? "<span class='daytime'>AM</span>" : ''}${arg.event.extendedProps.assignment.activityAm.activityDescription}<span  class="loc">${arg.event.extendedProps.assignment.activityAm.courtRoomCode ? "(" + arg.event.extendedProps.assignment.activityAm.courtRoomCode + ")" : ''}</span></div>
                        ${arg.event.extendedProps.showPMLocation ? "<div class='city' style='margin-top: 20px;'>" + arg.event.extendedProps.assignment.activityPm.locationName + (arg.event.extendedProps.assignment.isVideo ? "<span class='camera'></span>" : '') + "</div>" : ''}
                        ${arg.event.extendedProps.showPM ? "<div class='eventTitle' style='margin-top: 10px;'><span class='daytime'>PM</span>" + arg.event.extendedProps.assignment.activityPm.activityDescription + "<span  class='loc'>" + (arg.event.extendedProps.assignment.activityPm.courtRoomCode ? "(" + arg.event.extendedProps.assignment.activityPm.courtRoomCode + ")" : '') + "</span></div>" : ''}
                        </div>
                        `
                    };
                }
                return {
                    html: `
                        <div class="custom-events-container">
                        ${"<div class='city'>" + arg.event.extendedProps.rotaInitials + ' - ' + arg.event.extendedProps.assignment.locationName + "</div>"}
                        <div class="eventTitle">${arg.event.extendedProps.showAM ? "<span class='daytime'>AM</span>" : ''}${arg.event.extendedProps.assignment.activityAm.activityDescription}<span  class="loc">${arg.event.extendedProps.assignment.activityAm.courtRoomCode ? "(" + arg.event.extendedProps.assignment.activityAm.courtRoomCode + ")" : ''}</span></div>
                        ${arg.event.extendedProps.showPMLocation ? "<div class='city'>" + arg.event.extendedProps.assignment.activityPm.locationName + "</div>" : ''}
                        ${arg.event.extendedProps.showPM ? "<div class='eventTitle'><span class='daytime'>PM</span>" + arg.event.extendedProps.assignment.activityPm.activityDescription + "<span  class='loc'>" + (arg.event.extendedProps.assignment.activityPm.courtRoomCode ? "(" + arg.event.extendedProps.assignment.activityPm.courtRoomCode + ")" : '') + "</span></div>" : ''}
                        </div>
                        `
                };
            }
        },

        customizeDayCell(info) {
            const dayEl = info.el;
            const linkDate = new Date(info.date.toString());
            const link = `<a href="${linkDate.getDate()}/${linkDate.getFullYear()}/${linkDate.getMonth() + 1}" class="custom-day"></a>`
            dayEl.innerHTML = (new Date().setHours(0, 0, 0, 1) <= new Date(info.date.toString()).setHours(23, 59, 0, 1) && new Date(info.date.toString()) <= new Date(Date.now() + 10 * 24 * 60 * 60 * 1000)) ? link : '';
            dayEl.classList.add('custom-day-class');
        },

        handleDateClick(info) {
           // console.log(info);
            //  this.popoverEvent = info.event;
            // const popover = this.$refs.popover;

            //  if (this.popperInstance) {
            //        this.popperInstance.destroy();
            //      }
            //  this.popperInstance = createPopper(info.el, popover, {
            //        placement: 'top',
            // });
            //  popover.style.display = 'block';
        },
        closePopover() {
            this.popoverEvent = null;
            if (this.popperInstance) {
                this.popperInstance.destroy();
                this.popperInstance = null;
            }
        },
        handleEventClick(info) {
            console.log(info);

            //       this.showSelectedEventModal = true;
            //       this.selectedEvent = {...info.event._def, date: info.event.start? info.event.start : null}; 
        }
    },
    watch: {
        events: function (newValue) {
            this.calendarOptions.events = [...newValue];
        },
        sizeChange: function () {
            this.$refs.calendar.getApi().updateSize();
        },
        deep: true,
    },
    data: function () {
        return {
            calendarConfig: {
                year: new Date().getFullYear(),
                month: new Date().getMonth() + 1
            },
            date: {
                from: null,
                to: null,
                month: null,
                year: null
            },
            showJumpToDateModal: false,
            currentSelectedMonth: new Date().getMonth() + 1,
            currentSelectedYear: new Date().getFullYear(),
            showDateModal: false,
            selectedDate: null,
            popoverEvent: null,
            popperInstance: null,
            selectedEvent: null,
            showSelectedEventModal: false,
            calendarOptions: {
                height: 'auto',
                plugins: [dayGridPlugin, interactionPlugin],
                initialView: 'dayGridMonth',
                weekends: true,
                datesSet: this.handleMonthChange,
                events: this.events,
                eventContent: this.customEventContent,
                dayCellDidMount: this.customizeDayCell,
                customButtons: {
                    customJumpToDate: {
                        text: `Schedule for ${new Date().toLocaleDateString('en-US', { year: 'numeric', month: 'long' })}`,
                        click: () => {
                            // Define what happens when the button is clicked
                            this.showJumpToDateModal = true;
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

.daytime {
    padding: 1px 8px;
    font-weight: normal;
    color: #fff;
    background-color: #183A4A;
    margin-right: 8px;
    border-radius: 32px;
}

.popover {
    display: none;
    position: absolute;
    background-color: white;
    padding: 10px;
    border: 1px solid #ccc;
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
    z-index: 1000;
}

.fc-day {
    position: relative;
}

.custom-day {
    position: absolute;
    top: 5px;
    right: 5px;
    width: 15px;
    height: 10px;
    background-image: url('../../assets/today-list.svg');
    background-repeat: no-repeat;
    background-position: center;
    z-index: 10;
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

.fc-col-header-cell,
.fc-col-header-cell a {
    border-left: 0 !important;
    border-right: 0 !important;
    border-top: 0 !important;

    font-family: WorkSans-Medium, "Work Sans Medium", "Work Sans", sans-serif;
    text-transform: capitalize;
    text-decoration: none;
    color: #494949;
    font-weight: 500;
}

.fc-toolbar-chunk:nth-child(2) {
    width: 100%;
    display: Flex;
    align-items: center;
    justify-content: center;
}

.fc-scrollgrid-sync-table .fc-day {
    height: 120px;
}

.fc .fc-daygrid-day-top,
.fc .fc-daygrid-day-top a {
    flex-direction: row;
    font-family: WorkSans-Medium, "Work Sans Medium", "Work Sans", sans-serif;
    font-weight: bold;
    text-decoration: none;
    color: #494949;
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
    font-family: WorkSans-Bold, "Work Sans Bold", "Work Sans", sans-serif;
    font-weight: 700;
}

.city {
    font-family: WorkSans-Medium, "Work Sans Medium", "Work Sans", sans-serif;
    font-weight: 500;
    color: rgb(49, 49, 50);
}

.loc {
    float: right;
    margin-left: 10px;
    font-family: WorkSans-Regular, "Work Sans", sans-serif;
    font-weight: 400;
    color: rgb(49, 49, 50);
}

.title {
    font-family: WorkSans-Bold, "Work Sans Bold", "Work Sans", sans-serif;
    font-weight: 700;
    color: rgb(30, 152, 215);
}

.month-picker__container {
    box-shadow: none;
    margin: auto;
}

.month-picker__year {
    box-shadow: none;
}

.month-picker__year p {
    font-weight: normal;
}

.month-picker__year button,
.month-picker__year button:hover {
    box-shadow: none;
    background-color: transparent;
    border: 0px;
}

.month-picker__month {
    margin: 5px;
    box-shadow: none;
    color: #183A4A;
    border: 1px solid transparent;
    background-color: #f2f2f2;
}

.month-picker {
    box-shadow: none;
}

.month-picker__month:hover,
.month-picker__container .selected {
    border: 1px solid #55b0f5;
    font-weight: bold;
    background-color: #ecf4ff;
    box-shadow: none;
    color: #183A4A;
    text-shadow: none;
}

.event-modal-body {
    padding: 10px;
    font-size: 14px;
}

.event-modal-body .title {
    color: #000;
    font-size: 14px;
}

.event-modal-body .date {
    padding: 10px 0;
    font-weight: bold;
}

.event-modal-body .city-title:before {
    content: '';
    background-image: url('../../assets/video.svg');
    width: 15px;
    height: 10px;
    margin-right: 10px;
    display: block;
}

.event-modal-body .city-title {
    display: Flex;
    align-items: center;
    justify-content: flex-start;
}

.event-modal-body .city .icon-block {
    width: 15px;
    height: 10px;
    background-image: url('../../assets/video.svg');
}

.event-modal-body .city {
    display: Flex;
    align-items: center;
    justify-content: space-between;
}

.event-modal-body .row {
    padding: 10px 0;
    border-top: 1px solid #c5c3c5;
}

.event-modal-body .continuation {
    margin-top: 10px;
    flex-direction: column;
}

.event-modal-body .continuation ul {
    list-style: none;
    display: block
}
</style>
<template>
    <div>
        <FullCalendar :events='events' :options='calendarOptions' ref='calendar' />

        <!-- <b-modal id="day-click-modal" ref="dayModal" v-model="showDateModal">
            <div>
                Day Clicked
            </div>
        </b-modal> -->
        <div v-if="popoverEvent" ref="popover" class="popover">
            <h3>{{ popoverEvent.title }}</h3>
            <p>{{ popoverEvent.start.toLocaleString() }}</p>
            <button @click="closePopover">Close</button>
        </div>

        <b-modal id="JumpMonth-click-modal" ref="jumpMonthModal" v-model="showJumpToDateModal" hide-footer hide-header>
            <div>
                <month-picker @input="changeMonth" :default-month="currentSelectedMonth"
                    :default-year="currentSelectedYear"></month-picker>
            </div>
        </b-modal>

        <b-modal id="selectedEvent-click-modal" ref="selectedEventModal" v-model="showSelectedEventModal" hide-backdrop
            hide-footer hide-header>
            <div v-if="selectedEvent" class="event-modal-body">
                <div class="date" v-if="selectedEvent && selectedEvent.date">
                    {{ selectedEvent.date.toLocaleDateString('en-US', {
                        day: 'numeric',
                        month: 'long',
                    year: 'numeric'
                    })}}</div>
                <div class="title row">{{ selectedEvent.title }}</div>

                <div class="city row"><span class="city-title">{{ selectedEvent.extendedProps.location }}</span> <span
                        class="icon-block"></span></div>
                <div class="location row" v-if="selectedEvent.extendedProps.room">{{ selectedEvent.extendedProps.room }}
                </div>
                <div class="continuation row">
                    <div class="continuation-title">Continuations</div>
                    <ul>
                        <li><a href="#">1234567</a></li>
                        <li><a href="#">1234567</a></li>
                    </ul>
                </div>

            </div>
        </b-modal>
    </div>

</template>
