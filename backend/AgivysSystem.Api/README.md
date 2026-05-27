# AgiVysSystem API 🚀

API de backend robusta construída em **.NET 8** para o ecossistema AgiVysSystem, focada na gestão de empresas, usuários, sistemas e processos financeiros.

---

## 🛠️ Stack Tecnológica

- **Framework:** .NET 8.0 (Web API)
- **Banco de Dados:** MySQL (via Pomelo Entity Framework Core)
- **Autenticação:** ASP.NET Core Identity + JWT Bearer Token
- **Integrações Externas:**
  - **Asaas:** Gateway de pagamentos (Checkout e Webhooks)
  - **E-mail:** SMTP via MailKit
- **Documentação:** Swagger/OpenAPI (v1)
- **Versionamento:** Asp.Versioning para suporte a múltiplas versões de API

---

## 📂 Estrutura de Pastas

O projeto segue uma arquitetura em camadas simplificada dentro de um único projeto para facilitar a manutenção e agilidade:

- `Controllers/`: Endpoints da API agrupados por contexto (`Auth`, `Company`, `Financial`, etc.).
- `Services/`: Camada de lógica de negócio (Ex: `CheckoutService`, `EmailService`).
- `Models/`: Entidades de domínio e configurações do Identity.
- `Data/`: Contexto do banco de Dados (`AppDbContext`) e Migrations.
- `DTOs/`: Objetos de transferência de dados para requisições e respostas.
- `Interfaces/`: Contratos para injeção de dependência.
- `Configuration/`: Seeds de banco de dados (Roles) e extensões de configuração.

---

## 🏗️ Arquitetura e Padrões

### 1. Segurança e Autenticação
- Utiliza **JWT (JSON Web Tokens)** para stateless authentication.
- **Identity Framework** gerencia usuários (`User`), roles e permissões.
- Os tokens contêm claims de permissão que são validadas via atributos `[Authorize(Roles = "...")]`.
- O cadastro de usuário exige `idSystem` no endpoint de registro e esse valor é gravado no usuário.
- O login inclui `idSystem` no payload JWT e também na resposta JSON para suportar validações multi-sistema/multi-tenant.

#### Exemplo de registro
```http
POST /api/v1/Auth/register
Content-Type: application/json

{
  "idSystem": 1,
  "name": "João Silva",
  "document": "12345678909",
  "email": "joao@exemplo.com",
  "password": "SenhaForte123!",
  "birthDate": "1990-01-01T00:00:00",
  "addressDescription": "Principal",
  "zipCode": "12345000",
  "street": "Rua Exemplo",
  "number": "100",
  "complement": "Sala 1",
  "neighborhood": "Centro",
  "city": "São Paulo",
  "state": "SP"
}
```

Resposta de sucesso:
```json
{
  "message": "Usuário cadastrado com sucesso!"
}
```

#### Exemplo de login
```http
POST /api/v1/Auth/login
Content-Type: application/json

{
  "email": "joao@exemplo.com",
  "password": "SenhaForte123!"
}
```

Resposta de sucesso:
```json
{
  "token": "<JWT_TOKEN>",
  "expiration": "2026-05-27T14:00:00Z",
  "user": {
    "id": 1,
    "email": "joao@exemplo.com",
    "companyId": null,
    "companyName": null,
    "idSystem": 1,
    "roles": [
      { "name": "UserType", "value": "Owner" }
    ]
  },
  "person": {
    "id": 1,
    "name": "João Silva",
    "email": "joao@exemplo.com"
  }
}
```

### 2. Banco de Dados e Mapeamento
- **EF Core (Code-First):** As entidades em `Models/` definem o schema.
- **AppDbContext:** Centraliza as configurações de `Fluent API`, índices únicos e relacionamentos (Ex: Planos <-> Menus).

### 3. Comunicação Externa (Services)
- **HttpClient Typed:** O `AsaasService` é injetado com configurações pré-definidas (BaseUrl, ApiKey) via `Program.cs`.
- **Injeção de Dependência:** Todo serviço de negócio possui uma interface correspondente (ex: `IEmailService`) para facilitar testes e desacoplamento.

---

## 🤖 Guia para IA (Assistant Guide)

Se você é uma IA trabalhando neste projeto, aqui estão as diretrizes fundamentais:

### 💡 Naming Conventions & Patterns
- **Controllers:** Devem herdar de `ControllerBase`, usar o atributo `[ApiController]` e seguir o padrão de rotas `api/v{version:apiVersion}/[controller]`.
- **Services:** Devem ser registrados em `Program.cs` como `Scoped` (geralmente). Injetar via construtor.
- **DTOs:** Sempre use DTOs para entrada e saída de dados. Evite expor as entidades de `Models/` diretamente nos controllers.
- **Respostas:** Tente manter um padrão de retorno (ex: `Ok`, `BadRequest`, `NotFound`) com mensagens claras.

### 🔍 Onde encontrar as coisas?
- **Novas tabelas?** Adicione em `Models/`, registre no `AppDbContext` e rode `dotnet ef migrations add Name`.
- **Nova lógica de checkout?** Verifique `Services/Financial/CheckoutService.cs`.
- **Alterar permissões?** Veja `Configuration/RoleSeeder.cs`.

### 🛠️ Métodos e Fluxos Principais
- **Autenticação:** `AuthController` lida com Login, Registro e recuperação de senha.
- **Financeiro:** `CheckoutService` processa pagamentos via Asaas.
- **Sistema:** `AppSystem`, `Plan`, `Menu` e `Submenu` definem o que o usuário pode ver no frontend dinamicamente.

---

## 🚀 Como Executar

1.  **Configuração:** Ajuste a `DefaultConnection` no `appsettings.json` para seu MySQL local.
2.  **Migrations:**
    ```bash
    dotnet ef database update
    ```
3.  **Run:**
    ```bash
    dotnet run
    ```
4.  **Swagger:** Acesse `http://localhost:5000/swagger` para visualizar todos os endpoints documentados.

---

## 📝 Notas de Versão (v1)
- Suporte a Proxy Reverso (Forwarded Headers).
- CORS configurado para aceitar requisições de qualquer origem (Ajustar em produção).
- Logs configurados para Console/Debug.

---
*Este arquivo serve como o cérebro do projeto para humanos e máquinas. Mantenha-o atualizado ao adicionar novas funcionalidades centrais.*
