export const environment = {
  production: true,
  defaultLanguage: 'pt-BR',
  supportedLanguages: ['pt-BR'],

  //=============================PRODUÇÃO===============================
  // Tudo aqui passa pelo nginx em joederblanca.com.br: /agivys-api/ é
  // proxy pra API (porta 5000), que por sua vez expõe suas rotas em
  // /api/v1 e /api/v2 (por isso o "api" aparece duas vezes no caminho).
  apiUrlLoginv1: 'https://joederblanca.com.br/agivys-api/api/v1/auth/',
  apiUrlAuthV2: 'https://joederblanca.com.br/agivys-api/api/v2/authentication/',
  apiUrlAgiVys: 'https://joederblanca.com.br/agivys-api/api/v1/',
  apiUrlBrApi: 'https://brasilapi.com.br/api/',
  apiRulViaCep: 'https://viacep.com.br/ws/',
  //======================================================================
};
