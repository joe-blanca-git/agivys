import os
import json # <--- Novo import adicionado
import tempfile
from typing import List
from fastapi import APIRouter, UploadFile, File, HTTPException, Depends
import geopandas as gpd
from app.core.security import verify_jwt_token

router = APIRouter()

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