import zipfile
import os

zip_path = r"C:\Users\mathe\.gemini\antigravity-ide\scratch\SignalNoAr\src\Installer\payload.zip"
staging_dir = r"C:\Users\mathe\.gemini\antigravity-ide\scratch\SignalNoAr\staging"

files_to_pack = [
    ("SignalNoAr.exe", os.path.join(staging_dir, "SignalNoAr.exe")),
    ("app.ico", os.path.join(staging_dir, "app.ico"))
]

with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as zf:
    for arcname, path in files_to_pack:
        if os.path.exists(path):
            zf.write(path, arcname)

print("Repacked payload.zip:", os.path.getsize(zip_path))
