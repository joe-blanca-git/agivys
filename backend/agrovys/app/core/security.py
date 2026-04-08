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
    if not authorization:
        raise HTTPException(status_code=401, detail="Token de autenticação não fornecido.")
    

    parts = authorization.split()
    if len(parts) != 2 or parts[0].lower() != "bearer":
        raise HTTPException(status_code=401, detail="Formato de token inválido. Use 'Bearer <token>'.")
    
    token = parts[1]

    try:
        async with httpx.AsyncClient() as client:
            response = await client.post(
                CSHARP_AUTH_URL, 
                json={"token": token}
            )
            
            if response.status_code == 200 and response.json() is True:
                return token
            else:
                raise HTTPException(status_code=401, detail="Autenticação inválida ou expirada.")
                
    except httpx.RequestError as e:
        raise HTTPException(status_code=500, detail=f"Erro ao contatar o servidor de autenticação: {str(e)}")