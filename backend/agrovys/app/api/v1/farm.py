import os
import json
import uuid
import tempfile
from typing import List
from pydantic import BaseModel
from fastapi import APIRouter, UploadFile, File, Form, HTTPException, Depends, Response
import geopandas as gpd
from shapely.geometry import mapping
from sqlalchemy.orm import Session
from sqlalchemy import func, text

from app.core.security import verify_jwt_token
from app.core.database import get_db
from app.models.agrovys import Farm, Boundary

router = APIRouter(prefix="/farms", tags=["Farms"])

class BoundarySummary(BaseModel):
    id: str
    name: str
    area: float

@router.get("/{farm_id}/boundaries", response_model=List[BoundarySummary], summary="Retorna os talhões de uma fazenda (apenas id, nome e área)", dependencies=[Depends(verify_jwt_token)])
def get_farm_boundaries(
    farm_id: str,
    db: Session = Depends(get_db)
):
    """
    Retorna apenas o id, nome e área dos talhões de uma fazenda específica.
    """
    results = db.query(
        Boundary.id,
        Boundary.name,
        Boundary.area_ha
    ).filter(Boundary.farm_id == farm_id).all()

    return [
        BoundarySummary(
            id=str(row.id),
            name=row.name,
            area=float(row.area_ha) if row.area_ha else 0.0
        ) for row in results
    ]

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
                # Prioriza COD_TALHAO, depois id_talhao, e por fim usa o índice + 1
                field_id=str(row.get('COD_TALHAO') or row.get('id_talhao') or (index + 1)),
                # Prioriza COD_TALHAO, depois nome_talha, e por fim usa "Talhão X"
                name=str(row.get('COD_TALHAO') or row.get('nome_talha') or f"Talhão {index + 1}"),
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

@router.get("/", summary="Lista todas as fazendas", dependencies=[Depends(verify_jwt_token)])
def list_farms(
    # agivys_user_id: str, # Descomente se quiser filtrar só as fazendas do usuário logado
    db: Session = Depends(get_db)
):
    """
    Retorna a lista de fazendas com a soma das áreas dos talhões 
    e a data da última alteração.
    """
    
    # Query otimizada fazendo JOIN e GROUP BY no banco de dados
    results = db.query(
        Farm.id,
        Farm.name,
        Farm.description,
        func.coalesce(Farm.updated_at, Farm.created_at).label('last_update'),
        func.sum(Boundary.area_ha).label('total_area')
    ).outerjoin(Boundary, Farm.id == Boundary.farm_id)\
     .filter(Farm.active == True)\
     .group_by(Farm.id)\
     .all()

    # Formatando a resposta para o Angular
    farms_list = []
    for row in results:
        farms_list.append({
            "id": str(row.id), # Sempre bom mandar o ID para o front poder clicar e abrir detalhes
            "name": row.name,
            "description": row.description or "Sem descrição",
            "area": round(float(row.total_area), 2) if row.total_area else 0.0,
            "lastUpdate": row.last_update.isoformat() if row.last_update else None
        })

    return farms_list

class FarmSimpleCreate(BaseModel):
    name: str
    client_unit_id: str
    crop_year: str
    agivys_user_id: str

@router.post("/simple/", summary="Cadastra Fazenda sem Shapefile", dependencies=[Depends(verify_jwt_token)])
def cadastrar_fazenda_simples(
    data: FarmSimpleCreate,
    db: Session = Depends(get_db)
):
    """
    Cria uma nova Fazenda recebendo apenas os dados básicos (nome, cliente e safra), sem Shapefile.
    """
    try:
        nova_fazenda = Farm(
            client_unit_id=data.client_unit_id,
            name=data.name,
            agivys_user_id=data.agivys_user_id,
            created_by=data.agivys_user_id,
            description=f"Safra: {data.crop_year}"  # Salvando safra na descrição provisoriamente, pois BD exige shape p/ boundary
        )
        db.add(nova_fazenda)
        db.commit()
        db.refresh(nova_fazenda)

        return {
            "mensagem": "Fazenda cadastrada com sucesso sem áreas!",
            "farm_id": str(nova_fazenda.id)
        }

    except Exception as e:
        db.rollback() 
        raise HTTPException(status_code=500, detail=f"Erro ao salvar no banco de dados: {str(e)}")

class ArchiveFarmsRequest(BaseModel):
    farm_ids: List[str]

@router.patch("/archive/", summary="Arquiva uma ou mais fazendas", dependencies=[Depends(verify_jwt_token)])
def archive_farms(
    data: ArchiveFarmsRequest,
    db: Session = Depends(get_db)
):
    try:
        if not data.farm_ids:
            return {"mensagem": "Nenhuma fazenda informada.", "arquivadas": 0}

        db.query(Farm).filter(Farm.id.in_(data.farm_ids)).update({"active": False}, synchronize_session=False)
        db.commit()

        return {"mensagem": "Fazendas arquivadas com sucesso!", "arquivadas": len(data.farm_ids)}
    except Exception as e:
        db.rollback()
        raise HTTPException(status_code=500, detail=f"Erro ao arquivar fazendas: {str(e)}")

@router.get("/boundaries/geojson", summary="Retorna todas as boundaries ativas em um único GeoJSON", dependencies=[Depends(verify_jwt_token)])
def get_boundaries_geojson(db: Session = Depends(get_db)):
    try:
        feature_expr = func.jsonb_build_object(
            'type', 'Feature',
            'properties', func.jsonb_build_object(
                'farm_name', Farm.name,
                'field_id', Boundary.field_id,
                'name', Boundary.name,
                'area_ha', Boundary.area_ha,
                'crop_year', Boundary.crop_year
            ),
            'geometry', Boundary.geojson_data
        )

        query = db.query(func.jsonb_build_object(
            'type', 'FeatureCollection',
            'features', func.coalesce(func.jsonb_agg(feature_expr), text("'[]'::jsonb"))
        )).select_from(Boundary)\
          .join(Farm, Farm.id == Boundary.farm_id)\
          .filter(Farm.active == True)

        geojson = query.scalar()

        if not geojson or not geojson.get("features"):
            return {"type": "FeatureCollection", "features": []}

        return geojson
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Erro ao carregar o GeoJSON geral: {str(e)}")

@router.get("/export/geojson", summary="Exporta talhões em GeoJSON", dependencies=[Depends(verify_jwt_token)])
def export_boundaries_geojson(
    farm_ids: str,
    db: Session = Depends(get_db)
):
    try:
        # Converte a string de IDs para uma lista de UUIDs
        id_list = [uuid.UUID(fid.strip()) for fid in farm_ids.split(",") if fid.strip()]
        
        feature_expr = func.jsonb_build_object(
            'type', 'Feature',
            'properties', func.jsonb_build_object(
                'farm_name', Farm.name,
                'field_id', Boundary.field_id,
                'name', Boundary.name,
                'area_ha', Boundary.area_ha,
                'crop_year', Boundary.crop_year
            ),
            'geometry', Boundary.geojson_data
        )

        query = db.query(func.jsonb_build_object(
            'type', 'FeatureCollection',
            'features', func.coalesce(func.jsonb_agg(feature_expr), text("'[]'::jsonb"))
        )).select_from(Boundary)\
          .join(Farm, Farm.id == Boundary.farm_id)\
          .filter(Farm.id.in_(id_list))

        geojson = query.scalar()

        if not geojson or not geojson.get("features"):
            return {"type": "FeatureCollection", "features": []}

        return geojson
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Erro ao exportar GeoJSON: {str(e)}")
@router.get("/export/kml", summary="Exporta talhões em KML", dependencies=[Depends(verify_jwt_token)])
def export_boundaries_kml(
    farm_ids: str,
    db: Session = Depends(get_db)
):
    try:
        id_list = [uuid.UUID(fid.strip()) for fid in farm_ids.split(",") if fid.strip()]
        
        results = db.query(Boundary, Farm.name.label('farm_name'))\
            .join(Farm, Farm.id == Boundary.farm_id)\
            .filter(Farm.id.in_(id_list)).all()

        kml_header = """<?xml version="1.0" encoding="UTF-8"?>
<kml xmlns="http://www.opengis.net/kml/2.2">
  <Document>
    <name>Exportação Agrovys</name>
"""
        kml_footer = """  </Document>
</kml>"""
        
        placemarks = []
        for boundary, farm_name in results:
            geom = boundary.geojson_data
            coords_xml = ""
            
            if geom['type'] == 'Polygon':
                coords_xml = f"          <Polygon><outerBoundaryIs><LinearRing><coordinates>{' '.join([f'{p[0]},{p[1]},0' for p in geom['coordinates'][0]])}</coordinates></LinearRing></outerBoundaryIs></Polygon>"
            elif geom['type'] == 'MultiPolygon':
                polygons_xml = ""
                for poly in geom['coordinates']:
                    polygons_xml += f"          <Polygon><outerBoundaryIs><LinearRing><coordinates>{' '.join([f'{p[0]},{p[1]},0' for p in poly[0]])}</coordinates></LinearRing></outerBoundaryIs></Polygon>"
                coords_xml = f"          <MultiGeometry>{polygons_xml}</MultiGeometry>"

            placemarks.append(f"""    <Placemark>
      <name>{farm_name} - {boundary.name}</name>
      <description>Área: {boundary.area_ha} ha / Safra: {boundary.crop_year}</description>
{coords_xml}
    </Placemark>""")

        kml_content = kml_header + "\n".join(placemarks) + kml_footer
        
        return Response(content=kml_content, media_type="application/vnd.google-earth.kml+xml")
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Erro ao exportar KML: {str(e)}")
