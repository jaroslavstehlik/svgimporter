rmdir /Q /S "E:\Google Drive\Dream.Digital\Projects\SVG Importer\Builds\SVG Importer"
mkdir "E:\Unity\SVG Importer-5-6-0\Assets\SVG Importer"
xcopy /E /I "E:\Unity\SVGImporter\Assets\SVG Importer" "E:\Google Drive\Dream.Digital\Projects\SVG Importer\Builds\SVG Importer"
"C:\Program Files\7-Zip\7z.exe" a "E:\Google Drive\Dream.Digital\Projects\SVG Importer\Builds\SVG Importer v1-1-4 Unity 5-6-1f1.zip" "E:\Google Drive\Dream.Digital\Projects\SVG Importer\Builds\SVG Importer\"
pause