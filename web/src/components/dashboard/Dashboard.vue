<template>
  <v-container class="p-0">
    <v-skeleton-loader
      v-if="isLoading"
      type="date-picker"
      :loading="isLoading"
    ></v-skeleton-loader>

    <div v-else class="d-flex flex-column">
      <CourtToday v-if="todaySchedule" :today="todaySchedule" />
      <CalendarToolbar v-if="selectedDate" v-model="selectedDate" />
      <MyCalendar
        v-if="calendarData && selectedDate"
        :data="calendarData"
        :selectedDate
      />
    </div>
  </v-container>
  <!-- <div class="judges-dashboard">
    <section class="dashboard-container">
      <div class="tools-container">
        <button class="filters" @click="toggleLeft()">
          <img src="../../assets/filters.svg" alt="filters" />
          <span> Filters </span>
        </button>
        <button class="more" @click="toggleRight()">
          <img src="../../assets/more3dots.svg" alt="more" />
          <span>More</span>
        </button>
      </div>
      <div class="calendar-container">
        <div
          :class="{
            'left-menu-active': showLeftMenu,
            'left-menu': !showLeftMenu,
          }"
        >
          <div class="accordion-content">
            <button
              class="accordion-button"
              v-b-toggle.locations
              variant="primary"
            >
              Locations
              <img src="../../assets/arrow-down.svg" alt="more" />
            </button>

            <b-collapse id="locations" class="mt-2">
              <b-card>
                <b-form-checkbox-group
                  id="locations-filter-box"
                  v-model="selectedLocations"
                  :options="getLocations"
                  name="locations-filter"
                  :value-field="'locationId'"
                  :text-field="'name'"
                  @change="onLocationChecked"
                ></b-form-checkbox-group>
              </b-card>
              <button
                v-if="locations.length > 0"
                class="moreItems"
                @click="showAllLocations()"
              >
                <span>See all locations</span>
              </button>
            </b-collapse>
          </div>

          <div class="accordion-content" v-if="selectedLocations.length > 0">
            <button
              class="accordion-button"
              v-b-toggle.presiders
              variant="primary"
            >
              Presiders
              <img src="../../assets/arrow-down.svg" alt="more" />
            </button>

            <b-collapse id="presiders" class="mt-2">
              <b-card>
                <b-form-checkbox
                  id="select-all-presiders"
                  v-model="areAllPresidersChecked"
                  @change="onSelectAllPresiders"
                >
                  All Persiders
                </b-form-checkbox>
                <b-form-checkbox-group
                  id="presiders-filter-box"
                  v-model="selectedPresiders"
                  :options="getPresiders"
                  name="presiders-filter"
                  :value-field="'value'"
                  :text-field="'text'"
                  @change="onPreciderChecked"
                ></b-form-checkbox-group>
              </b-card>
              <button
                v-if="presiders.length > 0"
                class="moreItems"
                @click="showAllPresiders()"
              >
                <span>See all presiders</span>
              </button>
            </b-collapse>
          </div>
          <div class="accordion-content" v-if="selectedLocations.length > 0">
            <button
              class="accordion-button"
              v-b-toggle.activities
              variant="primary"
            >
              Activities
              <img src="../../assets/arrow-down.svg" alt="more" />
            </button>

            <b-collapse id="activities" class="mt-2">
              <b-card>
                <b-form-checkbox
                  id="select-all-activities"
                  v-model="areAllActivitiesChecked"
                  @change="onSelectAllActivities"
                >
                  All Activities
                </b-form-checkbox>
                <b-form-checkbox-group
                  id="activities-filter-box"
                  v-model="selectedActivities"
                  :options="getActivities"
                  name="activities-filter"
                  :value-field="'value'"
                  :text-field="'text'"
                  @change="onActivityChecked"
                ></b-form-checkbox-group>
              </b-card>
              <button class="moreItems" @click="showAllActivities()">
                <span v-if="activities.length > 1">See all activities</span>
              </button>
            </b-collapse>
          </div>
          <div class="activitiesPanel" v-if="selectedLocations.length > 0">
            <b-form-checkbox
              class="singleCheckbox"
              id="sittingActivities"
              v-model="sittingActivities"
            >
              Show sitting activities
            </b-form-checkbox>
          </div>
          <div v-if="selectedLocations.length > 0">
            <b-form-checkbox
              class="singleCheckbox"
              id="nonSittingActivities"
              v-model="nonSittingActivities"
            >
              Show non-sitting activities
            </b-form-checkbox>
          </div>

          <div>
            <button class="defaultButton">
              <span>Back to my calendar</span>
            </button>

            <button class="inverseButton">
              <span>Favourite current filters</span>
            </button>
          </div>
        </div>
        <div
          class="dashboard-calendar"
          :class="{ 'calendar-active': menuActive, '': !menuActive }"
        >

          <Calendar
            :events="arEvents"
            @monthChange="getMonthlySchedule"
            :sizeChange="sizeChange"
            :locations="locations"
            :isMySchedule="isMySchedule"
          />
        </div>
        <div
          :class="{
            'right-menu-active': showRightMenu,
            'right-menu': !showRightMenu,
          }"
        >
          <div>&nbsp;</div>
          <div>&nbsp;</div>
        </div>
      </div>
    </section>
    <section class="dashboard-collapse-section">
      <div class="dashboard-collapse">Reserved Judgement (5)</div>
      <div class="dashboard-collapse">Reserved Judgement (5)</div>
    </section>
    <b-modal
      id="locations-click-modal"
      hide-footer
      centered
      size="lg"
      ref="locationsModal"
      v-model="showLocationModal"
    >
      <div>
        <div class="modal-header-title">Locations</div>
        <div class="locations-columns">
          <div class="column">
            <b-form-checkbox-group
              id="locations-filter-box"
              v-model="selectedLocations"
              :options="locations"
              name="locations-filter"
              :value-field="'locationId'"
              :text-field="'name'"
            ></b-form-checkbox-group>
          </div>
        </div>
        <div class="modal-buttons">
          <button class="moreItems" @click="onResetLocations">Reset</button>
          <button class="modal-button-right" @click="onFilterLocations">
            Save
          </button>
        </div>
      </div>
    </b-modal>
    <b-modal
      id="presiders-click-modal"
      hide-footer
      centered
      size="lg"
      hide-backdrop
      ref="presidersModal"
      v-model="isPresiderModalVisible"
    >
      <div>
        <div class="modal-header-title">Presiders</div>
        <div class="presiders-columns">
          <div class="column">
            <b-form-checkbox-group
              id="presiders-filter-box"
              v-model="selectedPresiders"
              :options="presiders"
              name="presiders-filter"
              @change="onPreciderChecked"
            ></b-form-checkbox-group>
          </div>
        </div>
        <div class="modal-buttons">
          <button class="moreItems" @click="resetPresiders()">Reset</button>
          <button class="modal-button-right">Save</button>
        </div>
      </div>
    </b-modal>

    <b-modal
      id="activities-click-modal"
      hide-footer
      centered
      size="lg"
      hide-backdrop
      ref="activitiesModal"
      v-model="isActivitiesModalVisible"
    >
      <div>
        <div class="modal-header-title">Activities</div>
        <div class="activities-columns">
          <div class="column">
            <b-form-checkbox-group
              id="activities-filter-box"
              v-model="selectedActivities"
              :options="activities"
              name="activities-filter"
              @change="onActivityChecked"
            ></b-form-checkbox-group>
          </div>
        </div>
        <div class="modal-buttons">
          <button class="moreItems" @click="resetActivities()">Reset</button>
          <button class="modal-button-right">Save</button>
        </div>
      </div>
    </b-modal>
  </div> -->
</template>
<script setup lang="ts">
  import { DashboardService, LocationService } from '@/services';
  import {
    Activity,
    CalendarDay,
    CalendarDayV2,
    Location,
    Presider,
  } from '@/types';
  import { formatDateInstanceToDDMMMYYYY } from '@/utils/dateUtils';
  import { computed, inject, onMounted, Ref, ref, watch } from 'vue';
  import CalendarToolbar from './CalendarToolbar.vue';
  import CourtToday from './CourtToday.vue';
  import MyCalendar from './MyCalendar.vue';

  const locationsService = inject<LocationService>('locationService');
  const dashboardService = inject<DashboardService>('dashboardService');

  if (!locationsService || !dashboardService) {
    throw new Error('Service is not available!');
  }

  const isLoading = ref(true);

  const isMySchedule = ref(true);

  const locations = ref<Location[]>([]);
  const selectedLocations = ref<string[]>([]);
  const isLocationModalVisible = ref(false);

  const presiders = ref<Presider[]>([]);
  const selectedPresiders = ref<string[]>([]);
  const isPresiderModalVisible = ref(false);
  const areAllPresidersChecked = ref(true);

  const activities = ref<Activity[]>([]);
  const selectedActivities = ref<string[]>([]);
  const areAllActivitiesChecked = ref(true);
  const isActivitiesModalVisible = ref(false);

  const filteredEvents = ref<CalendarDay[]>([]);

  let allEvents: CalendarDay[] = [];

  let sizeChange = 0;

  const showLeftMenu = ref(false);
  const showRightMenu = ref(false);
  const menuActive = ref(false);

  const sittingActivities = ref(false);
  const nonSittingActivities = ref(false);

  let currentCalendarDate = new Date('dd-mm-yyyy');

  const selectedDate = ref(new Date());
  let startDay = new Date(
    selectedDate.value.getFullYear(),
    selectedDate.value.getMonth(),
    1
  );
  let endDay = new Date(
    selectedDate.value.getFullYear(),
    selectedDate.value.getMonth() + 1,
    0
  );
  const todaySchedule = ref<CalendarDayV2>();
  const calendarData = ref<CalendarDayV2[]>([]);

  onMounted(async () => {
    isLoading.value = true;
    await Promise.all([loadLocations(), loadCalendarData()]);
    isLoading.value = false;
  });

  watch(selectedDate, async (newVal) => {
    startDay = new Date(newVal.getFullYear(), newVal.getMonth(), 1);
    endDay = new Date(newVal.getFullYear(), newVal.getMonth() + 1, 0);

    await loadCalendarData();
  });

  const loadCalendarData = async () => {
    const { payload } = await dashboardService.getMySchedule(
      formatDateInstanceToDDMMMYYYY(startDay),
      formatDateInstanceToDDMMMYYYY(endDay)
    );

    todaySchedule.value = payload.today;
    calendarData.value = [...payload.days];
  };

  const loadLocations = async () => {
    const data = await locationsService.getLocations();
    // Location with locationId comes from PCSS. Exclude data from JC for now
    locations.value = [...data.filter((l) => 'locationId' in l)];
  };

  const showAllLocations = () => {
    isLocationModalVisible.value = true;
  };

  const onFilterLocations = async () => {
    if (selectedLocations.value.length === 0) {
      return;
    }

    isLocationModalVisible.value = false;
    await getMonthlySchedule(currentCalendarDate);
  };

  const onResetLocations = async () => {
    selectedLocations.value.length = 0;
    isLocationModalVisible.value = false;
    await getMonthlySchedule(currentCalendarDate);
  };

  const loadPresiders = (data: Presider[]) => {
    if (selectedLocations.value.length === 0) {
      presiders.value.length = 0;
      selectedPresiders.value.length = 0;
      areAllPresidersChecked.value = false;
      return;
    }

    areAllPresidersChecked.value = true;
    selectedPresiders.value = data.map((p) => p.value);
    presiders.value = [...data];
  };

  const showAllPresiders = () => {
    isPresiderModalVisible.value = true;
  };

  const resetPresiders = () => {
    areAllPresidersChecked.value = true;
    selectedPresiders.value = presiders.value.map((p) => p.value);
    isPresiderModalVisible.value = false;
    filterByPresiders();
  };

  const loadActivities = (data: Activity[]) => {
    if (selectedLocations.value.length == 0) {
      activities.value.length = 0;
      selectedActivities.value.length = 0;
      areAllActivitiesChecked.value = false;
      return;
    }

    areAllActivitiesChecked.value = true;
    selectedActivities.value = data.map((a) => a.value);
    activities.value = [...data];
  };

  const showAllActivities = () => {
    isActivitiesModalVisible.value = true;
  };

  const resetActivities = () => {
    areAllActivitiesChecked.value = true;
    selectedActivities.value = activities.value.map((a) => a.value);
    isActivitiesModalVisible.value = false;
    filterByActivities();
  };

  const toggleRight = () => {
    showRightMenu.value = !showRightMenu.value;
    showLeftMenu.value = false;
    menuActive.value = showRightMenu.value;
    sizeChange++;
  };

  const toggleLeft = () => {
    showLeftMenu.value = !showLeftMenu.value;
    showRightMenu.value = false;
    menuActive.value = showLeftMenu.value;
    sizeChange++;
  };

  const getMonthlySchedule = async (currentMonth) => {
    currentCalendarDate = currentMonth;

    const locationIds =
      selectedLocations.value.length > 0
        ? selectedLocations.value.join(',')
        : '';
    const year = currentMonth.getFullYear();
    const month = String(currentMonth.getMonth() + 1).padStart(2, '0');

    const { schedule, presiders, activities } =
      await dashboardService.getMonthlySchedule(year, +month, locationIds);

    const firstDay = new Date(
      currentCalendarDate.getFullYear(),
      currentCalendarDate.getMonth(),
      1
    );
    const lastDay = new Date(
      currentCalendarDate.getFullYear(),
      currentCalendarDate.getMonth() + 1,
      0
    );

    const { payload } = await dashboardService.getMySchedule(
      formatDateInstanceToDDMMMYYYY(firstDay),
      formatDateInstanceToDDMMMYYYY(lastDay)
    );
    todaySchedule.value = payload.today;

    allEvents = [...schedule];
    filteredEvents.value = [...schedule];
    calendarData.value = payload.days;

    loadPresiders(presiders);
    loadActivities(activities);
  };

  const onLocationChecked = () => {
    isMySchedule.value = selectedLocations.value.length === 0;
    getMonthlySchedule(currentCalendarDate);
  };

  const onSelectAllPresiders = () => {
    if (areAllPresidersChecked.value) {
      selectedPresiders.value = presiders.value.map((p) => p.value);
    } else {
      selectedPresiders.value.splice(0);
    }
    filterByPresiders();
  };

  const onPreciderChecked = () => {
    areAllPresidersChecked.value =
      selectedPresiders.value.length === presiders.value.length;
    filterByPresiders();
  };

  const filterByPresiders = () => {
    if (selectedPresiders.value.length) {
      filteredEvents.value = allEvents.filter(
        (event) =>
          selectedPresiders.value.includes(String(event.assignment.judgeId)) ||
          selectedPresiders.value.includes(String(event.judgeId))
      );
    } else {
      filteredEvents.value = [];
    }
  };

  const onActivityChecked = () => {
    areAllActivitiesChecked.value =
      selectedActivities.value.length === activities.value.length;
    filterByActivities();
  };

  const onSelectAllActivities = () => {
    if (areAllActivitiesChecked.value) {
      selectedActivities.value = activities.value.map((p) => p.value);
    } else {
      selectedActivities.value.splice(0);
    }
    filterByActivities();
  };

  const filterByActivities = () => {
    if (selectedActivities.value.length > 0) {
      filteredEvents.value = allEvents.filter(
        (event) =>
          selectedActivities.value.includes(event.assignment.activityCode) ||
          selectedActivities.value.includes(
            event.assignment.activityAm?.activityCode
          ) ||
          selectedActivities.value.includes(
            event.assignment.activityPm?.activityCode
          )
      );
    } else {
      filteredEvents.value = [];
    }
  };

  const getFirstNItemsFromList = <T,>(items: Ref<T[]>, limit = 7): Ref<T[]> => {
    return computed(() => {
      return items.value.length > limit
        ? items.value.slice(0, limit)
        : items.value;
    });
  };

  const getLocations = getFirstNItemsFromList(locations);
  const getPresiders = getFirstNItemsFromList(presiders);
  const getActivities = getFirstNItemsFromList(activities);
</script>
