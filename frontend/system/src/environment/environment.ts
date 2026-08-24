export const environment = {
  production: false,
  defaultLanguage: 'pt-BR',
  supportedLanguages: ['pt-BR'],

  //=============================DESENVOLVIMENTO===============================
  // Auth v2 (cookie HttpOnly) exige HTTPS: o cookie é Secure e só é
  // gravado/enviado numa conexão https://. Por isso a API aqui aponta pro
  // perfil https do backend (7179), não pro http simples (5029).
  apiUrlLoginv1: 'https://localhost:7179/api/v1/auth/',
  apiUrlAuthV2: 'https://localhost:7179/api/v2/authentication/',
  apiUrlAgiVys: 'https://localhost:7179/api/v1/',
  apiUrlBrApi: 'https://brasilapi.com.br/api/',
  apiRulViaCep: 'https://viacep.com.br/ws/',
  //===========================================================================
};
