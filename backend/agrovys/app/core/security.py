import jwt # PyJWT
import httpx
from fastapi import APIRouter, Depends, HTTPException, Header
from typing import Optional

CSHARP_AUTH_URL = "http://147.182.209.185:5000/api/v1/auth/validate-token"

async def verify_jwt_token(authorization: Optional[str] = Header(None)):
    if not authorization:
        raise HTTPException(status_code=401, detail="Token não fornecido.")
    
    parts = authorization.split()
    if len(parts) != 2 or parts[0].lower() != "bearer":
        raise HTTPException(status_code=401, detail="Formato inválido.")
    
    token = parts[1]

    try:
        async with httpx.AsyncClient() as client:
            response = await client.post(CSHARP_AUTH_URL, json={"token": token})
            
            # Se o C# validou (True)
            if response.status_code == 200 and response.json() is True:
                # Decodificamos o token para pegar o ID do usuário (id ou sub)
                # options={"verify_signature": False} porque o C# já validou a assinatura para nós
                payload = jwt.decode(token, options={"verify_signature": False})
                return payload # AGORA RETORNA UM DICIONÁRIO COM OS DADOS
            else:
                raise HTTPException(status_code=401, detail="Autenticação inválida.")
                
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Erro na validação: {str(e)}")