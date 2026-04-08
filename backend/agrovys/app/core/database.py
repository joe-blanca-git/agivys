import os
from sqlalchemy import create_engine
from sqlalchemy.orm import declarative_base, sessionmaker

# A string de conexão. Em produção, coloque isso no seu arquivo .env
# Formato: postgresql://usuario:senha@ip_da_vps:porta/banco
SQLALCHEMY_DATABASE_URL = os.getenv(
    "DATABASE_URL", 
    "postgresql://agrovys_user:Likeaboos%401980@147.182.209.185:5432/agrovys_db"
)

# pool_pre_ping=True testa a conexão antes de usá-la, evitando quedas por inatividade
engine = create_engine(SQLALCHEMY_DATABASE_URL, pool_pre_ping=True)

SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

Base = declarative_base()

# Dependência do FastAPI para injetar o banco nas rotas
def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()