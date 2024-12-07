print("Progress 0 Initializing processing...")

import arcpy
import os

raster = os.path.abspath(r"Data\Unzipped\cloudsoutput.grib2")
variables = ["LCDC@LCY", "MCDC@MCY", "HCDC@HCY", "TCDC@EATM", "HGT@LFC"] 
arcpy.env.overwriteOutput = True

output_coordinate_system = arcpy.SpatialReference(3857)  # WGS_1984_Web_Mercator_Auxiliary_Sphere
extent = arcpy.Extent(-14600000, 2600000, -6800000, 6500000)  # Specified extent
pixel_type = "32_BIT_FLOAT"  # Pixel type
raster_size = (2048, 1024)  # Width (columns) and Height (rows)
cell_size = (extent.XMax - extent.XMin) / raster_size[0]  # Cell size for both X and Y

os.makedirs("Temp_Data", exist_ok=True)

i = 0
for var in variables:
    final_raster = os.path.abspath(f"Data/Raster/output{var}.tif")

    print(f"Progress {i * 100 // len(variables)} Importing {var}...")
    r = arcpy.md.SubsetMultidimensionalRaster(raster, "Temp_Data/Temp" ,variables=var) # USE THIS IT'S WAY FASTER
    # Probably more Project function stuff here

    # Set up ArcPy environment
    print("Setting up ArcPy environment...")
    arcpy.env.workspace = os.getcwd()

    # Reproject and adjust extent in a single step
    print("Reprojecting raster and adjusting extent...")

    # Reproject and adjust extent
    arcpy.env.overwriteOutput = True
    r = arcpy.management.ProjectRaster(
        in_raster=r,
        out_raster="Temp_Data/Temp",
        out_coor_system=output_coordinate_system,
        resampling_type="CUBIC",  # Choose resampling method (e.g., NEAREST, BILINEAR, CUBIC)
        geographic_transform=""
    )
    print(r)

    # Set extent after reprojection
    print("Setting raster extent...")
    arcpy.env.extent = extent
    arcpy.env.cellSize = cell_size
    arcpy.env.resamplingMethod = "CUBIC"
    arcpy.CopyRaster_management(r, final_raster, format="TIFF", pixel_type=pixel_type)

    # Verify raster size
    print("Verifying and setting raster size...")
    raster_info = arcpy.Raster(final_raster)
    actual_raster_size = (raster_info.width, raster_info.height)
    print(f"Final raster size: {actual_raster_size}")
    i += 1

print("Progress 100 Done!")