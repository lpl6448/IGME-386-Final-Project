print("Progress 0 Initializing...")

import arcpy
import os

raster = r"C:\Users\lukel\Downloads\rap.t00z.awip32f00.grib2"
variables = ["LCDC@LCY", "MCDC@MCY", "HCDC@HCY"] # and others eventually
arcpy.env.overwriteOutput = True

print("Progress 20 Importing raster layer...")
multiraster = arcpy.MakeMultidimensionalRasterLayer_md(raster, variables=variables)

print("Progress 70 Exporting raster layers...")
for var in variables:
    r = arcpy.MakeMultidimensionalRasterLayer_md(multiraster, variables=var)
    # Probably more Project function stuff here
    arcpy.CopyRaster_management(r, os.path.abspath("output1.tif"))
arcpy.CopyRaster_management(arcpy.MakeMultidimensionalRasterLayer_md(multiraster, variables="HCDC@HCY"), os.path.abspath("output3.tif"))

print("Progress 100 Done!")
