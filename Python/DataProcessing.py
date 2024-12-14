print("Progress 0 Initializing...")

import os
import arcpy
import requests
import gzip
import shutil
import sys
import re

# Archive Data Setup
def validate_timestamp_format(timestamp):
    """Validates if the timestamp is in the format YYYYMMDD-HH00."""
    pattern = r"^\d{8}-\d{4}$"  # Regex for YYYYMMDD-HH00
    return re.match(pattern, timestamp) is not None

def format_url_r(timestamp):
    """Formats the timestamp into the required URL."""
    date_part = timestamp.split('-')[0]  # Extract YYYYMMDD
    xml_link = f"https://noaa-mrms-pds.s3.amazonaws.com/?list-type=2&prefix=CONUS/ReflectivityAtLowestAltitude_00.50/{date_part}/MRMS_ReflectivityAtLowestAltitude_00.50_{timestamp}"
    try:
        response = requests.get(xml_link)
        response.raise_for_status()  # Raise an error for HTTP issues
        xml_content = response.text  # Get the XML content as a string

        # Use a regex pattern to find the value inside <Key>...</Key>
        key = re.search(r'<Key>(.*?)</Key>', xml_content)
        return key.group(1).strip()

    except Exception as e:
        print(f"Error reading XML file: {e}")
        return None
    
def format_url_p(timestamp):
    """Formats the timestamp into the required URL."""
    date_part = timestamp.split('-')[0]  # Extract YYYYMMDD
    xml_link = f"https://noaa-mrms-pds.s3.amazonaws.com/?list-type=2&prefix=CONUS/PrecipFlag_00.00/{date_part}/MRMS_PrecipFlag_00.00_{timestamp}"
    try:
        response = requests.get(xml_link)
        response.raise_for_status()  # Raise an error for HTTP issues
        xml_content = response.text  # Get the XML content as a string

        # Use a regex pattern to find the value inside <Key>...</Key>
        key = re.search(r'<Key>(.*?)</Key>', xml_content)
        return key.group(1).strip()

    except Exception as e:
        print(f"Error reading XML file: {e}")
        return None

if len(sys.argv) > 1:
    timestamp = sys.argv[1]
    if validate_timestamp_format(timestamp):
        print(f"Valid timestamp: {timestamp}")
        reflect = format_url_r(timestamp)
        precip = format_url_p(timestamp)
        download_url = f"https://noaa-mrms-pds.s3.amazonaws.com/{reflect}"
        download_url_precip = f"https://noaa-mrms-pds.s3.amazonaws.com/{precip}"
    else:
        print("Invalid timestamp format. Expected YYYYMMDD-HH00.")

else:
    download_url = "https://mrms.ncep.noaa.gov/data/2D/ReflectivityAtLowestAltitude/MRMS_ReflectivityAtLowestAltitude.latest.grib2.gz"
    download_url_precip = "https://mrms.ncep.noaa.gov/data/2D/PrecipFlag/MRMS_PrecipFlag.latest.grib2.gz"


# Paths and settings
download_path = r"Data\Zipped\MRMS_ReflectivityAtLowestAltitude.latest.grib2.gz"
unzip_path = r"Data\Unzipped\MRMS_ReflectivityAtLowestAltitude.latest.grib2"
final_raster_path = r"Data\Raster\Reflectivity_Reprojected.tif"
final_raster_path_extent = r"Data\Raster\Reflectivity_Extent.PNG"

# Precipitation data
download_path_precip = r"Data\Zipped\MRMS_PrecipFlag.latest.grib2.gz"
unzip_path_precip = r"Data\Unzipped\MRMS_PrecipFlag.latest.grib2"
final_raster_path_precip = r"Data\Raster\Precipitation_Reprojected.tif"
final_raster_path_extent_precip = r"Data\Raster\Precipitation_Extent.PNG"

output_coordinate_system = arcpy.SpatialReference(3857)  # WGS_1984_Web_Mercator_Auxiliary_Sphere
extent = arcpy.Extent(-14600000, 2600000, -6800000, 6500000)  # Specified extent
pixel_type = "8_BIT_UNSIGNED"  # Pixel type
raster_size = (8192, 4096)  # Width (columns) and Height (rows)
cell_size = (extent.XMax - extent.XMin) / raster_size[0]  # Cell size for both X and Y

# Step 0: Delete existing files if they exist
if os.path.exists(download_path):
    print(f"Deleting existing download file: {download_path}")
    os.remove(download_path)

if os.path.exists(unzip_path):
    print(f"Deleting existing unzipped file: {unzip_path}")
    os.remove(unzip_path)

# Ensure necessary directories exist
os.makedirs(os.path.dirname(download_path), exist_ok=True)
os.makedirs(os.path.dirname(unzip_path), exist_ok=True)
os.makedirs(os.path.dirname(final_raster_path), exist_ok=True)

# Step 1: Download the file
print("Progress 10 Downloading the reflectivity GRIB2 file...")
response = requests.get(download_url, stream=True, verify=False)
with open(download_path, 'wb') as f:
    f.write(response.content)

# Step 2: Unzip the file
print("Progress 20 Unzipping the reflectivity file...")
with gzip.open(download_path, 'rb') as gz_file:
    with open(unzip_path, 'wb') as out_file:
        shutil.copyfileobj(gz_file, out_file)

# Step 3: Set up ArcPy environment
print("Setting up ArcPy environment...")
arcpy.env.workspace = os.getcwd()

# Step 5: Reproject and adjust extent in a single step
print("Progress 30 Reprojecting reflectivity raster...")

# Ensure the directory exists
os.makedirs(os.path.dirname(final_raster_path), exist_ok=True)

# Delete existing final raster if it exists
if os.path.exists(final_raster_path):
    print(f"Deleting existing final raster file: {final_raster_path}")
    os.remove(final_raster_path)

# Reproject and adjust extent
arcpy.env.overwriteOutput = True
arcpy.management.ProjectRaster(
    in_raster=unzip_path,
    out_raster=final_raster_path,
    out_coor_system=output_coordinate_system,
    resampling_type="CUBIC",  # Choose resampling method (e.g., NEAREST, BILINEAR, CUBIC)
    geographic_transform=""
)

# Set extent after reprojection
print("Progress 40 Setting reflectivity raster extent...")
arcpy.env.extent = extent
arcpy.env.cellSize = (extent.YMax - extent.YMin) / 4096
arcpy.env.resamplingMethod = "CUBIC"
arcpy.CopyRaster_management(final_raster_path, os.path.abspath(final_raster_path_extent), format="PNG", pixel_type=pixel_type)

# Step 6: Verify raster size
print("Verifying and setting raster size...")
raster_info = arcpy.Raster(final_raster_path_extent)
actual_raster_size = (raster_info.width, raster_info.height)
print(f"Final raster size: {actual_raster_size}")


"""
Precipitation Data Processing
"""

# Step 0: Delete existing files if they exist
if os.path.exists(download_path_precip):
    print(f"Deleting existing download file: {download_path_precip}")
    os.remove(download_path_precip)

if os.path.exists(unzip_path_precip):
    print(f"Deleting existing unzipped file: {unzip_path_precip}")
    os.remove(unzip_path_precip)

# Ensure necessary directories exist
os.makedirs(os.path.dirname(download_path_precip), exist_ok=True)
os.makedirs(os.path.dirname(unzip_path_precip), exist_ok=True)
os.makedirs(os.path.dirname(final_raster_path_precip), exist_ok=True)

# Step 1: Download the file
print("Progress 55 Downloading the Precipitation GRIB2 file...")
response = requests.get(download_url_precip, stream=True, verify=False)
with open(download_path_precip, 'wb') as f:
    f.write(response.content)

# Step 2: Unzip the file
print("Progress 65 Unzipping the precipitation file...")
with gzip.open(download_path_precip, 'rb') as gz_file:
    with open(unzip_path_precip, 'wb') as out_file:
        shutil.copyfileobj(gz_file, out_file)

# Step 3: Set up ArcPy environment
print("Setting up ArcPy environment...")
arcpy.env.workspace = os.getcwd()

# Step 5: Reproject and adjust extent in a single step
print("Progress 75 Reprojecting precipitation raster...")

# Ensure the directory exists
os.makedirs(os.path.dirname(final_raster_path_precip), exist_ok=True)

# Delete existing final raster if it exists
if os.path.exists(final_raster_path_precip):
    print(f"Deleting existing final raster file: {final_raster_path_precip}")
    os.remove(final_raster_path_precip)

# Reproject and adjust extent
arcpy.env.overwriteOutput = True
arcpy.env.resamplingMethod = "NEAREST"
arcpy.management.ProjectRaster(
    in_raster=unzip_path_precip,
    out_raster=final_raster_path_precip,
    out_coor_system=output_coordinate_system,
    resampling_type="NEAREST",  # Choose resampling method (e.g., NEAREST, BILINEAR, CUBIC)
    geographic_transform=""
)

# Set extent after reprojection
print("Progress 85 Setting precipitation raster extent...")
arcpy.env.extent = extent
arcpy.env.cellSize = "MAXOF" # do not perform any scaling for PrecipFlag since we don't want to resample it
arcpy.env.resamplingMethod = "NEAREST"
arcpy.CopyRaster_management(final_raster_path_precip, os.path.abspath(final_raster_path_extent_precip), format="PNG", pixel_type=pixel_type)

# Step 6: Verify raster size
print("Verifying and setting raster size...")
raster_info = arcpy.Raster(final_raster_path_extent_precip)
actual_raster_size = (raster_info.width, raster_info.height)
print(f"Final raster size: {actual_raster_size}")
print("Progress 100 Done")