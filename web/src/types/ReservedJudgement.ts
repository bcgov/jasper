export type ReservedJudgement = {
    id: string;
    appearanceId: string;
    appearanceDate: string;
    judgeId?: number;
    courtClass: string;
    physicalFileId: string;
    courtFileNumber: string;
    fileNumber: string;
    ageInDays: number;
    styleOfCause: string;
    reason: string;
    dueDate: string;
    partId: string;
    updatedDate?: string;
};