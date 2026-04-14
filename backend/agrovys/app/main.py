from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from app.api.v1 import farm, client

app = FastAPI(
    title="Agrovys - API",
    description="API de Integração com sistema Agrovys",
    version="1.0.0",
)

# 1. Defina explicitamente onde o seu Angular está rodando
origens_permitidas = [
    "http://localhost:65492",
    "http://localhost:4200",
    "http://127.0.0.1:4200",
]

# 2. Configuração do Middleware de CORS
app.add_middleware(
    CORSMiddleware,
    allow_origins=origens_permitidas,
    allow_credentials=True,
    allow_methods=["*"],  # Permite POST, GET, OPTIONS, etc.
    allow_headers=["*"],  # Permite qualquer cabeçalho (necessário para arquivos e tokens)
)

app.include_router(farm.router, prefix="/api/v1", tags=["Geoprocessamento"])
app.include_router(client.router, prefix="/api/v1")

@app.get("/", tags=["Health Check"])
def root():
    return {"mensagem": "API está online e funcionando!"}