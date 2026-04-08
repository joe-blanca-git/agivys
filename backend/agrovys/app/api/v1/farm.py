import os
import json
import tempfile
from typing import List
from fastapi import APIRouter, UploadFile, File, Form, HTTPException, Depends
import geopandas as gpd
from shapely.geometry import mapping
from sqlalchemy.orm import Session  # <--- Aqui está ele

# Imports do seu projeto
from app.core.security import verify_jwt_token
from app.core.database import get_db
from app.models.agrovys import Farm, Boundary

router = APIRouter(prefix="/farms", tags=["Farms"])

@router.post("/upload-boundary/", summary="Recebe Arquivos ShapeFile", dependencies=[Depends(verify_jwt_token)])
async def upload_boundary(files: List[UploadFile] = File(...)):
    """
    Recebe múltiplos arquivos (shp, shx, dbf, prj), extrai dados geográficos e retorna um GeoJSON.
    """

    extensions = [os.path.splitext(f.filename)[1].lower() for f in files]
    required_exts = ['.shp', '.shx', '.dbf', '.prj']

    for ext in required_exts:
        if ext not in extensions:
            raise HTTPException(
                status_code=400,
                detail=f"Arquivo obrigatório ausente: {ext}. Um ShapeFile precisa do .shp, .shx, .dbf. e .prj"
            )
            
    with tempfile.TemporaryDirectory() as tempdir:
        shp_path = None

        for file in files:
            file_path = os.path.join(tempdir, file.filename)

            content = await file.read()
            with open(file_path, "wb") as f_out:
                f_out.write(content)

            if file.filename.lower().endswith('.shp'):
                shp_path = file_path

        try:
            # 1. Lê o Shapefile
            gdf = gpd.read_file(shp_path)

            # 2. Converte para EPSG:4326 para o Leaflet exibir corretamente
            if gdf.crs is None or gdf.crs.to_string() != "EPSG:4326":
                gdf = gdf.to_crs("EPSG:4326")

            # --- NOVIDADE: CÁLCULO DE ÁREA ---
            # 3. Projeta temporariamente para EPSG:6933 (Projeção Equal-Area em metros) 
            gdf_area = gdf.to_crs("EPSG:6933")
            # Calcula a área de todos os polígonos, soma, e divide por 10.000 (m² -> ha)
            area_total_ha = round(gdf_area.geometry.area.sum() / 10000, 2)
            # ---------------------------------

            # 4. Converte para GeoJSON
            geojson_str = gdf.to_json()
            geojson_data = json.loads(geojson_str)

            # 5. Retorna tudo para o front
            return {
                "mensagem": "Shapefile processado com sucesso!",
                "total_features": len(gdf),
                "area_total_ha": area_total_ha, # <--- Enviando a área calculada
                "geojson": geojson_data
            }
        
        except Exception as e:
            raise HTTPException(status_code=500, detail=f"Erro ao processar o shapefile: {str(e)}")
        
@router.post("/new-farm/", summary="Cadastra Fazenda e Talhões via Shapefile", dependencies=[Depends(verify_jwt_token)])
async def cadastrar_fazenda(

    name: str = Form(...),
    client_unit_id: str = Form(...),
    agivys_user_id: str = Form(...),
    crop_year: str = Form("2024/2025"),
    files: List[UploadFile] = File(...),
    db: Session = Depends(get_db)
):
    """
    Cria uma nova Fazenda, processa os arquivos Shapefile (shp, shx, dbf, prj) e 
    salva os talhões (boundaries) associados a essa fazenda no PostgreSQL + PostGIS.
    """

    # 1. Validação de Extensões
    extensions = [os.path.splitext(f.filename)[1].lower() for f in files]
    required_exts = ['.shp', '.shx', '.dbf', '.prj']

    for ext in required_exts:
        if ext not in extensions:
            raise HTTPException(
                status_code=400,
                detail=f"Arquivo obrigatório ausente: {ext}. O Shapefile precisa de .shp, .shx, .dbf e .prj."
            )

    # 2. Processamento do Shapefile
    with tempfile.TemporaryDirectory() as tempdir:
        shp_path = None

        for file in files:
            file_path = os.path.join(tempdir, file.filename)
            content = await file.read()
            with open(file_path, "wb") as f_out:
                f_out.write(content)

            if file.filename.lower().endswith('.shp'):
                shp_path = file_path

        try:
            gdf = gpd.read_file(shp_path)

            # Conversão para WGS84 (Padrão Leaflet/PostGIS)
            if gdf.crs is None or gdf.crs.to_string() != "EPSG:4326":
                gdf = gdf.to_crs("EPSG:4326")

            # Cálculo de Área (Projetando para EPSG:6933 temporariamente)
            gdf_area = gdf.to_crs("EPSG:6933")
            gdf['area_calculada_ha'] = gdf_area.geometry.area / 10000

        except Exception as e:
            raise HTTPException(status_code=400, detail=f"Erro ao ler o shapefile: {str(e)}")

    # 3. Transação com o Banco de Dados (ACID Compliance)
    try:
        # A. Cadastra a Fazenda
        nova_fazenda = Farm(
            client_unit_id=client_unit_id,
            name=name,
            agivys_user_id=agivys_user_id,
            created_by=agivys_user_id
        )
        db.add(nova_fazenda)
        db.flush() # Gera o ID da fazenda sem comitar a transação

        # B. Percorre o Shapefile e cadastra os Limites (Boundaries)
        for index, row in gdf.iterrows():
            
            # Converte a geometria para WKT e assegura que é MULTIPOLYGON
            geom_type = row.geometry.geom_type
            if geom_type == 'Polygon':
                from shapely.geometry import MultiPolygon
                multi_geom = MultiPolygon([row.geometry])
                geom_wkt = multi_geom.wkt
            else:
                geom_wkt = row.geometry.wkt

            novo_talhao = Boundary(
                farm_id=nova_fazenda.id,
                field_id=str(row.get('id_talhao', index)), # Pega da tabela de atributos ou usa o index
                name=str(row.get('nome_talha', f"Talhão {index}")),
                geom=f"SRID=4326;{geom_wkt}", # Padrão do PostGIS via GeoAlchemy2
                geojson_data=mapping(row.geometry), # GeoJSON puro e leve para o Angular
                area_ha=round(row['area_calculada_ha'], 2),
                crop_year=crop_year,
                created_by=agivys_user_id
            )
            db.add(novo_talhao)

        # C. Efetiva todas as inserções
        db.commit()
        db.refresh(nova_fazenda)

        return {
            "mensagem": "Fazenda e talhões cadastrados com sucesso!",
            "farm_id": str(nova_fazenda.id),
            "total_talhoes_salvos": len(gdf),
            "area_total_ha": round(gdf['area_calculada_ha'].sum(), 2)
        }

    except Exception as e:
        db.rollback() # Se qualquer coisa der erro, desfaz tudo (evita lixo no banco)
        raise HTTPException(status_code=500, detail=f"Erro ao salvar no banco de dados: {str(e)}")
    
