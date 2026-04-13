import uuid
from sqlalchemy import Column, String, Boolean, Numeric, ForeignKey, TIMESTAMP, func
from sqlalchemy.dialects.postgresql import UUID, JSONB
from sqlalchemy.orm import relationship
from geoalchemy2 import Geometry
from app.core.database import Base

class Farm(Base):
    __tablename__ = "farm"

    id = Column(UUID(as_uuid=True), primary_key=True, default=uuid.uuid4)
    client_unit_id = Column(String(255), nullable=False)
    agivys_user_id = Column(String(255), nullable=False)
    name = Column(String(100), nullable=False)
    description = Column(String)
    created_at = Column(TIMESTAMP, server_default=func.now())
    created_by = Column(String(255), nullable=False)
    updated_at = Column(TIMESTAMP)
    updated_by = Column(String(255))

    boundaries = relationship("Boundary", back_populates="farm", cascade="all, delete-orphan")

class Boundary(Base):
    __tablename__ = "boundary"

    id = Column(UUID(as_uuid=True), primary_key=True, default=uuid.uuid4)
    farm_id = Column(UUID(as_uuid=True), ForeignKey("farm.id", ondelete="CASCADE"), nullable=False)
    field_id = Column(String(100))
    name = Column(String(150))
    
    # O motor geográfico
    geom = Column(Geometry(geometry_type='MULTIPOLYGON', srid=4326), nullable=False)
    
    geojson_data = Column(JSONB)
    area_ha = Column(Numeric(10, 2))
    crop_year = Column(String(9))
    is_grouped = Column(Boolean, default=True)
    
    created_at = Column(TIMESTAMP, server_default=func.now())
    created_by = Column(String(255), nullable=False)
    updated_at = Column(TIMESTAMP)
    updated_by = Column(String(255))

    farm = relationship("Farm", back_populates="boundaries")

class ClientUnit(Base):
    __tablename__ = "client_unit"

    id = Column(String(255), primary_key=True)
    name = Column(String(150), nullable=False)
    agivys_user_id = Column(String(255), nullable=False)
    agivys_company_id = Column(String(255), nullable=True)
    created_at = Column(TIMESTAMP, server_default=func.now())
    created_by = Column(String(255), nullable=False)