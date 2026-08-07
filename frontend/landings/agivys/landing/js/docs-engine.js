/**
 * AGIVYS API - Documentation Engine
 * Reads swagger.json and renders a SPA Documentation Portal.
 */

let spec = null;
let globalToken = localStorage.getItem('agivys_api_token') || '';

// DOM Elements
const contentContainer = document.getElementById('contentContainer');
const loadingView = document.getElementById('loadingView');
const sidebarGroups = document.getElementById('dynamicSidebarGroups');
const schemasSidebarGroup = document.getElementById('schemasSidebarGroup');
const searchInput = document.getElementById('searchInput');
const searchResults = document.getElementById('searchResults');
const tryitDrawer = document.getElementById('tryitDrawer');
const drawerOverlay = document.getElementById('drawerOverlay');

// Global State
let currentPath = '';
let currentMethod = '';
let endpointsByTag = {};
let allEndpointsFlat = []; // For search

async function init() {
    try {
        const response = await fetch('swagger.json');
        if (!response.ok) throw new Error('Failed to load swagger.json');
        spec = await response.json();
        
        processSpec();
        renderSidebar();
        setupEvents();
        
        loadingView.style.display = 'none';
        contentContainer.style.display = 'block';
        
        handleHashChange();
    } catch (e) {
        loadingView.innerHTML = `<i class="ti-alert" style="color: #f87171; font-size: 2rem;"></i><p style="margin-top:20px; color: #f87171;">Erro ao carregar a especificação da API.</p><p style="font-size: 0.8rem; color: #8b949e;">${e.message}</p>`;
    }
}

function processSpec() {
    // Agrupar endpoints por Tag
    if (spec.paths) {
        Object.keys(spec.paths).forEach(pathUrl => {
            const methods = spec.paths[pathUrl];
            Object.keys(methods).forEach(method => {
                const operation = methods[method];
                const tag = (operation.tags && operation.tags.length > 0) ? operation.tags[0] : 'Default';
                
                if (!endpointsByTag[tag]) endpointsByTag[tag] = [];
                
                const ep = {
                    path: pathUrl,
                    method: method.toLowerCase(),
                    operation: operation
                };
                
                endpointsByTag[tag].push(ep);
                allEndpointsFlat.push(ep);
            });
        });
    }
}

function renderSidebar() {
    let html = '';
    
    // Configurar endpoints
    Object.keys(endpointsByTag).forEach(tag => {
        html += `<div class="sidebar-group">
            <div class="sidebar-title">${tag}</div>`;
        
        endpointsByTag[tag].forEach(ep => {
            const hash = `#endpoint-${ep.method}-${ep.path.replaceAll('/', '-').replaceAll('{', '').replaceAll('}', '')}`;
            html += `<a href="${hash}" class="sidebar-link" data-hash="${hash}">
                <span class="method-badge ${ep.method}" style="display:inline-block; width: 45px; text-align:center; padding: 2px; font-size: 0.6rem; margin-right: 8px;">${ep.method}</span>
                ${ep.operation.summary || ep.path}
            </a>`;
        });
        
        html += `</div>`;
    });
    
    sidebarGroups.innerHTML = html;
    
    // Configurar schemas
    if (spec.components && spec.components.schemas) {
        let schemasHtml = '<div class="sidebar-title">Schemas</div>';
        Object.keys(spec.components.schemas).forEach(schemaName => {
            schemasHtml += `<a href="#schema-${schemaName}" class="sidebar-link" data-hash="#schema-${schemaName}">${schemaName}</a>`;
        });
        schemasSidebarGroup.innerHTML = schemasHtml;
    }
}

function setupEvents() {
    window.addEventListener('hashchange', handleHashChange);
    
    // Mobile Menu
    document.getElementById('mobileMenuBtn').addEventListener('click', () => {
        document.getElementById('docsSidebar').classList.toggle('open');
    });
    
    // Fechar drawer ao clicar no overlay
    drawerOverlay.addEventListener('click', closeTryIt);
    document.getElementById('tryitClose').addEventListener('click', closeTryIt);
    
    // Busca
    searchInput.addEventListener('input', (e) => {
        const val = e.target.value.toLowerCase();
        if (val.length < 2) {
            searchResults.classList.remove('active');
            return;
        }
        
        const results = allEndpointsFlat.filter(ep => 
            ep.path.toLowerCase().includes(val) || 
            (ep.operation.summary && ep.operation.summary.toLowerCase().includes(val)) ||
            (ep.operation.tags && ep.operation.tags[0].toLowerCase().includes(val))
        ).slice(0, 10);
        
        if (results.length > 0) {
            searchResults.innerHTML = results.map(ep => {
                const hash = `#endpoint-${ep.method}-${ep.path.replaceAll('/', '-').replaceAll('{', '').replaceAll('}', '')}`;
                return `
                <div class="search-result-item" onclick="window.location.hash='${hash}'; document.getElementById('searchResults').classList.remove('active');">
                    <div class="search-result-title"><span class="method-badge ${ep.method}" style="padding:2px 4px; font-size:0.6rem;">${ep.method}</span> ${ep.operation.summary || 'Endpoint'}</div>
                    <div class="search-result-path">${ep.path}</div>
                </div>`;
            }).join('');
            searchResults.classList.add('active');
        } else {
            searchResults.innerHTML = '<div style="padding: 15px; color: #8b949e; font-size: 0.85rem;">Nenhum resultado encontrado.</div>';
            searchResults.classList.add('active');
        }
    });
    
    // Try it executor
    document.getElementById('tryitExecuteBtn').addEventListener('click', executeTryIt);
    
    // Copy TryIt Response
    document.getElementById('tryitCopyBtn').addEventListener('click', () => {
        const text = document.getElementById('tryitResponseBody').textContent;
        navigator.clipboard.writeText(text);
        const btn = document.getElementById('tryitCopyBtn');
        btn.textContent = 'Copied!';
        setTimeout(() => btn.textContent = 'Copy', 2000);
    });
}

function handleHashChange() {
    const hash = window.location.hash || '#overview';
    
    // Update active state in sidebar
    document.querySelectorAll('.sidebar-link').forEach(link => {
        if (link.getAttribute('href') === hash) link.classList.add('active');
        else link.classList.remove('active');
    });
    
    // Fechar menu mobile se estiver aberto
    document.getElementById('docsSidebar').classList.remove('open');
    
    // Render content
    if (hash === '#overview') {
        renderHome();
    } else if (hash === '#authentication') {
        renderAuthDoc();
    } else if (hash === '#getting-started') {
        renderHome(); // Ou renderizar getting started specfico
    } else if (hash === '#errors') {
        renderErrors();
    } else if (hash.startsWith('#schema-')) {
        const schemaName = hash.replace('#schema-', '');
        renderSchema(schemaName);
    } else if (hash.startsWith('#endpoint-')) {
        // Encontrar o endpoint
        const target = hash.replace('#endpoint-', '');
        const ep = allEndpointsFlat.find(e => {
            const h = `${e.method}-${e.path.replaceAll('/', '-').replaceAll('{', '').replaceAll('}', '')}`;
            return h === target;
        });
        if (ep) renderEndpoint(ep);
    }
    
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

function renderHome() {
    contentContainer.innerHTML = `
        <div class="docs-header">
            <span class="docs-badge-version" style="font-size: 0.85rem; padding: 4px 12px; background: rgba(0, 255, 136, 0.1); color: var(--docs-primary);">AGIVYS API • v1</span>
            <h1 class="docs-title" style="margin-top: 15px;">Construa mais. Comece com uma base pronta.</h1>
            <p class="docs-subtitle">Uma API para autenticação, usuários, permissões, sistemas, menus, planos, integrações e outros recursos essenciais para aplicações modernas.</p>
        </div>
        
        <div class="auth-banner">
            <div>
                <h4 style="margin:0 0 5px 0; color: #fff;">Base URL</h4>
                <p style="margin:0; font-size: 0.85rem; color: var(--docs-text-muted);">Todas as requisições devem ser feitas para esta URL base.</p>
            </div>
            <div class="auth-input-group">
                <input type="text" value="https://joederblanca.com.br/agivys-api" readonly id="baseUrlInput">
                <button class="code-copy" onclick="navigator.clipboard.writeText('https://joederblanca.com.br/agivys-api'); this.innerText='Copiado!'; setTimeout(()=>this.innerText='Copiar', 2000);">Copiar</button>
            </div>
        </div>
        
        <div class="cards-grid">
            <div class="docs-card" onclick="window.location.hash='#endpoint-post--api-v1-authentication-login'">
                <h3>Authentication</h3>
                <p>Autenticação e gerenciamento de credenciais via JWT.</p>
            </div>
            <div class="docs-card" onclick="window.location.hash='#endpoint-get--api-v1-person'">
                <h3>Users & Profiles</h3>
                <p>Gerenciamento de dados pessoais e perfis de usuário.</p>
            </div>
            <div class="docs-card" onclick="window.location.hash='#endpoint-get--api-v1-rls'">
                <h3>Access Control</h3>
                <p>Roles, permissões globais e restrições de acesso (RLS).</p>
            </div>
            <div class="docs-card" onclick="window.location.hash='#endpoint-get--api-v1-systems'">
                <h3>Systems & Menus</h3>
                <p>Gerenciamento de sistemas e construção de árvores de menus.</p>
            </div>
        </div>
    `;
}

function renderAuthDoc() {
    contentContainer.innerHTML = `
        <div class="docs-header">
            <h1 class="docs-title">Autenticação Global</h1>
            <p class="docs-subtitle">A API AGIVYS utiliza JWT (JSON Web Token) via header Authorization Bearer.</p>
        </div>
        
        <div style="background: var(--docs-surface); border: 1px solid var(--docs-border); padding: 25px; border-radius: 8px; margin-bottom: 30px;">
            <h3 style="margin-top: 0; color: var(--docs-text-main);">Configure seu Token para o Try It Out</h3>
            <p style="color: var(--docs-text-muted); font-size: 0.9rem; margin-bottom: 20px;">
                Para testar endpoints protegidos diretamente nesta documentação, cole seu Token JWT abaixo. Ele será enviado automaticamente no header <code>Authorization: Bearer</code>.
            </p>
            <div class="auth-input-group">
                <input type="password" id="globalTokenInput" placeholder="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." value="${globalToken}">
                <button class="try-it-btn" onclick="saveGlobalToken()">Salvar Token</button>
            </div>
            <div id="tokenSaveMsg" style="color: var(--docs-primary); font-size: 0.8rem; margin-top: 10px; display: none;">Token salvo localmente com sucesso!</div>
        </div>
        
        <h3>Como enviar nas suas requisições</h3>
        <p style="color: var(--docs-text-muted); font-size: 0.9rem;">Todas as rotas protegidas exigem o cabeçalho HTTP de Autorização.</p>
        <div class="code-block-wrapper">
            <div class="code-header"><span class="code-lang">HTTP</span></div>
            <pre class="code-content">Authorization: Bearer SEU_TOKEN_AQUI</pre>
        </div>
        
        <div class="auth-banner" style="background: rgba(251, 191, 36, 0.05); border-color: rgba(251, 191, 36, 0.2); margin-top: 30px;">
            <i class="ti-info-alt" style="color: var(--method-patch); font-size: 1.5rem;"></i>
            <div>
                <h4 style="margin: 0 0 5px 0; color: #fff;">Validade do Token</h4>
                <p style="margin:0; font-size: 0.85rem; color: var(--docs-text-muted);">
                    O token retornado pelo endpoint de Login é válido por <strong>4 horas</strong>. Após este período, será retornado HTTP 401 e um novo login será necessário.
                </p>
            </div>
        </div>
    `;
}

window.saveGlobalToken = function() {
    const val = document.getElementById('globalTokenInput').value;
    localStorage.setItem('agivys_api_token', val);
    globalToken = val;
    document.getElementById('tokenSaveMsg').style.display = 'block';
    setTimeout(() => document.getElementById('tokenSaveMsg').style.display = 'none', 3000);
}

function renderErrors() {
    contentContainer.innerHTML = `
        <div class="docs-header">
            <h1 class="docs-title">Tratamento de Erros</h1>
            <p class="docs-subtitle">A API utiliza códigos de status HTTP convencionais para indicar o sucesso ou falha de uma requisição.</p>
        </div>
        
        <table class="docs-table">
            <thead>
                <tr>
                    <th>Código</th>
                    <th>Descrição</th>
                </tr>
            </thead>
            <tbody>
                <tr>
                    <td><span class="status-badge status-2xx">200 OK</span></td>
                    <td>A requisição foi processada com sucesso.</td>
                </tr>
                <tr>
                    <td><span class="status-badge status-2xx">201 Created</span></td>
                    <td>O recurso foi criado com sucesso (usado em POST).</td>
                </tr>
                <tr>
                    <td><span class="status-badge status-4xx">400 Bad Request</span></td>
                    <td>A requisição é inválida (ex: falha na validação do schema, campos faltando).</td>
                </tr>
                <tr>
                    <td><span class="status-badge status-4xx">401 Unauthorized</span></td>
                    <td>Token JWT ausente, inválido ou expirado. Autenticação necessária.</td>
                </tr>
                <tr>
                    <td><span class="status-badge status-4xx">403 Forbidden</span></td>
                    <td>O usuário está autenticado, mas não possui permissão para acessar o recurso (Role/RLS).</td>
                </tr>
                <tr>
                    <td><span class="status-badge status-4xx">404 Not Found</span></td>
                    <td>O recurso solicitado não existe.</td>
                </tr>
                <tr>
                    <td><span class="status-badge status-5xx">500 Server Error</span></td>
                    <td>Ocorreu um erro interno no servidor ao processar a requisição.</td>
                </tr>
            </tbody>
        </table>
    `;
}

function renderSchema(schemaName) {
    const schema = spec.components.schemas[schemaName];
    if (!schema) return;
    
    let html = `
        <div class="docs-header">
            <div style="display:flex; align-items:center; gap: 10px; margin-bottom: 10px;">
                <h1 class="docs-title" style="margin:0;">${schemaName}</h1>
                <span class="docs-badge-version">Schema</span>
            </div>
            ${schema.description ? `<p class="docs-subtitle">${schema.description}</p>` : ''}
        </div>
    `;
    
    if (schema.properties) {
        html += `
        <table class="docs-table">
            <thead>
                <tr>
                    <th>Campo</th>
                    <th>Tipo</th>
                    <th>Obrigatório</th>
                    <th>Detalhes</th>
                </tr>
            </thead>
            <tbody>
        `;
        
        const required = schema.required || [];
        
        Object.keys(schema.properties).forEach(propName => {
            const prop = schema.properties[propName];
            const isReq = required.includes(propName);
            
            let typeStr = prop.type || '';
            if (prop.format) typeStr += ` &lt;${prop.format}&gt;`;
            
            let details = prop.description || '';
            if (prop.minLength !== undefined) details += ` Min: ${prop.minLength}.`;
            if (prop.maxLength !== undefined) details += ` Max: ${prop.maxLength}.`;
            if (prop.nullable) details += ` (Nullable)`;
            if (prop.$ref) {
                const refName = prop.$ref.split('/').pop();
                typeStr = `<a href="#schema-${refName}" style="color: #60a5fa; text-decoration: none;">${refName}</a>`;
            } else if (prop.type === 'array' && prop.items && prop.items.$ref) {
                const refName = prop.items.$ref.split('/').pop();
                typeStr = `Array of <a href="#schema-${refName}" style="color: #60a5fa; text-decoration: none;">${refName}</a>`;
            }
            
            html += `
                <tr>
                    <td class="param-name">${propName}</td>
                    <td class="param-type">${typeStr}</td>
                    <td>${isReq ? '<span class="param-req">Sim</span>' : '<span style="color: var(--docs-text-muted); font-size: 0.75rem;">Não</span>'}</td>
                    <td>${details}</td>
                </tr>
            `;
        });
        
        html += `</tbody></table>`;
    }
    
    contentContainer.innerHTML = html;
}

function renderEndpoint(ep) {
    const op = ep.operation;
    
    // Auth Check
    const requiresAuth = op.security && op.security.length > 0;
    
    let html = `
        <div class="endpoint-section">
            ${requiresAuth ? `<div style="display:inline-block; margin-bottom: 15px; padding: 4px 12px; background: rgba(251, 191, 36, 0.1); border: 1px solid rgba(251, 191, 36, 0.2); color: var(--method-patch); border-radius: 20px; font-size: 0.75rem; font-weight: 600;"><i class="ti-lock"></i> Requer Autenticação</div>` : `<div style="display:inline-block; margin-bottom: 15px; padding: 4px 12px; background: rgba(74, 222, 128, 0.1); border: 1px solid rgba(74, 222, 128, 0.2); color: var(--method-get); border-radius: 20px; font-size: 0.75rem; font-weight: 600;"><i class="ti-world"></i> Rota Pública</div>`}
            
            <div class="endpoint-header">
                <span class="method-badge ${ep.method}">${ep.method}</span>
                <span class="endpoint-path">${ep.path}</span>
                <button class="try-it-btn" onclick="openTryIt('${ep.method}', '${ep.path}')">Testar Endpoint <i class="ti-control-play"></i></button>
            </div>
            
            <h2 style="font-size: 1.5rem; color: #fff; margin-bottom: 10px;">${op.summary || ''}</h2>
            <div class="endpoint-desc">${op.description || ''}</div>
            
            <div class="endpoint-body">
                <div class="endpoint-body-left">
    `;
    
    // Parameters
    if (op.parameters && op.parameters.length > 0) {
        html += `
                    <div class="endpoint-params">
                        <h4>Parâmetros</h4>
                        <table class="docs-table">
                            <thead>
                                <tr>
                                    <th>Nome</th>
                                    <th>Local</th>
                                    <th>Tipo</th>
                                    <th>Obrigatório</th>
                                    <th>Descrição</th>
                                </tr>
                            </thead>
                            <tbody>
        `;
        op.parameters.forEach(p => {
            html += `
                                <tr>
                                    <td class="param-name">${p.name}</td>
                                    <td>${p.in}</td>
                                    <td class="param-type">${p.schema ? p.schema.type : 'string'}</td>
                                    <td>${p.required ? '<span class="param-req">Sim</span>' : 'Não'}</td>
                                    <td>${p.description || ''}</td>
                                </tr>
            `;
        });
        html += `</tbody></table></div>`;
    }
    
    // Request Body
    let reqBodyExample = null;
    if (op.requestBody && op.requestBody.content && op.requestBody.content['application/json']) {
        const schemaRef = op.requestBody.content['application/json'].schema.$ref;
        html += `
            <div class="endpoint-body-req">
                <h4>Request Body <span style="font-size:0.7rem; color:var(--docs-text-muted); float:right; text-transform:none;">application/json</span></h4>
        `;
        if (schemaRef) {
            const schemaName = schemaRef.split('/').pop();
            html += `<p style="font-size: 0.85rem; margin-bottom: 15px;">Schema: <a href="#schema-${schemaName}" style="color: #60a5fa; text-decoration: none;">${schemaName}</a></p>`;
            reqBodyExample = generateExampleFromSchema(schemaName);
        }
        html += `</div>`;
    }
    
    // Responses
    html += `
                    <div class="endpoint-responses">
                        <h4 style="margin-top: 30px;">Respostas</h4>
    `;
    if (op.responses) {
        Object.keys(op.responses).forEach(code => {
            const res = op.responses[code];
            let statusClass = 'status-2xx';
            if (code.startsWith('4')) statusClass = 'status-4xx';
            if (code.startsWith('5')) statusClass = 'status-5xx';
            
            html += `
                        <div class="response-block">
                            <div class="response-status">
                                <span class="status-badge ${statusClass}">${code}</span>
                                <span class="status-desc">${res.description}</span>
                            </div>
            `;
            // Check for schema
            if (res.content && res.content['application/json'] && res.content['application/json'].schema) {
                const sRef = res.content['application/json'].schema.$ref;
                if (sRef) {
                    const sName = sRef.split('/').pop();
                    const example = generateExampleFromSchema(sName);
                    html += `
                            <div class="code-block-wrapper" style="margin-top: 10px;">
                                <div class="code-header">
                                    <span class="code-lang">JSON</span>
                                </div>
                                <pre class="code-content">${JSON.stringify(example, null, 2)}</pre>
                            </div>
                    `;
                }
            }
            html += `</div>`;
        });
    }
    
    html += `
                </div>
            </div>
            
            <div class="endpoint-body-right">
    `;
    
    // CURL Example Generator
    const curlCmd = generateCurl(ep, reqBodyExample);
    
    html += `
                <div class="endpoint-params">
                    <h4>cURL Example</h4>
                </div>
                <div class="code-block-wrapper">
                    <div class="code-header">
                        <span class="code-lang">BASH</span>
                        <button class="code-copy" onclick="navigator.clipboard.writeText(this.parentElement.nextElementSibling.innerText); this.innerText='Copiado!'; setTimeout(()=>this.innerText='Copy', 2000);">Copy</button>
                    </div>
                    <pre class="code-content">${curlCmd}</pre>
                </div>
    `;
    
    if (reqBodyExample) {
        html += `
                <div class="endpoint-params" style="margin-top: 30px;">
                    <h4>Example Request Body</h4>
                </div>
                <div class="code-block-wrapper">
                    <div class="code-header">
                        <span class="code-lang">JSON</span>
                        <button class="code-copy" onclick="navigator.clipboard.writeText(this.parentElement.nextElementSibling.innerText); this.innerText='Copiado!'; setTimeout(()=>this.innerText='Copy', 2000);">Copy</button>
                    </div>
                    <pre class="code-content">${JSON.stringify(reqBodyExample, null, 2)}</pre>
                </div>
        `;
    }
    
    html += `
                </div>
            </div>
        </div>
    `;
    
    contentContainer.innerHTML = html;
}

// Utils
function generateExampleFromSchema(schemaName) {
    const schema = spec.components.schemas[schemaName];
    if (!schema) return {};
    
    let obj = {};
    if (schema.properties) {
        Object.keys(schema.properties).forEach(key => {
            const prop = schema.properties[key];
            if (prop.type === 'string') {
                if (prop.format === 'email') obj[key] = "usuario@empresa.com";
                else if (key.toLowerCase().includes('password')) obj[key] = "SuaSenha123!";
                else obj[key] = "string";
            } else if (prop.type === 'integer' || prop.type === 'number') {
                obj[key] = 0;
            } else if (prop.type === 'boolean') {
                obj[key] = true;
            } else if (prop.type === 'array') {
                obj[key] = [];
            } else if (prop.$ref) {
                // To avoid infinite recursion, just put a simple object string
                obj[key] = {};
            }
        });
    }
    return obj;
}

function generateCurl(ep, bodyExample) {
    let curl = `curl -X ${ep.method.toUpperCase()} \\\n  'https://joederblanca.com.br/agivys-api${ep.path}'`;
    
    curl += ` \\\n  -H 'Accept: text/plain'`;
    
    const requiresAuth = ep.operation.security && ep.operation.security.length > 0;
    if (requiresAuth) {
        curl += ` \\\n  -H 'Authorization: Bearer SEU_TOKEN'`;
    }
    
    if (bodyExample) {
        curl += ` \\\n  -H 'Content-Type: application/json' \\\n  -d '${JSON.stringify(bodyExample, null, 2)}'`;
    }
    
    return curl;
}

// TRY IT OUT Logic
function openTryIt(method, path) {
    currentMethod = method;
    currentPath = path;
    
    const ep = allEndpointsFlat.find(e => e.method === method && e.path === path);
    if (!ep) return;
    
    document.getElementById('tryitTitle').innerHTML = `<span class="method-badge ${method}">${method}</span> ${path}`;
    
    // Reset state
    document.getElementById('tryitResponseContainer').style.display = 'none';
    
    // Check Auth
    const requiresAuth = ep.operation.security && ep.operation.security.length > 0;
    document.getElementById('tryitAuthGroup').style.display = requiresAuth ? 'block' : 'none';
    document.getElementById('tryitTokenInput').value = globalToken;
    
    // Params
    let paramsHtml = '';
    if (ep.operation.parameters && ep.operation.parameters.length > 0) {
        ep.operation.parameters.forEach(p => {
            paramsHtml += `
                <div class="tryit-group">
                    <label class="tryit-label">${p.name} <span style="font-weight:normal; color:#8b949e;">(${p.in})</span> ${p.required ? '<span style="color:#f87171;">*</span>' : ''}</label>
                    <input type="text" class="tryit-input tryit-param-input" data-name="${p.name}" data-in="${p.in}" placeholder="${p.schema?.type || 'string'}">
                </div>
            `;
        });
    }
    document.getElementById('tryitParamsContainer').innerHTML = paramsHtml;
    
    // Body
    if (ep.operation.requestBody && ep.operation.requestBody.content && ep.operation.requestBody.content['application/json']) {
        document.getElementById('tryitBodyContainer').style.display = 'block';
        const schemaRef = ep.operation.requestBody.content['application/json'].schema.$ref;
        if (schemaRef) {
            const schemaName = schemaRef.split('/').pop();
            const example = generateExampleFromSchema(schemaName);
            document.getElementById('tryitBodyTextarea').value = JSON.stringify(example, null, 2);
        } else {
            document.getElementById('tryitBodyTextarea').value = '{}';
        }
    } else {
        document.getElementById('tryitBodyContainer').style.display = 'none';
        document.getElementById('tryitBodyTextarea').value = '';
    }
    
    drawerOverlay.classList.add('open');
    tryitDrawer.classList.add('open');
}

function closeTryIt() {
    drawerOverlay.classList.remove('open');
    tryitDrawer.classList.remove('open');
}

async function executeTryIt() {
    const btn = document.getElementById('tryitExecuteBtn');
    btn.innerHTML = '<i class="ti-reload" style="animation: spin 1s linear infinite;"></i> Executando...';
    btn.disabled = true;
    
    document.getElementById('tryitResponseContainer').style.display = 'none';
    
    let finalPath = currentPath;
    let queryParams = [];
    
    // Build Path and Query Params
    const paramInputs = document.querySelectorAll('.tryit-param-input');
    paramInputs.forEach(input => {
        const name = input.getAttribute('data-name');
        const loc = input.getAttribute('data-in');
        const val = input.value;
        
        if (val) {
            if (loc === 'path') {
                finalPath = finalPath.replace(`{${name}}`, encodeURIComponent(val));
            } else if (loc === 'query') {
                queryParams.push(`${encodeURIComponent(name)}=${encodeURIComponent(val)}`);
            }
        }
    });
    
    let url = `https://joederblanca.com.br/agivys-api${finalPath}`;
    if (queryParams.length > 0) {
        url += '?' + queryParams.join('&');
    }
    
    const fetchOptions = {
        method: currentMethod.toUpperCase(),
        headers: {
            'Accept': 'application/json'
        }
    };
    
    // Token
    if (document.getElementById('tryitAuthGroup').style.display !== 'none') {
        const token = document.getElementById('tryitTokenInput').value;
        if (token) {
            fetchOptions.headers['Authorization'] = `Bearer ${token}`;
        }
    }
    
    // Body
    if (document.getElementById('tryitBodyContainer').style.display !== 'none') {
        fetchOptions.headers['Content-Type'] = 'application/json';
        const bodyVal = document.getElementById('tryitBodyTextarea').value;
        if (bodyVal) {
            fetchOptions.body = bodyVal;
        }
    }
    
    const startTime = performance.now();
    
    try {
        const response = await fetch(url, fetchOptions);
        const endTime = performance.now();
        const duration = Math.round(endTime - startTime);
        
        let resData = null;
        try {
            resData = await response.json();
        } catch(e) {
            try {
                resData = await response.text();
            } catch(e2) {
                resData = "No content";
            }
        }
        
        showTryItResponse(response.status, resData, duration);
    } catch (e) {
        showTryItResponse('Error', e.message, 0);
    } finally {
        btn.innerHTML = 'Executar Requisição';
        btn.disabled = false;
    }
}

function showTryItResponse(status, data, timeMs) {
    const container = document.getElementById('tryitResponseContainer');
    const badge = document.getElementById('tryitStatus');
    const time = document.getElementById('tryitTime');
    const body = document.getElementById('tryitResponseBody');
    
    badge.className = 'status-badge';
    if (status === 'Error') {
        badge.classList.add('status-5xx');
        badge.innerText = 'Network Error';
    } else {
        if (status >= 200 && status < 300) badge.classList.add('status-2xx');
        else if (status >= 400 && status < 500) badge.classList.add('status-4xx');
        else if (status >= 500) badge.classList.add('status-5xx');
        badge.innerText = `${status} Status`;
    }
    
    time.innerText = timeMs > 0 ? `${timeMs}ms` : '';
    
    if (typeof data === 'object') {
        body.innerText = JSON.stringify(data, null, 2);
    } else {
        body.innerText = data;
    }
    
    container.style.display = 'block';
    
    // Scroll down to response
    const drawerBody = document.querySelector('.tryit-body');
    drawerBody.scrollTop = drawerBody.scrollHeight;
}

// Start
document.addEventListener('DOMContentLoaded', init);
