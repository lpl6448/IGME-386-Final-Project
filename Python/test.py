import sys
import time
import arcpy
import os
from osgeo import gdal

raster = r"D:\Profiles\lpl6448\Downloads\rap.t00z.awip32f01.grib2"
# arcpy.env.workspace = r"D:\Profiles\lpl6448\Downloads\rap.t00z.awip32f01.grib2"
arcpy.env.workspace = os.getcwd()
arcpy.env.overwriteOutput = True
print("HI")
# print(arcpy.MakeRasterLayer_management(raster, "test.tif", band_index=1))

r = arcpy.Raster(raster, True)
arcpy.MakeRasterLayer_management(r, "test", "Variable = LCDC@LCY", band_index=1)
# arcpy.Raster("test").save(os.path.abspath("test.tif"))
arcpy.CopyRaster_management("test", os.path.abspath("test.gdb/test2"))

# print(os.path.abspath("test.tif"))
# rl.save("test.tif")
# print(len(arcpy.Describe(rl).children))
# arcpy.Raster(raster).save("test.tif")