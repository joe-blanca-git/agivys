from fastapi import APIRouter, HTTPException, Depends, Form
from sqlalchemy.orm import Session
from typing import Optional
import uuid

from app.core.database import get_db
from app.core.security import verify_jwt_token
from app.models.agrovys import Farm 
from app.models.agrovys import ClientUnit 

router = APIRouter(prefix="/clients", tags=["Clients"])

@router.post("/", summary="Create a new Client Unit", dependencies=[Depends(verify_jwt_token)])
def create_client_unit(
    name: str = Form(...),
    agivys_user_id: str = Form(...),
    description: Optional[str] = Form(None),
    db: Session = Depends(get_db)
):
    """
    Registra uma nova unidade de cliente (empresa/grupo) no sistema.
    """
    try:

        new_client = ClientUnit(
            id=str(uuid.uuid4()),
            name=name,
            description=description,
            agivys_user_id=agivys_user_id,
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