export class CitiesPublicModel {
  nome!: string;
  codigo_ibge!: string;
}

export class StatePublicModel {
  id!: 35;
  sigla!: string;
  nome!: string;
  regiao!: region[];
}
class region {
  id!: number;
  sigla!: string;
  nome!: string;
}
