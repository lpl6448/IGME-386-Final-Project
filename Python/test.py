print("Progress 0 Initializing...")

import arcpy
import os

raster = r"D:\Profiles\lpl6448\Downloads\rap.t00z.awip32f01.grib2"
variables = ["LCDC@LCY", "MCDC@MCY", "HCDC@HCY"] # and others eventually
arcpy.env.overwriteOutput = True

print("Progress 20 Importing raster layer...")
multiraster = arcpy.MakeMultidimensionalRasterLayer_md(raster, variables=variables)

print("Progress 70 Exporting raster layers...")
for var in variables:
    r = arcpy.MakeMultidimensionalRasterLayer_md(multiraster, variables=var)
    # Probably more Project function stuff here
    arcpy.CopyRaster_management(r, os.path.abspath(f"output{var}.tif"))

print("Progress 100 Done!")
