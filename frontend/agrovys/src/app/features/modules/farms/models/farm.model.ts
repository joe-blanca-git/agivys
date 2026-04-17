export class FarmSimpleCreateModel {
    name!: string;
    client_unit_id!: string;
    crop_year!: string;
    agivys_user_id!: string;
}

export class ListFarmsModel {
    id!: string;
    area!: number;
    description!: string;
    lastUpdate!: Date;
    name!: string;
    selected?: boolean;
    isEditing?: boolean;
    tempName?: string;
    boundaries?: BoundarySummaryModel[];
    isLoadingBoundaries?: boolean;
}

export interface BoundarySummaryModel {
    id: string;
    name: string;
    area: number;
}