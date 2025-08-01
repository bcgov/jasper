/**
 * Formats a given date string into the format "DD-MMM-YYYY".
 *
 * @param dateString - The date string to format. It should be a valid date string.
 * @returns {string} A formatted date string in "DD-MMM-YYYY" format (e.g., "01-Jan-2023").
 */
export const formatDateToDDMMMYYYY = (dateString: string): string => {
  if (!dateString) {
    return '';
  }
  const normalizedDateString = dateString.split(' ')[0]; // Extract only the date part
  const date = normalizedDateString
    ? new Date(`${normalizedDateString}T00:00:00`)
    : null;
  if (!date) {
    return '';
  }

  const day = new Intl.DateTimeFormat('en', { day: '2-digit' }).format(date);
  const month = new Intl.DateTimeFormat('en', { month: 'short' }).format(date);
  const year = new Intl.DateTimeFormat('en', { year: 'numeric' }).format(date);

  return `${day}-${month}-${year}`;
};

/**
 * Formats a Date() instance into the format "DD-MMM-YYYY".
 * @param date - The Date object
 * @returns A formatted date string in "DD-MMM-YYYY" format (e.g., "01-Jan-2023").
 */
export const formatDateInstanceToDDMMMYYYY = (date: Date): string => {
  if (!date || isNaN(date.getTime())) return '';

  const day = new Intl.DateTimeFormat('en', { day: '2-digit' }).format(date);
  const month = new Intl.DateTimeFormat('en', { month: 'short' }).format(date);
  const year = new Intl.DateTimeFormat('en', { year: 'numeric' }).format(date);

  return `${day}-${month}-${year}`;
};

/**
 * Formats a Date() instance into the format "MMM YYYY".
 * @param date - The Date object
 * @returns A formatted date string in "MMM YYYY" format (e.g., "Jan 2023").
 */
export const formatDateInstanceToMMMYYYY = (date: Date): string => {
  if (!date || isNaN(date.getTime())) return '';

  const month = new Intl.DateTimeFormat('en', { month: 'short' }).format(date);
  const year = new Intl.DateTimeFormat('en', { year: 'numeric' }).format(date);

  return `${month} ${year}`;
};

/**
 * Formats hours and minutes into a human-readable string.
 *
 * @param hours - The number of hours as a string. Should be a valid integer in string format.
 * @param minutes - The number of minutes as a string. Should be a valid integer in string format.
 * @returns A formatted string representing the hours and minutes.
 */
export const hoursMinsFormatter = (hours: string, minutes: string) => {
  const hrs = parseInt(hours, 10);
  const mins = parseInt(minutes, 10);
  let result = '';
  if (hrs) {
    result += `${hrs} Hr(s)`;
  }
  if (mins) {
    result += `${result ? ' ' : ''}${mins} Min(s)`;
  }
  return result || '0 Mins';
};

/**
 * Extracts and formats the time from a given date string.
 *
 * @param date - The date string in the format "YYYY-MM-DD HH:mm:ss".
 * @returns The formatted time in 12-hour format with AM/PM (e.g., "2:30 PM").
 */
export const extractTime = (date: string) => {
  const time = date.split(' ')[1];
  const hours = parseInt(time.slice(0, 2), 10);
  const minutes = time.slice(3, 5);
  const period = hours >= 12 ? 'PM' : 'AM';
  const formattedHours = hours % 12 || 12; // Convert to 12-hour format
  return `${formattedHours}:${minutes} ${period}`;
};

/**
 * Parses the dateStr with format "DD-MMM-YYYY" to new Date()
 * @param dateStr dateStr with format "DD-MMM-YYYY"
 * @returns Date object or null
 */
export const parseDDMMMYYYYToDate = (dateStr: string): Date | null => {
  if (!dateStr || typeof dateStr !== 'string') {
    return null;
  }

  const parts = dateStr.trim().split('-');
  if (parts.length !== 3) {
    return null;
  }

  const [dayStr, monthStr, yearStr] = parts;
  const day = Number(dayStr);
  const year = Number(yearStr);

  const months: Record<string, number> = {
    Jan: 0,
    Feb: 1,
    Mar: 2,
    Apr: 3,
    May: 4,
    Jun: 5,
    Jul: 6,
    Aug: 7,
    Sep: 8,
    Oct: 9,
    Nov: 10,
    Dec: 11,
  };

  const month = months[monthStr];
  if (isNaN(day) || isNaN(year) || month === undefined) {
    return null;
  }

  const date = new Date(year, month, day);
  if (
    date.getFullYear() !== year ||
    date.getMonth() !== month ||
    date.getDate() !== day
  ) {
    return null;
  }

  return date;
};
