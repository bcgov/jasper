export enum MonthList {
  'Jan' = 1,
  'Feb',
  'Mar',
  'Apr',
  'May',
  'Jun',
  'Jul',
  'Aug',
  'Sep',
  'Oct',
  'Nov',
  'Dec',
}

export function beautifyDate(date) {
  if (date)
    return (
      date.substr(8, 2) +
      ' ' +
      MonthList[Number(date.substr(5, 2))] +
      ' ' +
      date.substr(0, 4)
    );
  else return '';
}

export function beautifyDateTime(date) {
  if (date)
    return (
      date.substr(8, 2) +
      ' ' +
      MonthList[Number(date.substr(5, 2))] +
      ' ' +
      date.substr(0, 4) +
      ' ' +
      date.substr(11, 5)
    );
  else return '';
}

export function truncate(text: string, stop: number) {
  return stop + 3 < text.length ? text.slice(0, stop) + '...' : text;
}

export function convertTime(time) {
  const time12 = (Number(time.substr(0, 2)) % 12 || 12) + time.substr(2, 3);

  if (Number(time.substr(0, 2)) < 12) {
    return time12 + ' AM';
  } else {
    return time12 + ' PM';
  }
}
