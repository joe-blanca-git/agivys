export class ListFarmsModel{
    area!: number;
    description!: string;
    lastUpdate!: Date;
    name!: string;
    selected?: boolean;
    isEditing?: boolean;
    tempName?: string;
}