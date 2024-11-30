import os
import arcpy
import requests
import gzip
import shutil

# Paths and settings
download_url = "https://mrms.ncep.noaa.gov/data/2D/ReflectivityAtLowestAltitude/MRMS_ReflectivityAtLowestAltitude.latest.grib2.gz"
download_path = r"Data\Zipped\MRMS_ReflectivityAtLowestAltitude.latest.grib2.gz"
unzip_path = r"Data\Unzipped\MRMS_ReflectivityAtLowestAltitude.latest.grib2"
output_raster_path = r"Data\Raster\Reflectivity_Output.PNG"
final_raster_path = r"Data\Raster\Reflectivity_Reprojected.PNG"
final_raster_path_extent = r"Data\Raster\Reflectivity_Extent.PNG"
output_coordinate_system = arcpy.SpatialReference(3857)  # WGS_1984_Web_Mercator_Auxiliary_Sphere
extent = arcpy.Extent(-15000000, 2000000, -5000000, 7000000)  # Specified extent
pixel_type = "8_BIT_UNSIGNED"  # Pixel type
cell_size = "1200"  # Cell size for both X and Y
raster_size = (8192, 4096)  # Width (columns) and Height (rows)

# Step 0: Delete existing files if they exist
if os.path.exists(download_path):
    print(f"Deleting existing download file: {download_path}")
    os.remove(download_path)

if os.path.exists(unzip_path):
    print(f"Deleting existing unzipped file: {unzip_path}")
    os.remove(unzip_path)

if os.path.exists(output_raster_path):
    print(f"Deleting existing output raster file: {output_raster_path}")
    os.remove(output_raster_path)

# Ensure necessary directories exist
os.makedirs(os.path.dirname(download_path), exist_ok=True)
os.makedirs(os.path.dirname(unzip_path), exist_ok=True)
os.makedirs(os.path.dirname(output_raster_path), exist_ok=True)

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
arcpy.env.workspace = os.getcwd()

# Step 4: Copy raster with specified settings
print("Copying the raster...")
output_raster_path = os.path.abspath(output_raster_path)
if os.path.exists(output_raster_path):
    os.remove(output_raster_path)
if not os.path.exists(os.path.dirname(output_raster_path)):
    os.makedirs(os.path.dirname(output_raster_path))
arcpy.CopyRaster_management(unzip_path, out_rasterdataset=output_raster_path, format="PNG", pixel_type=pixel_type)


# Step 5: Reproject and adjust extent in a single step
print("Reprojecting raster and adjusting extent...")

# Ensure the directory exists
os.makedirs(os.path.dirname(final_raster_path), exist_ok=True)

# Delete existing final raster if it exists
if os.path.exists(final_raster_path):
    print(f"Deleting existing final raster file: {final_raster_path}")
    os.remove(final_raster_path)

# Reproject and adjust extent
arcpy.management.ProjectRaster(
    in_raster=output_raster_path,
    out_raster=final_raster_path,
    out_coor_system=output_coordinate_system,
    resampling_type="BILINEAR",  # Choose resampling method (e.g., NEAREST, BILINEAR, CUBIC)
    geographic_transform="",
    cell_size=cell_size  # Cell size for X and Y
)

# Set extent after reprojection
print("Setting raster extent...")
arcpy.management.Clip(
    in_raster=final_raster_path,
    rectangle=f"{extent.XMin} {extent.YMin} {extent.XMax} {extent.YMax}",
    out_raster=final_raster_path_extent,
    in_template_dataset="",  # Optional template dataset
    nodata_value="",
    clipping_geometry="NONE",
    maintain_clipping_extent="NO_MAINTAIN_EXTENT",
)

# Step 6: Verify raster size
print("Verifying and setting raster size...")
raster_info = arcpy.Raster(final_raster_path_extent)
actual_raster_size = (raster_info.width, raster_info.height)
print(f"Final raster size: {actual_raster_size}")
