import arcpy
import os

# Set the workspace
arcpy.env.workspace = os.getcwd()
grib_file = r"D:\Profiles\ijs3503\IGME-386-Final-Project\Data\Unzipped\rap.t15z.awip32f01.grib2"

# Load the GRIB as a multidimensional raster
raster = arcpy.Raster(grib_file, True)

print(raster.variableNames)

# # Variable names to extract
# variables = ["LCDC@LCY"]

# # Iterate through the raster's subdatasets (bands)
# for variable in variables:
#     try:
#         # Make a raster layer using the variable name directly if it exists
#         layer_name = f"{variable}_layer"
#         arcpy.MakeRasterLayer_management(grib_file, layer_name, where_clause=f"Variable = '{variable}'")

#         # Save or analyze the layer
#         output_raster = f"{arcpy.env.workspace}/{variable}.tif"
#         arcpy.CopyRaster_management(layer_name, output_raster)
#         print(f"Saved {variable} to {output_raster}")

#     except arcpy.ExecuteError:
#         print(f"Variable {variable} not found or issue with extraction.")
