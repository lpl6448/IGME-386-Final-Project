print("Progress 0 Initializing...")

import arcpy
import os

raster = r"C:\Users\lukel\Downloads\rap.t00z.awip32f00.grib2"
variables = ["LCDC@LCY", "MCDC@MCY", "HCDC@HCY"] # and others eventually
arcpy.env.overwriteOutput = True

i = 0
for var in variables:
    print(f"Progress {i * 100 // len(variables)} Importing {var}...")
    r = arcpy.md.SubsetMultidimensionalRaster(raster, variables=var) # USE THIS IT'S WAY FASTER
    # Probably more Project function stuff here
    arcpy.CopyRaster_management(r, os.path.abspath(f"output{var}.tif"))
    i += 1

print("Progress 100 Done!")
