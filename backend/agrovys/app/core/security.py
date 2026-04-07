import httpx
from fastapi import APIRouter, Depends, HTTPException, Header
from typing import Optional

# Coloque aqui a URL real da sua API em C#
CSHARP_AUTH_URL = "http://147.182.209.185:5000/api/v1/auth/validate-token"

async def verify_jwt_token(authorization: Optional[str] = Header(None)):
    """
    Dependência que intercepta o cabeçalho Authorization, extrai o Token JWT
    e valida na API em C#.
    """
    print("\n" + "="*50)
    print("🔒 INICIANDO VALIDAÇÃO DE TOKEN")
    print("="*50)
    
    # 1. O QUE ESTAMOS RECEBENDO DO FRONTEND
    print(f"[1] Cabeçalho recebido do Angular: {authorization}")
    
    if not authorization:
        raise HTTPException(status_code=401, detail="Token de autenticação não fornecido.")

    parts = authorization.split()
    if len(parts) != 2 or parts[0].lower() != "bearer":
        raise HTTPException(status_code=401, detail="Formato de token inválido. Use 'Bearer <token>'.")
    
    token = parts[1]
    print(f"[2] Token limpo extraído: {token[:20]}... (truncado para leitura)")

    try:
        # 2. O QUE ESTAMOS ENVIANDO PARA O C#
        payload = {"token": token}
        print(f"[3] Enviando requisição para: {CSHARP_AUTH_URL}")
        print(f"[4] Corpo do JSON enviado: {payload}")
        
        async with httpx.AsyncClient() as client:
            response = await client.post(
                CSHARP_AUTH_URL, 
                json=payload
            )
            
            # 3. O QUE O C# NOS RESPONDEU
            print(f"[5] Status Code do C#: {response.status_code}")
            print(f"[6] Corpo da Resposta do C#: {response.text}")
            print("="*50 + "\n")
            
            # Aqui fazemos a verificação do que o C# retornou
            # Dependendo do que aparecer no print [6], vamos ajustar esta linha abaixo:
            if response.status_code == 200 and response.text.strip().lower() == "true":
                return token
            else:
                raise HTTPException(status_code=401, detail="Autenticação inválida ou expirada segundo o C#.")
                
    except httpx.RequestError as e:
        print(f"[ERRO DE CONEXÃO] {str(e)}")
        raise HTTPException(status_code=500, detail=f"Erro ao contatar o servidor de autenticação: {str(e)}")