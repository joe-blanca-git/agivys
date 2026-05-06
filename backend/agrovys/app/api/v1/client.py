import httpx
import uuid
import jwt # Adicionado para garantir a decodificação aqui
from fastapi import APIRouter, HTTPException, Depends, Form, Request
from sqlalchemy.orm import Session
from typing import Optional, List

from app.core.database import get_db
from app.core.security import verify_jwt_token
from app.models.agrovys import ClientUnit 

# Configuração
CSHARP_API_URL = "http://147.182.209.185:5000/api/v1/companies/owner"
router = APIRouter(prefix="/clients", tags=["Clients"])

@router.post("/", summary="Cria uma nova Client Unit")
def create_client_unit(
    name: str = Form(...),
    agivys_user_id: str = Form(...),
    agivys_company_id: str = Form(...),
    db: Session = Depends(get_db),
    token_data = Depends(verify_jwt_token) # Aceita string ou dict
):
    # Evitar duplicidade: Verifica se já existe um cadastro para este ID do C#
    existing = db.query(ClientUnit).filter(ClientUnit.agivys_company_id == agivys_company_id).first()
    if existing:
        return {"message": "Client already exists", "client": {"id": existing.id, "name": existing.name}}

    try:
        new_client = ClientUnit(
            id=str(uuid.uuid4()),
            name=name,
            agivys_user_id=agivys_user_id,
            agivys_company_id=agivys_company_id,
            created_by=agivys_user_id
        )
        
        db.add(new_client)
        db.commit()
        db.refresh(new_client)
        
        return {
            "message": "Client Unit created successfully!",
            "client": {
                "id": new_client.id,
                "name": new_client.name
            }
        }
    except Exception as e:
        db.rollback()
        raise HTTPException(status_code=500, detail=f"Database error: {str(e)}")

@router.get("/", summary="Lista empresas do dono agregadas")
async def get_my_clients_aggregated(
    request: Request, 
    current_user = Depends(verify_jwt_token),
    db: Session = Depends(get_db)
):
    try:
        auth_header = request.headers.get("Authorization")

        payload = current_user
        if isinstance(current_user, str):
            payload = jwt.decode(current_user, options={"verify_signature": False})
        
        if not isinstance(payload, dict):
             raise HTTPException(status_code=401, detail="Token inválido ou formato inesperado")
        
        user_id = (
            payload.get("id") or 
            payload.get("sub") or 
            payload.get("nameid") or
            payload.get("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
        )

        if not user_id:
            raise HTTPException(status_code=401, detail="ID não encontrado nas claims do Token")

        user_id_str = str(user_id)

        async with httpx.AsyncClient() as client:
            try:
                response = await client.get(
                    f"{CSHARP_API_URL}/{user_id_str}", 
                    headers={"Authorization": auth_header}, 
                    timeout=15.0
                )
                response.raise_for_status()
                data = response.json()
                
                csharp_companies = data if isinstance(data, list) else []
            except Exception as e:
                raise HTTPException(status_code=502, detail=f"Erro na API C#: {str(e)}")

        aggregated_list = []
        
        for co in csharp_companies:
            if not isinstance(co, dict):
                continue

            co_id_str = str(co.get('id'))
            
            local_data = db.query(ClientUnit).filter(
                ClientUnit.agivys_company_id == co_id_str
            ).first()

            aggregated_list.append({
                "id": co.get("id"),
                "name": co.get("name"),
                "cnpj": co.get("cnpj"),
                "logoUrl": co.get("logoUrl"),
                "createdAt": co.get("createdAt"),
                "agrovys_uuid": local_data.id if local_data else None,
                "is_registered_in_agrovys": local_data is not None,
                "agivys_user_id": local_data.agivys_user_id if local_data else None
            })

        return aggregated_list

    except HTTPException as http_exc:
        raise http_exc
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Erro interno no Servidor Python: {str(e)}")