import arcpy
info = arcpy.GetInstallInfo()
print(f"Progress {info['ProductName']} v{info['Version']}")