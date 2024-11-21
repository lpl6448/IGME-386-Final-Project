import os
import arcpy
import requests
import ssl
import gzip
import shutil

# Paths and settings
download_url = "https://mrms.ncep.noaa.gov/data/2D/ReflectivityAtLowestAltitude/MRMS_ReflectivityAtLowestAltitude.latest.grib2.gz"
download_path = r"Data\Zipped\MRMS_ReflectivityAtLowestAltitude.latest.grib2.gz"
unzip_path = r"Data\Unzipped\MRMS_ReflectivityAtLowestAltitude.latest.grib2"
output_raster_path = r"Data\Raster\Reflectivity_Output.PNG"
output_coordinate_system = arcpy.SpatialReference(3857)  # WGS_1984_Web_Mercator_Auxiliary_Sphere
extent = arcpy.Extent(-15000000, 2000000, -5000000, 7000000)  # Specified extent
pixel_type = "8_BIT_UNSIGNED"  # Pixel type
cell_size = "1200"  # Cell size for both X and Y
raster_size = (8192, 4096)  # Width (columns) and Height (rows)

# Step 1: Download the file
print("Downloading the GRIB2 file...")
response = requests.get(download_url, stream=True, verify=False)
with open(download_path, 'wb') as f:
    f.write(response.content)

# Step 2: Unzip the file
print("Unzipping the file...")
with gzip.open(download_path, 'rb') as gz_file:
    with open(unzip_path, 'wb') as out_file:
        shutil.copyfileobj(gz_file, out_file)

# Step 3: Set up ArcPy environment
print("Setting up ArcPy environment...")
arcpy.env.workspace = os.path.dirname(unzip_path)

# Step 5: Copy raster with specified settings
print("Copying the raster...")
arcpy.CopyRaster_management(unzip_path, out_rasterdataset=output_raster_path, format="PNG", pixel_type=pixel_type)


# # Step 6: Reproject the raster to the desired coordinate system
# print("Reprojecting the raster...")
# arcpy.management.ProjectRaster(
#     in_raster=output_raster_path,
#     out_raster=output_raster_path,
#     out_coor_system=output_coordinate_system,
#     resampling_type="BILINEAR",
#     cell_size=cell_size,
#     geographic_transform=""
# )

# print(f"Raster exported successfully to: {output_raster_path}")
